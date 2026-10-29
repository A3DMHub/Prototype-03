using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private float TimeRemaining = 100;
    private float TimeAdd = 50;
    public float TimeDecreaseSpeed = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TimeRemaining -= TimeDecreaseSpeed*Time.deltaTime;
    }

    void WindUp()
    {
        TimeRemaining += TimeAdd;
    }
}
