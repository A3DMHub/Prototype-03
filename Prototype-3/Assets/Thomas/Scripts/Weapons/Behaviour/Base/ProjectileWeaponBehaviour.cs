using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileWeaponBehaviour : MonoBehaviour
{
    [Header("Weapon Stats")]
    public WeaponScriptableObject weaponData;
    
    protected Vector3 direction;
    protected virtual void Start()
    {
        Destroy(gameObject, weaponData.destroyTime);
    }

    public void DirectionCheck(Vector3 dir)
    {
        direction = dir;

        const float directionThreshold = 0.0001f;
        float dirx = direction.x;
        float diry = Mathf.Abs(direction.z) > directionThreshold && Mathf.Abs(direction.y) <= directionThreshold
            ? direction.z
            : direction.y;

        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.x = -90f;
        rotation.y = 0f;

        if (Mathf.Abs(dirx) > directionThreshold || Mathf.Abs(diry) > directionThreshold)
        {
            if (Mathf.Abs(dirx) >= Mathf.Abs(diry))
            {
                rotation.z = dirx < 0f ? -90f : 90f; // left or right
            }
            else
            {
                rotation.z = -180f; // up or down
            }
        }

        transform.rotation = Quaternion.Euler(rotation);
    }
}
