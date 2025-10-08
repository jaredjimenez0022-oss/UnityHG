using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;

    private PlayerInput playerInput;
    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            var moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: false);
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();
        }
    }

    void FixedUpdate()
    {
        if (Camera.main != null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 move = (camForward * moveInput.y + camRight * moveInput.x) * speed;
            rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
        }
    }

    void LateUpdate()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            var jumpAction = playerInput.actions.FindAction("Jump", throwIfNotFound: false);
            if (jumpAction != null && jumpAction.triggered && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;
            }
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // Asegúrate de ponerle el Tag "Ground" al piso en Unity
        if (col.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }
}
