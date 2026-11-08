using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponBehaviour : MonoBehaviour
{
    [Header("Weapon Stats")]
    public MeleeWeaponScriptableObject weaponData;
    
    protected Vector3 direction;
    protected virtual void Start()
    {
        if (weaponData.cooldown < weaponData.destroyTime)
        {
            weaponData.destroyTime = weaponData.cooldown;
        }
        
        Destroy(gameObject, weaponData.destroyTime);
    }

    public void DirectionCheck(Vector3 dir)
    {
        direction = dir;

        Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            transform.localPosition = Vector3.forward * 1.5f;
            return;
        }

        Vector3 localDirection = transform.parent != null
            ? transform.parent.InverseTransformDirection(flatDirection)
            : flatDirection;
        transform.localPosition = localDirection.normalized * 1.5f;

        float yaw = Vector3.SignedAngle(Vector3.left, flatDirection.normalized, Vector3.up);
        Quaternion flatSpriteRotation = Quaternion.Euler(-90f, 90f, 0f);
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * flatSpriteRotation;
    }
}
