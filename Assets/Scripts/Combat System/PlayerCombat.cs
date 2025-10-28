using UnityEngine;
using Fusion;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerShield))]
[RequireComponent(typeof(HealthRegeneration))]
public class PlayerCombat : NetworkBehaviour
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

    void Update()
    {
        if (HasStateAuthority && Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage(10);
            Debug.Log("TEST: Aplicando 10 de daño");
        }
    }


    public void TakeDamage(int damageAmount)
    {
        if (!HasStateAuthority || playerHealth.GetIsDead()) return;

        //El escudo absorbe el daño y devuelve el sobrante si no alcanzó
        int remainingDamage = playerShield.AbsorbDamage(damageAmount);

        //Si no alcanzó el escudo baja la vida
        if (remainingDamage > 0)
        {
            playerHealth.TakeDamage(remainingDamage);
        }

        healthRegeneration.OnDamageTaken();
    }

    // RPC para poder recibir daño de otros jugadores
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damageAmount, PlayerRef damageSource)
    {
        TakeDamage(damageAmount);
        Debug.Log($"Daño recibido de jugador {damageSource.PlayerId}");
    }

    // Método público para que otros jugadores apliquen daño
    public void ApplyDamage(int damageAmount, PlayerRef damageSource)
    {
        if (HasStateAuthority)
        {
            // Si es el dueño, aplicar directamente
            TakeDamage(damageAmount);
        }
        else
        {
            // Si no es el dueño, enviar RPC al dueño
            RPC_TakeDamage(damageAmount, damageSource);
        }
    }

    public bool IsAlive() => playerHealth != null && !playerHealth.GetIsDead();
}
