using System.Collections.Generic;
using UnityEngine;

public class EnemyAggroSource : MonoBehaviour
{
    public static readonly List<EnemyAggroSource> ActiveSources =
        new List<EnemyAggroSource>(16);

    [Header("Aggro")]
    [Tooltip("Enemies within this radius will target this source when they have line of sight.")]
    [SerializeField, Min(0.1f)] private float radius = 15f;

    [Tooltip("Higher priority overrides lower within overlapping ranges. Ties go to the closest.")]
    [SerializeField] private int priority = 0;

    public float Radius => radius;
    public float RadiusSqr => radius * radius;
    public int Priority => priority;
    public Vector3 Position => transform.position;

    private bool registered;

    private void OnEnable()
    {
        if (registered) return;
        ActiveSources.Add(this);
        registered = true;
    }

    private void OnDisable()
    {
        if (!registered) return;
        ActiveSources.Remove(this);
        registered = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}