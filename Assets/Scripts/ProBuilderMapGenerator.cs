using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public struct Cell
{
    public bool isRoom;    // true = místnost (#), false = prázdno (.)
    public int itemId;     // 0 = žádný předmět, 1 = health, 2 = energy, 3 = obojí
    public bool enemySpawn; // true = kde je spawner nepřátel (*)
    public bool playerSpawn; // true = kde začíná hráč (P)
}

// Generuje 3D dungeon z ASCII mapy pomocí ProBuilder
// Každý znak ASCII mapy = jedna místnost o rozměrech size x height x size
// Sousední místnosti jsou propojeny dveřními otvory ve zdech

public class ProBuilderMapGenerator : MonoBehaviour
{
    [Header("Zdroj mapy")]
    public string fileName = "map"; // název souboru v Resources/ (bez přípony), použije se pokud AIMapGenerator není ve scéně

    [Header("Rozměry místností")]
    public float size = 15f;            // šířka/hloubka místnosti 
    public float height = 11f;          // výška místnosti
    public float wallThickness = 0.2f;  // tloušťka zdí
    public float doorWidth = 8f;        // šířka dveřního otvoru
    public float doorHeight = 5f;       // výška dveřního otvoru (od podlahy)

    // Statická data přístupná ostatním skriptům 
    public static List<Vector3> DoorPositions = new List<Vector3>(); // středové pozice všech dveří/zdí
    public static List<Vector3> RoomCenters = new List<Vector3>();   // středové pozice podlah místností
    public static Cell[,] Grid;       // parsovaná mřížka buněk
    public static int MapRows, MapCols; // rozměry mapy v buňkách
    public static float RoomSize;       // kopie size pro ostatní skripty

    [Header("Prefaby")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public GameObject enemySpawnerPrefab;

    public GameObject item1Prefab; // health pickup
    public GameObject item2Prefab; // energy pickup
    public GameObject item3Prefab; // health + energy pickup

    public Material roomMaterial;

    private string[] lines; // řádky ASCII mapy

    Cell[,] grid; // lokální reference na mřížku během generování

    void Start()
    {
        // Pokud je ve scéně AIMapGenerator, počkáme, ten zavolá Build() sám po obdržení mapy od GPT
        // Pokud není, spustíme generování okamžitě ze záložního souboru v Resources/
        if (FindObjectOfType<AIMapGenerator>() == null)
            Build(null);
    }


    // Vstupní bod pro spuštění generování mapy
    // Volá ho AIMapGenerator po obdržení ASCII mapy od GPT
    public void Build(string mapOverride)
    {
        GenerateMap(mapOverride);
    }


    // Načte ASCII mapu, naparsuje ji do mřížky Cell[,] a postaví 3D dungeon
    void GenerateMap(string mapOverride = null)
    {
        // Načtení textu mapy, buď z parametru (od AI), nebo ze souboru v Resources/
        string rawText = mapOverride;
        if (rawText == null)
        {
            TextAsset mapAsset = Resources.Load<TextAsset>(fileName);
            rawText = mapAsset.text;
        }

        // Rozdělení textu na řádky, prázdné řádky se ignorují
        lines = rawText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        int rows = lines.Length;
        int cols = 0;
        int spawnerCount = 0;

        // Zjistíme nejdelší řádek, to je počet sloupců mapy
        foreach (var line in lines)
            cols = Mathf.Max(cols, line.Length);

        // Uložíme rozměry do statických proměnných pro ostatní skripty
        MapRows = rows;
        MapCols = cols;
        RoomSize = size;
        grid = new Cell[rows, cols];

        // ******** Parsování ASCII mapy do mřížky buněk ********
        for (int y = 0; y < rows; y++)
        {
            string line = lines[y];

            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];

                // Buňka je místnost pokud obsahuje #, spawn hráče, spawn nepřítele nebo předmět
                if (c == '#' || c == '*' || c == 'P' || c == 'p' || char.IsDigit(c))
                {
                    grid[y, x].isRoom = true;
                }

                // Číslice určuje typ předmětu v místnosti
                if (char.IsDigit(c))
                {
                    grid[y, x].itemId = (int)char.GetNumericValue(c);
                }

                // Hvězdička označuje spawner nepřátel (krystal)
                if (c == '*')
                {
                    grid[y, x].enemySpawn = true;
                    spawnerCount++;
                }

                // P nebo p označuje spawn hráče
                if (c == 'P' || c == 'p')
                {
                    grid[y, x].playerSpawn = true;
                }
            }
        }

        // ********** Stavění 3D dungeonu z naparsované mřížky ************
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (grid[y, x].isRoom)
                {
                    // Převod souřadnic mřížky na 3D pozici ve světě
                    // Osa Y mapy jde dolů → ve 3D jdeme záporným Z
                    Vector3 pos = new Vector3(x * size, height / 2f, -y * size);

                    // Střed podlahy místnosti (pro navigaci a spawn objektů)
                    RoomCenters.Add(new Vector3(pos.x, pos.y - height / 2f + 1f, pos.z));

                    BuildRoom(x, y, pos);        // postaví zdi, podlahu, strop
                    SpawnRoomLight(pos);          // přidá bodové světlo

                    if (grid[y, x].itemId != 0) SpawnItem(grid[y, x].itemId, pos);
                    if (grid[y, x].enemySpawn)  SpawnSpawnerCrystal(pos);
                    if (grid[y, x].playerSpawn) SpawnPlayer(pos);
                }
            }
        }

        // Uložení výsledné mřížky a počtu spawnerů do statických proměnných
        Grid = grid;
        MapGeneratorSpawnerCount.cachedSpawnerCount = spawnerCount;

        // Vypneme stíny všem světlům (zlepšuje výkon, dungeon je malý)
        foreach (var l in FindObjectsOfType<Light>())
            l.shadows = LightShadows.None;

        Debug.Log("map generated :D");
    }

    // Postaví jednu místnost na pozici (x, y) v mřížce
    // Zkontroluje sousedy a pro každou stranu buď vytvoří zeď nebo dveřní otvor
    void BuildRoom(int x, int y, Vector3 position)
    {
        bool north = HasRoom(x, y - 1); // sever = menší y v mřížce
        bool south = HasRoom(x, y + 1); // jih = větší y v mřížce
        bool east  = HasRoom(x + 1, y);
        bool west  = HasRoom(x - 1, y);

        // Podlaha, o 1 jednotku větší na každou stranu, aby se podlahy sousedních místností
        // překrývaly ve dveřních otvorech -> NavMesh má spojitý povrch pro navigaci nepřátel
        CreateWall(position + new Vector3(0, -height / 2f, 0), new Vector3(size + 1f, wallThickness, size + 1f), new Color(0.3f, 0.3f, 0.3f));

        // Strop
        CreateWall(position + new Vector3(0, height / 2f, 0), new Vector3(size, wallThickness, size));

        // Jižní stěna (–Z)
        if (south) CreateDoorZ(position, -size / 2f);
        else       CreateWallZ(position, -size / 2f);

        // Severní stěna (+Z)
        if (north) CreateDoorZ(position, size / 2f);
        else       CreateWallZ(position, size / 2f);

        // Východní stěna (+X)
        if (east) CreateDoorX(position, size / 2f);
        else      CreateWallX(position, size / 2f);

        // Západní stěna (–X)
        if (west) CreateDoorX(position, -size / 2f);
        else      CreateWallX(position, -size / 2f);
    }


    // Vrátí true pokud je na souřadnicích (x, y) v mřížce místnost
    // Souřadnice mimo mřížku jsou automaticky false
    bool HasRoom(int x, int y)
    {
        if (y < 0 || y >= grid.GetLength(0)) return false;
        if (x < 0 || x >= grid.GetLength(1)) return false;
        return grid[y, x].isRoom;
    }

    // Vytvoří plnou zeď na severní nebo jižní straně místnosti (rovnoběžnou s osou X)
    // Zaznamená také pozici středu zdi jako potenciální místo dveří pro jiné systémy

    void CreateWallZ(Vector3 roomPos, float localZ)
    {
        Vector3 pos   = new Vector3(roomPos.x, roomPos.y, roomPos.z + localZ);
        Vector3 scale = new Vector3(size, height, wallThickness);
        CreateWall(pos, scale);

        // Střed zdi ve výšce podlahy, používají to jiné skripty 
        DoorPositions.Add(new Vector3(roomPos.x, 1f, roomPos.z + localZ));
    }


    // Vytvoří plnou zeď na východní nebo západní straně místnosti (rovnoběžnou s osou Z)
    void CreateWallX(Vector3 roomPos, float localX)
    {
        Vector3 pos   = new Vector3(roomPos.x + localX, roomPos.y, roomPos.z);
        Vector3 scale = new Vector3(wallThickness, height, size);
        CreateWall(pos, scale);

        DoorPositions.Add(new Vector3(roomPos.x + localX, 1f, roomPos.z));
    }


    // Vytvoří dveřní otvor ve zdi rovnoběžné s osou X (sever/jih)
    // Zeď se skládá ze tří kusů: levý sloupek, pravý sloupek, nadpraží

    void CreateDoorZ(Vector3 roomPos, float localZ)
    {
        float sideWidth = (size - doorWidth) / 2f; // šířka každého sloupku vedle dveří
        float topH      = height - doorHeight;      // výška nadpraží nad dveřním otvorem

        float wallZ = roomPos.z + localZ;

        // Levý sloupek
        CreateWall(
            new Vector3(roomPos.x - (doorWidth / 2f + sideWidth / 2f), roomPos.y, wallZ),
            new Vector3(sideWidth, height, wallThickness));

        // Pravý sloupek
        CreateWall(
            new Vector3(roomPos.x + (doorWidth / 2f + sideWidth / 2f), roomPos.y, wallZ),
            new Vector3(sideWidth, height, wallThickness));

        // Nadpraží, panel nad dveřním otvorem
        float floorY = roomPos.y - height / 2f;
        float topY   = floorY + doorHeight + topH / 2f;
        CreateWall(
            new Vector3(roomPos.x, topY, wallZ),
            new Vector3(doorWidth, topH, wallThickness));
    }


    // Vytvoří dveřní otvor ve zdi rovnoběžné s osou Z (východ/západ)
    // Stejná struktura jako CreateDoorZ, ale otočená o 90 stupňů
    void CreateDoorX(Vector3 roomPos, float localX)
    {
        float sideWidth = (size - doorWidth) / 2f;
        float topH      = height - doorHeight;

        float wallX = roomPos.x + localX;

        // Levý sloupek (záporný Z)
        CreateWall(
            new Vector3(wallX, roomPos.y, roomPos.z - (doorWidth / 2f + sideWidth / 2f)),
            new Vector3(wallThickness, height, sideWidth));

        // Pravý sloupek (kladný Z)
        CreateWall(
            new Vector3(wallX, roomPos.y, roomPos.z + (doorWidth / 2f + sideWidth / 2f)),
            new Vector3(wallThickness, height, sideWidth));

        // Nadpraží
        float floorY = roomPos.y - height / 2f;
        float topY   = floorY + doorHeight + topH / 2f;
        CreateWall(
            new Vector3(wallX, topY, roomPos.z),
            new Vector3(wallThickness, topH, doorWidth));
    }

    // Vytvoří jeden kus zdi/podlahy/stropu pomocí ProBuilder ShapeGenerator
    // Každý kus je samostatný GameObject s MeshRenderer a BoxCollider
    // Stíny jsou vypnuté pro výkon

    void CreateWall(Vector3 position, Vector3 scale, Color? color = null)
    {
        // Vytvoříme ProBuilder kostku a nastavíme její transformaci
        ProBuilderMesh wall = ShapeGenerator.CreateShape(ShapeType.Cube);
        wall.transform.position   = position;
        wall.transform.localScale = scale;
        wall.ToMesh();   // přegeneruje mesh podle nové transformace
        wall.Refresh();  // aktualizuje normály, UV a další data meshe

        // Přiřadíme materiál s barvou (výchozí je bílá)
        var mr  = wall.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color          = color ?? Color.white;
        mr.sharedMaterial  = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows  = false;

        // Zdi jsou child objekty tohoto generátoru pro přehlednost v hierarchii
        wall.transform.parent = transform;

        // BoxCollider s velikostí 1×1×1 — scale GameObjectu ho automaticky roztáhne
        BoxCollider bc = wall.gameObject.AddComponent<BoxCollider>();
        bc.size = Vector3.one;
    }

    // Umístí prefab hráče na střed podlahy místnosti označené jako spawn
    void SpawnPlayer(Vector3 pos)
    {
        if (playerPrefab == null) { Debug.LogWarning("No player prefab!"); return; }

        // Y = podlaha místnosti + 1 jednotka (aby hráč nestál v podlaze)
        Vector3 p = new Vector3(pos.x, pos.y - height / 2f + 1f, pos.z);
        Instantiate(playerPrefab, p, Quaternion.identity);
    }


    // Umístí prefab nepřítele na střed podlahy místnosti (aktuálně nepoužívané nepřátelé se spawní přes EnemySpawner krystal, ne přímo při generování mapy)

    void SpawnEnemy(Vector3 pos)
    {
        if (enemyPrefab == null) { Debug.LogWarning("No enemy prefab!"); return; }

        Vector3 p = new Vector3(pos.x, pos.y - height / 2f + 1f, pos.z);
        Instantiate(enemyPrefab, p, Quaternion.identity);
    }


    // Umístí spawner nepřátel (krystal) na střed podlahy místnosti označené *
    // Krystal má 5 HP a každých 25 s spawní nepřítele. Zničení všech krystalů = výhra

    void SpawnSpawnerCrystal(Vector3 pos)
    {
        if (enemySpawnerPrefab == null)
        {
            Debug.LogWarning("No enemySpawnerPrefab assigned!");
            return;
        }

        Vector3 p = new Vector3(pos.x, pos.y - height / 2f + 1f, pos.z);
        Instantiate(enemySpawnerPrefab, p, Quaternion.identity);
    }


    // Přidá do každé místnosti bodové světlo
    // Umístěno ve výšce 30% od stropu pro přirozené osvětlení
    void SpawnRoomLight(Vector3 pos)
    {
        var go = new GameObject("RoomLight");
        go.transform.parent   = transform;
        go.transform.position = new Vector3(pos.x, pos.y + height * 0.3f, pos.z);

        var l       = go.AddComponent<Light>();
        l.type      = LightType.Point;
        l.range     = size * 1.5f;       // dosah pokrývá celou místnost + trochu sousedních
        l.intensity = 0.8f;
        l.color     = new Color(1f, 0.9f, 0.75f); 
        l.shadows   = LightShadows.None; // bez stínů pro výkon
    }


    // Umístí pickup předmět podle itemId na střed podlahy místnosti
    // itemId: 1 = health, 2 = energy, 3 = health + energy
    void SpawnItem(int id, Vector3 pos)
    {
        GameObject prefab = null;
        if (id == 1) prefab = item1Prefab;
        if (id == 2) prefab = item2Prefab;
        if (id == 3) prefab = item3Prefab;

        if (prefab == null) return;

        // Y = podlaha + 0.5 jednotky, aby předmět "levitoval" těsně nad zemí
        Vector3 p = new Vector3(pos.x, pos.y - height / 2f + 0.5f, pos.z);
        Instantiate(prefab, p, Quaternion.identity);
    }
}
