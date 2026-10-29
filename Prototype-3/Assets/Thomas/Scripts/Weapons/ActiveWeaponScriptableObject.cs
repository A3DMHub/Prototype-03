using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Active Weapon", menuName = "Scriptable Objects/Active Weapon")]
public class ActiveWeaponScriptableObject : ScriptableObject
{
    public GameObject prefab;
    public GameObject explosionPrefab;

    [Header("General Stats")]
    public float damage; // damage on hit
    public float cooldown; // time between attacks
    public float windUpTime; // how long it takes to wind up before attacking
    public float destroyTime; // weapon lifetime
    public float range; // explosion range
    public bool followPlayerDir; // if the weapon uses the player's rotation/direction or not
    public float attackDir; // the direction the weapon is facing when attacking if followPlayerDir is false (0 = up, 90 = right, 180 = down, 270 = left)

    [Header("Ranged Stats")]
    public float speed; // projectile speed
    public int projectileCount; // how many projectiles are shot at once
    public int projectileSpread; // the angle spread of the projectiles

    [Header("Misc Settings")]
    public float currentCooldown;
}
