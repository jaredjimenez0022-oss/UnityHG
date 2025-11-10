using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    /*Esta clase controlara los inputs que realice el jugador*/
    public class PlayerController : MonoBehaviour
    {

        [SerializeField] private Animator playerAnimator;
        [SerializeField] private FirstPersonMovement playerMovement;
        [SerializeField] private PlayerAnimatorNetwork playerAnimatorNetwork;
        [SerializeField] private CharacterController characterController;
        /*Creamos un weapon inicial para tenerlo como base y una lista donde iremos intercambiando las armas*/
        [SerializeField] private Weapon currentWeapon;
        [SerializeField] private List<Weapon> listWeapons = new List<Weapon>();
        private bool isWalk;
        private bool isCrouching = false;
        private bool isSprinting = false;
        private int currentindex = 0;
        private float originalHeight;
        private float crouchHeight = 1f; // Altura cuando está agachado

        private void Start()
        {
            if (characterController != null)
            {
                originalHeight = characterController.height;
            }

            // Desactivar todas las armas al inicio
            foreach (var weapon in listWeapons)
            {
                if (weapon != null)
                {
                    weapon.gameObject.SetActive(false);
                }
            }
            
            // El jugador empieza sin arma equipada
            currentWeapon = null;
        }

        private void Update()
        {
            if (PauseMenuController.IsPaused)
            {
                playerMovement.SetDirection(0f, 0f);
                playerAnimator.SetBool("Walk", false);
                return;
            }

            InputMove();
            InputChangeWeapon();
            InputAttack();
            InputCrouch();
            InputSprint();
        }
        /*Detecta los movimientos por teclado WASD o Flechas direccion, para mandar la direccion en la que se desplaza*/
        private void InputMove()
        {
            float movX = Input.GetAxis("Horizontal");
            float movZ = Input.GetAxis("Vertical");
            playerMovement.SetDirection(movX, movZ);
            if (movX != 0 || movZ != 0)
            {
                isWalk = true;
            }
            else
            {
                isWalk = false;
            }
            
            // Actualiza ambos parámetros (Walk para compatibilidad y isRun para el sistema actual)
            playerAnimator.SetBool("Walk", isWalk);
            playerAnimator.SetBool("isRun", isWalk);  // Agregado para Run
            
            // Envía los valores de dirección al Animator para el Blend Tree
            playerAnimator.SetFloat("Horizontal", movX);
            playerAnimator.SetFloat("Vertical", movZ);
            
            // También actualiza el parámetro de caminar/correr en el PlayerAnimatorNetwork
            if (playerAnimatorNetwork != null)
            {
                playerAnimatorNetwork.SetIsRun(isWalk);
            }
        }

        /*Detecta el ataque segun el arma actual que tiene el juegador*/
        private void InputAttack()
        {
            if (Input.GetMouseButtonDown(0) && currentWeapon != null)
            {
                currentWeapon.StartAttack();
            }
        }

        /*Detecta cuando el jugador presiona la tecla C para agacharse*/
        private void InputCrouch()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                isCrouching = !isCrouching; // Alterna entre agachado y de pie
                
                // No puede estar agachado y sprintando al mismo tiempo
                if (isCrouching && isSprinting)
                {
                    isSprinting = false;
                    playerAnimator.SetBool("isSprint", false);
                    if (playerMovement != null)
                    {
                        playerMovement.SetSprinting(false);
                    }
                }
                
                // Actualiza la animación
                if (playerAnimatorNetwork != null)
                {
                    playerAnimatorNetwork.SetIsCrounch(isCrouching);
                }

                // Informa al sistema de movimiento si está agachado
                if (playerMovement != null)
                {
                    playerMovement.SetCrouching(isCrouching);
                }

                // Ajusta la altura del CharacterController
                if (characterController != null)
                {
                    if (isCrouching)
                    {
                        characterController.height = crouchHeight;
                        characterController.center = new Vector3(0, crouchHeight / 2, 0);
                    }
                    else
                    {
                        characterController.height = originalHeight;
                        characterController.center = new Vector3(0, originalHeight / 2, 0);
                    }
                }
            }
        }

        /*Detecta cuando el jugador mantiene presionada la tecla Shift para sprintar*/
        private void InputSprint()
        {
            // Usar Shift de nuevo (ya sabemos que funciona)
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift);
            
            // Solo puede sprintar si está moviendo y no está agachado
            if (shiftPressed && !isCrouching && isWalk)
            {
                isSprinting = true;
            }
            else
            {
                isSprinting = false;
            }

            // Actualiza el Animator
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isSprint", isSprinting);
                
                // Debug para verificar que se está actualizando
                if (isSprinting)
                {
                    Debug.Log($"Actualizando Animator: isSprint = true. Animator name: {playerAnimator.name}");
                    
                    // Verificar el valor real del parámetro
                    bool valorActual = playerAnimator.GetBool("isSprint");
                    Debug.Log($"Valor actual del parámetro isSprint en Animator: {valorActual}");
                }
            }
            else
            {
                Debug.LogError("playerAnimator es NULL! Asignalo en el Inspector.");
            }

            // Informa al sistema de movimiento
            if (playerMovement != null)
            {
                playerMovement.SetSprinting(isSprinting);
            }
        }

        /*Detecta los cambios de armas segun el orden que se necesite, teclas o scroll*/
        private void InputChangeWeapon()
        {
            InputByKeys();
            InputByScroll();
            currentWeapon = ChangeWeapon();
        }

        /*Detecta el scroll del mouse incrementando o decrementando el valor del currentIndex, que es el que usamos para
         *identificar que armar esta actualmente segun la lista.
         *Tambien controlamos los limites que contiene volviendo al inicio o al final dependiendo al limite maximo y minimo
         */
        private void InputByScroll()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentindex += (int)Mathf.Sign(scroll);
                /*Maximo*/
                if (currentindex >= listWeapons.Count)
                {
                    currentindex = 0;
                }
                /*Minimo*/
                if (currentindex < 0)
                {
                    currentindex = listWeapons.Count - 1;
                }
            }
        }
        /*Detecta los cambio por teclado 1 2 3 cada uno con un indice respectivo*/
        private void InputByKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && listWeapons.Count > 0)
            {
                currentindex = 0;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) && listWeapons.Count > 1)
            {
                currentindex = 1;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) && listWeapons.Count > 2)
            {
                currentindex = 2;
            }
        }
        /*Cambia el arma y devuelve la que usara el usuario dependiendo del currentIndex que es nuestro indice
         *Tambien controlamos los limites para que no exista o se intente acceder a un indice no deseado
         */
        private Weapon ChangeWeapon()
        {
            if (listWeapons.Count == 0)
            {
                return null;
            }

            if (currentindex >= listWeapons.Count)
            {
                currentindex = listWeapons.Count - 1;
            }

            if (currentindex < 0)
            {
                currentindex = 0;
            }

            for (int i = 0; i < listWeapons.Count; i++)
            {
                // Verifica que el arma no sea null antes de activar/desactivar
                if (listWeapons[i] != null)
                {
                    bool isActive = i == currentindex;
                    listWeapons[i].gameObject.SetActive(isActive);
                }
            }

            // Retorna el arma actual solo si no es null
            return (currentindex >= 0 && currentindex < listWeapons.Count) ? listWeapons[currentindex] : null;
        }

        public Weapon GetCurrentWeapon()
        {
            return currentWeapon;
        }

        /// <summary>
        /// Añade un arma al inventario del jugador
        /// </summary>
        public void AddWeapon(Weapon newWeapon)
        {
            if (newWeapon != null && !listWeapons.Contains(newWeapon))
            {
                listWeapons.Add(newWeapon);
                newWeapon.gameObject.SetActive(false);
                
                // Si no tiene arma equipada, equipar esta
                if (currentWeapon == null)
                {
                    currentindex = listWeapons.Count - 1;
                    currentWeapon = ChangeWeapon();
                }
            }
        }

        /// <summary>
        /// Remueve un arma del inventario
        /// </summary>
        public void RemoveWeapon(Weapon weaponToRemove)
        {
            if (listWeapons.Contains(weaponToRemove))
            {
                listWeapons.Remove(weaponToRemove);
                
                // Si era el arma actual, cambiar a otra
                if (currentWeapon == weaponToRemove)
                {
                    currentindex = 0;
                    currentWeapon = ChangeWeapon();
                }
            }
        }
    }
}
