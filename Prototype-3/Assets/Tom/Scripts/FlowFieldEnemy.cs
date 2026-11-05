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

    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Resources")]
    public GameObject resourceDropPrefab;

    private Transform cachedTransform;
    private float individualSpeed;
    private float individualTurnSpeed;
    private float steeringPhase;
    private bool registered;

    public FlowFieldGrid FlowField => flowField;
    public Transform CachedTransform => cachedTransform;
    public float MoveSpeed => individualSpeed;
    public float TurnSpeed => individualTurnSpeed;
    public float SteeringPhase => steeringPhase;

    public void SetFlowField(FlowFieldGrid grid) => flowField = grid;

    private void Awake()
    {
        currentHealth = maxHealth;
        cachedTransform = transform;
        individualSpeed = moveSpeed * Random.Range(0.92f, 1.08f);
        individualTurnSpeed = turnSpeed * Random.Range(0.85f, 1.15f);
        steeringPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        if (registered) return;
        ActiveEnemies.Add(this);
        registered = true;
    }

    private void OnDisable()
    {
        if (!registered) return;

        ActiveEnemies.Remove(this);
        registered = false;
    }

    public void TakeDamage(int damage)
    {
        //Can be updated later to include and resistances
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            KillEnemy();
        }
    }

    private void KillEnemy()
    {
        if (resourceDropPrefab != null)
        {
            Instantiate(resourceDropPrefab, cachedTransform.position, Quaternion.identity);   
        }
        Destroy(gameObject);
    }
}