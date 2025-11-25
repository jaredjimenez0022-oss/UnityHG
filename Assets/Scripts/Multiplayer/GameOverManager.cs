using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class GameOverManager : NetworkBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject gameOverCanvasPrefab;
    [SerializeField] private GameObject eliminationCanvasPrefab;

    private GameObject gameOverPanel;
    private GameObject eliminationPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI winnerNameText;
    //private TextMeshProUGUI eliminationText;
    private Button mainMenuButton;
    private Button eliminationMenuButton;
    private GameObject canvasInstance;

    [Header("Visual Settings")]
    [SerializeField] private Color victoryColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color drawColor = Color.white;
    [SerializeField] private Color eliminationColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float fadeInDuration = 1.5f;

    [Networked] private bool gameEnded { get; set; }
    private CanvasGroup canvasGroup;
    private bool screenActive = false;
    private Coroutine cursorForcerCoroutine;
    private EventSystem eventSystem;
    
    // Flag local para saber qué tipo de pantalla mostrar
    private bool isEliminated = false;

    public override void Spawned()
    {
        if (canvasInstance == null)
        {
            CreateGameOverUI();
        }
    }

    private void CreateGameOverUI()
    {
        if (gameOverCanvasPrefab != null && canvasInstance == null)
        {
            // Crear canvas principal
            canvasInstance = Instantiate(gameOverCanvasPrefab);

            Canvas canvas = canvasInstance.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 9999;
            }

            // Configurar panel de victoria/empate
            gameOverPanel = FindComponent<RectTransform>(canvasInstance, "GameOverPanel")?.gameObject;
            titleText = FindComponent<TextMeshProUGUI>(canvasInstance, "TitleText");
            winnerNameText = FindComponent<TextMeshProUGUI>(canvasInstance, "WinnerName");
            mainMenuButton = FindComponent<Button>(canvasInstance, "MainMenuButton");

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
                SetupButtonEffects(mainMenuButton);
            }

            // Configurar panel de eliminación (duplicado del original)
            SetupEliminationPanel();

            canvasGroup = canvasInstance.GetComponent<CanvasGroup>();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (eliminationPanel != null)
            {
                eliminationPanel.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
    }

    private void SetupEliminationPanel()
    {
        // Si no hay canvas duplicamos el de gameOver
        if (eliminationCanvasPrefab != null)
        {
            GameObject elimInstance = Instantiate(eliminationCanvasPrefab, canvasInstance.transform);
            eliminationPanel = elimInstance;
        }
        else if (gameOverPanel != null)
        {
            // Duplicar el panel existente y modificarlo
            eliminationPanel = Instantiate(gameOverPanel, gameOverPanel.transform.parent);
            eliminationPanel.name = "EliminationPanel";
        }

        if (eliminationPanel != null)
        {

            eliminationMenuButton = FindComponent<Button>(eliminationPanel, "MainMenuButton");
            if (eliminationMenuButton != null)
            {
                eliminationMenuButton.onClick.RemoveAllListeners();
                eliminationMenuButton.onClick.AddListener(ReturnToMainMenu);
                SetupButtonEffects(eliminationMenuButton);
            }

            eliminationPanel.SetActive(false);
        }
    }

    private void SetupButtonEffects(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
        colors.pressedColor = new Color(0.1f, 0.5f, 0.9f);
        colors.selectedColor = new Color(0.2f, 0.6f, 1f);
        button.colors = colors;

        if (!button.gameObject.GetComponent<ButtonScaleEffect>())
        {
            button.gameObject.AddComponent<ButtonScaleEffect>();
        }
    }

    private T FindComponent<T>(GameObject parent, string name) where T : Component
    {
        if (parent == null) return null;

        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
                return child.GetComponent<T>();
            
            T found = FindComponent<T>(child.gameObject, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // Método para cuando el jugador LOCAL es eliminado
    public void ShowEliminationScreen()
    {
        if (screenActive) return;
        
        isEliminated = true;
        ShowLocalScreen(true);
    }

    // Método para cuando termina el juego (solo lo llama el host)
    public void EndGame(string winnerName)
    {
        if (!Object.HasStateAuthority) return;
        if (gameEnded) return;
        
        gameEnded = true;
        RPC_ShowGameOver(winnerName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGameOver(string winnerName)
    {
        Debug.Log("RPC_ShowGameOver recibido: " + winnerName);
        // Solo mostrar si el jugador no está ya eliminado
        if (!isEliminated)
        {
            ShowGameOverPanel(winnerName);
        }
    }

    private void ShowGameOverPanel(string winnerInfo)
    {
        Debug.Log("ShowGameOverPanel llamado: " + winnerInfo);
        ShowLocalScreen(false, winnerInfo);
    }

    // Método para mostrar cualquier pantalla
    private void ShowLocalScreen(bool isEliminationScreen, string winnerInfo = "")
    {
        if (screenActive) return;
        screenActive = true;

        if (canvasInstance == null)
        {
            CreateGameOverUI();
        }

        // Notificar al PauseMenuController que deje de controlar el cursor
        DisablePauseMenuCursorControl();

        CreateEventSystem();
        StartAbsoluteCursorControl();

        if (isEliminationScreen)
        {
            // Mostrar pantalla de eliminación
            if (eliminationPanel != null)
            {
                eliminationPanel.SetActive(true);
            }
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }
        else
        {
            // Mostrar pantalla de victoria/empate
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                UpdateUIElements(winnerInfo);
            }
            if (eliminationPanel != null)
            {
                eliminationPanel.SetActive(false);
            }
        }

        StartCoroutine(FadeInCanvas());
        DisableLocalGameSystems();
    }

    private void CreateEventSystem()
    {
        eventSystem = FindFirstObjectByType<EventSystem>();
        
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("GameOverEventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            
            DontDestroyOnLoad(eventSystemGO);
        }

        EventSystem.current = eventSystem;
    }

    private void StartAbsoluteCursorControl()
    {
        if (cursorForcerCoroutine != null)
        {
            StopCoroutine(cursorForcerCoroutine);
        }

        cursorForcerCoroutine = StartCoroutine(ForceCursorPermanently());
    }

    private IEnumerator ForceCursorPermanently()
    {
        while (screenActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (eventSystem != null)
            {
                EventSystem.current = eventSystem;
            }

            yield return null; // Cambiar a null para ejecutar cada frame, más rápido que WaitForEndOfFrame
        }
    }

    private void DisablePauseMenuCursorControl()
    {
        // Buscar y deshabilitar temporalmente el control del cursor del PauseMenuController
        PauseMenuController pauseMenu = FindFirstObjectByType<PauseMenuController>();
        if (pauseMenu != null)
        {
            pauseMenu.ForceCursorForGameOver();
        }
    }

    private void DisableLocalGameSystems()
    {
        // Deshabilitar input de pausa PRIMERO para evitar que interfiera
        PauseMenuController pauseMenu = FindFirstObjectByType<PauseMenuController>();
        if (pauseMenu != null)
        {
            pauseMenu.enabled = false; // Deshabilitar completamente el componente
        }

        // Solo deshabilitar los sistemas del jugador LOCAL
        var localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            var playerMovement = localPlayer.GetComponent<NetworkPlayer>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            var playerCombat = localPlayer.GetComponent<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.enabled = false;
            }
        }

        // Deshabilitar input provider local
        var fusionInputProvider = FindFirstObjectByType<FusionInputProvider>();
        if (fusionInputProvider != null)
        {
            fusionInputProvider.SetInputEnabled(false);
        }

        UnityEngine.Input.ResetInputAxes();
    }

    private GameObject FindLocalPlayer()
    {
        // Buscar el jugador que pertenece a este cliente
        var allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                return player.gameObject;
            }
        }
        return null;
    }

    private void UpdateUIElements(string winnerInfo)
    {
        bool isDraw = winnerInfo == "EMPATE - NINGÚN JUGADOR" || winnerInfo == "NINGÚN JUGADOR";

        if (titleText != null)
        {
            titleText.text = isDraw ? "¡EMPATE!" : "¡VICTORIA!";
            titleText.color = isDraw ? drawColor : victoryColor;
        }

        if (winnerNameText != null)
        {
            winnerNameText.text = isDraw ? "Ningún jugador sobrevivió" : winnerInfo;
            winnerNameText.color = isDraw ? drawColor : victoryColor;
            
            if (!isDraw)
            {
                winnerNameText.fontSize = 42;
                winnerNameText.fontStyle = FontStyles.Bold;
            }
        }
    }

    private IEnumerator FadeInCanvas()
    {
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }

    private void ReturnToMainMenu()
    {
        screenActive = false;
        if (cursorForcerCoroutine != null)
        {
            StopCoroutine(cursorForcerCoroutine);
            cursorForcerCoroutine = null;
        }

        // Destruir el canvas LOCAL antes de desconectar
        if (canvasInstance != null)
        {
            Destroy(canvasInstance);
            canvasInstance = null;
        }

        // Desconectar solo ESTE cliente
        NetworkConnectionHandler connectionHandler = FindFirstObjectByType<NetworkConnectionHandler>();
        if (connectionHandler != null)
        {
            connectionHandler.Disconnect();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnDestroy()
    {
        screenActive = false;
        if (cursorForcerCoroutine != null)
        {
            StopCoroutine(cursorForcerCoroutine);
        }
        
        if (canvasInstance != null)
        {
            Destroy(canvasInstance);
        }
    }
}