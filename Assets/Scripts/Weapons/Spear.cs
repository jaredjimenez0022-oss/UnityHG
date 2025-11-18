using UnityEngine;

namespace Scripts
{
    /*La lanza hererda de Weapon*/
    public class Spear : Weapon
    {
        [SerializeField] private Animator spearAnimator;

        /*Sobreescribimos la clase ataque y lo realizamos mediante una animacion*/
        public override void Attack()
        {
            base.Attack();
            if (spearAnimator != null)
            {
                spearAnimator.SetTrigger("Attack");
            }
        }
        /*Utilizamos el trigger para poder detectar cuando el ataque le llega al enemigo, por se de corto alcance*/
        public void OnTriggerEnter(Collider other)
        {

            if (other.gameObject == gameObject) return;

            PlayerCombat playerCombat = other.gameObject.GetComponent<PlayerCombat>();
            if (playerCombat != null && playerCombat.IsAlive())
            {
                playerCombat.ApplyDamage(damage, Fusion.PlayerRef.None);
                Debug.Log($"Lanza aplicó {damage} de daño a {other.gameObject.name}");
            }
        }
    }
}