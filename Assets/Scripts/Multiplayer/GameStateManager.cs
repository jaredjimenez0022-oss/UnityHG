using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : NetworkBehaviour
{
    [Networked] public int AlivePlayersCount { get; private set; }
    [Networked] public PlayerRef WinnerPlayerRef { get; private set; }
    [Networked] public bool IsGameOver { get; private set; }

    private HashSet<PlayerRef> alivePlayers = new HashSet<PlayerRef>();
    private GameOverManager gameOverManager;
    private NetworkConnectionHandler networkConnectionHandler; 

    public static GameStateManager Instance { get; private set; }

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        networkConnectionHandler = FindFirstObjectByType<NetworkConnectionHandler>();
        gameOverManager = FindFirstObjectByType<GameOverManager>();

        if (HasStateAuthority)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                RegisterPlayer(player);
            }
        }
    }

    private string GetPlayerName(PlayerRef playerRef)
    {
        if (networkConnectionHandler != null)
        {
            return networkConnectionHandler.GetPlayerName(playerRef);
        }
        
        return $"Player {playerRef.PlayerId}";
    }

    public void RegisterPlayer(PlayerRef playerRef)
    {
        if (!HasStateAuthority) return;

        if (alivePlayers.Add(playerRef))
        {
            UpdateAlivePlayersCount();
            Debug.Log($"JUGADOR REGISTRADO: Player {playerRef.PlayerId}. Total: {AlivePlayersCount}");
        }
    }

    public void UnregisterPlayer(PlayerRef playerRef)
    {
        if (!HasStateAuthority) return;

        if (alivePlayers.Remove(playerRef))
        {
            UpdateAlivePlayersCount();
            CheckForGameEnd();
            Debug.Log($"Jugador {playerRef.PlayerId} eliminado. Jugadores vivos: {AlivePlayersCount}");
        }
    }

    public void OnPlayerDeath(PlayerRef deadPlayer)
    {
        if (!HasStateAuthority) return;

        UnregisterPlayer(deadPlayer);

        RPC_NotifyPlayerEliminated(deadPlayer);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerEliminated(PlayerRef eliminatedPlayer)
    {
        Debug.Log($"JUGADOR ELIMINADO: Player {eliminatedPlayer.PlayerId}");

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowEliminationMessage($"Player {GetPlayerName(eliminatedPlayer)}");
        }
    }

    private void UpdateAlivePlayersCount()
    {
        AlivePlayersCount = alivePlayers.Count;
    }

    private void CheckForGameEnd()
    {
        if (!HasStateAuthority || IsGameOver) return;

        if (AlivePlayersCount == 1)
        {
            //el ganador
            foreach (var playerRef in alivePlayers)
            {
                WinnerPlayerRef = playerRef;
                break;
            }
            //EndGame($"Jugador {WinnerPlayerRef.PlayerId}");
            EndGame($"Jugador {GetPlayerName(WinnerPlayerRef)}");
        }
        else if (AlivePlayersCount == 0)
        {
            //empate
            EndGame("NINGÚN JUGADOR");
        }
    }

    private void EndGame(string winnerInfo)
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log($"FIN DEL JUEGO DETECTADO: {winnerInfo}");
        RPC_EndGame(winnerInfo);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndGame(string winnerInfo)
    {
        Debug.Log($"FIN DEL JUEGO: {winnerInfo} gana!");

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowVictoryMessage(winnerInfo);
        }

        if (gameOverManager != null)
        {
            gameOverManager.EndGame(winnerInfo);
        }
        else
        {
            Debug.LogError("GameOverManager no encontrado");
        }
    }

    public void OnPlayerAbandoned(PlayerRef abandonedPlayer)
    {
        if (!HasStateAuthority) return;

        Debug.Log($"PROCESANDO ABANDONO: Player {abandonedPlayer.PlayerId}");
        UnregisterPlayer(abandonedPlayer);

        //Notificar a todos que un jugador abandonó
        RPC_NotifyPlayerAbandoned(abandonedPlayer);

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerAbandoned(PlayerRef abandonedPlayer)
    {
        Debug.Log($"JUGADOR ABANDONÓ: Player {abandonedPlayer.PlayerId}");

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowAbandonMessage($"Player {GetPlayerName(abandonedPlayer)}");
        }
    }

}