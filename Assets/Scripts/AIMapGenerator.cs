using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using OpenAI.Chat;

public class AIMapGenerator : MonoBehaviour
{
    const int WIDTH = 12;
    const int HEIGHT = 7;

    void Start()
    {
        GenerateUntilValid();
    }

    async void GenerateUntilValid()
    {
        var mapGen = FindObjectOfType<ProBuilderMapGenerator>();

        // načteni API klíče z Resources/OpenAIConfiguration
        var client = new OpenAIClient();

        // načtení nastavení z menu před tím než ho GetPrompt() resetuje
        int wantedSpawners = PersistentMapConfig.HasCustomConfig ? PersistentMapConfig.SpawnerCount : 2;
        int wantedItems    = PersistentMapConfig.HasCustomConfig ? PersistentMapConfig.ItemCount    : 0;
        string prompt      = GetPrompt();

        for (int attempt = 0; attempt < 15; attempt++)
        {
            var messages = new List<Message>
            {
                new Message(Role.System,
                    "You generate dungeon maps as plain text. Output ONLY the map — no explanation, no code block, no extra lines."),
                new Message(Role.User, prompt)
            };

            string mapText;
            try
            {
                var request  = new ChatRequest(messages: messages, model: "gpt-4o-mini", temperature: 1.1f);
                var response = await client.ChatEndpoint.GetCompletionAsync(request);
                mapText = response.Choices[0].Message.Content.ToString().Trim();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Generování mapy selhalo: {e.Message}");
                continue;
            }

            string sanitized = Sanitize(mapText);
            if (sanitized == null) continue;

            sanitized = RepairConnectivity(sanitized);
            sanitized = FillMissing(sanitized, wantedSpawners, wantedItems);

            mapGen.Build(sanitized);
            return;
        }

        // záložní mapa pokud AI selže
        mapGen.Build(null);
    }

    string GetPrompt()
    {
        int spawners = PersistentMapConfig.HasCustomConfig ? PersistentMapConfig.SpawnerCount : 2;
        int items    = PersistentMapConfig.HasCustomConfig ? PersistentMapConfig.ItemCount    : 3;
        PersistentMapConfig.Reset();

        string spawnerRule = $"* = enemy spawner (exactly {spawners}, spread apart, counts as #)";
        string itemRule    = items > 0
            ? $"1 2 3 = item pickup types (place exactly {items} total across the map, reuse digits as needed e.g. 1 1 2 3 1, counts as #)"
            : "1 2 3 = item pickup (place none, do not use these characters)";

        return
$@"Generate a dungeon map. Output ONLY the 7 lines of the map, nothing else.

Character meaning:
    # = dungeon room / walkable floor
    . = empty void (nothing is built here)
    P = player spawn (exactly ONE, counts as #)
    {spawnerRule}
    {itemRule}

Rules:
- Exactly 12 columns wide, exactly 7 rows tall
- All walkable cells must be reachable from P
- Surround the dungeon with . (void) on the outside edges
- Make an interesting layout: multiple rooms connected by corridors

Example (exactly 12 chars per line, uppercase P, 3 reachable spawners):
............
.##......*..
.#P######...
.##....##...
..####..##..
..*.#....*..
....#.......

Now generate a NEW different map following the same rules. Use this random seed for variety: {Random.Range(1000, 9999)}";
    }

    // náhodné doplnění chybějících spawnerů a itemů na volná políčka
    string FillMissing(string map, int wantedSpawners, int wantedItems)
    {
        string[] lines = map.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        char[][] grid  = System.Array.ConvertAll(lines, l => l.ToCharArray());

        int actualSpawners = lines.Sum(l => l.Count(c => c == '*'));
        int actualItems    = lines.Sum(l => l.Count(c => c == '1' || c == '2' || c == '3'));

        // hledání volných políček
        var free = new List<Vector2Int>();
        for (int y = 0; y < HEIGHT; y++)
            for (int x = 0; x < WIDTH; x++)
                if (grid[y][x] == '#') free.Add(new Vector2Int(x, y));

        // náhodné zamíchání seznamu políček
        for (int i = free.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (free[i], free[j]) = (free[j], free[i]);
        }

        int freeIdx = 0;

        for (int i = actualSpawners; i < wantedSpawners && freeIdx < free.Count; i++, freeIdx++)
        {
            var cell = free[freeIdx];
            grid[cell.y][cell.x] = '*';
        }

        char[] itemChars = { '1', '2', '3' };
        for (int i = actualItems; i < wantedItems && freeIdx < free.Count; i++, freeIdx++)
        {
            var cell = free[freeIdx];
            grid[cell.y][cell.x] = itemChars[i % 3];
        }

        return string.Join("\n", System.Array.ConvertAll(grid, l => new string(l)));
    }

    // odstranění místností nedosažitelných hráčem
    string RepairConnectivity(string map)
    {
        string[] lines = map.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        char[][] grid = System.Array.ConvertAll(lines, l => l.ToCharArray());

        // hledání pozice hráče
        Vector2Int player = new Vector2Int(-1, -1);
        for (int y = 0; y < HEIGHT && player.x == -1; y++)
            for (int x = 0; x < WIDTH && player.x == -1; x++)
                if (grid[y][x] == 'P' || grid[y][x] == 'p')
                    player = new Vector2Int(x, y);

        if (player.x == -1) return null;

        // flood fill
        bool[,] reachable = new bool[HEIGHT, WIDTH];
        var stack = new Stack<Vector2Int>();
        stack.Push(player);
        reachable[player.y, player.x] = true;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (stack.Count > 0)
        {
            var pos = stack.Pop();
            foreach (var dir in dirs)
            {
                var next = pos + dir;
                if (next.x < 0 || next.x >= WIDTH || next.y < 0 || next.y >= HEIGHT) continue;
                if (reachable[next.y, next.x] || !IsWalkable(grid[next.y][next.x])) continue;
                reachable[next.y, next.x] = true;
                stack.Push(next);
            }
        }

        // nedosažitelné místnosti nahrazeny voidem
        for (int y = 0; y < HEIGHT; y++)
            for (int x = 0; x < WIDTH; x++)
                if (IsWalkable(grid[y][x]) && !reachable[y, x])
                    grid[y][x] = '.';

        return string.Join("\n", System.Array.ConvertAll(grid, l => new string(l)));
    }

    string Sanitize(string raw)
    {
        string[] lines = raw.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        var result = new System.Text.StringBuilder();
        bool hasPlayer = false;

        for (int y = 0; y < HEIGHT; y++)
        {
            string line = y < lines.Length ? lines[y] : "";
            var sb = new System.Text.StringBuilder();

            for (int x = 0; x < WIDTH; x++)
            {
                char c = x < line.Length ? line[x] : '.';

                if (c == ' ' || c == '\t') c = '.';
                else if (c != '#' && c != '.' && c != '*' &&
                         c != 'P' && c != 'p' &&
                         c != '1' && c != '2' && c != '3')
                    c = '#';

                // zajištění jediného spawnu hráče
                if (c == 'P' || c == 'p')
                {
                    if (hasPlayer) c = '#';
                    else { c = 'P'; hasPlayer = true; }
                }

                sb.Append(c);
            }

            result.AppendLine(sb.ToString());
        }

        return hasPlayer ? result.ToString().TrimEnd() : null;
    }

    bool IsValid(string map)
    {
        string[] lines = map.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length != HEIGHT) return false;
        if (lines.Any(l => l.TrimEnd().Length != WIDTH)) return false;
        if (lines.Sum(l => l.Count(c => c == 'P')) != 1) return false;
        if (lines.Sum(l => l.Count(c => c == '*')) < 1) return false;
        return IsConnected(lines);
    }

    // vše kromě "." (void) je průchozí
    bool IsWalkable(char c) => c != '.';

    bool IsConnected(string[] map)
    {
        Vector2Int player = new Vector2Int(-1, -1);
        for (int y = 0; y < HEIGHT && player.x == -1; y++)
            for (int x = 0; x < WIDTH && player.x == -1; x++)
                if (map[y][x] == 'P' || map[y][x] == 'p')
                    player = new Vector2Int(x, y);

        if (player.x == -1) return false;

        bool[,] visited = new bool[HEIGHT, WIDTH];
        var stack = new Stack<Vector2Int>();
        stack.Push(player);
        visited[player.y, player.x] = true;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (stack.Count > 0)
        {
            var pos = stack.Pop();
            foreach (var dir in dirs)
            {
                var next = pos + dir;
                if (next.x < 0 || next.x >= WIDTH || next.y < 0 || next.y >= HEIGHT) continue;
                if (visited[next.y, next.x] || !IsWalkable(map[next.y][next.x])) continue;
                visited[next.y, next.x] = true;
                stack.Push(next);
            }
        }

        // podmínka splněna pokud hráč dosáhne alespoň jeden spawner
        for (int y = 0; y < HEIGHT; y++)
            for (int x = 0; x < WIDTH; x++)
                if (map[y][x] == '*' && visited[y, x])
                    return true;

        return false;
    }
}
