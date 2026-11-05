using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonScript : MonoBehaviour, IPointerDownHandler
{
    public float NumSpendOrWind;
    public TimingScript Timing;
    public bool IsWindUp;
    public bool IsSpend;
    public bool IsWalk;
    public bool IsRun;
    public bool IsIdle;
    public bool IsJump;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("ruber bas");
        if (IsSpend)
        {
            Timing.PaySomeTime(NumSpendOrWind);
        }
        if (IsWalk)
        {
            Timing.TimeDecreaseSpeed = 2;
        }
        if (IsIdle)
        {
            Timing.TimeDecreaseSpeed = 1;
        }
        if (IsWindUp)
        {
            Timing.WindUp(NumSpendOrWind);
        }
        if (IsRun)
        {
            Timing.TimeDecreaseSpeed = 3;
        }
        if (IsJump)
        {
            Timing.PaySomeTime(NumSpendOrWind);
        }
    }
}
