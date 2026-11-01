using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private float TimeRemaining;

    //public float TimeAdd = 50;
    
    public float TimeDecreaseSpeed = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WindUp(100);
    }


    // Update is called once per frame
    void Update()
    {
        TimeRemaining -= TimeDecreaseSpeed*Time.deltaTime;
        Mathf.Clamp(TimeRemaining, 0, 100);
        Debug.Log(TimeRemaining);
    }

    void WindUp(int WindUpAmount)
    {
        TimeRemaining += WindUpAmount;
    }

    void PaySomeTime(float cost)
    {
        TimeRemaining -= cost;
    }
}
