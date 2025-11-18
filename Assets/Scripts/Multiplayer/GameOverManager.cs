using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class GameOverManager : NetworkBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private GameObject gameOverCanvasPrefab;

    private GameObject gameOverPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI winnerNameText;
    private Button mainMenuButton;
    private GameObject canvasInstance;

    [Header("Visual Settings")]
    [SerializeField] private Color victoryColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color drawColor = Color.white;
    [SerializeField] private float fadeInDuration = 1.5f;

    [Networked] private bool gameEnded { get; set; }
    private CanvasGroup canvasGroup;
    private bool gameOverActive = false;
    private Coroutine cursorForcerCoroutine;
    private EventSystem eventSystem;

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
            canvasInstance = Instantiate(gameOverCanvasPrefab);

            Canvas canvas = canvasInstance.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 9999;
            }

            gameOverPanel = FindComponent<RectTransform>(canvasInstance, "GameOverPanel")?.gameObject;
            titleText = FindComponent<TextMeshProUGUI>(canvasInstance, "TitleText");
            winnerNameText = FindComponent<TextMeshProUGUI>(canvasInstance, "WinnerName");
            mainMenuButton = FindComponent<Button>(canvasInstance, "MainMenuButton");
            canvasGroup = canvasInstance.GetComponent<CanvasGroup>();

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
                SetupButtonEffects(mainMenuButton);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

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

        button.gameObject.AddComponent<ButtonScaleEffect>();
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

    public void EndGame(string winnerName)
    {
        if (gameEnded) return;
        
        gameEnded = true;
        RPC_ShowGameOver(winnerName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGameOver(string winnerName)
    {
        Debug.Log("RPC_ShowGameOver recibido: " + winnerName);
        ShowGameOverPanel(winnerName);
    }

    private void ShowGameOverPanel(string winnerInfo)
    {
        Debug.Log("ShowGameOverPanel llamado: " + winnerInfo);

        if (gameOverActive) return;
        gameOverActive = true;

        if (canvasInstance == null)
        {
            CreateGameOverUI();
        }

        if (gameOverPanel != null)
        {
            CreateEventSystem();
            StartAbsoluteCursorControl();

            gameOverPanel.SetActive(true);
            StartCoroutine(FadeInCanvas());
            UpdateUIElements(winnerInfo);

            DisableAllGameSystems();
        }
    }

    private void CreateEventSystem()
    {
        eventSystem = FindFirstObjectByType<EventSystem>();
        
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("GameOverEventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            
            eventSystemGO.AddComponent<StandaloneInputModule>();
            eventSystemGO.AddComponent<BaseInput>();
            
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

        while (gameOverActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (eventSystem != null)
            {
                EventSystem.current = eventSystem;
            }

            yield return new WaitForEndOfFrame();
        }

    }

    private void DisableAllGameSystems()
    {

        var fusionInputProvider = FindFirstObjectByType<FusionInputProvider>();
        if (fusionInputProvider != null)
        {
            fusionInputProvider.SetInputEnabled(false);
        }

        var playerMovement = FindFirstObjectByType<NetworkPlayer>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        var playerCombat = FindFirstObjectByType<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
        }

        UnityEngine.Input.ResetInputAxes();
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
        gameOverActive = false;
        if (cursorForcerCoroutine != null)
        {
            StopCoroutine(cursorForcerCoroutine);
            cursorForcerCoroutine = null;
        }

        NetworkConnectionHandler connectionHandler = FindFirstObjectByType<NetworkConnectionHandler>();
        if (connectionHandler != null)
        {
            connectionHandler.Disconnect();
        }
        else
        {
            if (canvasInstance != null)
                Destroy(canvasInstance);
                
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnDestroy()
    {
        gameOverActive = false;
        if (cursorForcerCoroutine != null)
        {
            StopCoroutine(cursorForcerCoroutine);
        }
        
        if (canvasInstance != null)
            Destroy(canvasInstance);
    }
}