using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 10;

    private void OnCollisionEnter(Collision col)
    {
        if (col.collider.CompareTag("Enemy"))
        {
            var e = col.collider.GetComponent<Enemy>();
            if (e != null) e.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}