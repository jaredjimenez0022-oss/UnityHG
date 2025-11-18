using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class WeaponManagerNetwork : NetworkBehaviour
    {
        [SerializeField] private Weapon currentWeapon;
        [SerializeField] private List<Weapon> listWeapons = new List<Weapon>();
        //Controlador de sonido se puede realizar en un script aparte para mejor control, ahora solo esta para pruebas
        [SerializeField] private AudioSource audioSource;
        private int currentindex = 0;

        private void Start()
        {
            for (int i = 0; i < listWeapons.Count; i++)
            {
                if (!listWeapons[i].isAvailable)
                {
                    listWeapons[i].gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            InputChangeWeapon();
            InputAttack();
        }

        /*Detecta el ataque segun el arma actual que tiene el juegador*/
        private void InputAttack()
        {
            if(currentWeapon == null)
            {
                return;
            }

            if (!currentWeapon.isAvailable)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                currentWeapon.StartAttack();
                audioSource.PlayOneShot(currentWeapon.audioClipEffect);
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
                bool isActive = i == currentindex && listWeapons[i].isAvailable;
                listWeapons[i].gameObject.SetActive(isActive);
            }

            return listWeapons[currentindex];
        }

        public Weapon GetCurrentWeapon()
        {
            return currentWeapon;
        }

        public void ActiveWeapon(string nameWeapon)
        {
            for (int i = 0; i < listWeapons.Count; i++)
            {
                if (listWeapons[i].nameWeapon == nameWeapon && !listWeapons[i].isAvailable)
                {
                    listWeapons[i].isAvailable = true;
                    currentindex = i;
                }
            }
        }
    }
}
