using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTrackPlayer : MonoBehaviour
{
    public GameObject player;
    private Camera mainCamera;
    void Start()
    {
        mainCamera = Camera.main;
    }

    void FixedUpdate()
    {
        if (mainCamera == null || mainCamera.transform == null) return;

        Vector3 cameraPosition = mainCamera.transform.position;
        cameraPosition.x = player.transform.position.x;
        cameraPosition.z = player.transform.position.z -7;
        mainCamera.transform.position = cameraPosition;
    }
}
