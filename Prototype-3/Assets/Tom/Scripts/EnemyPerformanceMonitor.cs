using UnityEngine;

public class EnemyPerformanceMonitor : MonoBehaviour
{
    private float smoothedFps;
    private float smoothedUpdateMs;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt > 0f)
        {
            float fps = 1f / dt;
            smoothedFps = smoothedFps <= 0f ? fps : Mathf.Lerp(smoothedFps, fps, 0.05f);
        }

        smoothedUpdateMs = Mathf.Lerp(
            smoothedUpdateMs,
            (float)EnemyManagerPerformance.LastUpdateMs,
            0.1f);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 640, 24),
            $"Active enemies: {EnemyManagerPerformance.LastEnemyCount} " +
            $"(list {FlowFieldEnemy.ActiveEnemies.Count})");

        GUI.Label(new Rect(10, 34, 640, 24),
            $"Manager schedule: {EnemyManagerPerformance.LastUpdateMs:F2} ms");

        GUI.Label(new Rect(10, 58, 640, 24),
            $"Smoothed schedule: {smoothedUpdateMs:F2} ms");

        GUI.Label(new Rect(10, 82, 640, 24),
            $"Smoothed FPS: {smoothedFps:F1}");
    }
}