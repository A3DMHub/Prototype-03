using UnityEngine;
using UnityEngine.AI;

public class RatPathScript : MonoBehaviour
{
    public GameObject Target;
    private NavMeshAgent RatAgent;
    private float Distance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RatAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        Distance = Vector3.Distance(RatAgent.transform.position, Target.transform.position);
        RatAgent.destination = Target.transform.position;
    }
}
