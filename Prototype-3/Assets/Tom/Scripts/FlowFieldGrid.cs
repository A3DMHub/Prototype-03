using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct FlowFieldSnapshot
{
    public NativeArray<byte> Walkable;
    public NativeArray<float3> Directions;
    public NativeArray<int> Distances;
    public float3 Origin;
    public float CellSize;
    public float InvCellSize;
    public int Width;
    public int Height;
    public byte Ready;
    public float3 TargetPosition;
    public byte DirectSteeringEnabled;
    public int DirectLineSamples;
}

public class FlowFieldGrid : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Grid")]
    [SerializeField] private Vector2 gridWorldSize = new Vector2(1000f, 1000f);
    [SerializeField, Min(0.25f)] private float cellSize = 2f;
    [SerializeField] private float groundHeight = 0.5f;

    [Header("Obstacle Sampling")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField, Min(0.05f)] private float obstacleCheckRadius = 0.8f;
    [SerializeField, Min(256)] private int obstacleChecksPerFrame = 4096;

    [Header("Flow Field Build")]
    [SerializeField, Min(256)] private int nodesPerFrame = 20000;
    [SerializeField, Min(256)] private int directionCellsPerFrame = 30000;
    [SerializeField, Min(0.5f)] private float targetRebuildDistance = 1.5f;

    [Header("Direction Stability")]
    [SerializeField, Min(1f)] private float cardinalDominanceThreshold = 1.6f;

    [Header("Immediate Response")]
    [SerializeField, Min(4)] private int directLineSamples = 24;

    [Header("Debug Display")]
    [SerializeField] private bool showGrid;
    [SerializeField, Min(1)] private int drawEveryNthCell = 4;
    [SerializeField, Min(0.1f)] private float arrowLength = 0.5f;

    private int width;
    private int height;
    private Vector3 origin;
    private float invCellSize = 1f;
    private bool[] walkable;

    private int[] activeDistances;
    private Vector3[] activeDirections;
    private int[] buildDistances;
    private Vector3[] buildDirections;

    private int[] heapNodes;
    private int[] heapPositions;
    private int heapCount;

    private NativeArray<byte> walkableNative;
    private NativeArray<float3> directionsNative;
    private NativeArray<int> distancesNative;
    private bool nativeDirty;

    private bool gridReady;
    private bool buildRunning;
    private int builtTargetCell = -1;
    private int latestTargetCell = -1;
    private int pendingTargetCell = -1;
    private Coroutine buildRoutine;

    private static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] Dz = { 0, 0, 1, -1, 1, -1, 1, -1 };
    private static readonly int[] MoveCost = { 100, 100, 100, 100, 142, 142, 142, 142 };

    public bool IsRebuildPending => buildRunning || pendingTargetCell >= 0;
    public bool GridReady => gridReady;
    public Vector3 WorldOrigin => origin;
    public Vector2 WorldSize => gridWorldSize;
    public float CellSize => cellSize;
    public bool HasNativeData =>
    walkableNative.IsCreated &&
    directionsNative.IsCreated &&
    distancesNative.IsCreated;

    public Vector3 TargetNavigationPosition
    {
        get
        {
            if (latestTargetCell >= 0 && width > 0 && height > 0)
                return CellCenter(latestTargetCell % width, latestTargetCell / width);
            return target != null ? target.position : transform.position;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target == null)
        {
            Debug.LogError("FlowFieldGrid: Assign a target or tag the player Player.", this);
            enabled = false;
            return;
        }

        StartCoroutine(BuildGridRoutine());
    }

    private IEnumerator BuildGridRoutine()
    {
        width = Mathf.CeilToInt(gridWorldSize.x / cellSize);
        height = Mathf.CeilToInt(gridWorldSize.y / cellSize);
        origin = transform.position - new Vector3(width * cellSize * 0.5f, 0f, height * cellSize * 0.5f);
        invCellSize = 1f / cellSize;

        int count = width * height;
        walkable = new bool[count];
        activeDistances = new int[count];
        activeDirections = new Vector3[count];
        buildDistances = new int[count];
        buildDirections = new Vector3[count];
        heapNodes = new int[count];
        heapPositions = new int[count];

        int checks = Mathf.Max(256, obstacleChecksPerFrame);
        for (int i = 0; i < count; i++)
        {
            int x = i % width;
            int z = i / width;
            walkable[i] = !Physics.CheckSphere(
                CellCenter(x, z),
                obstacleCheckRadius,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            if ((i + 1) % checks == 0) yield return null;
        }

        gridReady = true;

        int initialTargetCell = FindNearestWalkableCell(target.position);
        latestTargetCell = initialTargetCell;

        if (initialTargetCell >= 0)
            StartRebuild(initialTargetCell);
    }

    private void Update()
    {
        if (!gridReady || target == null) return;

        int targetCell = FindNearestWalkableCell(target.position);
        if (targetCell < 0) return;

        latestTargetCell = targetCell;

        if (buildRunning)
        {
            if (targetCell != pendingTargetCell && NeedsRebuild(targetCell, target.position))
                pendingTargetCell = targetCell;
            return;
        }

        if (targetCell == builtTargetCell) return;
        if (!NeedsRebuild(targetCell, target.position)) return;

        pendingTargetCell = targetCell;
        StartRebuild(pendingTargetCell);
    }

    private bool NeedsRebuild(int targetCell, Vector3 targetPosition)
    {
        if (targetCell < 0) return false;
        if (builtTargetCell < 0) return true;
        if (targetCell == builtTargetCell) return false;

        Vector3 builtPosition = CellCenter(
            builtTargetCell % width,
            builtTargetCell / width);

        return (targetPosition - builtPosition).sqrMagnitude >=
               targetRebuildDistance * targetRebuildDistance;
    }

    private void StartRebuild(int targetCell)
    {
        if (targetCell < 0 || buildRunning) return;
        pendingTargetCell = -1;
        buildRoutine = StartCoroutine(RebuildFlowFieldRoutine(targetCell));
    }

    private IEnumerator RebuildFlowFieldRoutine(int targetCell)
    {
        if (targetCell < 0)
        {
            buildRunning = false;
            buildRoutine = null;
            yield break;
        }

        buildRunning = true;
        int count = width * height;

        const int resetBudget = 20000;
        for (int i = 0; i < count; i++)
        {
            buildDistances[i] = int.MaxValue;
            heapPositions[i] = -1;
            buildDirections[i] = Vector3.zero;
            if ((i + 1) % resetBudget == 0) yield return null;
        }

        heapCount = 0;
        buildDistances[targetCell] = 0;
        HeapInsertOrDecrease(targetCell);

        int processed = 0;
        int budget = Mathf.Max(256, nodesPerFrame);

        while (heapCount > 0)
        {
            int current = HeapPopMin();
            int currentDistance = buildDistances[current];
            int x = current % width;
            int z = current / width;
            int start = (x + z) & 7;

            for (int k = 0; k < 8; k++)
            {
                int n = (start + k) & 7;
                int nx = x + Dx[n];
                int nz = z + Dz[n];

                if (!InsideGrid(nx, nz)) continue;
                int next = ToIndex(nx, nz);
                if (!walkable[next]) continue;

                if (Dx[n] != 0 && Dz[n] != 0)
                {
                    if (!walkable[ToIndex(x + Dx[n], z)] ||
                        !walkable[ToIndex(x, z + Dz[n])])
                        continue;
                }

                int candidate = currentDistance + MoveCost[n];
                if (candidate >= buildDistances[next]) continue;

                buildDistances[next] = candidate;
                HeapInsertOrDecrease(next);
            }

            processed++;
            if (processed >= budget)
            {
                processed = 0;
                yield return null;
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (buildDistances[i] == int.MaxValue || !walkable[i])
            {
                buildDirections[i] = Vector3.zero;
                continue;
            }

            int x = i % width;
            int z = i / width;
            int centerDistance = buildDistances[i];

            int left = DistanceAt(x - 1, z, centerDistance);
            int right = DistanceAt(x + 1, z, centerDistance);
            int down = DistanceAt(x, z - 1, centerDistance);
            int up = DistanceAt(x, z + 1, centerDistance);

            Vector3 gradient = new Vector3(left - right, 0f, down - up);

            if (gradient.sqrMagnitude < 0.0001f)
                gradient = FindLowestNeighbourDirection(x, z, centerDistance);

            buildDirections[i] = QuantizeToEight(gradient);

            if ((i + 1) % Mathf.Max(256, directionCellsPerFrame) == 0)
                yield return null;
        }

        int[] oldDistances = activeDistances;
        activeDistances = buildDistances;
        buildDistances = oldDistances;

        Vector3[] oldDirections = activeDirections;
        activeDirections = buildDirections;
        buildDirections = oldDirections;

        builtTargetCell = targetCell;
        buildRunning = false;
        buildRoutine = null;
        nativeDirty = true;

        int nextTarget = pendingTargetCell;
        pendingTargetCell = -1;

        if (target != null &&
            nextTarget >= 0 &&
            nextTarget != builtTargetCell &&
            NeedsRebuild(nextTarget, target.position))
        {
            StartRebuild(nextTarget);
        }
    }

    public void PublishNativeNow()
    {
        if (!nativeDirty || !gridReady) return;
        nativeDirty = false;

        int count = width * height;

        if (!walkableNative.IsCreated || walkableNative.Length != count)
        {
            if (walkableNative.IsCreated) walkableNative.Dispose();
            walkableNative = new NativeArray<byte>(count, Allocator.Persistent);
        }

        if (!directionsNative.IsCreated || directionsNative.Length != count)
        {
            if (directionsNative.IsCreated) directionsNative.Dispose();
            directionsNative = new NativeArray<float3>(count, Allocator.Persistent);
        }

        if (!distancesNative.IsCreated || distancesNative.Length != count)
        {
            if (distancesNative.IsCreated) distancesNative.Dispose();
            distancesNative = new NativeArray<int>(count, Allocator.Persistent);
        }

        for (int i = 0; i < count; i++)
        {
            walkableNative[i] = walkable[i] ? (byte)1 : (byte)0;
            Vector3 d = activeDirections[i];
            directionsNative[i] = new float3(d.x, d.y, d.z);
            distancesNative[i] = activeDistances[i];
        }
    }

    public FlowFieldSnapshot CreateSnapshot(bool directSteeringEnabled)
    {
        bool ready = gridReady && walkableNative.IsCreated && directionsNative.IsCreated
                     && builtTargetCell >= 0;

        return new FlowFieldSnapshot
        {
            Walkable = walkableNative,
            Directions = directionsNative,
            Distances = distancesNative,
            Origin = new float3(origin.x, origin.y, origin.z),
            CellSize = cellSize,
            InvCellSize = invCellSize,
            Width = width,
            Height = height,
            Ready = (byte)(ready ? 1 : 0),
            TargetPosition = new float3(
                TargetNavigationPosition.x,
                TargetNavigationPosition.y,
                TargetNavigationPosition.z),
            DirectSteeringEnabled = (byte)(directSteeringEnabled ? 1 : 0),
            DirectLineSamples = Mathf.Max(4, directLineSamples)
        };
    }

    public void DisposeNative()
    {
        if (walkableNative.IsCreated) walkableNative.Dispose();
        if (directionsNative.IsCreated) directionsNative.Dispose();
        if (distancesNative.IsCreated) distancesNative.Dispose();
    }

    private void OnDestroy() => DisposeNative();

    private Vector3 QuantizeToEight(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return Vector3.zero;

        float ax = Mathf.Abs(direction.x);
        float az = Mathf.Abs(direction.z);
        float threshold = Mathf.Max(1f, cardinalDominanceThreshold);

        if (ax > az * threshold) return new Vector3(Mathf.Sign(direction.x), 0f, 0f);
        if (az > ax * threshold) return new Vector3(0f, 0f, Mathf.Sign(direction.z));

        return new Vector3(Mathf.Sign(direction.x), 0f, Mathf.Sign(direction.z)).normalized;
    }

    private Vector3 FindLowestNeighbourDirection(int x, int z, int centerDistance)
    {
        int bestDistance = centerDistance;
        Vector3 bestDirection = Vector3.zero;
        int start = (x + z) & 7;

        for (int k = 0; k < 8; k++)
        {
            int n = (start + k) & 7;
            int nx = x + Dx[n];
            int nz = z + Dz[n];

            if (!InsideGrid(nx, nz)) continue;

            if (Dx[n] != 0 && Dz[n] != 0 &&
                (!walkable[ToIndex(x + Dx[n], z)] ||
                 !walkable[ToIndex(x, z + Dz[n])]))
                continue;

            int next = ToIndex(nx, nz);
            if (!walkable[next]) continue;

            int d = buildDistances[next];
            if (d < bestDistance)
            {
                bestDistance = d;
                bestDirection = new Vector3(Dx[n], 0f, Dz[n]);
            }
        }

        return bestDirection;
    }

    private int DistanceAt(int x, int z, int fallback)
    {
        if (!InsideGrid(x, z)) return fallback;
        int index = ToIndex(x, z);
        if (!walkable[index] || buildDistances[index] == int.MaxValue) return fallback;
        return buildDistances[index];
    }

    private void HeapInsertOrDecrease(int node)
    {
        int position = heapPositions[node];
        if (position < 0)
        {
            position = heapCount++;
            heapNodes[position] = node;
            heapPositions[node] = position;
        }
        SiftUp(position);
    }

    private int HeapPopMin()
    {
        int result = heapNodes[0];
        heapPositions[result] = -1;
        heapCount--;
        if (heapCount > 0)
        {
            int last = heapNodes[heapCount];
            heapNodes[0] = last;
            heapPositions[last] = 0;
            SiftDown(0);
        }
        return result;
    }

    private void SiftUp(int position)
    {
        int node = heapNodes[position];
        while (position > 0)
        {
            int parent = (position - 1) / 2;
            if (buildDistances[heapNodes[parent]] <= buildDistances[node]) break;
            heapNodes[position] = heapNodes[parent];
            heapPositions[heapNodes[position]] = position;
            position = parent;
        }
        heapNodes[position] = node;
        heapPositions[node] = position;
    }

    private void SiftDown(int position)
    {
        int node = heapNodes[position];
        while (true)
        {
            int left = position * 2 + 1;
            if (left >= heapCount) break;
            int right = left + 1;
            int smallest = left;
            if (right < heapCount &&
                buildDistances[heapNodes[right]] < buildDistances[heapNodes[left]])
                smallest = right;
            if (buildDistances[node] <= buildDistances[heapNodes[smallest]]) break;
            heapNodes[position] = heapNodes[smallest];
            heapPositions[heapNodes[position]] = position;
            position = smallest;
        }
        heapNodes[position] = node;
        heapPositions[node] = position;
    }

    private int FindNearestWalkableCell(Vector3 position)
    {
        if (width <= 0 || height <= 0) return -1;

        int cx = Mathf.Clamp(Mathf.FloorToInt((position.x - origin.x) * invCellSize), 0, width - 1);
        int cz = Mathf.Clamp(Mathf.FloorToInt((position.z - origin.z) * invCellSize), 0, height - 1);
        int maxRadius = 12;

        for (int r = 0; r <= maxRadius; r++)
        {
            float bestSqr = float.MaxValue;
            int best = -1;
            int minX = Mathf.Max(0, cx - r);
            int maxX = Mathf.Min(width - 1, cx + r);
            int minZ = Mathf.Max(0, cz - r);
            int maxZ = Mathf.Min(height - 1, cz + r);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (r > 0 && x != minX && x != maxX && z != minZ && z != maxZ)
                        continue;
                    int i = ToIndex(x, z);
                    if (!walkable[i]) continue;
                    float sqr = (CellCenter(x, z) - position).sqrMagnitude;
                    if (sqr < bestSqr) { bestSqr = sqr; best = i; }
                }
            }

            if (best >= 0) return best;
        }

        return -1;
    }

    public Vector3 GetDirection(Vector3 worldPosition)
    {
        if (!gridReady || activeDirections == null || builtTargetCell < 0)
            return Vector3.zero;

        int x = Mathf.FloorToInt((worldPosition.x - origin.x) * invCellSize);
        int z = Mathf.FloorToInt((worldPosition.z - origin.z) * invCellSize);

        if (!InsideGrid(x, z)) return Vector3.zero;

        int index = ToIndex(x, z);
        if (!walkable[index]) return Vector3.zero;

        float fx = (worldPosition.x - origin.x) * invCellSize - 0.5f;
        float fz = (worldPosition.z - origin.z) * invCellSize - 0.5f;

        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, width - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, height - 1);
        int x1 = x0 + 1 < width ? x0 + 1 : width - 1;
        int z1 = z0 + 1 < height ? z0 + 1 : height - 1;

        Vector3 d00 = DirectionAt(x0, z0);
        Vector3 d10 = DirectionAt(x1, z0);
        Vector3 d01 = DirectionAt(x0, z1);
        Vector3 d11 = DirectionAt(x1, z1);

        if (d00 == d10 && d00 == d01 && d00 == d11)
            return d00;

        float tx = Mathf.Clamp01(fx - Mathf.Floor(fx));
        float tz = Mathf.Clamp01(fz - Mathf.Floor(fz));

        Vector3 a = Vector3.Lerp(d00, d10, tx);
        Vector3 b = Vector3.Lerp(d01, d11, tx);
        Vector3 result = Vector3.Lerp(a, b, tz);
        result.y = 0f;

        if (result.sqrMagnitude > 0.0001f) return result.normalized;
        Vector3 fallback = activeDirections[index];
        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.zero;
    }

    public bool IsWalkable(Vector3 worldPosition)
    {
        if (!gridReady) return false;
        int x = Mathf.FloorToInt((worldPosition.x - origin.x) * invCellSize);
        int z = Mathf.FloorToInt((worldPosition.z - origin.z) * invCellSize);
        return InsideGrid(x, z) && walkable[ToIndex(x, z)];
    }

    private Vector3 DirectionAt(int x, int z)
    {
        int i = ToIndex(x, z);
        return walkable[i] ? activeDirections[i] : Vector3.zero;
    }

    private bool InsideGrid(int x, int z) => x >= 0 && x < width && z >= 0 && z < height;
    private int ToIndex(int x, int z) => z * width + x;
    private Vector3 CellCenter(int x, int z) =>
        origin + new Vector3((x + 0.5f) * cellSize, groundHeight, (z + 0.5f) * cellSize);

    private void OnDrawGizmosSelected()
    {
        if (!showGrid || !gridReady) return;
        int step = Mathf.Max(1, drawEveryNthCell);
        for (int z = 0; z < height; z += step)
        {
            for (int x = 0; x < width; x += step)
            {
                int i = ToIndex(x, z);
                Vector3 center = CellCenter(x, z);
                if (!walkable[i])
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(center, Vector3.one * cellSize * 0.7f);
                }
                else if (activeDirections != null && activeDirections[i].sqrMagnitude > 0.001f)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawRay(center, activeDirections[i] * arrowLength);
                }
            }
        }
    }
}