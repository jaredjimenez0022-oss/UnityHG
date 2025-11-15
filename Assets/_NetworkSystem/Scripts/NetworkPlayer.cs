using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private PlayerAnimatorNetwork playerAnimator;
    [Header("Crounch Settings")]
    [SerializeField] private Transform headCrounch;
    [SerializeField] private float crounchHeigh = 1f;
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpImpulse = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float maxLookAngle = 80f;

    private SimpleKCC kcc;
    private Camera localCamera;
    private AudioListener audioListener;

    [Networked] private NetworkButtons previousButtons { get; set; }
    [Networked] private float cameraPitch { get; set; }
    
    [Header("HUD")]
    [SerializeField] private GameObject hudPrefab;
    [SerializeField] private Canvas hudCanvas;

    private Vector3 velocity;
    private float gravity = -9.81f;
    bool isCrounch = false;

    private void Awake()
    {
        kcc = GetComponent<SimpleKCC>();
        if (kcc == null)
        {
            Debug.LogError("SimpleKCC component missing on Player prefab!");
        }

        if (!playerAnimator)
            playerAnimator = GetComponent<PlayerAnimatorNetwork>();
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
            
            // Crear HUD
            if (hudPrefab != null)
            {
                GameObject hudInstance = Instantiate(hudPrefab);
                hudCanvas = hudInstance.GetComponent<Canvas>();
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

        // Solo el servidor (StateAuthority) ejecuta la lógica de movimiento
        if (HasStateAuthority)
        {
            HandleLookRotation(input);
            HandleMovement(input);
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

    private void HandleMovement(NetworkInputData input)
    {
        Vector3 moveDirection = new Vector3(input.move.x, 0f, input.move.y);

        float currentSpeed = moveSpeed;
        if (input.buttons.IsSet(InputButtons.Sprint))
        {
            currentSpeed *= sprintMultiplier;
        }

        Vector3 moveVelocity = kcc.TransformRotation * moveDirection * currentSpeed;

        float jumpImpulseValue = 0f;
        var pressed = input.buttons.GetPressed(previousButtons);
        if (pressed.IsSet(InputButtons.Jump) && kcc.IsGrounded)
        {
            jumpImpulseValue = jumpImpulse;
            isCrounch = false;
        }

        if (kcc.IsGrounded && input.buttons.IsSet(InputButtons.Crouch))
        {
            isCrounch = !isCrounch;
        }

        kcc.Move(moveVelocity, jumpImpulseValue);
        previousButtons = input.buttons;

        playerAnimator.SetIsJump(!kcc.IsGrounded);
        playerAnimator.SetIsRun(moveVelocity.magnitude > 0);
        playerAnimator.SetIsCrounch(isCrounch);
        
        if (localCamera != null)
        {
            if (isCrounch)
            {
                kcc.SetHeight(crounchHeigh);
                Vector3 newPositionCamera = localCamera.transform.position;
                newPositionCamera.y = headCrounch.position.y;
                localCamera.transform.position = newPositionCamera;
            }
            else
            {
                kcc.SetHeight(2f);
                Vector3 newPositionCamera = localCamera.transform.position;
                newPositionCamera.y = cameraTarget.position.y;
                localCamera.transform.position = newPositionCamera;
            }
        }
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
}
