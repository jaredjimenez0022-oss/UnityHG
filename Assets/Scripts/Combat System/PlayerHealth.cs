using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Atributos de Vida")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }


    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log(gameObject.name + " recibió " + damageAmount + " de daño. Vida actual: " + currentHealth);

        if (currentHealth == 0)
        {
            StartCoroutine(DieAfterFrame()); //Si no se hace esto el hud de vida no se termina de actualizar 
                                             // porque el jugador se inactiva antes
        }
    }

    private System.Collections.IEnumerator DieAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        Die();
    }


    public void Heal(int healAmount)
    {
        currentHealth += healAmount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        Debug.Log(gameObject.name + " fue curado. Vida actual: " + currentHealth);
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " ha muerto!");
        gameObject.SetActive(false);
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}
