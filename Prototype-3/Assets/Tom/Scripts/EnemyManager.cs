using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct SteeringJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Positions;
    [ReadOnly] public NativeArray<EnemyStaticData> Statics;

    public NativeArray<float3> Directions;
    public NativeArray<quaternion> TargetRotations;

    // Flow field snapshot
    [ReadOnly] public NativeArray<byte> Walkable;
    [ReadOnly] public NativeArray<float3> FlowDirections;
    [ReadOnly] public NativeArray<int> Distances;
    public float3 FlowOrigin;
    public float FlowCellSize;
    public float FlowInvCellSize;
    public int FlowWidth;
    public int FlowHeight;
    public byte FlowReady;
    public float3 FlowTargetPosition;
    public byte DirectSteeringEnabled;
    public int DirectLineSamples;

    // Spatial grid snapshot
    [ReadOnly] public NativeArray<int> CellStart;
    [ReadOnly] public NativeArray<int> SortedIndices;
    public float3 SpatialOrigin;
    public float SpatialCellSize;
    public int SpatialWidth;
    public int SpatialHeight;

    // Steering tunables
    public float NeighbourRadius;
    public float MinimumSpacing;
    public float SeparationStrength;
    public int MaxNeighbours;
    public float WanderStrength;
    public float DirectSteeringWeight;
    public float Time;

    // Reduces diagonal convergence by pulling interpolated directions
    // toward the dominant cardinal axis. 1.0 = no bias, 0.0 = fully cardinal.
    public float DiagonalSuppressionFactor;

    public void Execute(int i)
    {
        if (Statics[i].Valid == 0)
        {
            Directions[i] = float3.zero;
            TargetRotations[i] = quaternion.identity;
            return;
        }

        if (FlowReady == 0)
        {
            Directions[i] = float3.zero;
            TargetRotations[i] = quaternion.identity;
            return;
        }

        float3 position = Positions[i];
        float3 flow = SampleFlow(position);

        float3 steeringBase = flow;

        if (DirectSteeringEnabled == 1 &&
            TryDirectDirection(position, out float3 direct))
        {
            float w = math.saturate(DirectSteeringWeight);
            if (math.lengthsq(flow) > 0.0001f)
                steeringBase = math.lerp(flow, direct, w);
            else
                steeringBase = direct * w;
        }

        if (math.lengthsq(steeringBase) > 0.0001f)
            steeringBase = math.normalize(steeringBase);
        else
            steeringBase = flow;

        float3 separation = ComputeSeparation(i, position);

        float3 lateral = new float3(-steeringBase.z, 0f, steeringBase.x);
        float wander = math.sin(Time * 1.2f + Statics[i].SteeringPhase) * WanderStrength;

        float3 direction =
            steeringBase +
            separation * SeparationStrength +
            lateral * wander;

        if (math.lengthsq(direction) < 0.0001f) direction = steeringBase;
        if (math.lengthsq(direction) < 0.0001f)
        {
            Directions[i] = float3.zero;
            TargetRotations[i] = quaternion.identity;
            return;
        }

        direction = math.normalize(direction);

        Directions[i] = direction;
        TargetRotations[i] = quaternion.LookRotationSafe(direction, math.up());
    }

    private float3 SampleFlow(float3 pos)
    {
        int x = (int)math.floor((pos.x - FlowOrigin.x) * FlowInvCellSize);
        int z = (int)math.floor((pos.z - FlowOrigin.z) * FlowInvCellSize);

        if (x < 0 || x >= FlowWidth || z < 0 || z >= FlowHeight) return float3.zero;

        int index = z * FlowWidth + x;
        if (Walkable[index] == 0) return float3.zero;

        float fx = (pos.x - FlowOrigin.x) * FlowInvCellSize - 0.5f;
        float fz = (pos.z - FlowOrigin.z) * FlowInvCellSize - 0.5f;

        int x0 = math.clamp((int)math.floor(fx), 0, FlowWidth - 1);
        int z0 = math.clamp((int)math.floor(fz), 0, FlowHeight - 1);
        int x1 = math.min(x0 + 1, FlowWidth - 1);
        int z1 = math.min(z0 + 1, FlowHeight - 1);

        float3 d00 = DirAt(x0, z0);
        float3 d10 = DirAt(x1, z0);
        float3 d01 = DirAt(x0, z1);
        float3 d11 = DirAt(x1, z1);

        if (math.all(d00 == d10) && math.all(d00 == d01) && math.all(d00 == d11))
            return d00;

        float tx = math.saturate(fx - math.floor(fx));
        float tz = math.saturate(fz - math.floor(fz));

        float3 a = math.lerp(d00, d10, tx);
        float3 b = math.lerp(d01, d11, tx);
        float3 result = math.lerp(a, b, tz);

        float lenSq = math.lengthsq(result);
        if (lenSq <= 0.0001f) return FlowDirections[index];

        result = result * math.rsqrt(lenSq);

        // Pull intermediate directions toward the dominant cardinal axis.
        float suppress = math.saturate(DiagonalSuppressionFactor);
        if (suppress < 0.999f)
        {
            float ax = math.abs(result.x);
            float az = math.abs(result.z);

            if (ax > az)
                result.z *= suppress;
            else if (az > ax)
                result.x *= suppress;

            lenSq = math.lengthsq(result);
            if (lenSq > 0.0001f)
                result = result * math.rsqrt(lenSq);
        }

        return result;
    }

    private float3 DirAt(int x, int z)
    {
        int i = z * FlowWidth + x;
        return Walkable[i] != 0 ? FlowDirections[i] : float3.zero;
    }

    private bool TryDirectDirection(float3 pos, out float3 direction)
    {
        direction = float3.zero;

        if (!IsWalkable(pos)) return false;

        int cellX = (int)math.floor((pos.x - FlowOrigin.x) * FlowInvCellSize);
        int cellZ = (int)math.floor((pos.z - FlowOrigin.z) * FlowInvCellSize);
        if (cellX < 0 || cellX >= FlowWidth || cellZ < 0 || cellZ >= FlowHeight)
            return false;

        int cellDist = Distances[cellZ * FlowWidth + cellX];
        if (cellDist == int.MaxValue) return false;

        float3 toTarget = FlowTargetPosition - pos;
        toTarget.y = 0f;
        float sqr = math.lengthsq(toTarget);
        if (sqr < 0.0001f) return false;

        float dist = math.sqrt(sqr);
        direction = toTarget / dist;

        // In an open field the grid-path cost equals the straight-line distance
        // scaled to cost units: dist / cellSize * 100. Any detour (a wall, a
        // corner, an obstacle cluster) inflates cellDist beyond that estimate.
        // If cellDist is significantly larger, the path is NOT direct — fall
        // back to the flow field.
        float estimatedCost = dist * FlowInvCellSize * 100f;
        if (cellDist > estimatedCost * 1.15f + 200f) return false;

        return IsLineWalkable(pos, FlowTargetPosition);
    }

    private bool IsLineWalkable(float3 from, float3 to)
    {
        if (!IsWalkable(from)) return false;

        float dx = to.x - from.x;
        float dz = to.z - from.z;
        float dist = math.sqrt(dx * dx + dz * dz);
        if (dist < 0.001f) return true;

        float step = math.max(0.1f, FlowCellSize * 0.5f);
        int maxSamples = math.max(2, DirectLineSamples);
        float checkDistance = math.min(dist, step * maxSamples);
        int samples = math.max(1, (int)math.ceil(checkDistance / step));

        for (int i = 1; i <= samples; i++)
        {
            float d = math.min(i * step, dist);
            float t = d / dist;
            float3 p = new float3(from.x + dx * t, from.y, from.z + dz * t);
            if (!IsWalkable(p)) return false;
            if (d >= checkDistance) break;
        }

        return true;
    }

    private bool IsWalkable(float3 p)
    {
        if (FlowReady == 0) return false;
        int x = (int)math.floor((p.x - FlowOrigin.x) * FlowInvCellSize);
        int z = (int)math.floor((p.z - FlowOrigin.z) * FlowInvCellSize);
        if (x < 0 || x >= FlowWidth || z < 0 || z >= FlowHeight) return false;
        return Walkable[z * FlowWidth + x] != 0;
    }

    private float3 ComputeSeparation(int self, float3 pos)
    {
        float3 push = float3.zero;

        int cx = (int)math.floor((pos.x - SpatialOrigin.x) / SpatialCellSize);
        int cz = (int)math.floor((pos.z - SpatialOrigin.z) / SpatialCellSize);
        int range = (int)math.ceil(NeighbourRadius / SpatialCellSize);

        float radiusSqr = NeighbourRadius * NeighbourRadius;
        int considered = 0;

        for (int dz = -range; dz <= range; dz++)
        {
            int z = cz + dz;
            if (z < 0 || z >= SpatialHeight) continue;

            for (int dx = -range; dx <= range; dx++)
            {
                int x = cx + dx;
                if (x < 0 || x >= SpatialWidth) continue;

                int cell = z * SpatialWidth + x;
                int start = CellStart[cell];
                int end = CellStart[cell + 1];

                for (int k = start; k < end; k++)
                {
                    int other = SortedIndices[k];
                    if (other == self) continue;

                    float3 offset = pos - Positions[other];
                    offset.y = 0f;
                    float sqr = math.lengthsq(offset);
                    if (sqr < 0.0001f || sqr >= radiusSqr) continue;

                    float distance = math.sqrt(sqr);
                    float weight = math.saturate((NeighbourRadius - distance) / NeighbourRadius);

                    if (distance < MinimumSpacing)
                        weight += (MinimumSpacing - distance) / math.max(0.01f, MinimumSpacing);

                    push += offset / distance * weight;
                    considered++;

                    if (considered >= MaxNeighbours)
                        return ClampMag(push, 2f);
                }
            }
        }

        return ClampMag(push, 2f);
    }

    private static float3 ClampMag(float3 v, float max)
    {
        float sqr = math.lengthsq(v);
        float maxSqr = max * max;
        if (sqr > maxSqr && sqr > 0f)
            return v * (max * math.rsqrt(sqr));
        return v;
    }
}

[BurstCompile]
public struct MoveJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<EnemyStaticData> Statics;
    [ReadOnly] public NativeArray<float3> Directions;
    [ReadOnly] public NativeArray<quaternion> TargetRotations;

    [NativeDisableParallelForRestriction] public NativeArray<float3> Positions;

    // Flow field snapshot (walkability only)
    [ReadOnly] public NativeArray<byte> Walkable;
    public float3 FlowOrigin;
    public float FlowInvCellSize;
    public int FlowWidth;
    public int FlowHeight;
    public byte FlowReady;

    // Spatial grid snapshot
    [ReadOnly] public NativeArray<int> CellStart;
    [ReadOnly] public NativeArray<int> SortedIndices;
    public float3 SpatialOrigin;
    public float SpatialCellSize;
    public int SpatialWidth;
    public int SpatialHeight;

    public float DeltaTime;
    public float MinimumSpacing;
    public byte EnableOverlapResolution;
    public byte UseRotationAlignmentCheck;

    public void Execute(int i, TransformAccess transform)
    {
        if (Statics[i].Valid == 0) return;

        float3 direction = Directions[i];
        if (math.lengthsq(direction) < 0.0001f) return;

        float3 position = Positions[i];
        float speed = Statics[i].MoveSpeed;

        float3 proposed = position + direction * speed * DeltaTime;

        if (EnableOverlapResolution == 1)
            proposed = ResolveOverlaps(i, proposed);

        if (FlowReady == 1 && !IsWalkable(proposed))
        {
            float3 fallback = position + direction * speed * DeltaTime;
            if (IsWalkable(fallback)) proposed = fallback;
            else proposed = position;
        }

        Positions[i] = proposed;
        transform.position = new Vector3(proposed.x, proposed.y, proposed.z);

        quaternion targetRot = TargetRotations[i];
        quaternion currentRot = transform.rotation;

        if (UseRotationAlignmentCheck == 1)
        {
            float dot = math.dot(currentRot.value, targetRot.value);
            if (math.abs(dot) >= 0.9995f) return;
        }

        float t = math.saturate(Statics[i].TurnSpeed * DeltaTime);
        transform.rotation = math.slerp(currentRot, targetRot, t);
    }

    private float3 ResolveOverlaps(int self, float3 candidate)
    {
        float minDistSqr = MinimumSpacing * MinimumSpacing;

        int cx = (int)math.floor((candidate.x - SpatialOrigin.x) / SpatialCellSize);
        int cz = (int)math.floor((candidate.z - SpatialOrigin.z) / SpatialCellSize);
        int range = math.max(1, (int)math.ceil(MinimumSpacing / SpatialCellSize));

        for (int dz = -range; dz <= range; dz++)
        {
            int z = cz + dz;
            if (z < 0 || z >= SpatialHeight) continue;
            for (int dx = -range; dx <= range; dx++)
            {
                int x = cx + dx;
                if (x < 0 || x >= SpatialWidth) continue;
                int cell = z * SpatialWidth + x;
                int start = CellStart[cell];
                int end = CellStart[cell + 1];

                for (int k = start; k < end; k++)
                {
                    int other = SortedIndices[k];
                    if (other == self) continue;

                    float3 offset = candidate - Positions[other];
                    offset.y = 0f;
                    float sqr = math.lengthsq(offset);
                    if (sqr >= minDistSqr) continue;

                    float3 away;
                    if (sqr < 0.0001f)
                    {
                        float angle = (self * 0.0174533f) % (math.PI * 2f);
                        away = new float3(math.cos(angle), 0f, math.sin(angle));
                    }
                    else
                    {
                        away = offset * math.rsqrt(sqr);
                    }

                    float dist = math.sqrt(math.max(sqr, 0f));
                    candidate += away * (MinimumSpacing - dist);
                }
            }
        }

        return candidate;
    }

    private bool IsWalkable(float3 p)
    {
        if (FlowReady == 0) return false;
        int x = (int)math.floor((p.x - FlowOrigin.x) * FlowInvCellSize);
        int z = (int)math.floor((p.z - FlowOrigin.z) * FlowInvCellSize);
        if (x < 0 || x >= FlowWidth || z < 0 || z >= FlowHeight) return false;
        return Walkable[z * FlowWidth + x] != 0;
    }
}

public class EnemyManager : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool movementEnabled = true;
    [SerializeField, Min(0.1f)] private float neighbourRadius = 1.8f;
    [SerializeField, Min(0.1f)] private float minimumEnemySpacing = 0.62f;
    [SerializeField, Min(0f)] private float separationStrength = 1.4f;
    [SerializeField, Min(1)] private int maxNeighbours = 12;
    [SerializeField, Min(0f)] private float wanderStrength = 0.035f;

    [Header("Diagonal Convergence Control")]
    [Tooltip("1 = no bias (smooth, but drifts onto diagonal lines). 0 = fully cardinal. 0.5-0.7 recommended.")]
    [SerializeField, Range(0f, 1f)] private float diagonalSuppressionFactor = 0.6f;

    [Header("Immediate Target Steering")]
    [SerializeField, Range(0f, 1f)] private float directTargetSteeringWeight = 1f;
    [SerializeField, Min(4)] private int directLineSamples = 24;

    [Header("Performance")]
    [SerializeField] private bool enableOverlapResolution = true;
    [SerializeField] private bool useRotationAlignmentCheck = true;
    [SerializeField, Min(1)] private int innerLoopBatchCount = 64;

    private TransformAccessArray transformArray;
    private NativeArray<EnemyStaticData> statics;
    private NativeArray<float3> positions;
    private NativeArray<float3> directions;
    private NativeArray<quaternion> targetRotations;

    private int cachedCount = -1;
    private bool setupDirty;
    private JobHandle frameHandle;
    private bool hasOutstandingJob;

    private FlowFieldGrid flowField;
    private EnemySpatialGrid spatialGrid;

    private void OnDisable()
    {
        if (hasOutstandingJob)
        {
            frameHandle.Complete();
            hasOutstandingJob = false;
        }
    }

    private void OnDestroy()
    {
        OnDisable();
        DisposeArrays();
    }

    private void Update()
    {
        double startTime = Time.realtimeSinceStartupAsDouble;

        if (!movementEnabled)
        {
            EnemyManagerPerformance.LastEnemyCount = FlowFieldEnemy.ActiveEnemies.Count;
            EnemyManagerPerformance.LastUpdateMs =
                (Time.realtimeSinceStartupAsDouble - startTime) * 1000.0;
            return;
        }

        if (hasOutstandingJob)
        {
            frameHandle.Complete();
            hasOutstandingJob = false;
        }

        if (!EnsureSetup())
        {
            EnemyManagerPerformance.LastEnemyCount = FlowFieldEnemy.ActiveEnemies.Count;
            EnemyManagerPerformance.LastUpdateMs =
                (Time.realtimeSinceStartupAsDouble - startTime) * 1000.0;
            return;
        }

        // The flow field publishes its native arrays only after its first
        // rebuild completes. Until then it holds unconstructed NativeArrays
        // and we must not schedule any job that references them.
        flowField.PublishNativeNow();

        if (!flowField.HasNativeData)
        {
            // Flow field is still sampling obstacles or building the first
            // field. There is nothing valid to move toward yet.
            EnemyManagerPerformance.LastEnemyCount = cachedCount;
            EnemyManagerPerformance.LastUpdateMs =
                (Time.realtimeSinceStartupAsDouble - startTime) * 1000.0;
            return;
        }

        FlowFieldSnapshot flowSnapshot = flowField.CreateSnapshot(directTargetSteeringWeight > 0f);

        // EnemyManager is the authoritative source for the direct-steering
        // sample count. FlowFieldGrid keeps its own default only as a fallback
        // for callers that read the snapshot without going through here.
        flowSnapshot.DirectLineSamples = Mathf.Max(4, directLineSamples);

        // Spatial grid must fit inside the flow field bounds.
        Vector3 fo = flowField.WorldOrigin;
        Vector2 fs = flowField.WorldSize;
        float bucket = spatialGrid.BucketSize;
        int sw = Mathf.CeilToInt(fs.x / bucket);
        int sh = Mathf.CeilToInt(fs.y / bucket);
        spatialGrid.EnsureAllocated(new float3(fo.x, 0f, fo.z), sw, sh);
        spatialGrid.EnsureSortedCapacity(cachedCount);

        float deltaTime = Time.deltaTime;
        float time = Time.time;

        var rebuildHandle = spatialGrid.ScheduleRebuild(
            positions, statics, cachedCount, frameHandle);

        var steeringJob = new SteeringJob
        {
            Positions = positions,
            Statics = statics,
            Directions = directions,
            TargetRotations = targetRotations,

            Walkable = flowSnapshot.Walkable,
            FlowDirections = flowSnapshot.Directions,
            Distances = flowSnapshot.Distances,
            FlowOrigin = flowSnapshot.Origin,
            FlowCellSize = flowSnapshot.CellSize,
            FlowInvCellSize = flowSnapshot.InvCellSize,
            FlowWidth = flowSnapshot.Width,
            FlowHeight = flowSnapshot.Height,
            FlowReady = flowSnapshot.Ready,
            FlowTargetPosition = flowSnapshot.TargetPosition,
            DirectSteeringEnabled = flowSnapshot.DirectSteeringEnabled,
            DirectLineSamples = flowSnapshot.DirectLineSamples,

            CellStart = spatialGrid.CellStart,
            SortedIndices = spatialGrid.SortedIndices,
            SpatialOrigin = spatialGrid.Origin,
            SpatialCellSize = spatialGrid.BucketSize,
            SpatialWidth = spatialGrid.Width,
            SpatialHeight = spatialGrid.Height,

            NeighbourRadius = neighbourRadius,
            MinimumSpacing = minimumEnemySpacing,
            SeparationStrength = separationStrength,
            MaxNeighbours = maxNeighbours,
            WanderStrength = wanderStrength,
            DirectSteeringWeight = directTargetSteeringWeight,
            Time = time,
            DiagonalSuppressionFactor = diagonalSuppressionFactor
        };

        var steeringHandle = steeringJob.Schedule(cachedCount, innerLoopBatchCount, rebuildHandle);

        var moveJob = new MoveJob
        {
            Statics = statics,
            Directions = directions,
            TargetRotations = targetRotations,
            Positions = positions,

            Walkable = flowSnapshot.Walkable,
            FlowOrigin = flowSnapshot.Origin,
            FlowInvCellSize = flowSnapshot.InvCellSize,
            FlowWidth = flowSnapshot.Width,
            FlowHeight = flowSnapshot.Height,
            FlowReady = flowSnapshot.Ready,

            CellStart = spatialGrid.CellStart,
            SortedIndices = spatialGrid.SortedIndices,
            SpatialOrigin = spatialGrid.Origin,
            SpatialCellSize = spatialGrid.BucketSize,
            SpatialWidth = spatialGrid.Width,
            SpatialHeight = spatialGrid.Height,

            DeltaTime = deltaTime,
            MinimumSpacing = minimumEnemySpacing,
            EnableOverlapResolution = (byte)(enableOverlapResolution ? 1 : 0),
            UseRotationAlignmentCheck = (byte)(useRotationAlignmentCheck ? 1 : 0)
        };

        frameHandle = moveJob.Schedule(transformArray, steeringHandle);
        hasOutstandingJob = true;

        EnemyManagerPerformance.LastEnemyCount = cachedCount;
        EnemyManagerPerformance.LastUpdateMs =
            (Time.realtimeSinceStartupAsDouble - startTime) * 1000.0;
    }

    private void LateUpdate()
    {
        if (hasOutstandingJob)
        {
            frameHandle.Complete();
            hasOutstandingJob = false;
        }
    }

    private bool EnsureSetup()
    {
        int liveCount = FlowFieldEnemy.ActiveEnemies.Count;

        if (liveCount == 0)
        {
            if (cachedCount != 0) { DisposeArrays(); cachedCount = 0; }
            return false;
        }

        if (!setupDirty && liveCount == cachedCount)
            return true;

        // Resolve flow field.
        if (flowField == null)
        {
            for (int i = 0; i < liveCount; i++)
            {
                var e = FlowFieldEnemy.ActiveEnemies[i];
                if (e != null && e.FlowField != null) { flowField = e.FlowField; break; }
            }
        }

        if (flowField == null) return false;

        spatialGrid = EnemySpatialGrid.Instance;

        DisposeArrays();

        cachedCount = liveCount;
        statics = new NativeArray<EnemyStaticData>(liveCount, Allocator.Persistent);
        positions = new NativeArray<float3>(liveCount, Allocator.Persistent);
        directions = new NativeArray<float3>(liveCount, Allocator.Persistent);
        targetRotations = new NativeArray<quaternion>(liveCount, Allocator.Persistent);
        transformArray = new TransformAccessArray(liveCount);

        for (int i = 0; i < liveCount; i++)
        {
            var enemy = FlowFieldEnemy.ActiveEnemies[i];
            if (enemy == null)
            {
                statics[i] = new EnemyStaticData { Valid = 0 };
                positions[i] = float3.zero;
                directions[i] = float3.zero;
                targetRotations[i] = quaternion.identity;
                transformArray.Add(null);
                continue;
            }

            Vector3 p = enemy.CachedTransform.position;
            statics[i] = new EnemyStaticData
            {
                MoveSpeed = enemy.MoveSpeed,
                TurnSpeed = enemy.TurnSpeed,
                SteeringPhase = enemy.SteeringPhase,
                Valid = 1
            };
            positions[i] = new float3(p.x, p.y, p.z);
            directions[i] = float3.zero;
            targetRotations[i] = quaternion.identity;
            transformArray.Add(enemy.CachedTransform);
        }

        setupDirty = false;
        return true;
    }

    private void DisposeArrays()
    {
        if (hasOutstandingJob)
        {
            frameHandle.Complete();
            hasOutstandingJob = false;
        }

        if (transformArray.isCreated) transformArray.Dispose();
        if (statics.IsCreated) statics.Dispose();
        if (positions.IsCreated) positions.Dispose();
        if (directions.IsCreated) directions.Dispose();
        if (targetRotations.IsCreated) targetRotations.Dispose();
        cachedCount = -1;
    }
}

public static class EnemyManagerPerformance
{
    public static double LastUpdateMs { get; set; }
    public static int LastEnemyCount { get; set; }
}