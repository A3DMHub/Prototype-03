
using System.Diagnostics;
using UnityEngine;

public class EnemyPerformanceMonitor : MonoBehaviour
{
    private void OnGUI()
    {
        GUI.Label(
            new Rect(10, 10, 500, 25),
            $"Active enemies: {FlowFieldEnemy.ActiveEnemies.Count}");

        GUI.Label(
            new Rect(10, 35, 500, 25),
            $"Manager movement: {EnemyManagerPerformance.LastUpdateMs:F2} ms");
    }
}

public static class EnemyManagerPerformance
{
    public static double LastUpdateMs { get; set; }
}
