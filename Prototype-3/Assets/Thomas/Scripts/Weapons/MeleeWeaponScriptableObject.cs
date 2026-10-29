using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Melee Weapon", menuName = "Scriptable Objects/Melee Weapon")]
public class MeleeWeaponScriptableObject : ScriptableObject
{
    public GameObject prefab;

    [Header("Stats")]
    public float damage; // damage on hit
    public float cooldown; // time between attacks
    public float destroyTime; // weapon lifetime
    public bool followPlayerDir; // if the weapon uses the player's rotation/direction or not
    public float attackDir; // the direction the weapon is facing when attacking if followPlayerDir is false (0 = up, 90 = right, 180 = down, 270 = left)

    [Header("Misc Settings")]
    public float currentCooldown;
}
