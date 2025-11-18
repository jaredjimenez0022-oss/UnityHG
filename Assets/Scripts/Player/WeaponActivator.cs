using UnityEngine;

namespace Scripts
{
    public class WeaponActivator : MonoBehaviour
    {
        [SerializeField] private GameObject keyIndicator;
        [SerializeField] private string nameWeapon;
        
        private void OnTriggerEnter(Collider other)
        {
            WeaponManagerNetwork weaponManager = other.gameObject.GetComponent<WeaponManagerNetwork>();
            if (weaponManager != null)
            {
                keyIndicator.SetActive(true);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            WeaponManagerNetwork weaponManager = other.gameObject.GetComponent<WeaponManagerNetwork>();
            if (weaponManager != null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    weaponManager.ActiveWeapon(nameWeapon);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            WeaponManagerNetwork weaponManager = other.gameObject.GetComponent<WeaponManagerNetwork>();
            if (weaponManager != null)
            {
                keyIndicator.SetActive(false);
            }
        }
    }
}
