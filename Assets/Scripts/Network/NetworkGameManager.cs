using UnityEngine;
using Fusion;

/// <summary>
/// Game manager que se spawnea en la red y persiste durante toda la partida.
/// Contiene sistemas compartidos como el contador de jugadores vivos.
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("Components")]
    [SerializeField] private AlivePlayersCounter playersCounter;

    public override void Spawned()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[NetworkGameManager] Game Manager spawned");
        }
        else
        {
            Debug.LogWarning("[NetworkGameManager] Duplicate GameManager detected! Destroying...");
            Runner.Despawn(Object);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public AlivePlayersCounter GetPlayersCounter()
    {
        return playersCounter;
    }
}
