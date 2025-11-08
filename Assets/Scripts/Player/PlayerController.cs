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
        /*Creamos un weapon inicial para tenerlo como base y una lista donde iremos intercambiando las armas*/
        [SerializeField] private Weapon currentWeapon;
        [SerializeField] private List<Weapon> listWeapons = new List<Weapon>();
        private bool isWalk;
        private int currentindex = 0;

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
            playerAnimator.SetBool("Walk", isWalk);
        }

        /*Detecta el ataque segun el arma actual que tiene el juegador*/
        private void InputAttack()
        {
            if (Input.GetMouseButtonDown(0))
            {
                currentWeapon.StartAttack();
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
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentindex = 0;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                currentindex = 1;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                currentindex = 2;
            }
        }
        /*Cambia el arma y devuelve la que usara el usuario dependiendo del currentIndex que es nuestro indice
         *Tambien controlamos los limites para que no exista o se intente acceder a un indice no deseado
         */
        private Weapon ChangeWeapon()
        {
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
                bool isActive = i == currentindex;
                listWeapons[i].gameObject.SetActive(isActive);
            }

            return listWeapons[currentindex];
        }

        public Weapon GetCurrentWeapon()
        {
            return currentWeapon;
        }
    }
}
