using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    [SerializeField] private string sessionName = "MiSalaDeJuego";
    [SerializeField] private int maxPlayers = 10;

    [Header("UI References")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

    private void Awake()
    {
        // Asegurarse de que solo haya una instancia
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UpdateStatusText("Listo para conectar");
    }

    #region Public Methods - Llamados desde Botones UI

    /// <summary>
    /// Inicia el juego como HOST (servidor + cliente)
    /// El host puede jugar Y controla la sesión
    /// </summary>
    public async void StartAsHost()
    {
        Debug.Log("=== INICIANDO COMO HOST ===");
        UpdateStatusText("Iniciando como HOST...");
        ShowLoading(true);
        await StartGame(GameMode.Host);
    }

    /// <summary>
    /// Inicia el juego como CLIENT (solo cliente)
    /// Se conecta a una sesión existente
    /// </summary>
    public async void StartAsClient()
    {
        Debug.Log("=== INICIANDO COMO CLIENT ===");
        UpdateStatusText("Buscando sesión...");
        ShowLoading(true);
        await StartGame(GameMode.Client);
    }

    /// <summary>
    /// Desconectar y volver al lobby
    /// </summary>
    public async void Disconnect()
    {
        if (networkRunner != null)
        {
            Debug.Log("=== DESCONECTANDO ===");
            UpdateStatusText("Desconectando...");
            await networkRunner.Shutdown();
            networkRunner = null;
        }
        
        ShowLoading(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        UpdateStatusText("Desconectado");
    }

    #endregion

    #region Game Start Logic

    private async Task StartGame(GameMode mode)
    {
        // Crear el NetworkRunner
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
            networkRunner.name = "NetworkRunner";
        }

        // El runner provee input solo si es Host o Client
        networkRunner.ProvideInput = (mode == GameMode.Host || mode == GameMode.Client);

        // Obtener la escena actual
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        Debug.Log($"Configurando juego - Modo: {mode}, Escena: {sceneIndex}, Sesión: {sessionName}");

        // Configurar los parámetros de inicio
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(sceneIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers
        };

        // Iniciar el juego
        UpdateStatusText($"Conectando como {mode}...");
        var result = await networkRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"✓ Juego iniciado correctamente como {mode}");
            UpdateStatusText($"Conectado como {mode}");
            ShowLoading(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
        }
        else
        {
            Debug.LogError($"✗ Error al iniciar el juego: {result.ShutdownReason}");
            UpdateStatusText($"Error: {result.ShutdownReason}");
            ShowLoading(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
        }
    }

    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[Status] {message}");
    }

    #endregion

    #region INetworkRunnerCallbacks - Eventos de Red

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"✓ Jugador {player.PlayerId} se unió a la sesión");
        UpdateStatusText($"Jugador {player.PlayerId} entró");

        // Solo el servidor/host spawna jugadores
        if (runner.IsServer && playerPrefab != null)
        {
            // Calcular posición de spawn
            Vector3 spawnPosition = GetSpawnPosition(player.PlayerId);
            
            Debug.Log($"Spawneando jugador {player.PlayerId} en posición {spawnPosition}");
            
            // Spawn del jugador
            NetworkObject networkPlayer = runner.Spawn(
                playerPrefab, 
                spawnPosition, 
                Quaternion.identity, 
                player
            );
            
            Debug.Log($"✓ Jugador spawneado: {networkPlayer.name}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"✗ Jugador {player.PlayerId} dejó la sesión");
        UpdateStatusText($"Jugador {player.PlayerId} salió");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("✓ Conectado al servidor exitosamente");
        UpdateStatusText("Conectado al servidor");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"✗ Desconectado del servidor. Razón: {reason}");
        UpdateStatusText($"Desconectado: {reason}");
        ShowLoading(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"✗ Falló la conexión. Razón: {reason}");
        UpdateStatusText($"Conexión fallida: {reason}");
        ShowLoading(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"NetworkRunner apagado. Razón: {shutdownReason}");
        UpdateStatusText("Desconectado");
        networkRunner = null;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // El input se maneja en el script del jugador
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
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
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

    #endregion
}