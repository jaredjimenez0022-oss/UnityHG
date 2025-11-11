using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.UI;

public class GameOverManager : NetworkBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private GameObject gameOverCanvasPrefab;

    private GameObject gameOverPanel;
    private TextMeshProUGUI winnerText;
    private TextMeshProUGUI eliminationLog;
    private Button mainMenuButton;
    private GameObject canvasInstance;

    [Networked] private bool gameEnded { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority && canvasInstance == null)
        {
            CreateGameOverUI();
        }
    }

    private void CreateGameOverUI()
    {
        if (gameOverCanvasPrefab != null && canvasInstance == null)
        {
            canvasInstance = Instantiate(gameOverCanvasPrefab);
            DontDestroyOnLoad(canvasInstance);

            gameOverPanel = FindComponent<RectTransform>(canvasInstance, "GameOverPanel")?.gameObject;
            winnerText = FindComponent<TextMeshProUGUI>(canvasInstance, "WinnerText");
            eliminationLog = FindComponent<TextMeshProUGUI>(canvasInstance, "EliminationLog");
            mainMenuButton = FindComponent<Button>(canvasInstance, "MainMenuButton");

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
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

        if (canvasInstance == null && gameOverCanvasPrefab != null)
        {
            CreateGameOverUI();
        }

        if (winnerText != null)
            {
                if (winnerInfo == "NINGÚN JUGADOR")
                {
                    winnerText.text = "¡EMPATE!\n\nNingún jugador sobrevivió";
                    winnerText.color = Color.white;
                }
                else
                {
                    winnerText.text = $"¡VICTORIA!\n\n{winnerInfo}";
                    winnerText.color = Color.yellow;
                }
            }
        
        UnlockCursor();
    }

    public void AddEliminationMessage(string message)
    {
        if (eliminationLog != null)
        {
            eliminationLog.text += $"\n{message}";
        }
    }
    
    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Cursor liberado");
    }

    private void ReturnToMainMenu()
    {
        Debug.Log("Volviendo al menú principal");
        if (canvasInstance != null)
            Destroy(canvasInstance);
            
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public static void TriggerGameOver(string winnerName)
    {
        Debug.Log("TriggerGameOver llamado: " + winnerName);
        GameOverManager manager = FindFirstObjectByType<GameOverManager>();
        if (manager != null)
        {
            manager.EndGame(winnerName);
        }
        else
        {
            Debug.LogError("No se encontró GameOverManager en la escena");
        }
    }

    private void OnDestroy()
    {
        if (canvasInstance != null)
            Destroy(canvasInstance);
    }
}