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
        
        // Sincronizar el índice del arma actual y el estado de ataque
        [Networked] private int currentindex { get; set; }
        [Networked] private TickTimer attackTimer { get; set; }
        
        // Sincronizar qué armas están disponibles (máximo 8 armas)
        [Networked, Capacity(8)] private NetworkArray<NetworkBool> weaponsAvailable { get; }
        
        // Flag para saber si ya se inicializó
        private bool isInitialized = false;

        public override void Spawned()
        {
            base.Spawned();
            
            // Inicializar disponibilidad de armas solo en el servidor
            if (Object.HasStateAuthority)
            {
                for (int i = 0; i < listWeapons.Count && i < weaponsAvailable.Length; i++)
                {
                    weaponsAvailable.Set(i, listWeapons[i].isAvailable);
                }
            }
            
            // Sincronizar el estado inicial de las armas
            SyncWeaponsFromNetwork();
            isInitialized = true;
        }

        public override void FixedUpdateNetwork()
        {
            // Solo el jugador con autoridad de input maneja el cambio de armas y ataques
            if (Object.HasInputAuthority)
            {
                InputChangeWeapon();
                InputAttack();
            }
            
            // Sincronizar disponibilidad desde la red en todos los clientes
            if (isInitialized)
            {
                SyncWeaponsFromNetwork();
            }
            
            // CRÍTICO: Todos los clientes deben actualizar la visibilidad del arma
            // para que se vea la sincronización del índice de arma
            UpdateWeaponVisibility();
            
            // Ejecutar ataque si el timer está activo
            if (attackTimer.ExpiredOrNotRunning(Runner) == false)
            {
                if (currentWeapon != null)
                {
                    currentWeapon.StartAttack();
                    if (audioSource != null && currentWeapon.audioClipEffect != null)
                    {
                        audioSource.PlayOneShot(currentWeapon.audioClipEffect);
                    }
                }
                attackTimer = TickTimer.None;
            }
        }

        private void Update()
        {
            // Removido - ahora se maneja en FixedUpdateNetwork
        }

        /*Detecta el ataque segun el arma actual que tiene el juegador*/
        private void InputAttack()
        {
            // Solo modificar si tenemos autoridad
            if (Object == null || !Object.HasStateAuthority)
                return;
                
            if(currentWeapon == null)
            {
                return;
            }

            if (!currentWeapon.isAvailable)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // Activar el timer para sincronizar el ataque
                attackTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
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
            // Solo modificar si tenemos autoridad
            if (Object == null || !Object.HasStateAuthority)
                return;
                
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                int oldIndex = currentindex;
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
                
                Debug.Log($"[WeaponManager] Arma cambiada: {oldIndex} → {currentindex} ({listWeapons[currentindex].nameWeapon})");
            }
        }
        /*Detecta los cambio por teclado 1 2 3 cada uno con un indice respectivo*/
        private void InputByKeys()
        {
            // Solo modificar si tenemos autoridad
            if (Object == null || !Object.HasStateAuthority)
                return;
                
            int oldIndex = currentindex;
            bool changed = false;
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentindex = 0;
                changed = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                currentindex = 1;
                changed = true;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                currentindex = 2;
                changed = true;
            }
            
            if (changed && currentindex < listWeapons.Count)
            {
                Debug.Log($"[WeaponManager] Arma cambiada: {oldIndex} → {currentindex} ({listWeapons[currentindex].nameWeapon})");
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

            UpdateWeaponVisibility();
            return listWeapons[currentindex];
        }

        // Sincronizar disponibilidad de armas desde el NetworkArray
        private void SyncWeaponsFromNetwork()
        {
            for (int i = 0; i < listWeapons.Count && i < weaponsAvailable.Length; i++)
            {
                if (listWeapons[i] != null)
                {
                    listWeapons[i].isAvailable = weaponsAvailable[i];
                }
            }
        }
        
        // Método mejorado para actualizar la visibilidad del arma (sincronizado para todos)
        private void UpdateWeaponVisibility()
        {
            if (listWeapons == null || listWeapons.Count == 0)
                return;

            // Asegurar que el índice esté dentro del rango válido
            int safeIndex = Mathf.Clamp(currentindex, 0, listWeapons.Count - 1);

            // Actualizar la visibilidad de todas las armas
            for (int i = 0; i < listWeapons.Count; i++)
            {
                if (listWeapons[i] != null)
                {
                    // Solo mostrar el arma actual si está disponible
                    bool shouldBeActive = (i == safeIndex) && listWeapons[i].isAvailable;
                    
                    // Solo cambiar si es necesario para evitar llamadas innecesarias
                    if (listWeapons[i].gameObject.activeSelf != shouldBeActive)
                    {
                        listWeapons[i].gameObject.SetActive(shouldBeActive);
                    }
                }
            }

            // Actualizar referencia al arma actual
            if (safeIndex >= 0 && safeIndex < listWeapons.Count)
            {
                currentWeapon = listWeapons[safeIndex];
            }
        }

        public Weapon GetCurrentWeapon()
        {
            return currentWeapon;
        }

        public void ActiveWeapon(string nameWeapon)
        {
            // Solo el servidor puede activar armas
            if (!Object.HasStateAuthority)
                return;
                
            for (int i = 0; i < listWeapons.Count && i < weaponsAvailable.Length; i++)
            {
                if (listWeapons[i].nameWeapon == nameWeapon && !weaponsAvailable[i])
                {
                    // Sincronizar por red
                    weaponsAvailable.Set(i, true);
                    listWeapons[i].isAvailable = true;
                    currentindex = i;
                    
                    Debug.Log($"[WeaponManagerNetwork] Arma activada: {nameWeapon} (índice {i})");
                }
            }
        }
    }
}
