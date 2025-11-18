using Fusion;
using UnityEngine;

namespace Scripts
{
    /*Clase padre para las diferentes armas, donde sus caracteristicas principales son daño, nombre, objetivo y tiempo de ataque*/
    public class Weapon : NetworkBehaviour
    {
        [SerializeField] protected int damage;
        [SerializeField] public string nameWeapon;
        [SerializeField] protected string target;
        [SerializeField] protected float cooldownTime;
        [SerializeField] public AudioClip audioClipEffect;
        private bool canAttack = true;
        public bool isAvailable = false;

        private void OnEnable()
        {
            canAttack = true;
        }
        /*Es llamado para realizar un ataque*/
        public virtual void Attack()
        {
            Debug.Log("Attack with " + nameWeapon);
        }
        /*Controla que no se hagan varios ataques a la vez*/
        public void StartAttack()
        {
            if (!canAttack)
            {
                return;
            }
            Attack();
            canAttack = false;
            Invoke("ActiveAttack", cooldownTime);
        }
        /*Vueve a reactivace despues de Invoke("ActiveAttack", cooldownTime);
         *permitiendo al jugador realizar el siguiente ataque
         */
        public virtual void ActiveAttack()
        {
            canAttack = true;
        }
    }
}