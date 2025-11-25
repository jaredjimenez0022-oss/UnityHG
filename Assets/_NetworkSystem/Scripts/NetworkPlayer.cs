using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;

/// <summary>
/// Controlador de jugador para Photon Fusion con SimpleKCC
/// Incluye: Movimiento, Sprint con Stamina, Crouch, Salto, Cámara, Inventario, Interacción con Cofres
/// </summary>
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 7.5f; // moveSpeed * 1.5
    [SerializeField] private float crouchSpeed = 2.5f; // moveSpeed * 0.5
    [SerializeField] private float jumpImpulse = 5f;

    [Header("Crouch Settings")]
    [SerializeField] private Transform headCrouch;
    [SerializeField] private float standHeight = 2.0f;
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchTransitionSpeed = 5f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float maxLookAngle = 80f;

    // Components
    [SerializeField] private PlayerAnimatorNetwork playerAnimator;
    private SimpleKCC kcc;
    private Camera localCamera;
    private AudioListener audioListener;
    private NetworkStaminaSystem staminaSystem;
    private NetworkInventorySystem inventorySystem;
    private CapsuleCollider capsuleCollider;

    // Sistema robusto para detectar cofres cercanos
    private List<NetworkChest> chestsInRange = new List<NetworkChest>();
    private float maxInteractionDistance = 3f;

    // Networked variables
    private NetworkButtons previousButtons;
    private float cameraPitch;
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

        if (!playerAnimator)
            playerAnimator = GetComponent<PlayerAnimatorNetwork>();

        staminaSystem = GetComponent<NetworkStaminaSystem>();
        inventorySystem = GetComponent<NetworkInventorySystem>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        currentHeight = standHeight;
    }

    private void OnDestroy()
    {
        // Limpiar la cámara cuando se destruya el jugador
        if (localCamera != null)
        {
            Destroy(localCamera.gameObject);
            localCamera = null;
            audioListener = null;
        }
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // Este es NUESTRO jugador local
            Debug.Log($"[NetworkPlayer] Spawned LOCAL player {Object.InputAuthority.PlayerId}");

            // Configurar cámara local
            SetupLocalCamera();

            // Color verde para identificar visualmente (opcional)
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }

            // HUD is now created and managed by PlayerHUD component

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
            // Este es un jugador REMOTO - desactivar cámara y audio
            Debug.Log($"[NetworkPlayer] Spawned REMOTE player {Object.InputAuthority.PlayerId}");

            DisableRemoteCamera();

            // Color azul para jugadores remotos (opcional)
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue;
            }
        }

        gameObject.name = $"Player_{Object.InputAuthority.PlayerId}";
    }

    /// <summary>
    /// Desactiva la cámara y audio listener de jugadores remotos
    /// </summary>
    private void DisableRemoteCamera()
    {
        // Buscar y desactivar MainCamera en los hijos
        Transform mainCameraChild = transform.Find("MainCamera");
        if (mainCameraChild != null)
        {
            Camera cam = mainCameraChild.GetComponent<Camera>();
            AudioListener listener = mainCameraChild.GetComponent<AudioListener>();

            if (cam != null)
            {
                cam.enabled = false;
                Debug.Log($"[NetworkPlayer] Desactivada cámara de jugador remoto");
            }
            if (listener != null)
            {
                listener.enabled = false;
            }

            // Desactivar el GameObject completo
            mainCameraChild.gameObject.SetActive(false);
        }

        // También desactivar cámara en cameraTarget si existe
        if (cameraTarget != null)
        {
            Camera cam = cameraTarget.GetComponent<Camera>();
            AudioListener listener = cameraTarget.GetComponent<AudioListener>();

            if (cam != null)
            {
                cam.enabled = false;
            }
            if (listener != null)
            {
                listener.enabled = false;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<NetworkInputData>(out var input))
            return;

        // Solo el jugador con InputAuthority ejecuta la lógica completa
        if (Object.HasInputAuthority)
        {
            HandleLookRotation(input);
            HandleCrouch(input);
            HandleMovement(input);
            HandleInteraction(input);

            // IMPORTANTE: Actualizar previousButtons AL FINAL
            previousButtons = input.buttons;
        }
        else if (HasStateAuthority)
        {
            // El servidor también maneja movimiento para jugadores remotos
            HandleLookRotation(input);
            HandleCrouch(input);
            HandleMovement(input);

            // Track previous buttons por tick para evitar toggles repetidos
            previousButtons = input.buttons;
        }
    }

    private void HandleLookRotation(NetworkInputData input)
    {
        // Get mouse sensitivity from settings (scale it down for better control)
        float sensitivity = GameSettings.MouseSensitivity * 0.1f;

        cameraPitch -= input.look.y * sensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        float yawDelta = input.look.x * sensitivity;
        kcc.AddLookRotation(0f, yawDelta);

        // Aplicar rotación a la cámara local directamente
        if (localCamera != null)
        {
            localCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        // También aplicar al cameraTarget para compatibilidad
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
                    Debug.Log("[NetworkPlayer] No hay espacio para levantarse");
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

        // Actualizar SimpleKCC
        if (kcc != null)
        {
            kcc.SetHeight(currentHeight);
        }

        // Actualizar collider
        if (capsuleCollider != null)
        {
            capsuleCollider.height = currentHeight;
            capsuleCollider.center = new Vector3(0, currentHeight / 2f, 0);
        }

        // Actualizar posición de cámara según altura
        if (localCamera != null)
        {
            Vector3 newPositionCamera = localCamera.transform.position;
            if (isCrouching && headCrouch != null)
            {
                newPositionCamera.y = headCrouch.position.y;
            }
            else if (cameraTarget != null)
            {
                newPositionCamera.y = cameraTarget.position.y;
            }
            localCamera.transform.position = newPositionCamera;
        }

        // Actualizar animaciones
        if (playerAnimator != null)
        {
            playerAnimator.SetIsCrounch(isCrouching);
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

        // Si NO está sprinteando, detener el drenado de stamina
        if (!isSprinting && staminaSystem != null)
        {
            staminaSystem.StopDraining();
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

        // Actualizar animaciones
        if (playerAnimator != null)
        {
            playerAnimator.SetIsJump(!kcc.IsGrounded);
            playerAnimator.SetIsRun(moveVelocity.magnitude > 0);
        }
    }

    private void HandleInteraction(NetworkInputData input)
    {
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

    private void SetupLocalCamera()
    {
        if (cameraTarget == null)
        {
            Debug.LogError("[NetworkPlayer] CameraTarget no asignado!");
            return;
        }

        // Buscar MainCamera en los hijos del Player
        Transform mainCameraChild = transform.Find("MainCamera");

        if (mainCameraChild != null)
        {
            // Si existe MainCamera como hijo, usarla SOLO para el jugador local
            localCamera = mainCameraChild.GetComponent<Camera>();
            audioListener = mainCameraChild.GetComponent<AudioListener>();

            if (localCamera == null)
                localCamera = mainCameraChild.gameObject.AddComponent<Camera>();
            if (audioListener == null)
                audioListener = mainCameraChild.gameObject.AddComponent<AudioListener>();

            // IMPORTANTE: Activar SOLO para el jugador local
            localCamera.enabled = true;
            audioListener.enabled = true;
            mainCameraChild.gameObject.SetActive(true);

            // Configurar etiqueta
            mainCameraChild.tag = "MainCamera";

            Debug.Log($"[NetworkPlayer] MainCamera existente activada para jugador local {Object.InputAuthority.PlayerId}");
        }
        else
        {
            // Si no existe MainCamera, crear una nueva
            Debug.LogWarning($"[NetworkPlayer] No se encontró MainCamera en el prefab. Creando una nueva...");

            GameObject cameraObj = new GameObject($"PlayerCamera_{Object.InputAuthority.PlayerId}");
            cameraObj.tag = "MainCamera";
            localCamera = cameraObj.AddComponent<Camera>();
            audioListener = cameraObj.AddComponent<AudioListener>();

            // Configuración de la cámara
            localCamera.nearClipPlane = 0.3f;
            localCamera.farClipPlane = 1000f;

            // Hacer la cámara hija del cameraTarget
            cameraObj.transform.SetParent(cameraTarget);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;
        }

        // Aplicar FOV desde settings
        localCamera.fieldOfView = GameSettings.FieldOfView;

        Debug.Log($"[NetworkPlayer] Cámara LOCAL configurada correctamente para Player {Object.InputAuthority.PlayerId}");
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
}
