using UnityEngine;

[CreateAssetMenu(fileName = "Weapon Upgrade (Basic)", menuName = "Scriptable Objects/Weapon Upgrade (Basic)")]
public class WeaponUpgradeBasic : ScriptableObject
{

    public string upgradeStat; // name of the upgraded stat (needs to be 1-1 with the weapon's scriptable object stats)

    public float upgradeAmount; // the amount the stat is upgraded by (can be negative for downgrades)

    public string upgradeDescription; // description of upgrade
}
