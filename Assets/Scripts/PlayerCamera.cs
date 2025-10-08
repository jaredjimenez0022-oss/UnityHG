using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    public Transform playerBody;
    public float sensitivity = 100f;
    private float xRotation = 0f;

    private PlayerInput playerInput;
    private InputAction lookAction;

    void Awake()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            lookAction = playerInput.actions.FindAction("Look", throwIfNotFound: false);
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (lookAction == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
