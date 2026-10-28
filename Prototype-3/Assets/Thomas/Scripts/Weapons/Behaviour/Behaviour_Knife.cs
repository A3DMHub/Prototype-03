using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Behaviour_Knife : ProjectileWeaponBehaviour
{

    protected override void Start()
    {
        base.Start();
    }

    void Update()
    {
        transform.position += direction * weaponData.speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider  other)
    {   
        if (other.CompareTag("Enemy"))
        {
            int pierceCount = weaponData.pierce;

            if (pierceCount > 0)
            {
                pierceCount--;
                Destroy(other.gameObject);  // todo hp damage instead
            }
            else
            {
                Destroy(other.gameObject);  // todo hp damage instead
                Destroy(gameObject);
            }
        }
    }
}
