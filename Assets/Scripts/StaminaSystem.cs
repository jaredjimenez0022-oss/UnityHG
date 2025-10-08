using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 20f;
    public float regenDelay = 15.5f;

    [Header("UI References")]
    public Slider staminaBar;

    private float currentStamina;
    private float lastSprintTime;
    private bool isSprinting;
    private Keyboard currentKeyboard;

    public static StaminaSystem Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentStamina = maxStamina;
        currentKeyboard = Keyboard.current;

        if (staminaBar != null)
        {
            staminaBar.gameObject.SetActive(true);
            Debug.Log("StaminaBar activada");
        }
        else
        {
            Debug.LogError("StaminaBar no está asignada en el Inspector");
        }

        UpdateStaminaUI();
    }

    void Update()
    {
        HandleStamina();
        UpdateStaminaUI();
    }

    void HandleStamina()
    {
        // USANDO INPUT SYSTEM - forma correcta
        bool isTryingToSprint = currentKeyboard != null && currentKeyboard.leftShiftKey.isPressed;

        // Para el movimiento, necesitamos una alternativa
        bool isMovingForward = IsMovingForward();

        isSprinting = isTryingToSprint && isMovingForward && currentStamina > 0;

        if (isSprinting)
        {
            // Drenar stamina
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            lastSprintTime = Time.time;

            Debug.Log($"Sprinting... Stamina: {currentStamina:F0}");

            if (currentStamina <= 0)
            {
                ForceStopSprint();
            }
        }
        else if (currentStamina < maxStamina && (Time.time - lastSprintTime) > regenDelay)
        {
            // Regenerar stamina
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
    }

    // Método para detectar movimiento con Input System
    bool IsMovingForward()
    {
        // Método 1: Usar el PlayerInput del Starter Assets
        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            return moveInput.y > 0.1f; // Movimiento hacia adelante
        }

        // Método 2: Alternativa si no funciona lo anterior
        // Podemos asumir que si presiona W está moviéndose hacia adelante
        return currentKeyboard != null && currentKeyboard.wKey.isPressed;
    }

    void ForceStopSprint()
    {
        Debug.Log("Sin stamina - forzando caminar");
    }

    void UpdateStaminaUI()
    {
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;

            Image fillImage = staminaBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (currentStamina > maxStamina * 0.6f)
                    fillImage.color = Color.green;
                else if (currentStamina > maxStamina * 0.3f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
    }

    // Debug visual 
    void OnGUI()
    {
        GUI.Label(new Rect(10, 100, 300, 20), $"Stamina: {currentStamina:F0}/{maxStamina}");
        GUI.Label(new Rect(10, 120, 300, 20), $"Sprinting: {isSprinting}");

        if (currentKeyboard != null)
        {
            GUI.Label(new Rect(10, 140, 300, 20), $"Shift Pressed: {currentKeyboard.leftShiftKey.isPressed}");
            GUI.Label(new Rect(10, 160, 300, 20), $"W Pressed: {currentKeyboard.wKey.isPressed}");
        }

        // Verificar movimiento
        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
            GUI.Label(new Rect(10, 180, 300, 20), $"Move Input: {moveInput}");
        }

        if (staminaBar == null)
        {
            GUI.Label(new Rect(10, 200, 300, 20), "StaminaBar NO CONECTADA");
        }
    }

    public bool CanSprint()
    {
        return currentStamina > 0;
    }

    public float GetStaminaPercent()
    {
        return currentStamina / maxStamina;
    }
}