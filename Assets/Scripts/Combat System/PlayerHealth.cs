using Fusion;
using Unity.VisualScripting;
using UnityEngine;


public class PlayerHealth : NetworkBehaviour
{
    [Networked] public int CurrentHealth { get; private set; }
    [Networked] public int MaxHealth { get; private set; }
    [Networked] public bool IsDead { get; private set; }

    [SerializeField] private int initialHealth = 100;

    private GameStateManager gameStateManager;

    public System.Action<int, int> OnHealthChanged; //current, max
    public System.Action OnDeath;



    public override void Spawned()
    {
        gameStateManager = GameStateManager.Instance;
        if (HasStateAuthority)
        {
            MaxHealth = initialHealth;
            CurrentHealth = MaxHealth;
            IsDead = false;

            if (gameStateManager != null)
            {
                gameStateManager.RegisterPlayer(Object.InputAuthority);
            }
        }

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }


    public void TakeDamage(int damage)
    {
        if (IsDead || !HasStateAuthority) return;

        int newHealth = Mathf.Max(0, CurrentHealth - damage);
        RPC_SetHealth(newHealth);

        if (newHealth <= 0 && !IsDead)
        {
            Die();
        }
    }

    public void Heal(int healAmount)
    {
        if (IsDead || !HasStateAuthority) return;

        int newHealth = Mathf.Min(MaxHealth, CurrentHealth + healAmount);
        RPC_SetHealth(newHealth);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetHealth(int newHealth)
    {
        CurrentHealth = newHealth;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }


    private void Die()
    {
        if (!HasStateAuthority) return;
        if (IsDead) return;

        IsDead = true;
        RPC_Die();

        if (gameStateManager != null)
        {
            gameStateManager.OnPlayerDeath(Object.InputAuthority);
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Die()
    {
        IsDead = true;
        OnDeath?.Invoke();

        Debug.Log($"Player {Object.Id} ha muerto");

    }

    public int GetCurrentHealth() => CurrentHealth;
    public int GetMaxHealth() => MaxHealth;
    public bool GetIsDead() => IsDead;

    // Método para que otros componentes verifiquen si pueden atacar
    public bool CanBeAttacked() => !IsDead && HasStateAuthority;

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //Desregistrar jugador cuando abandona la partida
        if (gameStateManager != null && HasStateAuthority)
        {
            if (!IsDead)
            {
                Debug.Log($"Jugador {Object.InputAuthority.PlayerId} abandonó la partida (Alive)");
                gameStateManager.OnPlayerAbandoned(Object.InputAuthority);
            }
            /*
            else
            {
                Debug.Log($"🚪 Jugador {Object.InputAuthority.PlayerId} abandonó la partida (Dead)");
                gameStateManager.UnregisterPlayer(Object.InputAuthority);
            }*/
        }
    }
    
}
