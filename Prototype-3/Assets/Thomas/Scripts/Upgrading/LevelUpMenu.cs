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
    public GameObject Option2;
    public GameObject Option3;

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

        List<GameObject> availableWeapons = PlayerWeapons;

        int index1 = Random.Range(0, availableWeapons.Count);
        Option1.GetComponent<ButtonTargetWeapon>().TargetWeapon = availableWeapons[index1];
        availableWeapons.RemoveAt(index1);
        UpdateButton(Option1);

        int index2 = Random.Range(0, availableWeapons.Count);
        Option2.GetComponent<ButtonTargetWeapon>().TargetWeapon = availableWeapons[index2];
        availableWeapons.RemoveAt(index2);
        UpdateButton(Option2);

        int index3 = Random.Range(0, availableWeapons.Count);
        Option3.GetComponent<ButtonTargetWeapon>().TargetWeapon = availableWeapons[index3];
        availableWeapons.RemoveAt(index3);
        UpdateButton(Option3);
        // probably a better way to do above

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
        string upgradeDescription;
        List<WeaponUpgradeBasic> upgrades = (List<WeaponUpgradeBasic>)upgradesField.GetValue(weaponData);

        if (WeaponController.activeInHierarchy)
        {
            upgradeDescription = upgrades[upgradeTier].upgradeDescription;
        }
        else
        {
            upgradeDescription = "Unlocks this weapon.";
        }

        button.transform.Find("WeaponName").GetComponent<TMPro.TextMeshProUGUI>().text = weaponName;
        button.transform.Find("UpgradeDetails").GetComponent<TMPro.TextMeshProUGUI>().text = upgradeDescription;
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
        UpgradeWeapon(weaponData, WeaponController);
    }

    public void UpgradeWeapon(object weaponData, GameObject weapon)
    {
        if (!CanUpgrade) return;

        CanUpgrade = false;

        if (weaponData == null) return;

        if (!weapon.activeInHierarchy)
        {
            weapon.SetActive(true);
        }
        else
        {
            System.Type weaponDataType = weaponData.GetType();
            FieldInfo weaponNameField = weaponDataType.GetField("weaponName");
            FieldInfo upgradeTierField = weaponDataType.GetField("upgradeTier");
            FieldInfo upgradesField = weaponDataType.GetField("upgrades");
            if (weaponNameField == null || upgradeTierField == null || upgradesField == null) return;

            string weaponName = (string)weaponNameField.GetValue(weaponData);
            int upgradeTier = (int)upgradeTierField.GetValue(weaponData);
            List<WeaponUpgradeBasic> upgrades = (List<WeaponUpgradeBasic>)upgradesField.GetValue(weaponData);

            upgradeTierField.SetValue(weaponData, upgradeTier + 1);
            if (upgrades == null || upgradeTier < 0 || upgrades[upgradeTier] == null) return;

            WeaponUpgradeBasic upgrade = upgrades[upgradeTier];
            if (string.IsNullOrEmpty(upgrade.upgradeStat)) return;

            FieldInfo statField = weaponDataType.GetField(upgrade.upgradeStat);
            if (statField == null) return;

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
                return;
            }
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

        return null;
    }

    void GetWeapons()
    {
        for (int i = 0; i < PlayerWeaponContainer.transform.childCount; i++)
        {
            GameObject weapon = PlayerWeaponContainer.transform.GetChild(i).gameObject;
            PlayerWeapons.Add(weapon);
        }
    }
}
