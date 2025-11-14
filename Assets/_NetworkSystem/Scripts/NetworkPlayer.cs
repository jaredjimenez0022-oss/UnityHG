using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;

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

    // Sistema robusto para detectar cofres cercanos
    private List<NetworkChest> chestsInRange = new List<NetworkChest>();
    private float maxInteractionDistance = 3f;

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
        {
            Debug.LogWarning("[NetworkPlayer.FixedUpdateNetwork] GetInput devolvio false - no hay input disponible");
            return;
        }

        if (Object.HasInputAuthority)
        {
            TryAttachCamera();
            HandleLookRotation(input);
            HandleCrouch(input);
            HandleMovement(input);
            HandleInteraction(input);

            // IMPORTANTE: Actualizar previousButtons AL FINAL, después de procesar todas las interacciones
            previousButtons = input.buttons;
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
        // previousButtons se actualiza al final de FixedUpdateNetwork()
    }

    private void HandleInteraction(NetworkInputData input)
    {
        // DEBUG: Log estado de botones
        bool interactCurrentlyPressed = input.buttons.IsSet(InputButtons.Interact);
        bool previousInteractPressed = previousButtons.IsSet(InputButtons.Interact);

        if (interactCurrentlyPressed || previousInteractPressed)
        {
            Debug.Log($"[NetworkPlayer.HandleInteraction] Interact estado - Current: {interactCurrentlyPressed}, Previous: {previousInteractPressed}");
        }

        var pressed = input.buttons.GetPressed(previousButtons);

        if (pressed.IsSet(InputButtons.Interact))
        {
            Debug.Log("[NetworkPlayer] TECLA E PRESIONADA - buscando cofre cercano");

            // Buscar cofre cercano (sistema robusto)
            NetworkChest closestChest = FindClosestChest();

            if (closestChest != null)
            {
                float distance = Vector3.Distance(transform.position, closestChest.transform.position);
                Debug.Log($"[NetworkPlayer] Intentando abrir cofre a {distance:F2}m - Posicion: {closestChest.transform.position}");
                closestChest.TryOpen(this);
            }
            else
            {
                Debug.LogWarning("[NetworkPlayer] No hay cofre cercano dentro del rango de interaccion");
            }
        }
    }

    /// <summary>
    /// Encuentra el cofre más cercano que esté dentro del rango de interacción
    /// Usa tanto la lista de triggers como búsqueda por distancia directa
    /// </summary>
    private NetworkChest FindClosestChest()
    {
        NetworkChest closestChest = null;
        float closestDistance = maxInteractionDistance;

        // 1. Primero verificar la lista de chests detectados por trigger
        chestsInRange.RemoveAll(c => c == null || c.IsOpen); // Limpiar nulls y abiertos

        foreach (NetworkChest chest in chestsInRange)
        {
            float distance = Vector3.Distance(transform.position, chest.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestChest = chest;
            }
        }

        if (closestChest != null)
        {
            Debug.Log($"[NetworkPlayer] Chest encontrado via lista de triggers: {closestChest.name} a {closestDistance:F2}m");
            return closestChest;
        }

        // 2. Si la lista está vacía, buscar directamente por distancia (fallback robusto)
        Debug.Log("[NetworkPlayer] Lista de triggers vacia - buscando por distancia directa");
        NetworkChest[] allChests = FindObjectsByType<NetworkChest>(FindObjectsSortMode.None);

        foreach (NetworkChest chest in allChests)
        {
            if (chest.IsOpen) continue; // Ignorar cofres abiertos

            float distance = Vector3.Distance(transform.position, chest.transform.position);
            Debug.Log($"[NetworkPlayer] Evaluando chest {chest.name} a distancia {distance:F2}m");

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestChest = chest;
            }
        }

        if (closestChest != null)
        {
            Debug.Log($"[NetworkPlayer] Chest encontrado via busqueda directa: {closestChest.name} a {closestDistance:F2}m");
        }

        return closestChest;
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
        Debug.Log($"[NetworkPlayer] OnTriggerEnter detectado - Objeto: {other.gameObject.name}, HasInputAuthority: {Object.HasInputAuthority}");

        // Solo el jugador local detecta cofres
        if (!Object.HasInputAuthority) return;

        NetworkChest chest = other.GetComponent<NetworkChest>();
        Debug.Log($"[NetworkPlayer] NetworkChest component: {(chest != null ? "ENCONTRADO" : "NULL")}, IsOpen: {(chest != null ? chest.IsOpen.ToString() : "N/A")}");

        if (chest != null && !chest.IsOpen && !chestsInRange.Contains(chest))
        {
            chestsInRange.Add(chest);
            Debug.Log($"[NetworkPlayer] ENTER: Cofre agregado a lista. Total chests en rango: {chestsInRange.Count}. Posicion: {chest.transform.position}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[NetworkPlayer] OnTriggerExit detectado - Objeto: {other.gameObject.name}, HasInputAuthority: {Object.HasInputAuthority}");

        if (!Object.HasInputAuthority) return;

        NetworkChest chest = other.GetComponent<NetworkChest>();

        if (chest != null && chestsInRange.Contains(chest))
        {
            chestsInRange.Remove(chest);
            Debug.Log($"[NetworkPlayer] EXIT: Cofre removido de lista. Total chests en rango: {chestsInRange.Count}");
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