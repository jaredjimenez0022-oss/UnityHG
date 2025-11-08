using UnityEngine;

namespace Scripts
{
    /*La espada hereda de Weapon*/
    public class Sword : Weapon
    {
        //[SerializeField] private Animator swordAnimator;

        /*Sobreescribimos la clase ataque y lo realizamos mediante una animacion*/
        public override void Attack()
        {
            base.Attack();
            //if (swordAnimator != null)
            //{
            //    swordAnimator.SetTrigger("Attack");
            //}
            playerAnimatorNetwork.SetAttackSword();
        }
        /*Utilizamos el trigger para poder detectar cuando el ataque le llega al enemigo, por se de corto alcance*/
        public void OnTriggerEnter(Collider other)
        {
            //if (other.CompareTag(target))
            //{
            //    Health healthTarget = other.GetComponent<Health>();
            //    if (healthTarget != null)
            //    {
            //        healthTarget.DecrementHealth(damage);
            //        Debug.Log("Damage: " + damage + " with " + nameWeapon);
            //    }
            //}
            if (other.gameObject != gameObject)
            {
                PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
        }
    }
}
