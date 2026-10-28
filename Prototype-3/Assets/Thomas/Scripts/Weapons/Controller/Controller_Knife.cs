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
        GameObject spawnedKnife = Instantiate(weaponData.prefab, transform.position, transform.rotation);
        spawnedKnife.GetComponent<Behaviour_Knife>().DirectionCheck(transform.forward); // replace with player movement dir later
    }

}
