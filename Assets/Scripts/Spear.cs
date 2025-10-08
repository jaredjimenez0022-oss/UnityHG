using UnityEngine;
using UnityEngine.InputSystem;

public class Spear : MonoBehaviour, IWeapon
{
    public int damage = 15;
    public float range = 3.5f;

    public void Use()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Debug.Log("🪃 Lanza golpeó al enemigo!");
                }
            }
        }
    }
}
