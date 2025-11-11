using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;

public class NetworkConnectionHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Runner")]
    private NetworkRunner networkRunner;

    [Header("Player Settings")]
    [SerializeField] private NetworkPrefabRef playerPrefab;

    [Header("Session Settings")]
    [Tooltip("Default Photon Fusion session name used when hosting without a custom room code.")]
    [SerializeField] private string sessionName = "HungerGamesRoom";
    [SerializeField] private int maxPlayers = 12;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";  // Scene to load when game starts

    // Player tracking
    private readonly Dictionary<PlayerRef, string> connectedPlayers = new Dictionary<PlayerRef, string>();
    private PlayerRef hostPlayer; // Track who is the host

    // Callbacks for UI updates
    public Action<List<string>> OnPlayerListUpdated;
    public Action OnConnectionSuccess;
    public Action<string> OnConnectionFailed;

    private const string PlayerNameMessagePrefix = "NAME|";
    private const string PlayerListMessagePrefix = "LIST|";

    private void Awake()
    {
        // Asegurarse de que solo haya una instancia
        DontDestroyOnLoad(gameObject);
    }

    #region Public Methods - Llamados desde Botones UI

    /// <summary>
    /// Connect to lobby as HOST (stays in MainMenu scene)
    /// </summary>
    public async void StartAsHost(string roomCode = null)
    {
        // Use custom room code if provided, otherwise use default
        if (!string.IsNullOrEmpty(roomCode))
        {
            sessionName = roomCode;
        }

        await ConnectToLobby(GameMode.Host);
    }

    /// <summary>
    /// Connect to lobby as CLIENT (joins existing session in MainMenu)
    /// </summary>
    public async void StartAsClient(string roomCode)
    {
        if (string.IsNullOrEmpty(roomCode))
        {
            OnConnectionFailed?.Invoke("Room code is required");
            return;
        }

        sessionName = roomCode;
        await ConnectToLobby(GameMode.Client);
    }

    /// <summary>
    /// Load the game scene (only callable by host after lobby is ready)
    /// </summary>
    public async void LoadGameScene()
    {
        if (networkRunner == null || !networkRunner.IsRunning)
        {
            return;
        }

        if (!IsHost())
        {
            return;
        }

        int gameSceneIndex = GetSceneIndex(gameSceneName);
        if (gameSceneIndex == -1)
        {
            return;
        }

        // Load the game scene for all connected players
        await networkRunner.LoadScene(SceneRef.FromIndex(gameSceneIndex));
    }

    /// <summary>
    /// Desconectar y volver al menu
    /// </summary>
    public async void Disconnect()
    {
        if (networkRunner != null)
        {
            await networkRunner.Shutdown();
            networkRunner = null;
        }

        ResetPlayerTracking();

        // Only load MainMenu if we're not already there
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// Get list of connected player names
    /// </summary>
    public List<string> GetConnectedPlayers()
    {
        return connectedPlayers.Values.ToList();
    }

    #endregion

    #region Connection Logic

    /// <summary>
    /// Connect to lobby (stays in MainMenu scene)
    /// </summary>
    private async Task ConnectToLobby(GameMode mode)
    {
        // Ensure we start from a clean slate
        ResetPlayerTracking();

        // Crear el NetworkRunner
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
            networkRunner.name = "NetworkRunner";
        }

        // El runner provee input solo si es Host o Client
        networkRunner.ProvideInput = (mode == GameMode.Host || mode == GameMode.Client);

        // Get MainMenu scene index (stay in lobby)
        int mainMenuSceneIndex = GetSceneIndex("MainMenu");

        if (mainMenuSceneIndex == -1)
        {
            OnConnectionFailed?.Invoke("MainMenu scene not configured");
            return;
        }

        // Configurar los parámetros de inicio (stay in MainMenu)
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(mainMenuSceneIndex),  // Stay in MainMenu
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers
        };

        // Iniciar la conexión
        var result = await networkRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            // Add local player to the list
            string localPlayerName = PlayerPrefs.GetString("PlayerName", "Player");
            if (networkRunner.LocalPlayer != PlayerRef.None)
            {
                connectedPlayers[networkRunner.LocalPlayer] = localPlayerName;

                // Track host player
                if (mode == GameMode.Host)
                {
                    hostPlayer = networkRunner.LocalPlayer;
                }
            }

            // Add any existing players (for clients joining an existing session)
            foreach (var player in networkRunner.ActivePlayers)
            {
                if (!connectedPlayers.ContainsKey(player))
                {
                    string playerName = $"Player {player.PlayerId}";
                    connectedPlayers[player] = playerName;

                    // Identify host (player with lowest ID)
                    if (hostPlayer == PlayerRef.None || player.PlayerId < hostPlayer.PlayerId)
                    {
                        hostPlayer = player;
                    }
                }
            }

            OnConnectionSuccess?.Invoke();
            UpdatePlayerListUI();

            if (mode == GameMode.Client)
            {
                SendLocalPlayerNameToServer(localPlayerName);
            }
            else
            {
                BroadcastPlayerList();
            }
        }
        else
        {
            ResetPlayerTracking();
            OnConnectionFailed?.Invoke($"Failed to connect: {result.ShutdownReason}");
        }
    }

    private void UpdatePlayerListUI()
    {
        EnsureHostPlayer();

        // Build player list with host always first
        List<string> playerNames = new List<string>();

        // Add host first
        if (hostPlayer != PlayerRef.None && connectedPlayers.ContainsKey(hostPlayer))
        {
            playerNames.Add(connectedPlayers[hostPlayer]);
        }

        // Add other players
        foreach (var kvp in connectedPlayers)
        {
            if (!kvp.Key.Equals(hostPlayer))
            {
                playerNames.Add(kvp.Value);
            }
        }

        OnPlayerListUpdated?.Invoke(playerNames);
    }

    /// <summary>
    /// Check if we're fully connected and ready to start
    /// </summary>
    public bool IsConnected()
    {
        return networkRunner != null && networkRunner.IsRunning;
    }

    /// <summary>
    /// Check if this player is the host
    /// </summary>
    public bool IsHost()
    {
        return networkRunner != null && networkRunner.IsServer;
    }

    private int GetSceneIndex(string sceneName)
    {
        // Find scene index in build settings by name
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneNameFromPath == sceneName)
            {
                return i;
            }
        }
        return -1;  // Not found
    }

    #endregion

    #region INetworkRunnerCallbacks - Eventos de Red

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Check if we're in the game scene or lobby
        bool inGameScene = SceneManager.GetActiveScene().name == gameSceneName;

        if (inGameScene)
        {
            // In game scene: spawn player
            if (runner.IsServer && playerPrefab != null)
            {
                Vector3 spawnPosition = GetSpawnPosition(player.PlayerId);

                NetworkObject spawnedPlayer = runner.Spawn(
                    playerPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    player
                );

                runner.SetPlayerObject(player, spawnedPlayer);

                if (GameStateManager.Instance != null)
                {
                    Debug.Log($"Jugador {player.PlayerId} unido - registrando en GameStateManager");
                    GameStateManager.Instance.RegisterPlayer(player);
                }
            }
        }
        else
        {
            // In lobby: track player and update UI
            string playerName = $"Player {player.PlayerId}";

            if (!connectedPlayers.ContainsKey(player))
            {
                connectedPlayers[player] = playerName;

                // Identify the host (first player to join, or the server)
                if (hostPlayer == PlayerRef.None || player.PlayerId < hostPlayer.PlayerId)
                {
                    hostPlayer = player;
                }

                UpdatePlayerListUI();

                if (runner.IsServer)
                {
                    SendPlayerListToPlayer(player);
                    BroadcastPlayerList();
                }
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Remove from tracking
        if (connectedPlayers.ContainsKey(player))
        {
            connectedPlayers.Remove(player);
            EnsureHostPlayer();
            UpdatePlayerListUI();

            if (runner.IsServer)
            {
                BroadcastPlayerList();
            }
        }

        if (runner.IsServer && GameStateManager.Instance != null)
        {
            Debug.Log($"Jugador {player.PlayerId} abandonó - notificando GameStateManager");
            GameStateManager.Instance.OnPlayerAbandoned(player);
        }

        // Despawn player object if we're in a gameplay scene
        if (runner.TryGetPlayerObject(player, out var playerObject) && playerObject != null)
        {
            if (runner.IsServer)
            {
                runner.SetPlayerObject(player, null);
            }

            runner.Despawn(playerObject);
        }
    }

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        ResetPlayerTracking();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        OnConnectionFailed?.Invoke($"Connection failed: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        networkRunner = null;
        ResetPlayerTracking();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // El input se maneja en FusionInputProvider
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Aceptar todas las conexiones
        request.Accept();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (data.Count <= 0)
        {
            return;
        }

        string message = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);

        if (message.StartsWith(PlayerNameMessagePrefix, StringComparison.Ordinal))
        {
            if (!runner.IsServer)
            {
                return;
            }

            string encodedName = message.Substring(PlayerNameMessagePrefix.Length);
            string decodedName = DecodeName(encodedName);
            ApplyPlayerName(player, decodedName);
            BroadcastPlayerList();
        }
        else if (message.StartsWith(PlayerListMessagePrefix, StringComparison.Ordinal))
        {
            if (runner.IsServer)
            {
                return;
            }

            string payload = message.Substring(PlayerListMessagePrefix.Length);
            ApplyPlayerListFromHost(payload);
        }
    }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        bool inGameScene = SceneManager.GetActiveScene().name == gameSceneName;

        if (!inGameScene || !runner.IsServer)
        {
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogWarning("[NetworkConnectionHandler] Player prefab not assigned; cannot spawn players.");
            return;
        }

        foreach (var player in runner.ActivePlayers)
        {
            if (runner.TryGetPlayerObject(player, out var existingObject) && existingObject != null)
            {
                continue;
            }

            Vector3 spawnPosition = GetSpawnPosition(player.PlayerId);
            NetworkObject spawnedPlayer = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
            runner.SetPlayerObject(player, spawnedPlayer);
        }
    }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    #endregion

    #region Helper Methods

    private Vector3 GetSpawnPosition(int playerId)
    {
        // Spawns en círculo alrededor del origen
        float angle = playerId * (360f / maxPlayers);
        float radius = 5f;

        float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
        float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

        return new Vector3(x, 1f, z);
    }

    private void SendLocalPlayerNameToServer(string playerName)
    {
        if (networkRunner == null || !networkRunner.IsRunning || networkRunner.IsServer)
        {
            return;
        }

        if (string.IsNullOrEmpty(playerName))
        {
            playerName = $"Player {networkRunner.LocalPlayer.PlayerId}";
        }

        string message = $"{PlayerNameMessagePrefix}{EncodeName(playerName)}";
        byte[] payload = Encoding.UTF8.GetBytes(message);

        networkRunner.SendReliableDataToServer(default, payload);
    }

    private void BroadcastPlayerList()
    {
        if (networkRunner == null || !networkRunner.IsRunning || !networkRunner.IsServer)
        {
            return;
        }

        string message = BuildPlayerListPayload();
        byte[] payload = Encoding.UTF8.GetBytes(message);

        foreach (var player in networkRunner.ActivePlayers)
        {
            if (player == networkRunner.LocalPlayer)
            {
                continue;
            }

            networkRunner.SendReliableDataToPlayer(player, default, payload);
        }
    }

    private void SendPlayerListToPlayer(PlayerRef target)
    {
        if (networkRunner == null || !networkRunner.IsRunning || !networkRunner.IsServer)
        {
            return;
        }

        string message = BuildPlayerListPayload();
        byte[] payload = Encoding.UTF8.GetBytes(message);
        networkRunner.SendReliableDataToPlayer(target, default, payload);
    }

    private string BuildPlayerListPayload()
    {
        EnsureHostPlayer();

        if (networkRunner == null)
        {
            return PlayerListMessagePrefix;
        }

        var orderedPlayers = networkRunner.ActivePlayers.OrderBy(p => p.PlayerId).ToList();
        List<string> entries = new List<string>(orderedPlayers.Count);

        foreach (var player in orderedPlayers)
        {
            string name = connectedPlayers.TryGetValue(player, out var playerName)
                ? playerName
                : $"Player {player.PlayerId}";

            entries.Add($"{player.PlayerId}:{EncodeName(name)}");
        }

        return $"{PlayerListMessagePrefix}{string.Join("|", entries)}";
    }

    private void ApplyPlayerListFromHost(string payload)
    {
        if (networkRunner == null)
        {
            return;
        }

        connectedPlayers.Clear();

        if (!string.IsNullOrEmpty(payload))
        {
            string[] entries = payload.Split('|', StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in entries)
            {
                string[] parts = entry.Split(':');
                if (parts.Length != 2)
                {
                    continue;
                }

                if (!int.TryParse(parts[0], out int playerId))
                {
                    continue;
                }

                PlayerRef matchingPlayer = networkRunner.ActivePlayers.FirstOrDefault(p => p.PlayerId == playerId);
                if (matchingPlayer == PlayerRef.None)
                {
                    continue;
                }

                string playerName = DecodeName(parts[1]);
                connectedPlayers[matchingPlayer] = playerName;
            }
        }

        EnsureHostPlayer();
        UpdatePlayerListUI();
    }

    private void ApplyPlayerName(PlayerRef player, string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = $"Player {player.PlayerId}";
        }

        connectedPlayers[player] = playerName;
        EnsureHostPlayer();
        UpdatePlayerListUI();
    }

    private static string EncodeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string DecodeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            byte[] raw = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(raw);
        }
        catch
        {
            return value;
        }
    }

    private void EnsureHostPlayer()
    {
        if (connectedPlayers.Count == 0)
        {
            hostPlayer = PlayerRef.None;
            return;
        }

        if (hostPlayer == PlayerRef.None || !connectedPlayers.ContainsKey(hostPlayer))
        {
            hostPlayer = connectedPlayers.Keys.OrderBy(p => p.PlayerId).First();
        }
    }

    private void ResetPlayerTracking()
    {
        connectedPlayers.Clear();
        hostPlayer = PlayerRef.None;
        OnPlayerListUpdated?.Invoke(new List<string>());
    }

    #endregion
}
