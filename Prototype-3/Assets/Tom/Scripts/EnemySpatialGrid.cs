using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct EnemyStaticData
{
    public float MoveSpeed;
    public float TurnSpeed;
    public float SteeringPhase;
    public byte Valid;
}

[BurstCompile]
public struct SpatialGridRebuildJob : IJob
{
    [ReadOnly] public NativeArray<float3> Positions;
    [ReadOnly] public NativeArray<EnemyStaticData> Statics;
    public int Count;

    public float3 Origin;
    public float CellSize;
    public int Width;
    public int Height;

    public NativeArray<int> CellCounts;
    public NativeArray<int> CellCursor;
    public NativeArray<int> CellStart;
    public NativeArray<int> SortedIndices;

    public void Execute()
    {
        int cellCount = Width * Height;
        for (int i = 0; i < cellCount; i++) CellCounts[i] = 0;

        for (int i = 0; i < Count; i++)
        {
            if (Statics[i].Valid == 0) continue;
            int cell = CellIndex(Positions[i]);
            if (cell < 0) continue;
            CellCounts[cell]++;
        }

        int sum = 0;
        for (int i = 0; i < cellCount; i++)
        {
            CellStart[i] = sum;
            sum += CellCounts[i];
            CellCursor[i] = CellStart[i];
        }
        CellStart[cellCount] = sum;

        for (int i = 0; i < Count; i++)
        {
            if (Statics[i].Valid == 0) continue;
            int cell = CellIndex(Positions[i]);
            if (cell < 0) continue;
            int slot = CellCursor[cell]++;
            SortedIndices[slot] = i;
        }
    }

    private int CellIndex(float3 p)
    {
        int x = (int)math.floor((p.x - Origin.x) / CellSize);
        int z = (int)math.floor((p.z - Origin.z) / CellSize);
        if (x < 0 || x >= Width || z < 0 || z >= Height) return -1;
        return z * Width + x;
    }
}

public class EnemySpatialGrid : MonoBehaviour
{
    public static EnemySpatialGrid Instance { get; private set; }

    [Header("Spatial Grid")]
    [SerializeField, Min(0.25f)] private float bucketSize = 1.8f;

    private NativeArray<int> cellStart;
    private NativeArray<int> cellCounts;
    private NativeArray<int> cellCursor;
    private NativeArray<int> sortedIndices;

    private int width;
    private int height;
    private float3 origin;
    private bool allocated;

    public float BucketSize => bucketSize;
    public int Width => width;
    public int Height => height;
    public float3 Origin => origin;
    public NativeArray<int> CellStart => cellStart;
    public NativeArray<int> SortedIndices => sortedIndices;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one EnemySpatialGrid is allowed.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        DisposeArrays();
    }

    public void EnsureAllocated(float3 newOrigin, int newWidth, int newHeight)
    {
        int cellCount = newWidth * newHeight;
        if (allocated &&
            width == newWidth &&
            height == newHeight &&
            origin.Equals(newOrigin) &&
            cellStart.IsCreated &&
            cellStart.Length == cellCount + 1)
            return;

        DisposeArrays();

        origin = newOrigin;
        width = newWidth;
        height = newHeight;

        cellStart = new NativeArray<int>(cellCount + 1, Allocator.Persistent);
        cellCounts = new NativeArray<int>(cellCount, Allocator.Persistent);
        cellCursor = new NativeArray<int>(cellCount, Allocator.Persistent);
        sortedIndices = new NativeArray<int>(Mathf.Max(16, 1024), Allocator.Persistent);
        allocated = true;
    }

    public void EnsureSortedCapacity(int count)
    {
        if (!sortedIndices.IsCreated || sortedIndices.Length < count)
        {
            if (sortedIndices.IsCreated) sortedIndices.Dispose();
            sortedIndices = new NativeArray<int>(Mathf.Max(16, count), Allocator.Persistent);
        }
    }

    public JobHandle ScheduleRebuild(
        NativeArray<float3> positions,
        NativeArray<EnemyStaticData> statics,
        int count,
        JobHandle dependency)
    {
        var job = new SpatialGridRebuildJob
        {
            Positions = positions,
            Statics = statics,
            Count = count,
            Origin = origin,
            CellSize = bucketSize,
            Width = width,
            Height = height,
            CellCounts = cellCounts,
            CellCursor = cellCursor,
            CellStart = cellStart,
            SortedIndices = sortedIndices
        };
        return job.Schedule(dependency);
    }

    private void DisposeArrays()
    {
        if (cellStart.IsCreated) cellStart.Dispose();
        if (cellCounts.IsCreated) cellCounts.Dispose();
        if (cellCursor.IsCreated) cellCursor.Dispose();
        if (sortedIndices.IsCreated) sortedIndices.Dispose();
        allocated = false;
    }
}