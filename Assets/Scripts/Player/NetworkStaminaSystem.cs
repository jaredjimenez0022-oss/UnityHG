using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

/// <summary>
/// Sistema de stamina sincronizado para Photon Fusion
/// Cada jugador tiene su propia stamina, UI solo visible para jugador local
/// </summary>
public class NetworkStaminaSystem : NetworkBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 25f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float regenDelay = 3f;

    [Header("UI References (Asignar desde Inspector)")]
    [SerializeField] private GameObject staminaCanvas;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Image staminaFill;

    // Networked variables
    [Networked] private float currentStamina { get; set; }
    [Networked] private TickTimer regenTimer { get; set; }
    [Networked] private NetworkBool isDraining { get; set; }

    // Local variables
    private float slowRegenRate;
    private bool isLocalPlayer;

    public override void Spawned()
    {
        // IMPORTANTE: Inicializar currentStamina aquí, NO en Awake()
        // Las propiedades [Networked] solo se pueden acceder después de Spawned()
        if (HasStateAuthority)
        {
            currentStamina = maxStamina;
        }

        slowRegenRate = staminaRegenRate / 10f;
        isLocalPlayer = Object.HasInputAuthority;

        // Solo mostrar UI para jugador local
        if (staminaCanvas != null)
        {
            staminaCanvas.SetActive(isLocalPlayer);
        }

        if (isLocalPlayer)
        {
            InitializeUI();
        }
    }

    /// <summary>
    /// Llamado por NetworkPlayer cuando spawna el jugador local
    /// </summary>
    public void InitializeLocalPlayer()
    {
        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }
    }

    /// <summary>
    /// Configura las referencias UI dinámicamente (llamado por NetworkUIManager)
    /// </summary>
    public void SetUIReferences(GameObject canvas, Slider bar, Image fill)
    {
        if (!Object.HasInputAuthority) return;

        staminaCanvas = canvas;
        staminaBar = bar;
        staminaFill = fill;

        // Activar canvas solo para jugador local
        if (staminaCanvas != null)
        {
            staminaCanvas.SetActive(true);
        }

        // Inicializar UI
        InitializeUI();
    }

    public override void FixedUpdateNetwork()
    {
        // Solo el servidor tiene autoridad sobre las variables [Networked]
        if (!HasStateAuthority) return;

        // Regenerar stamina si no está drenando
        if (!isDraining && currentStamina < maxStamina)
        {
            if (regenTimer.ExpiredOrNotRunning(Runner))
            {
                currentStamina += slowRegenRate * Runner.DeltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            }
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        UpdateUI();
    }

    /// <summary>
    /// Drena stamina (llamado por NetworkPlayer cuando está corriendo)
    /// </summary>
    public void DrainStamina(float deltaTime)
    {
        // Solo el servidor puede modificar variables [Networked]
        if (!HasStateAuthority) return;

        currentStamina -= staminaDrainRate * deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        isDraining = true;

        // Reiniciar timer de regeneración
        regenTimer = TickTimer.CreateFromSeconds(Runner, regenDelay);
    }

    /// <summary>
    /// Detiene el drenado de stamina (llamado cuando el jugador deja de sprintear)
    /// </summary>
    public void StopDraining()
    {
        if (!HasStateAuthority) return;

        isDraining = false;
    }

    /// <summary>
    /// Verifica si hay suficiente stamina para sprintear
    /// </summary>
    public bool CanSprint()
    {
        return currentStamina > 0;
    }

    /// <summary>
    /// Obtiene el porcentaje actual de stamina (0-1)
    /// </summary>
    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    private void InitializeUI()
    {
        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }
    }

    private void UpdateUI()
    {
        if (staminaBar == null) return;

        staminaBar.value = currentStamina;

        // Cambiar color según porcentaje
        if (staminaFill != null)
        {
            float percentage = GetStaminaPercentage();

            if (percentage > 0.6f)
            {
                staminaFill.color = Color.green;
            }
            else if (percentage > 0.3f)
            {
                staminaFill.color = Color.yellow;
            }
            else
            {
                staminaFill.color = Color.red;
            }
        }
    }
}