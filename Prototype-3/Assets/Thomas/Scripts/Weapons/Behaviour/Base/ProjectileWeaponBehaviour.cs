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

        // rotate weapon for dirs
        float dirx = direction.x;
        float diry = direction.y;

        Vector3 rotation = transform.rotation.eulerAngles;

        if(dirx < 0 && diry == 0) // left
        {
            rotation.z = -90;
        }
        else if(dirx > 0 && diry == 0) // right
        {
            rotation.z = 90;
        }
        else if(dirx == 0 && diry > 0) // up
        {
            rotation.z = -180;
        }
        else if(dirx == 0 && diry < 0) // down
        {
            rotation.z = -180;
        }

        transform.rotation = Quaternion.Euler(rotation);
    }
}
