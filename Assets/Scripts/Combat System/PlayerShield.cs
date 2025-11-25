using Fusion;
using UnityEngine;

public class PlayerShield : NetworkBehaviour
{
    [Networked] public int CurrentShield { get; private set; }
    [Networked] public int MaxShield { get; private set; }

    [SerializeField] private int initialShield = 50;


    public System.Action<int, int> OnShieldChanged { get; set; } //current, max

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            MaxShield = initialShield;
            CurrentShield = MaxShield;
        }
        OnShieldChanged?.Invoke(CurrentShield, MaxShield);
    }


    public override void FixedUpdateNetwork()
    {
        
    }


    public int AbsorbDamage(int damageAmount)
    {
        if (!HasStateAuthority) return damageAmount;

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



    public void RegenerateShield()
    {
        if (!HasStateAuthority) return;

        int newShield = Mathf.Min(MaxShield, CurrentShield);
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
