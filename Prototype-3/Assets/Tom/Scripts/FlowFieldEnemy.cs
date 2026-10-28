
using System.Collections.Generic;
using UnityEngine;

public class FlowFieldEnemy : MonoBehaviour
{
    public static readonly List<FlowFieldEnemy> ActiveEnemies =
        new List<FlowFieldEnemy>(10000);

    [Header("References")]
    [SerializeField] private FlowFieldGrid flowField;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 8f;

    private Transform cachedTransform;

    private float individualSpeed;
    private float individualTurnSpeed;
    private float steeringPhase;

    public FlowFieldGrid FlowField => flowField;
    public Transform CachedTransform => cachedTransform;
    public float MoveSpeed => individualSpeed;
    public float TurnSpeed => individualTurnSpeed;
    public float SteeringPhase => steeringPhase;

    public void SetFlowField(FlowFieldGrid grid)
    {
        flowField = grid;
    }

    private void Awake()
    {
        cachedTransform = transform;

        individualSpeed = moveSpeed * Random.Range(0.90f, 1.10f);
        individualTurnSpeed = turnSpeed * Random.Range(0.85f, 1.15f);
        steeringPhase = Random.Range(0f, 6.2831853f);
    }

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
            ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }
}
