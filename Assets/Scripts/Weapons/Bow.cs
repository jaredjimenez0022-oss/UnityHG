using UnityEngine;

namespace Scripts
{
    /*El Bow hereda de Weapon, las caracteristicas principales*/
    public class Bow : Weapon, IAmmoWeapon
    {
        [Header("Ammo Settings")]
        [SerializeField] private int currentAmmo = 10;
        [SerializeField] private int maxAmmo = 30;

        public System.Action<int, int> OnAmmoChanged { get; set; }

        [SerializeField] private Arrow arrowPrefab;
        [SerializeField] private Transform pointer;
        private float maxDistance = 500;
        
        /*Se sobre escribe el metodo atacar para realizar el disparo*/
        public override void Attack()
        {
            if (currentAmmo > 0)
            {
                base.Attack();
                currentAmmo--;
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
                CreateArrow();
            }
            else
            {
                Debug.Log("No arrows left!");
            }
        }

        /*Crea y configura la flecha, desde la posición y la rotacion al puntero que se tiene como referencia a donde se quiere disparar*/
        public void CreateArrow()
        {
            Vector3 direction = pointer.position - transform.position;
            direction.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            GameObject newTarget = Instantiate(new GameObject("TargetObject"), transform.position + direction * maxDistance, Quaternion.identity);
            Debug.DrawLine(transform.position, newTarget.transform.position, Color.red, 5f);
            Arrow newArrow = Instantiate(arrowPrefab, transform.position, targetRotation);
            newArrow.InitArrow(damage, target, newTarget);
        }

        // Implementación de IAmmoWeapon
        public int GetCurrentAmmo() => currentAmmo;
        
        public int GetMaxAmmo() => maxAmmo;

        // Métodos para gestionar la munición
        public void AddAmmo(int amount)
        {
            currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        public void SetAmmo(int amount)
        {
            currentAmmo = Mathf.Clamp(amount, 0, maxAmmo);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        public void SetMaxAmmo(int newMaxAmmo)
        {
            maxAmmo = newMaxAmmo;
            if (currentAmmo > maxAmmo)
                currentAmmo = maxAmmo;
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        private void Start()
        {
            // Inicializar el HUD con los valores actuales
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }

        private void OnEnable()
        {
            base.ActiveAttack();
            // Notificar al HUD cuando el arma se activa
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        }


    }
}
