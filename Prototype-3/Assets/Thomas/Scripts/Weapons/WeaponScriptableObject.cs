using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapon")]
public class WeaponScriptableObject : ScriptableObject
{
    public GameObject prefab;

    // stats
    public float damage;
    public float speed;
    public float cooldown;
    public float currentCooldown;
    public int pierce;
    public float destroyTime;
}
