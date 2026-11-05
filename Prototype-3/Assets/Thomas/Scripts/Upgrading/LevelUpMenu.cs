using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class LevelUpMenu : MonoBehaviour
{
    public GameObject Container;
    public GameObject Player;

    [Header("Buttons")]
    public GameObject Option1;

    public bool CanUpgrade = true;

    private GameObject PlayerWeaponContainer;
    private List<GameObject> PlayerWeapons = new List<GameObject>();
    void Start()
    {
        Container.SetActive(false);
        PlayerWeaponContainer = Player.transform.Find("Weapons").gameObject;
    }
    public void StartUI()
    {
        GetWeapons();

        Option1.GetComponent<ButtonTargetWeapon>().TargetWeapon = PlayerWeapons[Random.Range(0, PlayerWeapons.Count)];
        UpdateButton(Option1);

        Container.SetActive(true);
    }

    void UpdateButton(GameObject button)
    {
        GameObject WeaponController = button.GetComponent<ButtonTargetWeapon>().TargetWeapon;
        object weaponData = GetWeaponData(WeaponController);
        System.Type weaponDataType = weaponData.GetType();
        FieldInfo weaponNameField = weaponDataType.GetField("weaponName");
        FieldInfo upgradeTierField = weaponDataType.GetField("upgradeTier");
        FieldInfo upgradesField = weaponDataType.GetField("upgrades");

        string weaponName = (string)weaponNameField.GetValue(weaponData);
        int upgradeTier = (int)upgradeTierField.GetValue(weaponData);
        List<WeaponUpgradeBasic> upgrades = (List<WeaponUpgradeBasic>)upgradesField.GetValue(weaponData);

        button.transform.Find("WeaponName").GetComponent<TMPro.TextMeshProUGUI>().text = weaponName;
        button.transform.Find("UpgradeDetails").GetComponent<TMPro.TextMeshProUGUI>().text = upgrades[upgradeTier].upgradeDescription;
    } 
    public void CloseUI()
    {
        Time.timeScale = 1f; // resume game
        Container.SetActive(false);
    }

    public void SelectButton(GameObject button)
    {
        GameObject WeaponController = button.GetComponent<ButtonTargetWeapon>().TargetWeapon;
        object weaponData = GetWeaponData(WeaponController);
        UpgradeWeapon(weaponData);
    }

    public void UpgradeWeapon(object weaponData)
    {
        if (!CanUpgrade) return;

        CanUpgrade = false;

        if (weaponData == null)
        {
            Debug.LogWarning("[LevelUpMenu] Cannot upgrade a weapon without weapon data.");
            return;
        }

        System.Type weaponDataType = weaponData.GetType();
        FieldInfo weaponNameField = weaponDataType.GetField("weaponName");
        FieldInfo upgradeTierField = weaponDataType.GetField("upgradeTier");
        FieldInfo upgradesField = weaponDataType.GetField("upgrades");
        if (weaponNameField == null || upgradeTierField == null || upgradesField == null)
        {
            Debug.LogWarning("[LevelUpMenu] Weapon data is missing weaponName, upgradeTier, or upgrades.");
            return;
        }

        string weaponName = (string)weaponNameField.GetValue(weaponData);
        int upgradeTier = (int)upgradeTierField.GetValue(weaponData);
        List<WeaponUpgradeBasic> upgrades = (List<WeaponUpgradeBasic>)upgradesField.GetValue(weaponData);

        upgradeTierField.SetValue(weaponData, upgradeTier + 1);
        if (upgrades == null || upgradeTier < 0 || upgrades[upgradeTier] == null)
        {
            Debug.LogWarning("[LevelUpMenu] No upgrade available for " + weaponName + " at tier " + upgradeTier);
            return;
        }

        WeaponUpgradeBasic upgrade = upgrades[upgradeTier];
        if (string.IsNullOrEmpty(upgrade.upgradeStat))
        {
            Debug.LogWarning("[LevelUpMenu] Upgrade for " + weaponName + " has no stat configured.");
            return;
        }

        FieldInfo statField = weaponDataType.GetField(upgrade.upgradeStat);
        if (statField == null)
        {
            Debug.LogWarning("[LevelUpMenu] Upgrade stat '" + upgrade.upgradeStat + "' was not found on " + weaponName);
            return;
        }

        object currentValue = statField.GetValue(weaponData);
        if (statField.FieldType == typeof(float))
        {
            statField.SetValue(weaponData, (float)currentValue + upgrade.upgradeAmount);
        }
        else if (statField.FieldType == typeof(int))
        {
            statField.SetValue(weaponData, Mathf.RoundToInt((int)currentValue + upgrade.upgradeAmount));
        }
        else
        {
            Debug.LogWarning("[LevelUpMenu] Upgrade stat '" + upgrade.upgradeStat + "' is not a supported numeric field.");
            return;
        }

        CanUpgrade = true;
        CloseUI();
    }

    object GetWeaponData(GameObject weaponController)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MonoBehaviour[] components = weaponController.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component == null)
            {
                continue;
            }

            System.Type type = component.GetType();
            FieldInfo field = type.GetField("weaponData", flags);
            if (field != null)
            {
                return field.GetValue(component);
            }

            PropertyInfo property = type.GetProperty("weaponData", flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(component);
            }
        }

        Debug.LogWarning("[LevelUpMenu] No component with a weaponData field or property found on " + weaponController.name);
        return null;
    }

    void GetWeapons()
    {
        for (int i = 0; i < PlayerWeaponContainer.transform.childCount; i++)
        {
            GameObject weapon = PlayerWeaponContainer.transform.GetChild(i).gameObject;
            if (weapon.activeInHierarchy)
            {
                PlayerWeapons.Add(weapon);
            }
        }
    }
}
