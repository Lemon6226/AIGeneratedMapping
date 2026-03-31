using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Enemy_Woman : MonoBehaviour
{
    public float health = 100f;
    public float walkSpeed = 2f;
    public float chaseSpeed = 3f;
    public float rotationSpeed = 10f;

    public float attackRange = 1f;
    public float damagePerSecond = 20f;

    public float wanderDelay = 3f;
    public float hearingRange = 20f;
    public float waypointReachDistance = 2f;

    private Rigidbody rb;
    private Transform player;
    private bool chasing = false;

    private List<Vector3> waypoints = new List<Vector3>();
    private int waypointIndex = 0;
    private float nextWanderTime = 0f;

    private void Awake()
    {
        // vypnutí NavMeshAgent při vytvoření, použit vlastní pathfinding
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = FindObjectOfType<Player>()?.transform;

        rb.freezeRotation = true;
        rb.isKinematic = false;

        // ignorování kolizí se spawnery a itemy aby nepřítel nepřeskakoval
        var myCol = GetComponent<Collider>();
        if (myCol != null)
            foreach (var spawner in FindObjectsOfType<EnemySpawner>())
                foreach (var col in spawner.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(myCol, col, true);

        int spawnerLayer = LayerMask.NameToLayer("Spawner");
        if (spawnerLayer != -1)
            Physics.IgnoreLayerCollision(gameObject.layer, spawnerLayer, true);

        int itemLayer = LayerMask.NameToLayer("Item");
        if (itemLayer != -1)
            Physics.IgnoreLayerCollision(gameObject.layer, itemLayer, true);

        PickNewWanderPath();
    }

    void Update()
    {
        // smrt
        if (health <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // přepnutí do pronásledování při detekci hráče zrakem nebo sluchem
        if (player != null && (CanSeePlayer() || CanHearPlayer()))
            chasing = true;

        if (chasing && player != null)
            ChasePlayer();
        else
            Wander();

        AttackIfClose();
    }


    //********* Toulání po mapě *********

    void Wander()
    {
        if (waypoints.Count == 0 || waypointIndex >= waypoints.Count)
        {
            if (Time.time >= nextWanderTime)
                PickNewWanderPath();
            return;
        }

        Vector3 target = waypoints[waypointIndex];
        target.y = transform.position.y;

        if (Vector3.Distance(transform.position, target) < waypointReachDistance)
        {
            waypointIndex++;
            if (waypointIndex >= waypoints.Count)
                nextWanderTime = Time.time + wanderDelay;
            return;
        }

        MoveAndRotate(target, walkSpeed);
    }

    void PickNewWanderPath()
    {
        // výběr náhodné místnosti jako cíl toulání
        var rooms = ProBuilderMapGenerator.RoomCenters;
        if (rooms == null || rooms.Count == 0) return;

        Vector3 target = rooms[Random.Range(0, rooms.Count)];
        waypoints = BuildPath(transform.position, target);
        waypointIndex = 0;
    }

    // ********* Pronásledování hráče ***********

    void ChasePlayer()
    {
        Vector2Int myRoom = GetRoomCoord(transform.position);
        Vector2Int playerRoom = GetRoomCoord(player.position);

        if (myRoom == playerRoom)
        {
            // stejná místnost, přímý pohyb k hráči
            Vector3 target = player.position;
            target.y = transform.position.y;
            MoveAndRotate(target, chaseSpeed);
            waypoints.Clear();
            return;
        }

        // přeplánování cesty po dosažení waypointu
        if (waypoints.Count == 0 || waypointIndex >= waypoints.Count)
        {
            waypoints = BuildPath(transform.position, player.position);
            waypointIndex = 0;
        }

        if (waypointIndex < waypoints.Count)
        {
            Vector3 wp = waypoints[waypointIndex];
            wp.y = transform.position.y;

            if (Vector3.Distance(transform.position, wp) < waypointReachDistance)
                waypointIndex++;
            else
                MoveAndRotate(wp, chaseSpeed);
        }
    }

    // ******** BFS pathfinding přes místnosti ********

    Vector2Int GetRoomCoord(Vector3 worldPos)
    {
        // převod světových souřadnic na souřadnice místnosti v gridu
        float s = ProBuilderMapGenerator.RoomSize;
        int x = Mathf.RoundToInt(worldPos.x / s);
        int y = Mathf.RoundToInt(-worldPos.z / s);
        return new Vector2Int(x, y);
    }

    List<Vector3> BuildPath(Vector3 from, Vector3 to)
    {
        Vector2Int start = GetRoomCoord(from);
        Vector2Int end = GetRoomCoord(to);
        List<Vector2Int> roomPath = BFSRoomPath(start, end);
        return RoomPathToWaypoints(roomPath);
    }

    List<Vector2Int> BFSRoomPath(Vector2Int start, Vector2Int end)
    {
        // BFS prohledávání grafu místností pro nalezení cesty
        var grid = ProBuilderMapGenerator.Grid;
        if (grid == null) return new List<Vector2Int>();

        int rows = ProBuilderMapGenerator.MapRows;
        int cols = ProBuilderMapGenerator.MapCols;

        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(start);
        cameFrom[start] = start;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == end) break;

            foreach (var dir in dirs)
            {
                var next = current + dir;
                if (next.x < 0 || next.x >= cols || next.y < 0 || next.y >= rows) continue;
                if (!grid[next.y, next.x].isRoom) continue;
                if (cameFrom.ContainsKey(next)) continue;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        // rekonstrukce cesty od cíle ke startu
        var path = new List<Vector2Int>();
        if (!cameFrom.ContainsKey(end)) return path;

        var curr = end;
        while (curr != start)
        {
            path.Add(curr);
            curr = cameFrom[curr];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    List<Vector3> RoomPathToWaypoints(List<Vector2Int> roomPath)
    {
        // převod seznamu místností na seznam 3D waypointů, středy místností a průchody
        float s = ProBuilderMapGenerator.RoomSize;
        float y = transform.position.y;
        var wps = new List<Vector3>();

        for (int i = 1; i < roomPath.Count; i++)
        {
            var prev = roomPath[i - 1];
            var curr = roomPath[i];

            Vector3 prevCenter = new Vector3(prev.x * s, y, -prev.y * s);
            Vector3 currCenter = new Vector3(curr.x * s, y, -curr.y * s);
            wps.Add((prevCenter + currCenter) / 2f);
            wps.Add(currCenter);
        }

        return wps;
    }

    // ********* Útok a detekce hráče **********

    void AttackIfClose()
    {
        // poškozování hráče při dostatečné blízkosti
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Player>(out Player p))
            {
                if (p.healthController != null)
                    p.healthController.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }

    bool CanHearPlayer()
    {
        // detekce hráče sluchem podle vzdálenosti
        return player != null && Vector3.Distance(transform.position, player.position) <= hearingRange;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        // sphere cast od očí nepřítele k hráči, kontrola viditelnosti
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * 1f;
        Vector3 direction = (target - origin).normalized;
        float distance = Vector3.Distance(origin, target);

        if (Physics.SphereCast(origin, 0.5f, direction, out RaycastHit hit, distance))
        {
            if (hit.transform.CompareTag("Player"))
                return true;
        }

        return false;
    }

    void MoveAndRotate(Vector3 target, float speed)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        rb.MovePosition(rb.position + dir * speed * Time.deltaTime);

        // plynulá rotace ve směru pohybu
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotationSpeed);
    }

    public void TakeDamage(float dmg)
    {
        health -= dmg;
    }
}
