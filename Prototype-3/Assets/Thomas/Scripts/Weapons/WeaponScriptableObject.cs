using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapon")]
public class WeaponScriptableObject : ScriptableObject
{
    public GameObject prefab;

    // general stats
    public float damage; // damage on hit
    public float cooldown; // time between attacks
    public int pierce; // how many enemies the weapon can pierce through
    public float destroyTime; // weapon lifetime
    public bool followPlayerDir; // if the weapon uses the player's rotation/direction or not

    public float attackDir; // the direction the weapon is facing when attacking if followPlayerDir is false (0 = up, 90 = right, 180 = down, 270 = left)

    // ranged stats
    public float speed; // projectile speed
    public int projectileCount; // how many projectiles are shot at once
    public int projectileSpread; // the angle spread of the projectiles

    // melee stats

    // misc
    public float currentCooldown;
}
