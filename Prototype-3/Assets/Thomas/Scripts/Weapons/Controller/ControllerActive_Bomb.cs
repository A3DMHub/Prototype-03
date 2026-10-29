using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerActive_Bomb : ActiveWeaponController
{
    protected override void Start()
    {
        base.Start();
    }

    public override void OnActiveItem1(InputValue inputValue)
    {
        base.OnActiveItem1(inputValue);
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
                    : 360f * i / weaponData.projectileCount; // if not following player dir, just spread evenly around the player
                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

                GameObject spawnedKnife = Instantiate(weaponData.prefab, transform.position, transform.rotation);
                spawnedKnife.GetComponent<Behaviour_Knife>().DirectionCheck(direction);
            }
        }
        else
        {
            GameObject spawnedKnife = Instantiate(weaponData.prefab, transform.position, transform.rotation);
            Vector3 direction = weaponData.followPlayerDir
                ? transform.forward
                : Quaternion.Euler(0f, weaponData.attackDir, 0f) * Vector3.forward;
            spawnedKnife.GetComponent<Behaviour_Knife>().DirectionCheck(direction);
        }
    }

}
