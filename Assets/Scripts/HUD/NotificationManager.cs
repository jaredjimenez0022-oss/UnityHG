using UnityEngine;
using Fusion;

public class NotificationManager : NetworkBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Networked] private string CurrentNotification { get; set; }
    [Networked] private float NotificationTimer { get; set; }
    [Networked] private bool IsShowingNotification { get; set; }

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && IsShowingNotification)
        {
            NotificationTimer -= Runner.DeltaTime;
            if (NotificationTimer <= 0f)
            {
                IsShowingNotification = false;
                CurrentNotification = "";
                RPC_HideNotification();
            }
        }
    }

    public void ShowNotification(string message)
    {
        CurrentNotification = message;
        NotificationTimer = 3f;
        IsShowingNotification = true;
        RPC_ShowNotification(message);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowNotification(string message)
    {
        foreach (var hud in FindObjectsByType<PlayerHUD>(FindObjectsSortMode.InstanceID))
        {
            if (hud.HasInputAuthority)
            {
                hud.ShowNotification(message, 3f);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideNotification()
    {
        foreach (var hud in FindObjectsByType<PlayerHUD>(FindObjectsSortMode.InstanceID))
        {
            if (hud.HasInputAuthority)
            {
                hud.HideNotification();
            }
        }
    }

    public void ShowEliminationMessage(string playerName)
    {
        if (HasStateAuthority)
        {
            RPC_ShowNotification($"{playerName} fue eliminado");
        }
    }

    public void ShowAbandonMessage(string playerName)
    {
        if (HasStateAuthority)
        {
            RPC_ShowNotification($"{playerName} abandonó la partida");
        }
    }

    public void ShowVictoryMessage(string winnerInfo)
    {
        if (HasStateAuthority)
        {
            RPC_ShowNotification($"{winnerInfo} gana la partida!");
        }
    }
}