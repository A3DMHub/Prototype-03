
using System.Collections.Generic;
using UnityEngine;

public class EnemySpatialGrid : MonoBehaviour
{
    public static EnemySpatialGrid Instance { get; private set; }

    [Header("Spatial Grid")]
    [SerializeField, Min(0.25f)]
    private float bucketSize = 2f;

    private readonly Dictionary<Vector2Int, List<FlowFieldEnemy>> buckets =
        new Dictionary<Vector2Int, List<FlowFieldEnemy>>();

    private readonly List<List<FlowFieldEnemy>> allBuckets =
        new List<List<FlowFieldEnemy>>();

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
        if (Instance == this)
            Instance = null;
    }

    private Vector2Int GetBucket(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / bucketSize),
            Mathf.FloorToInt(position.z / bucketSize));
    }

    private List<FlowFieldEnemy> GetOrCreateBucket(Vector2Int key)
    {
        if (buckets.TryGetValue(key, out List<FlowFieldEnemy> bucket))
            return bucket;

        bucket = new List<FlowFieldEnemy>(8);
        buckets.Add(key, bucket);
        allBuckets.Add(bucket);
        return bucket;
    }

    private void LateUpdate()
    {
        // Reuse the existing lists; do not allocate new buckets each frame.
        for (int i = 0; i < allBuckets.Count; i++)
            allBuckets[i].Clear();

        IReadOnlyList<FlowFieldEnemy> enemies =
            FlowFieldEnemy.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            FlowFieldEnemy enemy = enemies[i];

            if (enemy == null || !enemy.isActiveAndEnabled)
                continue;

            Vector2Int key = GetBucket(enemy.transform.position);
            GetOrCreateBucket(key).Add(enemy);
        }
    }

    public Vector3 GetSeparation(
        FlowFieldEnemy requester,
        Vector3 position,
        float neighbourRadius,
        float personalSpace,
        int maxNeighbours)
    {
        Vector3 push = Vector3.zero;

        if (requester == null || bucketSize <= 0f)
            return push;

        Vector2Int center = GetBucket(position);

        int range = Mathf.CeilToInt(neighbourRadius / bucketSize);
        int processed = 0;

        for (int z = -range; z <= range; z++)
        {
            for (int x = -range; x <= range; x++)
            {
                Vector2Int key = new Vector2Int(
                    center.x + x,
                    center.y + z);

                if (!buckets.TryGetValue(
                    key, out List<FlowFieldEnemy> bucket))
                    continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    FlowFieldEnemy other = bucket[i];

                    if (other == null || other == requester)
                        continue;

                    Vector3 offset =
                        position - other.transform.position;

                    offset.y = 0f;

                    float distance = offset.magnitude;

                    if (distance < 0.001f ||
                        distance >= personalSpace)
                        continue;

                    float strength = 1f - distance / personalSpace;
                    push += offset / distance * strength;

                    processed++;

                    if (processed >= maxNeighbours)
                        return push;
                }
            }
        }

        return push;
    }
}
