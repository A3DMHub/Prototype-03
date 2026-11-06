using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Area")]
    [Tooltip("Enemies spawn randomly inside this radius around the point.")]
    [SerializeField, Min(0f)] private float spawnRadius = 1f;

    [Header("Weighting")]
    [Tooltip("Higher = more enemies spawn here. 0 = never chosen.")]
    [SerializeField, Min(0f)] private float weight = 1f;

    [Header("Lifetime Cap (optional)")]
    [Tooltip("-1 = unlimited. Otherwise this point stops spawning after N lifetime spawns.")]
    [SerializeField] private int maxSpawnsFromThisPoint = -1;

    [Header("Flow Field Override (optional)")]
    [Tooltip("Leave empty to use the spawner's default flow field.")]
    [SerializeField] private FlowFieldGrid overrideFlowField;

    [HideInInspector] public int SpawnedCount;

    public float SpawnRadius => spawnRadius;
    public float Weight => weight;
    public int MaxSpawnsFromThisPoint => maxSpawnsFromThisPoint;
    public FlowFieldGrid OverrideFlowField => overrideFlowField;
    public Vector3 Position => transform.position;

    public bool CanSpawn =>
        maxSpawnsFromThisPoint < 0 || SpawnedCount < maxSpawnsFromThisPoint;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.15f);
        Gizmos.DrawSphere(transform.position, spawnRadius);
    }
}