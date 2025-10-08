using UnityEngine;
using Fusion;

public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private float jumpForce = 3f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Components")]
    private CharacterController characterController;
    
    [Networked] private Vector3 networkPosition { get; set; }
    [Networked] private Quaternion networkRotation { get; set; }
    [Networked] private bool isGrounded { get; set; }

    private Vector3 velocity;
    private float gravity = -9.81f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.5f;
            characterController.height = 2f;
            characterController.center = new Vector3(0, 1, 0);
        }
    }

    public override void Spawned()
    {
        // Se llama cuando el objeto es spawneado en la red
        if (HasStateAuthority)
        {
            Debug.Log($"[NetworkPlayer] Jugador LOCAL spawneado con autoridad - ID: {Object.InputAuthority.PlayerId}");
            
            // Cambiar color para distinguir jugador local
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }
        }
        else
        {
            Debug.Log($"[NetworkPlayer] Jugador REMOTO spawneado - ID: {Object.InputAuthority.PlayerId}");
            
            // Color diferente para jugadores remotos
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.blue;
            }
        }

        // Asignar nombre al jugador
        gameObject.name = $"Player_{Object.InputAuthority.PlayerId}";
    }

    public override void FixedUpdateNetwork()
    {
        // Solo el jugador con autoridad puede mover
        if (HasStateAuthority)
        {
            HandleMovement();
            HandleGravity();
        }
        else
        {
            // Interpolar posición para jugadores remotos (suaviza el movimiento)
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
        }
    }

    private void HandleMovement()
{
    // Obtener input
    float horizontal = Input.GetAxis("Horizontal"); // A/D o flechas
    float vertical = Input.GetAxis("Vertical");     // W/S o flechas

    // Calcular dirección de movimiento
    Vector3 movement = new Vector3(horizontal, 0, vertical).normalized;
    
    if (movement.magnitude > 0.1f)
    {
        // Mover el personaje
        Vector3 move = movement * moveSpeed * Runner.DeltaTime;
        characterController.Move(move);
        
        // Rotar hacia la dirección del movimiento
        Quaternion targetRotation = Quaternion.LookRotation(movement);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, 
            targetRotation, 
            rotationSpeed * Runner.DeltaTime
        );
    }

    // Salto - CAMBIADO: usa GetKeyDown en lugar de GetButtonDown para más respuesta
    // Y permite saltar aunque te estés moviendo
    if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
    {
        velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        Debug.Log("¡Saltando!");
    }

    // Actualizar variables de red
    networkPosition = transform.position;
    networkRotation = transform.rotation;
}

    private void HandleGravity()
    {
        // Verificar si está en el suelo
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequeña fuerza para mantenerlo pegado al suelo
        }

        // Aplicar gravedad
        velocity.y += gravity * Runner.DeltaTime;

        // Aplicar velocidad vertical
        characterController.Move(velocity * Runner.DeltaTime);
    }

    public override void Render()
    {
        // Se llama cada frame (no cada tick de red)
        // Útil para efectos visuales, animaciones, etc.
    }

    // Método para obtener información del jugador
    public int GetPlayerId()
    {
        return Object.InputAuthority.PlayerId;
    }

    public bool IsLocalPlayer()
    {
        return HasStateAuthority;
    }
}