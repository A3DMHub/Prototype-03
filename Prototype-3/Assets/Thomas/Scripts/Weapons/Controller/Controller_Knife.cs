using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_Knife : WeaponController
{
    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        base.Attack();
        if (weaponData.projectileCount > 1)
        {
            for (int i = 0; i < weaponData.projectileCount; i++)
            {
                float angle = weaponData.followPlayerDir
                    ? (weaponData.projectileSpread / 2f) - (weaponData.projectileSpread * i / (weaponData.projectileCount - 1))
                    : 360f * i / weaponData.projectileCount;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

                GameObject spawnedKnife = Instantiate(weaponData.prefab, transform.position, transform.rotation);
                spawnedKnife.GetComponent<Behaviour_Knife>().DirectionCheck(direction);

                Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                float yaw = Vector3.SignedAngle(Vector3.left, flatDirection, Vector3.up);
                Quaternion flatSpriteRotation = Quaternion.Euler(-90f, 90f, 0f);
                spawnedKnife.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * flatSpriteRotation;
            }
        }
        else
        {
            GameObject spawnedKnife = Instantiate(weaponData.prefab, transform.position, transform.rotation);
            Vector3 direction = weaponData.followPlayerDir
                ? transform.forward
                : Quaternion.Euler(0f, 0f, -weaponData.attackDir) * Vector3.up;
            spawnedKnife.GetComponent<Behaviour_Knife>().DirectionCheck(direction);
        }
    }

}
