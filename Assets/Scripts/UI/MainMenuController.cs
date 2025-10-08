using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    private VisualElement root;
    private Button playButton;
    private Button settingsButton;
    private Button quitButton;

    // Player Name Field
    private TextField playerNameField;

    // Settings Panel Elements
    private VisualElement settingsPanel;
    private Button closeSettingsButton;
    private DropdownField resolutionDropdown;
    private DropdownField framerateDropdown;
    private Slider masterVolumeSlider;
    private Slider mouseSensitivitySlider;
    private Slider fovSlider;
    private Label masterVolumeValue;
    private Label mouseSensitivityValue;
    private Label fovValue;

    void Start()
    {
        // Get the root visual element from the UI Document
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Find main menu elements
        playButton = root.Q<Button>("play-button");
        settingsButton = root.Q<Button>("settings-button");
        quitButton = root.Q<Button>("quit-button");

        // Find player name field
        playerNameField = root.Q<TextField>("player-name-field");

        // Find settings panel elements
        settingsPanel = root.Q<VisualElement>("settings-panel");
        closeSettingsButton = root.Q<Button>("close-settings-button");
        resolutionDropdown = root.Q<DropdownField>("resolution-dropdown");
        framerateDropdown = root.Q<DropdownField>("framerate-dropdown");
        masterVolumeSlider = root.Q<Slider>("master-volume-slider");
        mouseSensitivitySlider = root.Q<Slider>("mouse-sensitivity-slider");
        fovSlider = root.Q<Slider>("fov-slider");
        masterVolumeValue = root.Q<Label>("master-volume-value");
        mouseSensitivityValue = root.Q<Label>("mouse-sensitivity-value");
        fovValue = root.Q<Label>("fov-value");

        // Setup events and initialize dropdowns
        SetupButtonEvents();
        InitializeSettingsPanel();
    }

    private void SetupButtonEvents()
    {
        // Main menu buttons
        playButton.clicked += OnPlayClicked;
        settingsButton.clicked += OnSettingsClicked;
        quitButton.clicked += OnQuitClicked;

        // Settings panel buttons
        closeSettingsButton.clicked += OnCloseSettingsClicked;

        // Settings sliders events
        masterVolumeSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
        mouseSensitivitySlider.RegisterValueChangedCallback(OnMouseSensitivityChanged);
        fovSlider.RegisterValueChangedCallback(OnFOVChanged);
    }

    private void InitializeSettingsPanel()
    {
        // Initialize Resolution dropdown
        resolutionDropdown.choices = new List<string>
        {
            "1920x1080",
            "1366x768",
            "1280x720",
            "1024x768",
            "800x600"
        };
        resolutionDropdown.value = "1920x1080";

        // Initialize Framerate dropdown
        framerateDropdown.choices = new List<string>
        {
            "30",
            "60",
            "120",
            "Unlimited"
        };
        framerateDropdown.value = "60";

        // Initialize slider values
        UpdateVolumeLabel(masterVolumeSlider.value);
        UpdateMouseSensitivityLabel(mouseSensitivitySlider.value);
        UpdateFOVLabel(fovSlider.value);

        // Load saved player name if exists
        string savedPlayerName = PlayerPrefs.GetString("PlayerName", "Player");
        playerNameField.value = savedPlayerName;

        // Register player name focus out event
        playerNameField.RegisterCallback<FocusOutEvent>(OnPlayerNameFocusLost);
    }

    private void OnPlayClicked()
    {
        Debug.Log($"Play button clicked! Player name: {playerNameField.value}");
        // Save player name before loading game
        PlayerPrefs.SetString("PlayerName", playerNameField.value);
        PlayerPrefs.Save();
        // TODO: Load game scene or open lobby ui window
        // SceneManager.LoadScene("GameScene");
    }

    private void OnSettingsClicked()
    {
        Debug.Log("Settings button clicked!");
        settingsPanel.style.display = DisplayStyle.Flex;
    }

    private void OnCloseSettingsClicked()
    {
        Debug.Log("Close settings button clicked!");
        settingsPanel.style.display = DisplayStyle.None;
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quit button clicked!");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // Settings event handlers
    private void OnMasterVolumeChanged(ChangeEvent<float> evt)
    {
        UpdateVolumeLabel(evt.newValue);
        // TODO: Apply volume change
    }

    private void OnMouseSensitivityChanged(ChangeEvent<float> evt)
    {
        UpdateMouseSensitivityLabel(evt.newValue);
        // TODO: Apply sensitivity change
    }

    private void OnFOVChanged(ChangeEvent<float> evt)
    {
        UpdateFOVLabel(evt.newValue);
        // TODO: Apply FOV change
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
        fovValue.text = $"{value:F0}°";
    }

    private void OnPlayerNameFocusLost(FocusOutEvent evt)
    {
        // Save player name when field loses focus
        PlayerPrefs.SetString("PlayerName", playerNameField.value);
        PlayerPrefs.Save();
    }
}