using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Fusion;

namespace Scripts
{
    /*Clase para desplazar el personaje*/
    public class FirstPersonMovement : MonoBehaviour
{
    [SerializeField] private float movementVelocity;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f; // Velocidad al agacharse (50% de la velocidad normal)
    [SerializeField] private float sprintSpeedMultiplier = 1.5f; // Velocidad al sprintar (150% de la velocidad normal)
    [SerializeField] private float rotationVelocity;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform player;
    [SerializeField] private Camera cameraPlayer;

    private Vector3 movement;
    private float rotationX;
    private float movX;
    private float movZ;
    private bool isCrouching = false;
    private bool isSprinting = false;
    
    // Red - Referencia al NetworkObject para verificar autoridad
    private NetworkObject networkObject;
    private bool isNetworked = false;
    
    /*Desactivamos el cursor para tener mayor inmersion*/
    void Start()
    {
        // Detectar si estamos en modo red
        networkObject = GetComponent<NetworkObject>();
        isNetworked = networkObject != null;
        
        // Solo el jugador local controla el cursor
        if (!isNetworked || (networkObject != null && networkObject.HasInputAuthority))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        // En modo red: solo procesar si tenemos autoridad de input
        // En modo local: siempre procesar
        if (isNetworked && networkObject != null && !networkObject.HasInputAuthority)
        {
            return; // Jugador remoto - no procesar input
        }
        
        PlayerMove();
        CameraRotation();
    }
    /*Recive la direccion en la que se movera, caso de que sean 0 estara parado*/
    public void SetDirection(float dirX, float dirZ)
    {
        movX = dirX;
        movZ = dirZ;
    }
    
    /*Establece si el jugador está agachado o no*/
    public void SetCrouching(bool crouching)
    {
        isCrouching = crouching;
    }
    
    /*Establece si el jugador está sprintando o no*/
    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }
    
    /*Desplaza el personaje en la direccion deseada, usando el Forward para indicar y guiar como la parte frontal del personaje*/
    void PlayerMove()
    {
        movement = transform.right * movX + transform.forward * movZ;
        
        // Calcula la velocidad según el estado (agachado, normal, o sprintando)
        float currentSpeed = movementVelocity;
        
        if (isCrouching)
        {
            currentSpeed = movementVelocity * crouchSpeedMultiplier;
        }
        else if (isSprinting)
        {
            currentSpeed = movementVelocity * sprintSpeedMultiplier;
        }
        
        characterController.SimpleMove(movement * currentSpeed);
    }
    /*Rota la camara obteninedo el sentido en que se mueve el mouse
     *despues limitamos en el eje Y para que no vea mucho giro o movimientos no deseados
     *A su vez rotamos el objeto del personajes ya que se usa el forward para el desplazamiento
     */
    void CameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationVelocity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationVelocity * Time.deltaTime;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -80f, 80f);

        cameraPlayer.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
        
    }
}
}