using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    public MeleeWeaponScriptableObject weaponData;
    
    public GameObject player;

    protected virtual void Start()
    {
        weaponData.upgradeTier = 0;
        weaponData.currentCooldown = 0f; // attack immediately
        if(weaponData.cooldown <= 0f)
        {
            weaponData.cooldown = 0.1f;
        }
    }

    protected virtual void Update()
    {
        if (Time.timeScale == 0f) return;
        
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
