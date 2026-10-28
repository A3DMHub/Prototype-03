using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponBehaviour : MonoBehaviour
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
        rotation.x = -90f;

        transform.localPosition = new Vector3(0, 0, 1.5f);

        // if(dirx < 0 && diry == 0) // left
        // {
        //     rotation.z = -90;
        //     transform.localPosition = new Vector3(1.5f, 0, 0);
        // }
        // else if(dirx > 0 && diry == 0) // right
        // {
        //     rotation.z = 90;
        //     transform.localPosition = new Vector3(-1.5f, 0, 0);
        // }
        // else if(dirx == 0 && diry > 0) // up
        // {
        //     rotation.z = -180;
        //     transform.localPosition = new Vector3(0, 0, 1.5f);
        // }
        // else if(dirx == 0 && diry < 0) // down
        // {
        //     rotation.z = -180;
        //     transform.localPosition = new Vector3(0, 0, -1.5f);
        // }

        transform.rotation = Quaternion.Euler(rotation);
    }
}
