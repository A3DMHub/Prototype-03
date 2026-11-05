using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponUpgradeController : MonoBehaviour
{
    [Header("Levelling")]
    public int playerLevel;
    public int expRequiredForUpgrade = 5;
    private PlayerResourceController playerResourceController;

    [Header("UI")]
    public Canvas levelUpMenu;
    private LevelUpMenu levelUpMenuScript;

    void Start()
    {
        playerResourceController = GetComponent<PlayerResourceController>();
        levelUpMenuScript = levelUpMenu.GetComponent<LevelUpMenu>();
    }

    void Update()
    {
        if (playerResourceController.expResource >= expRequiredForUpgrade)
        {
            onLevelUp();
        }
    }

    public void onLevelUp()
    {
        playerLevel++;

        Debug.Log("[PlayerWeaponUpgradeController] Player level increased to: " + playerLevel);
        
        levelUpMenuScript.StartUI();
        Time.timeScale = 0f; // pause game so you dont die upgrading / getting weapons

        playerResourceController.expResource -= expRequiredForUpgrade;
        expRequiredForUpgrade = Mathf.CeilToInt(expRequiredForUpgrade * 1.5f); // 50% increase in req exp for next lvl
    }
}