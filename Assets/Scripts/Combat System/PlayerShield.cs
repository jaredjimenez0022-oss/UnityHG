using Fusion;
using UnityEngine;

public class PlayerShield : NetworkBehaviour
{
    [Networked] public int CurrentShield { get; private set; }
    [Networked] public int MaxShield { get; private set; }
    [Networked] public float LastDamageTime { get; private set; }

    [SerializeField] private int initialShield = 50;
    [SerializeField] private float shieldRegenDelay = 5f;
    [SerializeField] private int shieldRegenAmount = 5;
    [SerializeField] private float shieldRegenInterval = 1f;

    private TickTimer regenTimer;

    public System.Action<int, int> OnShieldChanged { get; set; } //current, max

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            MaxShield = initialShield;
            CurrentShield = MaxShield;
            LastDamageTime = -shieldRegenDelay;
        }
        OnShieldChanged?.Invoke(CurrentShield, MaxShield);
    }


    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || CurrentShield >= MaxShield) return;

        // Regenerar escudo después del delay
        if (Runner.SimulationTime - LastDamageTime > shieldRegenDelay)
        {
            if (regenTimer.ExpiredOrNotRunning(Runner))
            {
                RegenerateShield();
                regenTimer = TickTimer.CreateFromSeconds(Runner, shieldRegenInterval);
            }
        }
    }


    public int AbsorbDamage(int damageAmount)
    {
        if (!HasStateAuthority) return damageAmount;

        LastDamageTime = Runner.SimulationTime;
        regenTimer = TickTimer.None;

        int remainingDamage = damageAmount;

        if (CurrentShield > 0)
        {
            int shieldDamage = Mathf.Min(CurrentShield, damageAmount);
            remainingDamage = damageAmount - shieldDamage;

            RPC_SetShield(CurrentShield - shieldDamage);
        }

        //Si no hay escudo todo el daño pasa a la vida
        return remainingDamage;
    }

    public void AddShield(int shieldAmount)
    {
        if (!HasStateAuthority) return;

        int newShield = Mathf.Min(MaxShield, CurrentShield + shieldAmount);
        RPC_SetShield(newShield);
    }

    public void RegenerateShield()
    {
        if (!HasStateAuthority) return;

        int newShield = Mathf.Min(MaxShield, CurrentShield + shieldRegenAmount);
        RPC_SetShield(newShield);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetShield(int newShield)
    {
        CurrentShield = newShield;
        OnShieldChanged?.Invoke(CurrentShield, MaxShield);
    }

    public int GetCurrentShield() => CurrentShield;
    public int GetMaxShield() => MaxShield;

}
