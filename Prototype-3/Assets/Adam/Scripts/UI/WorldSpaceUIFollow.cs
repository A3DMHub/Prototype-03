using UnityEngine;

public class WorldSpaceUIFollow : MonoBehaviour
{
    public Transform targetPlayer;
    public Vector3 offset = new Vector3(0f, -0.9f, -1.2f);

    void LateUpdate()
    {
        if (targetPlayer != null)
        {
            transform.position = targetPlayer.position + offset;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}