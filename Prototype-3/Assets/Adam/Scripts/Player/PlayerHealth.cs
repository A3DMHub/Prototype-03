using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Damage Settings")]
    public float damagePerHit = 10f; // Adjust how hard enemies hit

    [Header("UI Reference")]
    public Slider healthBarSlider;

    [Header("Game Over Integration")]
    public GameObject gameOverPanel;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }
    }

    // Detect collision with enemies
    void OnCollisionEnter(Collision collision)
    {
        // Check if the object hitting us is tagged as an Enemy
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.name.Contains("Enemy"))
        {
            TakeDamage(damagePerHit);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
    }

    void Die()
    {
        Debug.Log("Player has died!");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f; // Freeze the game
    }
}