using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerShield))]
[RequireComponent(typeof(HealthRegeneration))]
public class PlayerCombat : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private PlayerShield playerShield;
    private HealthRegeneration healthRegeneration;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerShield = GetComponent<PlayerShield>();
        healthRegeneration = GetComponent<HealthRegeneration>();
    }


    public void TakeDamage(int damageAmount)
    {
        //El escudo absorbe el daño y devuelve el sobrante si no alcanzó
        int remainingDamage = playerShield.AbsorbDamage(damageAmount);

        //Si no alcanzó el escudo baja la vida
        if (remainingDamage > 0)
        {
            playerHealth.TakeDamage(remainingDamage);
        }

        healthRegeneration.OnDamageTaken();
    }
}
