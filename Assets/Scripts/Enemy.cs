using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Vida del Enemigo")]
    public int maxHealth = 100; // Cambiado a 100
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log("👾 Enemigo creado con " + currentHealth + " de vida.");
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log("💥 Enemigo recibió " + amount + " de daño. Vida restante: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("☠️ Enemigo derrotado.");
        Destroy(gameObject);
    }
}
