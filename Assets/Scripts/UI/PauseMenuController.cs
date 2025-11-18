using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Controls the in-game pause menu opened with ESC key.
/// Handles Resume, Settings, and Leave Game functionality.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    private VisualElement root;
    private VisualElement pauseMenu;
    private VisualElement settingsPanel;

    // Pause menu buttons
    private Button resumeButton;
    private Button settingsButton;
    private Button leaveButton;
    private Button closeSettingsButton;

    // Settings UI elements
    private DropdownField resolutionDropdown;
    private DropdownField framerateDropdown;
    private Toggle fullscreenToggle;
    private Toggle fpsCounterToggle;
    private Slider masterVolumeSlider;
    private Slider mouseSensitivitySlider;
    private Slider fovSlider;
    private Label masterVolumeValue;
    private Label mouseSensitivityValue;
    private Label fovValue;

    private GameSettings gameSettings;
    private bool isPaused;
    private NetworkConnectionHandler networkHandler;
    private FusionInputProvider fusionInputProvider;

    // Cursor management
    private CursorLockMode targetCursorLock = CursorLockMode.Locked;
    private bool targetCursorVisible;
    private bool cursorTargetDirty;

    public static bool IsPaused { get; private set; }
    public static event Action<bool> PauseStateChanged;

    private void Awake()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Find pause menu elements
        pauseMenu = root.Q<VisualElement>("pause-menu");
        settingsPanel = root.Q<VisualElement>("pause-settings-panel");

        resumeButton = root.Q<Button>("resume-button");
        settingsButton = root.Q<Button>("pause-settings-button");
        leaveButton = root.Q<Button>("leave-button");
        closeSettingsButton = root.Q<Button>("close-pause-settings-button");

        // Find settings elements
        resolutionDropdown = root.Q<DropdownField>("pause-resolution-dropdown");
        framerateDropdown = root.Q<DropdownField>("pause-framerate-dropdown");
        fullscreenToggle = root.Q<Toggle>("pause-fullscreen-toggle");
        fpsCounterToggle = root.Q<Toggle>("pause-fps-counter-toggle");
        masterVolumeSlider = root.Q<Slider>("pause-master-volume-slider");
        mouseSensitivitySlider = root.Q<Slider>("pause-mouse-sensitivity-slider");
        fovSlider = root.Q<Slider>("pause-fov-slider");
        masterVolumeValue = root.Q<Label>("pause-master-volume-value");
        mouseSensitivityValue = root.Q<Label>("pause-mouse-sensitivity-value");
        fovValue = root.Q<Label>("pause-fov-value");

        // Find GameSettings
        gameSettings = FindFirstObjectByType<GameSettings>();

        // Find network handler and input provider
        networkHandler = FindFirstObjectByType<NetworkConnectionHandler>();
        fusionInputProvider = FindFirstObjectByType<FusionInputProvider>();

        // Setup button events
        SetupEvents();

        // Initialize settings panel
        InitializeSettings();

        // Hide menu by default and ensure cursor is locked for gameplay
        pauseMenu.style.display = DisplayStyle.None;
        settingsPanel.style.display = DisplayStyle.None;

        isPaused = false;
        IsPaused = false;

        RefreshCursorTarget();
        ApplyCursorState();
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
        }

        if (isPaused)
        {
            SetPaused(false);
        }

        PauseStateChanged = null;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }

        if (isPaused)
        {
            if (settingsPanel.style.display == DisplayStyle.Flex)
            {
                CloseSettings();
            }
            else
            {
                SetPaused(false);
            }
        }
        else
        {
            SetPaused(true);
        }
    }

    private void SetupEvents()
    {
        resumeButton.clicked += () => SetPaused(false);
        settingsButton.clicked += OpenSettings;
        leaveButton.clicked += LeaveGame;
        closeSettingsButton.clicked += CloseSettings;

        // Settings events
        masterVolumeSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
        mouseSensitivitySlider.RegisterValueChangedCallback(OnMouseSensitivityChanged);
        fovSlider.RegisterValueChangedCallback(OnFOVChanged);
        fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);
        fpsCounterToggle.RegisterValueChangedCallback(OnFPSCounterChanged);
        resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
        framerateDropdown.RegisterValueChangedCallback(OnFramerateChanged);
    }

    private void InitializeSettings()
    {
        // Initialize dropdowns
        resolutionDropdown.choices = new List<string>
        {
            "1920x1080", "1366x768", "1280x720", "1024x768", "800x600"
        };
        framerateDropdown.choices = new List<string>
        {
            "30", "60", "120", "Unlimited"
        };

        // Load current settings
        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var settings = gameSettings.GetCurrentSettings();

        masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
        UpdateVolumeLabel(settings.masterVolume);

        resolutionDropdown.SetValueWithoutNotify(settings.resolution);
        framerateDropdown.SetValueWithoutNotify(settings.framerate);
        fullscreenToggle.SetValueWithoutNotify(settings.fullscreen);

        mouseSensitivitySlider.SetValueWithoutNotify(settings.mouseSensitivity);
        UpdateMouseSensitivityLabel(settings.mouseSensitivity);

        fovSlider.SetValueWithoutNotify(settings.fieldOfView);
        UpdateFOVLabel(settings.fieldOfView);

        fpsCounterToggle.SetValueWithoutNotify(settings.showFPSCounter);
    }

    private void OpenSettings()
    {
        settingsPanel.style.display = DisplayStyle.Flex;
        LoadCurrentSettings(); // Refresh settings UI
    }

    private void CloseSettings()
    {
        settingsPanel.style.display = DisplayStyle.None;
    }

    private async void LeaveGame()
    {

        SetPaused(false);
        ForceCursorForMenu();


        if (networkHandler != null)
        {
            await networkHandler.Disconnect();
        }
        // Disconnect() ya carga MainMenu, es necesario  hacerlo aquí de nuevo
    }

    private void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;

        pauseMenu.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;

        if (!paused)
        {
            settingsPanel.style.display = DisplayStyle.None;
        }

        IsPaused = paused;
        PauseStateChanged?.Invoke(paused);

        RefreshCursorTarget();
        ApplyCursorState();

        EnsureFusionInputProvider();
        fusionInputProvider?.SetInputEnabled(!paused);
    }

    private void EnsureFusionInputProvider()
    {
        if (fusionInputProvider == null)
        {
            fusionInputProvider = FindFirstObjectByType<FusionInputProvider>();
        }
    }

    private void RefreshCursorTarget()
    {
        bool inventoryOpen = InventorySystem.Instance != null && InventorySystem.Instance.IsInventoryOpen;

        CursorLockMode desiredLock;
        bool desiredVisible;

        if (isPaused)
        {
            desiredLock = CursorLockMode.None;
            desiredVisible = true;
        }
        else if (inventoryOpen)
        {
            desiredLock = CursorLockMode.None;
            desiredVisible = true;
        }
        else
        {
            desiredLock = CursorLockMode.Locked;
            desiredVisible = false;
        }

        if (desiredLock != targetCursorLock || desiredVisible != targetCursorVisible)
        {
            targetCursorLock = desiredLock;
            targetCursorVisible = desiredVisible;
            cursorTargetDirty = true;
        }
    }

    private void ApplyCursorState()
    {
        UnityEngine.Cursor.lockState = targetCursorLock;
        UnityEngine.Cursor.visible = targetCursorVisible;
        cursorTargetDirty = false;
    }

    private void LateUpdate()
    {
        RefreshCursorTarget();

        if (cursorTargetDirty ||
            UnityEngine.Cursor.lockState != targetCursorLock ||
            UnityEngine.Cursor.visible != targetCursorVisible)
        {
            ApplyCursorState();
        }
    }

    // Settings event handlers
    private void OnMasterVolumeChanged(ChangeEvent<float> evt)
    {
        UpdateVolumeLabel(evt.newValue);
        gameSettings.SetMasterVolume(evt.newValue);
    }

    private void OnMouseSensitivityChanged(ChangeEvent<float> evt)
    {
        UpdateMouseSensitivityLabel(evt.newValue);
        gameSettings.SetMouseSensitivity(evt.newValue);
    }

    private void OnFOVChanged(ChangeEvent<float> evt)
    {
        UpdateFOVLabel(evt.newValue);
        gameSettings.SetFieldOfView(evt.newValue);
    }

    private void OnFullscreenChanged(ChangeEvent<bool> evt)
    {
        gameSettings.SetFullscreen(evt.newValue);
    }

    private void OnFPSCounterChanged(ChangeEvent<bool> evt)
    {
        gameSettings.SetShowFPSCounter(evt.newValue);
    }

    private void OnResolutionChanged(ChangeEvent<string> evt)
    {
        gameSettings.SetResolution(evt.newValue);
    }

    private void OnFramerateChanged(ChangeEvent<string> evt)
    {
        gameSettings.SetFramerate(evt.newValue);
    }

    // Label update methods
    private void UpdateVolumeLabel(float value)
    {
        masterVolumeValue.text = $"{value:F0}%";
    }

    private void UpdateMouseSensitivityLabel(float value)
    {
        mouseSensitivityValue.text = $"{value:F1}";
    }

    private void UpdateFOVLabel(float value)
    {
        fovValue.text = $"{value:F0} deg";
    }

    private void ForceCursorForMenu()
    {
        targetCursorLock = CursorLockMode.None;
        targetCursorVisible = true;
        cursorTargetDirty = false;

        UnityEngine.Cursor.lockState = targetCursorLock;
        UnityEngine.Cursor.visible = targetCursorVisible;
    }
}
