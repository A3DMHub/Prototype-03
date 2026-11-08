using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResourceController : MonoBehaviour
{
    [Header("Resources")]
    public float timeResource;
    public float expResource;

    [Header("UI")]
    public Canvas resourceCanvas;
    public Slider expBar;
    
    private PlayerHealth playerHealth;
    private PlayerWeaponUpgradeController playerWeaponUpgradeController;
    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerWeaponUpgradeController = GetComponent<PlayerWeaponUpgradeController>();
        expBar = resourceCanvas.transform.Find("ResourceUI").Find("PlayerEXPBar").gameObject.GetComponent<Slider>();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        timeResource -= Time.deltaTime;
        
        resourceCanvas.transform.Find("ResourceUI").Find("Time").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = Mathf.RoundToInt(timeResource).ToString();

        if (expBar != null)
        {
            expBar.maxValue = playerWeaponUpgradeController.expRequiredForUpgrade;
            expBar.value = expResource;
        }

        if (timeResource <= 0)
        {
            playerHealth.Die();
        }
    }

    void OnTriggerEnter(Collider other)
    {   
        if (Time.timeScale == 0f) return; // dont pick up things while in the level up menu

        if (other.CompareTag("ResourcePickup"))
        {
            timeResource += other.GetComponent<ResourcePickup>().timeAmount;
            expResource += other.GetComponent<ResourcePickup>().expAmount;
            Destroy(other.gameObject);
        }
    }
}
