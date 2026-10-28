
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowFieldGrid : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Grid")]
    [SerializeField]
    private Vector2 gridWorldSize =
        new Vector2(1000f, 1000f);

    [SerializeField, Min(0.25f)]
    private float cellSize = 2f;

    [SerializeField] private float groundHeight = 0.5f;

    [Header("Chunks")]
    [SerializeField, Min(8)]
    private int chunkSizeCells = 32;

    [Header("Obstacles")]
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField, Min(0.05f)]
    private float obstacleCheckRadius = 0.8f;

    [Header("Debug Display")]
    [SerializeField] private bool showGrid = true;

    [SerializeField, Min(1)]
    private int drawEveryNthCell = 4;

    [SerializeField, Min(0.1f)]
    private float arrowLength = 0.5f;

    private int width;
    private int height;
    private int chunkColumns;
    private int chunkRows;

    private Vector3 origin;

    private bool[] walkable;
    private int[] distanceField;
    private Vector3[] directions;

    private ChunkData[] chunks;

    private bool gridReady;
    private int lastTargetCell = -1;
    private int currentTargetChunk = -1;

    private readonly Queue<int> cellQueue = new Queue<int>();
    private readonly Queue<int> chunkQueue = new Queue<int>();

    private static readonly Vector2Int[] Neighbours =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    private class ChunkData
    {
        public int x;
        public int z;

        public int startX;
        public int endX;
        public int startZ;
        public int endZ;

        public int nextChunk = -1;
        public int routeDistance = int.MaxValue;

        public bool dirty = true;

        public readonly List<int> neighbours = new List<int>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        if (target == null)
        {
            Debug.LogError(
                "FlowFieldGrid: No target found. Assign a target or tag the player Player.",
                this);

            enabled = false;
            return;
        }

        StartCoroutine(BuildGridRoutine());
    }

    private IEnumerator BuildGridRoutine()
    {
        width = Mathf.CeilToInt(gridWorldSize.x / cellSize);
        height = Mathf.CeilToInt(gridWorldSize.y / cellSize);

        origin = transform.position - new Vector3(
            width * cellSize * 0.5f,
            0f,
            height * cellSize * 0.5f);

        int count = width * height;

        walkable = new bool[count];
        distanceField = new int[count];
        directions = new Vector3[count];

        // Spread initial obstacle checks over multiple frames.
        const int cellsPerFrame = 4096;

        for (int i = 0; i < count; i++)
        {
            int x = i % width;
            int z = i / width;

            bool blocked = Physics.CheckSphere(
                CellCenter(x, z),
                obstacleCheckRadius,
                obstacleMask,
                QueryTriggerInteraction.Ignore);

            walkable[i] = !blocked;

            if ((i + 1) % cellsPerFrame == 0)
                yield return null;
        }

        CreateChunks();
        BuildChunkConnections();

        int targetCell = WorldToIndex(target.position);

        lastTargetCell = targetCell;

        if (targetCell >= 0)
        {
            currentTargetChunk = GetChunkIdFromCell(targetCell);
            RebuildChunkRoutes(currentTargetChunk);
        }

        gridReady = true;

        Debug.Log(
            $"Chunk flow field ready: {width} x {height} cells, " +
            $"{chunkColumns} x {chunkRows} chunks.");
    }

    private void Update()
    {
        if (!gridReady || target == null)
            return;

        int targetCell = WorldToIndex(target.position);

        if (targetCell < 0 || targetCell == lastTargetCell)
            return;

        int newTargetChunk = GetChunkIdFromCell(targetCell);

        if (newTargetChunk != currentTargetChunk)
        {
            // Recalculate only the small chunk-level route map.
            currentTargetChunk = newTargetChunk;
            RebuildChunkRoutes(currentTargetChunk);

            // Local fields are rebuilt on demand as enemies need them.
            for (int i = 0; i < chunks.Length; i++)
                chunks[i].dirty = true;
        }
        else
        {
            // Player moved within the same chunk.
            chunks[currentTargetChunk].dirty = true;
        }

        lastTargetCell = targetCell;
    }

    private void CreateChunks()
    {
        chunkColumns = Mathf.CeilToInt(
            (float)width / chunkSizeCells);

        chunkRows = Mathf.CeilToInt(
            (float)height / chunkSizeCells);

        chunks = new ChunkData[chunkColumns * chunkRows];

        for (int cz = 0; cz < chunkRows; cz++)
        {
            for (int cx = 0; cx < chunkColumns; cx++)
            {
                ChunkData chunk = new ChunkData();

                chunk.x = cx;
                chunk.z = cz;

                chunk.startX = cx * chunkSizeCells;
                chunk.endX = Mathf.Min(
                    chunk.startX + chunkSizeCells, width);

                chunk.startZ = cz * chunkSizeCells;
                chunk.endZ = Mathf.Min(
                    chunk.startZ + chunkSizeCells, height);

                chunks[ToChunkIndex(cx, cz)] = chunk;
            }
        }
    }

    private void BuildChunkConnections()
    {
        // Connect chunks only when walkable cells touch across
        // their shared border.
        for (int cz = 0; cz < chunkRows; cz++)
        {
            for (int cx = 0; cx < chunkColumns; cx++)
            {
                ChunkData chunk = chunks[ToChunkIndex(cx, cz)];

                if (cx + 1 < chunkColumns)
                {
                    ChunkData right =
                        chunks[ToChunkIndex(cx + 1, cz)];

                    int borderX = chunk.endX;
                    bool connected = false;

                    for (int z = chunk.startZ;
                         z < chunk.endZ; z++)
                    {
                        if (walkable[ToIndex(borderX - 1, z)] &&
                            walkable[ToIndex(borderX, z)])
                        {
                            connected = true;
                            break;
                        }
                    }

                    if (connected)
                    {
                        chunk.neighbours.Add(
                            ToChunkIndex(cx + 1, cz));

                        right.neighbours.Add(
                            ToChunkIndex(cx, cz));
                    }
                }

                if (cz + 1 < chunkRows)
                {
                    ChunkData above =
                        chunks[ToChunkIndex(cx, cz + 1)];

                    int borderZ = chunk.endZ;
                    bool connected = false;

                    for (int x = chunk.startX;
                         x < chunk.endX; x++)
                    {
                        if (walkable[ToIndex(x, borderZ - 1)] &&
                            walkable[ToIndex(x, borderZ)])
                        {
                            connected = true;
                            break;
                        }
                    }

                    if (connected)
                    {
                        chunk.neighbours.Add(
                            ToChunkIndex(cx, cz + 1));

                        above.neighbours.Add(
                            ToChunkIndex(cx, cz));
                    }
                }
            }
        }
    }

    private void RebuildChunkRoutes(int targetChunk)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].nextChunk = -1;
            chunks[i].routeDistance = int.MaxValue;
        }

        if (targetChunk < 0 || targetChunk >= chunks.Length)
            return;

        chunkQueue.Clear();

        chunks[targetChunk].routeDistance = 0;
        chunkQueue.Enqueue(targetChunk);

        // Route between chunks instead of searching every cell.
        while (chunkQueue.Count > 0)
        {
            int current = chunkQueue.Dequeue();
            ChunkData currentChunk = chunks[current];

            for (int i = 0;
                 i < currentChunk.neighbours.Count; i++)
            {
                int neighbour = currentChunk.neighbours[i];

                if (chunks[neighbour].routeDistance != int.MaxValue)
                    continue;

                chunks[neighbour].routeDistance =
                    currentChunk.routeDistance + 1;

                chunks[neighbour].nextChunk = current;
                chunkQueue.Enqueue(neighbour);
            }
        }
    }

    private void EnsureChunkField(int chunkId)
    {
        ChunkData chunk = chunks[chunkId];

        if (!chunk.dirty)
            return;

        // Reset only this chunk's cells.
        for (int z = chunk.startZ; z < chunk.endZ; z++)
        {
            for (int x = chunk.startX; x < chunk.endX; x++)
            {
                int index = ToIndex(x, z);

                distanceField[index] = int.MaxValue;
                directions[index] = Vector3.zero;
            }
        }

        cellQueue.Clear();

        if (chunkId == currentTargetChunk)
        {
            int targetCell = FindNearestWalkableInChunk(
                chunk, target.position);

            if (targetCell >= 0)
            {
                distanceField[targetCell] = 0;
                cellQueue.Enqueue(targetCell);
            }
        }
        else
        {
            AddExitSeeds(chunk);
        }

        // Calculate directions inside this chunk only.
        while (cellQueue.Count > 0)
        {
            int current = cellQueue.Dequeue();

            int x = current % width;
            int z = current / width;

            for (int n = 0; n < Neighbours.Length; n++)
            {
                int nx = x + Neighbours[n].x;
                int nz = z + Neighbours[n].y;

                if (!CanStepWithinChunk(
                    chunk, x, z, nx, nz))
                    continue;

                int next = ToIndex(nx, nz);

                if (distanceField[next] != int.MaxValue)
                    continue;

                distanceField[next] =
                    distanceField[current] + 1;

                cellQueue.Enqueue(next);
            }
        }

        // Convert local distances into movement directions.
        for (int z = chunk.startZ; z < chunk.endZ; z++)
        {
            for (int x = chunk.startX; x < chunk.endX; x++)
            {
                int current = ToIndex(x, z);

                if (!walkable[current] ||
                    distanceField[current] == int.MaxValue)
                    continue;

                if (distanceField[current] == 0)
                {
                    // Exit cells point into the next chunk.
                    if (chunkId != currentTargetChunk &&
                        chunk.nextChunk >= 0)
                    {
                        ChunkData next = chunks[chunk.nextChunk];

                        directions[current] = new Vector3(
                            next.x - chunk.x,
                            0f,
                            next.z - chunk.z).normalized;
                    }

                    continue;
                }

                int best = current;
                int bestDistance = distanceField[current];

                for (int n = 0; n < Neighbours.Length; n++)
                {
                    int nx = x + Neighbours[n].x;
                    int nz = z + Neighbours[n].y;

                    if (!CanStepWithinChunk(
                        chunk, x, z, nx, nz))
                        continue;

                    int next = ToIndex(nx, nz);

                    if (distanceField[next] < bestDistance)
                    {
                        best = next;
                        bestDistance = distanceField[next];
                    }
                }

                if (best != current)
                {
                    Vector3 delta =
                        CellCenter(best % width, best / width) -
                        CellCenter(x, z);

                    directions[current] = delta.normalized;
                }
            }
        }

        chunk.dirty = false;
    }

    private void AddExitSeeds(ChunkData chunk)
    {
        int nextChunkId = chunk.nextChunk;

        // No known route to the target.
        if (nextChunkId < 0 ||
            chunks[nextChunkId].routeDistance == int.MaxValue)
            return;

        ChunkData next = chunks[nextChunkId];

        // Seed cells along the shared border that can cross into
        // the next chunk.
        if (next.x > chunk.x)
        {
            int x = chunk.endX - 1;

            for (int z = chunk.startZ; z < chunk.endZ; z++)
            {
                if (walkable[ToIndex(x, z)] &&
                    walkable[ToIndex(x + 1, z)])
                    AddSeed(ToIndex(x, z));
            }
        }
        else if (next.x < chunk.x)
        {
            int x = chunk.startX;

            for (int z = chunk.startZ; z < chunk.endZ; z++)
            {
                if (walkable[ToIndex(x, z)] &&
                    walkable[ToIndex(x - 1, z)])
                    AddSeed(ToIndex(x, z));
            }
        }
        else if (next.z > chunk.z)
        {
            int z = chunk.endZ - 1;

            for (int x = chunk.startX; x < chunk.endX; x++)
            {
                if (walkable[ToIndex(x, z)] &&
                    walkable[ToIndex(x, z + 1)])
                    AddSeed(ToIndex(x, z));
            }
        }
        else if (next.z < chunk.z)
        {
            int z = chunk.startZ;

            for (int x = chunk.startX; x < chunk.endX; x++)
            {
                if (walkable[ToIndex(x, z)] &&
                    walkable[ToIndex(x, z - 1)])
                    AddSeed(ToIndex(x, z));
            }
        }
    }

    private void AddSeed(int index)
    {
        if (distanceField[index] == 0)
            return;

        distanceField[index] = 0;
        cellQueue.Enqueue(index);
    }

    private int FindNearestWalkableInChunk(
        ChunkData chunk, Vector3 position)
    {
        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int z = chunk.startZ; z < chunk.endZ; z++)
        {
            for (int x = chunk.startX; x < chunk.endX; x++)
            {
                int index = ToIndex(x, z);

                if (!walkable[index])
                    continue;

                float distance =
                    (CellCenter(x, z) - position).sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = index;
                }
            }
        }

        return bestIndex;
    }

    private bool CanStepWithinChunk(
        ChunkData chunk, int x, int z, int nx, int nz)
    {
        if (nx < chunk.startX || nx >= chunk.endX ||
            nz < chunk.startZ || nz >= chunk.endZ)
            return false;

        if (!walkable[ToIndex(nx, nz)])
            return false;

        int dx = nx - x;
        int dz = nz - z;

        // Prevent cutting diagonally between blocked cells.
        if (dx != 0 && dz != 0)
        {
            if (!walkable[ToIndex(x + dx, z)] ||
                !walkable[ToIndex(x, z + dz)])
                return false;
        }

        return true;
    }

    public Vector3 GetDirection(Vector3 worldPosition)
    {
        if (!gridReady)
            return Vector3.zero;

        int index = WorldToIndex(worldPosition);

        if (index < 0 || !walkable[index])
            return Vector3.zero;

        int chunkId = GetChunkIdFromCell(index);

        EnsureChunkField(chunkId);

        if (distanceField[index] == int.MaxValue)
            return Vector3.zero;

        return directions[index];
    }

    public bool IsWalkable(Vector3 worldPosition)
    {
        if (!gridReady)
            return false;

        int index = WorldToIndex(worldPosition);

        return index >= 0 && walkable[index];
    }

    private int WorldToIndex(Vector3 position)
    {
        int x = Mathf.FloorToInt(
            (position.x - origin.x) / cellSize);

        int z = Mathf.FloorToInt(
            (position.z - origin.z) / cellSize);

        if (!InsideGrid(x, z))
            return -1;

        return ToIndex(x, z);
    }

    private int GetChunkIdFromCell(int index)
    {
        int x = index % width;
        int z = index / width;

        return ToChunkIndex(
            x / chunkSizeCells,
            z / chunkSizeCells);
    }

    private int ToChunkIndex(int x, int z)
    {
        return z * chunkColumns + x;
    }

    private int ToIndex(int x, int z)
    {
        return z * width + x;
    }

    private bool InsideGrid(int x, int z)
    {
        return x >= 0 && x < width &&
               z >= 0 && z < height;
    }

    private Vector3 CellCenter(int x, int z)
    {
        return origin + new Vector3(
            (x + 0.5f) * cellSize,
            groundHeight,
            (z + 0.5f) * cellSize);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGrid || !gridReady)
            return;

        int step = Mathf.Max(1, drawEveryNthCell);

        for (int z = 0; z < height; z += step)
        {
            for (int x = 0; x < width; x += step)
            {
                int index = ToIndex(x, z);
                Vector3 center = CellCenter(x, z);

                if (!walkable[index])
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(
                        center, Vector3.one * cellSize * 0.7f);
                    continue;
                }

                Gizmos.color =
                    distanceField[index] == int.MaxValue
                    ? Color.gray
                    : Color.cyan;

                Vector3 direction = directions[index];

                if (direction.sqrMagnitude > 0.001f)
                    Gizmos.DrawRay(
                        center, direction * arrowLength);
            }
        }
    }
}
