using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

public class NetworkPlayer : NetworkBehaviour
{
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

    private Vector3 velocity;

    private void Awake()
    {
        kcc = GetComponent<SimpleKCC>();
        if (kcc == null)
        {
            Debug.LogError("SimpleKCC component missing on Player prefab!");
        }
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
        }

        kcc.Move(moveVelocity, jumpImpulseValue);
        previousButtons = input.buttons;
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
