using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Scripts
{
    /*Clase para desplazar el personaje*/
    public class FirstPersonMovement : MonoBehaviour
{
    [SerializeField] private float movementVelocity;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f; // Velocidad al agacharse (50% de la velocidad normal)
    [SerializeField] private float rotationVelocity;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform player;
    [SerializeField] private Camera cameraPlayer;

    private Vector3 movement;
    private float rotationX;
    private float movX;
    private float movZ;
    private bool isCrouching = false;
    /*Desactivamos el cursor para tener mayor inmersion*/
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
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
    
    /*Desplaza el personaje en la direccion deseada, usando el Forward para indicar y guiar como la parte frontal del personaje*/
    void PlayerMove()
    {
        movement = transform.right * movX + transform.forward * movZ;
        
        // Aplica velocidad reducida si está agachado
        float currentSpeed = isCrouching ? movementVelocity * crouchSpeedMultiplier : movementVelocity;
        
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