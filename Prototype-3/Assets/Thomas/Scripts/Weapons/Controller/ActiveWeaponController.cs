using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ActiveWeaponController : MonoBehaviour
{
    private static readonly Vector3 WindUpEffectOffset = new Vector3(0f, 3f, -3f);
    private static readonly Quaternion WindUpEffectRotation = Quaternion.Euler(85f, 0f, 0f);

    [Header("Weapon Stats")]
    public ActiveWeaponScriptableObject weaponData;
    
    public GameObject player;
    public GameObject windUpEffectPrefab;
    public bool canAttack = true;

    private GameObject activeWindUpEffect;

    protected virtual void Start()
    {
        weaponData.currentCooldown = 0f; // attack immediately 
    }

    protected virtual void Update()
    {
        if (weaponData.currentCooldown <= 0)
        {
            weaponData.currentCooldown = 0;
            canAttack = true;
        }
        else
        {
            weaponData.currentCooldown -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (activeWindUpEffect == null)
        {
            return;
        }

        activeWindUpEffect.transform.position = player.transform.position + WindUpEffectOffset;
        activeWindUpEffect.transform.rotation = WindUpEffectRotation;
    }

    public virtual void OnActiveItem1(InputValue inputValue)
    {
        if (canAttack)
        {
            canAttack = false;

            player.GetComponent<PlayerController>().moveSpeed -= 3f; // -3 speed while winding

            activeWindUpEffect = Instantiate(
                windUpEffectPrefab,
                player.transform.position + WindUpEffectOffset,
                WindUpEffectRotation,
                player.transform);

            StartCoroutine(WindUpWeapon(activeWindUpEffect));
        }
    }

    private IEnumerator WindUpWeapon(GameObject windUpEffect)
    {
        yield return new WaitForSeconds(weaponData.windUpTime);
        
        player.GetComponent<PlayerController>().moveSpeed += 3f;
        Destroy(windUpEffect);
        activeWindUpEffect = null;
        
        Attack();
    }
    protected virtual void Attack()
    {
        weaponData.currentCooldown = weaponData.cooldown;
    }
}
