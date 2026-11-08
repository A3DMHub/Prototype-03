using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_Sword : MeleeWeaponController
{

    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        base.Attack();
        if (weaponData.attackCount > 1)
        {
            for (int i = 0; i < weaponData.attackCount; i++)
            {
                float angle = 360f * i / weaponData.attackCount;

                GameObject spawnedSword = Instantiate(weaponData.prefab, transform.position, transform.rotation);
                spawnedSword.transform.parent = transform;
                spawnedSword.transform.localPosition = transform.localPosition;

                Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;


                spawnedSword.GetComponent<Behaviour_Sword>().DirectionCheck(direction);
            }
        }
        else
        {
            GameObject spawnedSword = Instantiate(weaponData.prefab, transform.position, transform.rotation);
            spawnedSword.transform.parent = transform;
            spawnedSword.transform.localPosition = transform.localPosition;

            Vector3 direction = weaponData.followPlayerDir
                ? transform.forward
                : Quaternion.Euler(0f, weaponData.attackDir, 0f) * Vector3.forward;

            spawnedSword.GetComponent<Behaviour_Sword>().DirectionCheck(direction);
        }
        
    }

}
