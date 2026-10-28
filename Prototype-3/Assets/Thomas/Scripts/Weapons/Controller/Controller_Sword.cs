using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller_Sword : WeaponController
{
    public GameObject player;

    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        base.Attack();
        
        GameObject spawnedSword = Instantiate(weaponData.prefab, transform.position, transform.rotation);
        spawnedSword.transform.parent = transform;
        spawnedSword.transform.localPosition = transform.localPosition;

        spawnedSword.GetComponent<Behaviour_Sword>().DirectionCheck(transform.forward); // replace with player movement dir later
    }

}
