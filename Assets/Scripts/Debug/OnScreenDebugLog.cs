using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Muestra los logs de Unity en pantalla - útil para testing en builds
/// Presiona F1 para mostrar/ocultar
/// </summary>
public class OnScreenDebugLog : MonoBehaviour
{
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private int maxMessages = 10;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private Queue<string> messages = new Queue<string>();
    private bool showLog = true;
    private Vector2 scrollPosition;

    void Awake()
    {
        showLog = showOnStart;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showLog = !showLog;
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Filtrar solo logs importantes
        if (logString.Contains("[NetworkPlayer]") || 
            logString.Contains("[NetworkConnectionHandler]") ||
            logString.Contains("[FusionInputProvider]") ||
            type == LogType.Error || 
            type == LogType.Warning)
        {
            string message = $"[{type}] {logString}";
            messages.Enqueue(message);

            if (messages.Count > maxMessages)
            {
                messages.Dequeue();
            }
        }
    }

    void OnGUI()
    {
        if (!showLog) return;

        // Estilo de fondo
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.8f));

        // Área de logs
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 300), boxStyle);
        GUILayout.Label($"<b>Debug Console (Press {toggleKey} to hide)</b>", new GUIStyle(GUI.skin.label) { richText = true });
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(260));
        
        foreach (string message in messages)
        {
            Color color = Color.white;
            if (message.Contains("Error")) color = Color.red;
            else if (message.Contains("Warning")) color = Color.yellow;
            else if (message.Contains("LOCAL")) color = Color.green;
            else if (message.Contains("REMOTE")) color = Color.cyan;

            GUI.color = color;
            GUILayout.Label(message);
        }
        
        GUI.color = Color.white;
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
