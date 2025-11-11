using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

/// <summary>
/// Controlador de jugador para Photon Fusion con SimpleKCC
/// Incluye: Movimiento, Sprint con Stamina, Crouch, Salto, Cámara
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 7.5f; // moveSpeed * 1.5
    [SerializeField] private float crouchSpeed = 2.5f; // moveSpeed * 0.5
    [SerializeField] private float jumpImpulse = 5f;

    [Header("Crouch Settings")]
    [SerializeField] private float standHeight = 2.0f;
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchTransitionSpeed = 5f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("UI References")]
    [SerializeField] private GameObject hudPrefab;
    [SerializeField] private Canvas hudCanvas;

    // Components
    private SimpleKCC kcc;
    private NetworkStaminaSystem staminaSystem;
    private NetworkInventorySystem inventorySystem;
    private NetworkChest nearbyChest;
    private CapsuleCollider capsuleCollider;
    private bool cameraAttached;

    // Networked variables
    [Networked] private NetworkButtons previousButtons { get; set; }
    [Networked] private float cameraPitch { get; set; }
    [Networked] private bool isCrouching { get; set; }

    // Local variables
    private float currentHeight;

    private void Awake()
    {
        kcc = GetComponent<SimpleKCC>();
        if (kcc == null)
        {
            Debug.LogError("[NetworkPlayer] SimpleKCC component missing!");
        }

        staminaSystem = GetComponent<NetworkStaminaSystem>();
        inventorySystem = GetComponent<NetworkInventorySystem>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        currentHeight = standHeight;
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // Jugador LOCAL
            Debug.Log($"[NetworkPlayer] LOCAL spawned - ID: {Object.InputAuthority.PlayerId}");

            TryAttachCamera();

            // Color verde para identificar
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }

            // Instanciar HUD
            if (hudPrefab != null)
            {
                GameObject hudInstance = Instantiate(hudPrefab);
                hudCanvas = hudInstance.GetComponent<Canvas>();
            }

            // Inicializar sistemas locales
            if (staminaSystem != null)
            {
                staminaSystem.InitializeLocalPlayer();
            }

            if (inventorySystem != null)
            {
                inventorySystem.InitializeLocalPlayer();
            }
        }
        else
        {
            // Jugador REMOTO
            Debug.Log($"[NetworkPlayer] REMOTE spawned - ID: {Object.InputAuthority.PlayerId}");

            // Color azul para otros jugadores
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue;
            }
        }

        gameObject.name = $"Player_{Object.InputAuthority.PlayerId}";
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<NetworkInputData>(out var input))
            return;

        if (Object.HasInputAuthority)
        {
            TryAttachCamera();
            HandleLookRotation(input);
            HandleCrouch(input);
            HandleMovement(input);
            HandleInteraction(input);
        }
        else if (HasStateAuthority)
        {
            HandleLookRotation(input);
            HandleCrouch(input);
            HandleMovement(input);
        }
    }

    private void HandleLookRotation(NetworkInputData input)
    {
        // Sensibilidad del mouse
        float sensitivity = GameSettings.MouseSensitivity * 0.1f;

        // Pitch (arriba/abajo)
        cameraPitch -= input.look.y * sensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        // Yaw (izquierda/derecha)
        float yawDelta = input.look.x * sensitivity;
        kcc.AddLookRotation(0f, yawDelta);

        // Aplicar pitch a la cámara
        if (cameraTarget != null)
        {
            cameraTarget.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleCrouch(NetworkInputData input)
    {
        var pressed = input.buttons.GetPressed(previousButtons);

        // Toggle crouch con C o Control
        if (pressed.IsSet(InputButtons.Crouch))
        {
            if (isCrouching)
            {
                // Intentar levantarse
                if (CanStandUp())
                {
                    isCrouching = false;
                }
                else
                {
                    Debug.Log("---[NetworkPlayer] No hay espacio para levantarse---");
                }
            }
            else
            {
                // Agacharse
                isCrouching = true;
            }
        }

        // Transición suave de altura
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);

        // Actualizar SimpleKCC y collider
        if (kcc != null)
        {
            kcc.SetHeight(currentHeight);
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.height = currentHeight;
            capsuleCollider.center = new Vector3(0, currentHeight / 2f, 0);
        }
    }

    private bool CanStandUp()
    {
        // Raycast hacia arriba para verificar espacio
        Vector3 checkPosition = transform.position + Vector3.up * (standHeight - crouchHeight);
        float checkRadius = capsuleCollider != null ? capsuleCollider.radius : 0.5f;

        return !Physics.CheckSphere(checkPosition, checkRadius, LayerMask.GetMask("Default"));
    }

    private void HandleMovement(NetworkInputData input)
    {
        Vector3 moveDirection = new Vector3(input.move.x, 0f, input.move.y);

        // Determinar velocidad según estado
        float currentSpeed = moveSpeed;
        bool isSprinting = false;

        if (isCrouching)
        {
            // Agachado: velocidad reducida
            currentSpeed = crouchSpeed;
        }
        else if (input.buttons.IsSet(InputButtons.Sprint))
        {
            // Intentar sprintear
            if (staminaSystem != null && staminaSystem.CanSprint())
            {
                currentSpeed = sprintSpeed;
                isSprinting = true;

                // Consumir stamina
                staminaSystem.DrainStamina(Runner.DeltaTime);
            }
        }

        // Aplicar movimiento
        Vector3 moveVelocity = kcc.TransformRotation * moveDirection * currentSpeed;

        // Salto
        float jumpImpulseValue = 0f;
        var pressed = input.buttons.GetPressed(previousButtons);
        if (pressed.IsSet(InputButtons.Jump) && kcc.IsGrounded && !isCrouching)
        {
            jumpImpulseValue = jumpImpulse;
        }

        kcc.Move(moveVelocity, jumpImpulseValue);
        previousButtons = input.buttons;
    }

    private void HandleInteraction(NetworkInputData input)
    {
        var pressed = input.buttons.GetPressed(previousButtons);

        if (pressed.IsSet(InputButtons.Interact))
        {
            // Intentar abrir cofre cercano
            if (nearbyChest != null)
            {
                nearbyChest.TryOpen(this);
            }
        }
    }

    public override void Render()
    {
        if (Object.HasInputAuthority)
        {
            TryAttachCamera();
        }
    }

    private void TryAttachCamera()
    {
        if (cameraAttached || cameraTarget == null)
            return;

        var controller = CameraController.EnsureInstance();
        if (controller == null)
            return;

        controller.SetTarget(cameraTarget);
        cameraAttached = true;
    }

    // Método público para que otros scripts puedan obtener el inventario
    public NetworkInventorySystem GetInventorySystem()
    {
        return inventorySystem;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo el jugador local detecta cofres
        if (!Object.HasInputAuthority) return;

        NetworkChest chest = other.GetComponent<NetworkChest>();
        if (chest != null && !chest.IsOpen)
        {
            nearbyChest = chest;
            Debug.Log("[NetworkPlayer] Cofre cercano - presiona E para abrir");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Object.HasInputAuthority) return;

        NetworkChest chest = other.GetComponent<NetworkChest>();
        if (chest != null && chest == nearbyChest)
        {
            nearbyChest = null;
            Debug.Log("[NetworkPlayer] Alejado del cofre");
        }
    }

    // Debugging para ver datos
    private void OnGUI()
    {
        if (!Object.HasInputAuthority) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label("=== NETWORK PLAYER ===");
        GUILayout.Label($"Speed: {(isCrouching ? "CROUCH" : "NORMAL")}");
        GUILayout.Label($"Height: {currentHeight:F2}m");
        GUILayout.Label($"Grounded: {kcc.IsGrounded}");
        GUILayout.Label($"Crouching: {isCrouching}");
        if (staminaSystem != null)
        {
            GUILayout.Label($"Stamina: {staminaSystem.GetStaminaPercentage() * 100:F0}%");
        }
        GUILayout.EndArea();
    }
}