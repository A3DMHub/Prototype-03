using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Behaviour_Sword : MeleeWeaponBehaviour
{

    protected override void Start()
    {
        base.Start();
    }

    void OnTriggerEnter(Collider  other)
    {
        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject); // todo hp damage instead
        }
    }
}
