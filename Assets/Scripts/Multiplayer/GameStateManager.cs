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

    public static GameStateManager Instance { get; private set; }

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        gameOverManager = FindFirstObjectByType<GameOverManager>();
        
        if (HasStateAuthority)
        {
            // Inicializar contador de jugadores vivos
            UpdateAlivePlayersCount();
        }
    }

    public void RegisterPlayer(PlayerRef playerRef)
    {
        if (!HasStateAuthority) return;

        alivePlayers.Add(playerRef);
        UpdateAlivePlayersCount();
        Debug.Log($"Jugador {playerRef.PlayerId} registrado. Jugadores vivos: {AlivePlayersCount}");
    }

    public void UnregisterPlayer(PlayerRef playerRef)
    {
        if (!HasStateAuthority) return;

        alivePlayers.Remove(playerRef);
        UpdateAlivePlayersCount();
        CheckForGameEnd();
        Debug.Log($"Jugador {playerRef.PlayerId} eliminado. Jugadores vivos: {AlivePlayersCount}");
    }

    public void OnPlayerDeath(PlayerRef deadPlayer)
    {
        if (!HasStateAuthority) return;

        alivePlayers.Remove(deadPlayer);
        UpdateAlivePlayersCount();
        
        // Notificar a todos los jugadores sobre la eliminación
        RPC_NotifyPlayerEliminated(deadPlayer);
        
        CheckForGameEnd();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerEliminated(PlayerRef eliminatedPlayer)
    {
        Debug.Log($"🎯 JUGADOR ELIMINADO: Player {eliminatedPlayer.PlayerId}");
        
        // Aquí puedes agregar efectos visuales/sonidos de eliminación
        // Por ejemplo: mostrar mensaje en pantalla, sonido, etc.
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
            // Un solo jugador vivo - es el ganador
            foreach (var playerRef in alivePlayers)
            {
                WinnerPlayerRef = playerRef;
                break;
            }
            EndGame($"Jugador {WinnerPlayerRef.PlayerId}");
        }
        else if (AlivePlayersCount == 0)
        {
            // Ningún jugador vivo - empate
            EndGame("NINGÚN JUGADOR");
        }
    }

    private void EndGame(string winnerInfo)
    {
        if (IsGameOver) return;

        IsGameOver = true;
        RPC_EndGame(winnerInfo);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndGame(string winnerInfo)
    {
        Debug.Log($"🏆 FIN DEL JUEGO: {winnerInfo} gana!");
        
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

        if (alivePlayers.Contains(abandonedPlayer))
        {
            alivePlayers.Remove(abandonedPlayer);
            UpdateAlivePlayersCount();
            
            // Notificar a todos que el jugador abandonó
            RPC_NotifyPlayerAbandoned(abandonedPlayer);
            CheckForGameEnd(); 
        }

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerAbandoned(PlayerRef abandonedPlayer)
    {
        Debug.Log($"🚪 JUGADOR ABANDONÓ: Player {abandonedPlayer.PlayerId}");
        // Aquí puedes agregar UI/efectos para abandonos
    }
}