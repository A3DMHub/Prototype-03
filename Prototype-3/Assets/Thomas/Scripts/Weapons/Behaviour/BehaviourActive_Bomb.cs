using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourActive_Bomb : ActiveWeaponBehaviour
{
    private bool hasExploded;
    private float explosionLifetime = 0.25f;

    protected override void Start()
    {
        base.Start();
    }

    void Update()
    {
        transform.position += direction * weaponData.speed * Time.deltaTime;
    }

    protected override void OnLifetimeExpired()
    {
        Explode();
    }

    private void Explode()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;
        GameObject explosion = Instantiate(weaponData.explosionPrefab, transform.position, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * weaponData.range;
        Destroy(explosion, explosionLifetime);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, weaponData.range);

        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                Destroy(hitCollider.gameObject); // todo hp damage
            }
        }

        Destroy(gameObject);
    }

}
