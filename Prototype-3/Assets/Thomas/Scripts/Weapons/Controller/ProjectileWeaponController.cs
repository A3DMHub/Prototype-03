using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileWeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    public ProjectileWeaponScriptableObject weaponData;

    protected virtual void Start()
    {
        weaponData.currentCooldown = 0f; // attack immediately 
    }

    protected virtual void Update()
    {
        weaponData.currentCooldown -= Time.deltaTime;
        if (weaponData.currentCooldown <= 0)
        {
            Attack();
        }
    }

    protected virtual void Attack()
    {
        weaponData.currentCooldown = weaponData.cooldown;
    }
}
