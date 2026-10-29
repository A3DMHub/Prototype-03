using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ActiveWeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    public WeaponScriptableObject weaponData;

    protected virtual void Start()
    {
        // placeholder
    }
    public virtual void OnActiveItem1(InputValue inputValue)
    {
        Attack();
    }
    protected virtual void Attack()
    {
        weaponData.currentCooldown = weaponData.cooldown;
    }
}
