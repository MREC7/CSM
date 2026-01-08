using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public DamageEffect damageEffect;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log("Player HP: " + currentHealth);

        if (damageEffect != null)
        {
            damageEffect.ShowDamage();
        }
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player Dead!");
        Destroy(gameObject);
        // Смерть игрока: отключить управление, показать UI и т.п.
    }
}