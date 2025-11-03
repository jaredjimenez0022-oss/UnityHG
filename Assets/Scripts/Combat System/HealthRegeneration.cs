using Fusion;
using UnityEngine;

public class HealthRegeneration : NetworkBehaviour
{
    [Networked] public float LastDamageTime { get; set; }
    [Networked] public bool IsRegenerating { get; set; }

    [SerializeField] private float healthRegenDelay = 8f;
    [SerializeField] private int healthRegenAmount = 10;
    [SerializeField] private float healthRegenInterval = 2f;


    private PlayerHealth playerHealth;
    private TickTimer regenTimer { get; set; }
    

    public override void Spawned()
    {
        playerHealth = GetComponent<PlayerHealth>();
        LastDamageTime = -healthRegenDelay;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority ||
            playerHealth == null ||
            playerHealth.GetIsDead() ||
            playerHealth.GetCurrentHealth() >= playerHealth.GetMaxHealth()) return;
            

        if (Runner.SimulationTime - LastDamageTime > healthRegenDelay)
        {
            if (!IsRegenerating)
            {
                IsRegenerating = true;
                regenTimer = TickTimer.CreateFromSeconds(Runner, healthRegenInterval);
            }

            if (regenTimer.Expired(Runner))
            {
                playerHealth.Heal(healthRegenAmount);
                regenTimer = TickTimer.CreateFromSeconds(Runner, healthRegenInterval);
            }
        }
        else
        {
            IsRegenerating = false;
        }
    }


    public void OnDamageTaken()
    {
        if (!HasStateAuthority) return;

        LastDamageTime = Runner.SimulationTime;
        IsRegenerating = false;
        regenTimer = TickTimer.None;
    }

}