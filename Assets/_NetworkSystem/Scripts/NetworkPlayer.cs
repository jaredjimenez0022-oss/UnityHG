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
    private bool cameraAttached;

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

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            TryAttachCamera();

            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }
            GameObject hudInstance = Instantiate(hudPrefab);
            hudCanvas = hudInstance.GetComponent<Canvas>();
        }
        else
        {
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
            HandleMovement(input);
        }
        else if (HasStateAuthority)
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
        Camera currentCamera = GetComponentInChildren<Camera>();
        if (isCrounch)
        {
            kcc.SetHeight(crounchHeigh);
            if (currentCamera)
            {
                Vector3 newPositionCamera = currentCamera.transform.position;
                newPositionCamera.y = headCrounch.position.y;
                currentCamera.transform.position = newPositionCamera;
            }
        }
        else
        {
            kcc.SetHeight(2f);
            Vector3 newPositionCamera = currentCamera.transform.position;
            newPositionCamera.y = cameraTarget.position.y;
            currentCamera.transform.position = newPositionCamera;
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
        if (cameraAttached)
        {
            return;
        }

        if (cameraTarget == null)
        {
            return;
        }

        var controller = CameraController.EnsureInstance();
        if (controller == null)
        {
            return;
        }

        controller.SetTarget(cameraTarget);
        cameraAttached = true;
    }
}
