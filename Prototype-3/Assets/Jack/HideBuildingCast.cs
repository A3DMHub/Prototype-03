using UnityEngine;

public class HideBuildingCast : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public LayerMask obstructionMask;

    private FadeObstacle currentObby;


    private void LateUpdate()
    {
        Vector3 dir = transform.position - cameraTransform.position;

        if (Physics.Raycast(cameraTransform.position, dir.normalized, out RaycastHit hit, dir.magnitude, obstructionMask))
        {
            FadeObstacle fade = hit.collider.GetComponent<FadeObstacle>();

            if (fade != null && fade != currentObby)
            {
                if (currentObby != null)
                    currentObby.FadeIn();

                fade.FadeOut();
                currentObby = fade;
            }
        }

        else
        {
            if (currentObby != null)
            {
                currentObby.FadeIn();
                currentObby = null;
            }
        }

    }
}
