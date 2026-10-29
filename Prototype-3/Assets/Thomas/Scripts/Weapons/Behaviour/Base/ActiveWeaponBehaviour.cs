using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveWeaponBehaviour : MonoBehaviour
{
    [Header("Weapon Stats")]
    public ActiveWeaponScriptableObject weaponData;
    
    protected Vector3 direction;
    protected virtual void Start()
    {
        StartCoroutine(WaitForDestroyTime());
    }

    private IEnumerator WaitForDestroyTime()
    {
        yield return new WaitForSeconds(weaponData.destroyTime);

        OnLifetimeExpired();
    }

    protected virtual void OnLifetimeExpired()
    {
        Destroy(gameObject);
    }

    public void DirectionCheck(Vector3 dir)
    {
        direction = dir;

        Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (flatDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float yaw = Vector3.SignedAngle(Vector3.left, flatDirection.normalized, Vector3.up);
        Quaternion flatSpriteRotation = Quaternion.Euler(-90f, 90f, 0f);
        transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * flatSpriteRotation;
    }
}
