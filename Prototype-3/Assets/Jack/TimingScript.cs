using UnityEngine;

public class TimingScript : MonoBehaviour
{
    private float TimeRemaining = 1;

    //public float TimeAdd = 50;
    
    public float TimeDecreaseSpeed = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WindUp(99);
    }


    // Update is called once per frame
    void Update()
    {
        TimeRemaining -= TimeDecreaseSpeed*Time.deltaTime;
        TimeRemaining = Mathf.Clamp(TimeRemaining, 0, 100);
        Debug.Log(TimeRemaining);

        if (TimeRemaining <= 0)
        {
            GameOver();
        }
    }

    public void WindUp(float WindUpAmount)
    {
        TimeRemaining += WindUpAmount;
    }

    public void PaySomeTime(float cost)
    {
        TimeRemaining -= cost;
    }

    void GameOver()
    {
        //Game over script TODO

    }
}
