// DO NOT USE IN FINAL IN CURRENT STATE

using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Flow Field")]
    [SerializeField] private FlowFieldGrid flowField;

    [Header("Spawn Grid")]
    [SerializeField, Min(1)] private int rows = 100;
    [SerializeField, Min(1)] private int columns = 100;
    [SerializeField, Min(0.1f)] private float spacing = 1f;

    [Header("Spawn Position")]
    [SerializeField] private Vector3 spawnOrigin = Vector3.zero;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemyGridSpawner: Enemy Prefab is not assigned.", this);
            return;
        }

        if (flowField == null)
        {
            Debug.LogError("EnemyGridSpawner: Flow Field is not assigned.", this);
            return;
        }

        int spawnedCount = 0;

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 spawnPosition = spawnOrigin + new Vector3(
                    x * spacing,
                    0f,
                    z * spacing
                );

                GameObject enemy = Instantiate(
                    enemyPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

                FlowFieldEnemy flowEnemy =
                    enemy.GetComponent<FlowFieldEnemy>();

                if (flowEnemy == null)
                {
                    Debug.LogError(
                        "Enemy prefab is missing FlowFieldEnemy.",
                        enemy
                    );

                    Destroy(enemy);
                    continue;
                }

                flowEnemy.SetFlowField(flowField);
                spawnedCount++;
            }
        }

        Debug.Log($"EnemySpawner: Spawned {spawnedCount} enemies.");
    }
}