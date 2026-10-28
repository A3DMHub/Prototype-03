using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private bool movementEnabled = true;

    private readonly List<FlowFieldEnemy> enemiesToRemove =
        new List<FlowFieldEnemy>();

    private void Update()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (!movementEnabled)
            return;

        List<FlowFieldEnemy> enemies =
            FlowFieldEnemy.ActiveEnemies;

        float deltaTime = Time.deltaTime;

        for (int i = 0; i < enemies.Count; i++)
        {
            FlowFieldEnemy enemy = enemies[i];

            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            FlowFieldGrid grid = enemy.FlowField;

            if (grid == null)
                continue;

            Transform enemyTransform = enemy.CachedTransform;
            Vector3 position = enemyTransform.position;

            Vector3 direction = grid.GetDirection(position);
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                continue;

            direction.Normalize();

            // Smooth abrupt changes in the flow-field direction.
            float phase = enemy.SteeringPhase;

            Vector3 lateral = new Vector3(
                -direction.z,
                0f,
                direction.x);

            float gentleWander =
                Mathf.Sin(Time.time * 1.5f + phase) * 0.12f;

            direction = (direction + lateral * gentleWander).normalized;

            Vector3 nextPosition =
                position + direction * enemy.MoveSpeed * deltaTime;

            if (grid.IsWalkable(nextPosition))
                enemyTransform.position = nextPosition;

            Quaternion targetRotation =
                Quaternion.LookRotation(direction, Vector3.up);

            enemyTransform.rotation = Quaternion.Slerp(
                enemyTransform.rotation,
                targetRotation,
                enemy.TurnSpeed * deltaTime);
        }

        for (int i = 0; i < enemiesToRemove.Count; i++)
        {
            FlowFieldEnemy enemy = enemiesToRemove[i];

            if (enemy != null)
                FlowFieldEnemy.ActiveEnemies.Remove(enemy);
        }

        enemiesToRemove.Clear();
        stopwatch.Stop();
        EnemyManagerPerformance.LastUpdateMs = stopwatch.Elapsed.TotalMilliseconds;
    }
}
