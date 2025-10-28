using UnityEngine;
using Fusion;

public class GameEndDetector : NetworkBehaviour
{
    private GameOverManager gameOverManager;

    public override void Spawned()
    {
        gameOverManager = FindFirstObjectByType<GameOverManager>();
    }

    public void CheckForGameEnd()
    {
        if (!HasStateAuthority) return;

        //Buscar todos los jugadores vivos
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        int alivePlayers = 0;
        string lastAlivePlayer = "";

        foreach (var player in allPlayers)
        {
            if (player.CurrentHealth > 0)
            {
                alivePlayers++;
                lastAlivePlayer = $"Jugador {player.Object.InputAuthority.PlayerId}";
            }
        }

        
        if (alivePlayers == 1)
        {
            GameOverManager.TriggerGameOver(lastAlivePlayer);
        }
        else if (alivePlayers == 0)
        {
            GameOverManager.TriggerGameOver("NINGÚN JUGADOR");
        }
    }
}