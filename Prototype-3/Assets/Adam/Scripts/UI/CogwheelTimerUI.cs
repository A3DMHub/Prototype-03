using UnityEngine;

public class CogwheelTimerUI : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = -60f; // Negative for clockwise spin

    void Update()
    {
        // Spins only this transform in place around its local Z axis
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}