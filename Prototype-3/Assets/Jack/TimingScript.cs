using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private float TimeRemaining;

    //public float TimeAdd = 50;
    
    public float TimeDecreaseSpeed = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TimeRemaining = 1;
        WindUp(99);
    }


    // Update is called once per frame
    void Update()
    {
        TimeRemaining -= TimeDecreaseSpeed*Time.deltaTime;
        Mathf.Clamp(TimeRemaining, 0, 100);
        Debug.Log(TimeRemaining);

        if (TimeRemaining <= 0)
        {
            GameOver();
        }
    }

    void WindUp(int WindUpAmount)
    {
        TimeRemaining += WindUpAmount;
    }

    void PaySomeTime(float cost)
    {
        TimeRemaining -= cost;
    }

    void GameOver()
    {
        //Game over script TODO
    }
}
