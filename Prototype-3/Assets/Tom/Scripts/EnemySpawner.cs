using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab & Flow Field")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private FlowFieldGrid defaultFlowField;

    [Header("Spawn Points")]
    [SerializeField] private bool autoFindSpawnPoints = true;
    [SerializeField] private EnemySpawnPoint[] manualSpawnPoints;

    [Header("Limits")]
    [SerializeField, Min(1)] private int maxAliveEnemies = 1000;

    [Header("Spawn Rate")]
    [SerializeField, Min(1f)] private float enemiesPerSecond = 25f;
    [SerializeField, Min(0.05f)] private float batchInterval = 0.5f;

    [Header("Startup")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField, Min(0)] private int initialBurstCount = 20;

    [Header("Continuous Spawning")]
    [SerializeField] private bool keepSpawning = false;

    [Header("Walkability Validation")]
    [Tooltip("Reject spawn positions whose cell is unwalkable.")]
    [SerializeField] private bool requireWalkableSpawn = true;

    [Tooltip("Reject spawn positions where the enemy capsule would overlap an obstacle (physics test). Prevents spawn-in-wall, most common cause of stuck enemies.")]
    [SerializeField] private bool usePhysicsClearanceCheck = true;

    [Tooltip("Radius of the sphere used for the physics clearance test. Slightly larger than the enemy capsule radius.")]
    [SerializeField, Min(0.1f)] private float spawnClearanceRadius = 0.55f;

    [Tooltip("Height offset for the physics check, so it is evaluated at the enemy body's centre rather than the floor.")]
    [SerializeField, Min(0f)] private float spawnClearanceHeight = 0.5f;

    [SerializeField, Min(1)] private int maxSpawnPositionAttempts = 12;

    private readonly List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();
    private readonly List<EnemySpawnPoint> eligiblePoints = new List<EnemySpawnPoint>();

    private bool running;
    private bool inStartupBurst;
    private bool initialBurstDone;
    private float nextSpawnTime;
    private float lastWarnTime;
    private float continuousCarry;

    public int AliveCount => FlowFieldEnemy.ActiveEnemies.Count;
    public int MaxAliveEnemies => maxAliveEnemies;
    public bool IsRunning => running;
    public IReadOnlyList<EnemySpawnPoint> SpawnPoints => spawnPoints;

    public float EnemiesPerSecond
    {
        get => enemiesPerSecond;
        set => enemiesPerSecond = Mathf.Max(1f, value);
    }

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Enemy Prefab is not assigned.", this);
            return;
        }

        CollectSpawnPoints();

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[EnemySpawner] No EnemySpawnPoint components found.", this);
            return;
        }

        if (spawnOnStart)
            StartSpawning();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        inStartupBurst = false;
    }

    public void StartSpawning()
    {
        if (running) return;
        running = true;

        if (!initialBurstDone && initialBurstCount > 0)
        {
            inStartupBurst = true;
            StartCoroutine(StartupBurstRoutine(initialBurstCount));
            return;
        }

        initialBurstDone = true;
        nextSpawnTime = Time.time;
    }

    public void StopSpawning()
    {
        running = false;
        StopAllCoroutines();
        inStartupBurst = false;
        initialBurstDone = false;
        continuousCarry = 0f;
    }

    private IEnumerator StartupBurstRoutine(int burstTarget)
    {
        while (running && requireWalkableSpawn && !IsFlowFieldReadyForSpawning())
            yield return null;

        int spawned = 0;
        float timer = 0f;

        int firstBatch = Mathf.Min(
            Mathf.Max(1, Mathf.RoundToInt(enemiesPerSecond * batchInterval)),
            Mathf.Min(burstTarget, maxAliveEnemies - FlowFieldEnemy.ActiveEnemies.Count));

        if (firstBatch > 0)
            spawned += SpawnBatch(firstBatch);

        while (spawned < burstTarget && running)
        {
            int slots = maxAliveEnemies - FlowFieldEnemy.ActiveEnemies.Count;
            if (slots <= 0) break;

            timer += Time.unscaledDeltaTime;
            if (timer < batchInterval)
            {
                yield return null;
                continue;
            }
            timer -= batchInterval;

            int perBatch = Mathf.Max(1, Mathf.RoundToInt(enemiesPerSecond * batchInterval));
            int thisBatch = Mathf.Min(perBatch, Mathf.Min(burstTarget - spawned, slots));
            if (thisBatch <= 0) break;

            int actually = SpawnBatch(thisBatch);
            spawned += actually;

            if (actually == 0)
            {
                WarnSpawnFailure(null);
                break;
            }

            yield return null;
        }

        initialBurstDone = true;
        inStartupBurst = false;
        nextSpawnTime = Time.time + batchInterval;
    }

    private void Update()
    {
        if (!running || inStartupBurst) return;

        if (!initialBurstDone && initialBurstCount > 0)
        {
            if (requireWalkableSpawn && !IsFlowFieldReadyForSpawning()) return;
            inStartupBurst = true;
            StartCoroutine(StartupBurstRoutine(initialBurstCount));
            return;
        }

        if (!keepSpawning) return;

        int alive = FlowFieldEnemy.ActiveEnemies.Count;
        if (alive >= maxAliveEnemies) return;
        if (requireWalkableSpawn && !IsFlowFieldReadyForSpawning()) return;
        if (Time.time < nextSpawnTime) return;

        continuousCarry += enemiesPerSecond * batchInterval;
        int perBatch = Mathf.Max(1, Mathf.FloorToInt(continuousCarry));
        continuousCarry -= perBatch;

        int slots = maxAliveEnemies - alive;
        int toSpawn = Mathf.Min(perBatch, slots);
        if (toSpawn > 0) SpawnBatch(toSpawn);

        nextSpawnTime = Time.time + batchInterval;
    }

    private int SpawnBatch(int count)
    {
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            EnemySpawnPoint point = PickSpawnPoint();
            if (point == null) break;
            if (SpawnAt(point)) spawned++;
        }

        return spawned;
    }

    private EnemySpawnPoint PickSpawnPoint()
    {
        eligiblePoints.Clear();
        float totalWeight = 0f;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            EnemySpawnPoint p = spawnPoints[i];
            if (p == null || !p.isActiveAndEnabled) continue;
            if (!p.CanSpawn) continue;
            if (p.Weight <= 0f) continue;

            eligiblePoints.Add(p);
            totalWeight += p.Weight;
        }

        if (eligiblePoints.Count == 0) return null;

        float pick = Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < eligiblePoints.Count; i++)
        {
            EnemySpawnPoint p = eligiblePoints[i];
            cumulative += p.Weight;
            if (pick <= cumulative) return p;
        }

        return eligiblePoints[eligiblePoints.Count - 1];
    }

    private bool SpawnAt(EnemySpawnPoint point)
    {
        FlowFieldGrid grid = GetFlowFieldFor(point);

        if (requireWalkableSpawn && grid != null && !grid.GridReady)
            return false;

        if (!TryGetValidSpawnPosition(point, grid, out Vector3 pos))
        {
            WarnSpawnFailure(point);
            return false;
        }

        GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);

        FlowFieldEnemy flowEnemy = enemy.GetComponent<FlowFieldEnemy>();
        if (flowEnemy == null)
        {
            Debug.LogError("[EnemySpawner] Enemy prefab is missing FlowFieldEnemy.", enemy);
            Destroy(enemy);
            return false;
        }

        if (grid != null)
            flowEnemy.SetFlowField(grid);

        point.SpawnedCount++;
        return true;
    }

    private bool TryGetValidSpawnPosition(
        EnemySpawnPoint point,
        FlowFieldGrid grid,
        out Vector3 position)
    {
        position = point.Position;

        if (grid == null)
        {
            if (point.SpawnRadius > 0.01f)
            {
                Vector3 off = Random.insideUnitSphere * point.SpawnRadius;
                off.y = 0f;
                position = point.Position + off;
            }
            return true;
        }

        if (point.SpawnRadius <= 0.01f)
            return IsValidSpawnPosition(point.Position, grid);

        for (int attempt = 0; attempt < maxSpawnPositionAttempts; attempt++)
        {
            Vector3 offset = Random.insideUnitSphere * point.SpawnRadius;
            offset.y = 0f;
            Vector3 candidate = point.Position + offset;

            if (IsValidSpawnPosition(candidate, grid))
            {
                position = candidate;
                return true;
            }
        }

        if (IsValidSpawnPosition(point.Position, grid))
        {
            position = point.Position;
            return true;
        }

        return false;
    }

    private bool IsValidSpawnPosition(Vector3 pos, FlowFieldGrid grid)
    {
        if (requireWalkableSpawn && !grid.IsWalkable(pos))
            return false;

        if (usePhysicsClearanceCheck && grid.ObstacleMask.value != 0)
        {
            Vector3 checkPos = pos + Vector3.up * spawnClearanceHeight;
            if (Physics.CheckSphere(
                    checkPos,
                    spawnClearanceRadius,
                    grid.ObstacleMask,
                    QueryTriggerInteraction.Ignore))
                return false;
        }

        return true;
    }

    private FlowFieldGrid GetFlowFieldFor(EnemySpawnPoint point)
    {
        if (point.OverrideFlowField != null) return point.OverrideFlowField;
        if (defaultFlowField != null) return defaultFlowField;
        return Object.FindFirstObjectByType<FlowFieldGrid>();
    }

    private bool IsFlowFieldReadyForSpawning()
    {
        if (!requireWalkableSpawn) return true;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            FlowFieldGrid grid = GetFlowFieldFor(spawnPoints[i]);
            if (grid != null && !grid.GridReady) return false;
        }

        return true;
    }

    private void WarnSpawnFailure(EnemySpawnPoint point)
    {
        if (Time.time - lastWarnTime < 5f) return;
        lastWarnTime = Time.time;

        string target = point != null ? $"'{point.name}'" : "all spawn points";

        Debug.LogWarning(
            $"[EnemySpawner] Could not find a valid spawn position for {target}. " +
            "Check spawn point placement (not inside a wall), Obstacle Mask on " +
            "FlowFieldGrid, and Obstacle Check Radius (recommend 1.05 for cellSize 2).",
            point != null ? point : (Object)this);
    }

    private void CollectSpawnPoints()
    {
        spawnPoints.Clear();

        if (autoFindSpawnPoints)
        {
            EnemySpawnPoint[] found = Object.FindObjectsByType<EnemySpawnPoint>(
                FindObjectsSortMode.None);

            for (int i = 0; i < found.Length; i++)
                spawnPoints.Add(found[i]);
        }

        if (manualSpawnPoints != null)
        {
            for (int i = 0; i < manualSpawnPoints.Length; i++)
            {
                EnemySpawnPoint p = manualSpawnPoints[i];
                if (p != null && !spawnPoints.Contains(p))
                    spawnPoints.Add(p);
            }
        }
    }
}