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

    // NEW: Flag para saber si ya se inicializó
    private bool initialized = false;

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple GameStateManagers detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        networkConnectionHandler = FindFirstObjectByType<NetworkConnectionHandler>();
        gameOverManager = FindFirstObjectByType<GameOverManager>();

        // NEW: No registrar jugadores inmediatamente
        // Esperar a que NetworkPlayer llame RegisterPlayer directamente
        initialized = true;
        
        Debug.Log($"[GameStateManager] Spawned. HasStateAuthority: {HasStateAuthority}");
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
        if (!HasStateAuthority)
        {
            Debug.LogWarning($"[GameStateManager] RegisterPlayer llamado sin autoridad para {playerRef.PlayerId}");
            return;
        }

        if (!initialized)
        {
            Debug.LogWarning($"[GameStateManager] RegisterPlayer llamado antes de inicializar para {playerRef.PlayerId}");
            return;
        }

        if (alivePlayers.Add(playerRef))
        {
            UpdateAlivePlayersCount();
            Debug.Log($"[GameStateManager] ✓ Jugador {playerRef.PlayerId} registrado. Total: {AlivePlayersCount}");
        }
        else
        {
            Debug.LogWarning($"[GameStateManager] Jugador {playerRef.PlayerId} ya estaba registrado");
        }
    }

    public void UnregisterPlayer(PlayerRef playerRef)
    {
        if (!HasStateAuthority) return;

        if (alivePlayers.Remove(playerRef))
        {
            UpdateAlivePlayersCount();
            CheckForGameEnd();
            Debug.Log($"[GameStateManager] Jugador {playerRef.PlayerId} eliminado. Jugadores vivos: {AlivePlayersCount}");
        }
    }

    public void OnPlayerDeath(PlayerRef deadPlayer)
    {
        if (!HasStateAuthority) return;

        // NEW: Buscar el GameObject del jugador para llamar ShowEliminationScreen solo en él
        if (Runner.TryGetPlayerObject(deadPlayer, out NetworkObject playerObj))
        {
            var networkPlayer = playerObj.GetComponent<NetworkPlayer>();
            if (networkPlayer != null && playerObj.HasInputAuthority)
            {
                // Solo mostrar eliminación al jugador local que murió
                RPC_ShowEliminationToPlayer(deadPlayer);
            }
        }

        UnregisterPlayer(deadPlayer);
        RPC_NotifyPlayerEliminated(deadPlayer);
    }

    // NEW: RPC específico para mostrar eliminación solo al jugador muerto
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowEliminationToPlayer(PlayerRef eliminatedPlayer)
    {
        // Solo ejecutar si somos el jugador eliminado
        if (Runner.LocalPlayer == eliminatedPlayer)
        {
            GameOverManager gameOverManager = FindFirstObjectByType<GameOverManager>();
            if (gameOverManager != null)
            {
                gameOverManager.ShowEliminationScreen();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerEliminated(PlayerRef eliminatedPlayer)
    {
        Debug.Log($"[GameStateManager] JUGADOR ELIMINADO: Player {eliminatedPlayer.PlayerId}");

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
            foreach (var playerRef in alivePlayers)
            {
                WinnerPlayerRef = playerRef;
                break;
            }
            EndGame($"Jugador {GetPlayerName(WinnerPlayerRef)}");
        }
        else if (AlivePlayersCount == 0)
        {
            EndGame("NINGÚN JUGADOR");
        }
    }

    private void EndGame(string winnerInfo)
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log($"[GameStateManager] FIN DEL JUEGO DETECTADO: {winnerInfo}");
        RPC_EndGame(winnerInfo);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndGame(string winnerInfo)
    {
        Debug.Log($"[GameStateManager] FIN DEL JUEGO: {winnerInfo} gana!");

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
            Debug.LogError("[GameStateManager] GameOverManager no encontrado");
        }
    }

    public void OnPlayerAbandoned(PlayerRef abandonedPlayer)
    {
        if (!HasStateAuthority) return;

        Debug.Log($"[GameStateManager] PROCESANDO ABANDONO: Player {abandonedPlayer.PlayerId}");
        UnregisterPlayer(abandonedPlayer);
        RPC_NotifyPlayerAbandoned(abandonedPlayer);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPlayerAbandoned(PlayerRef abandonedPlayer)
    {
        Debug.Log($"[GameStateManager] JUGADOR ABANDONÓ: Player {abandonedPlayer.PlayerId}");

        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowAbandonMessage($"Player {GetPlayerName(abandonedPlayer)}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}