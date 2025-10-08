using UnityEngine;

public class Arrow : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
/// <summary>
/// //
/// </summary>
    void Update()
    {
        // Flecha se orienta hacia donde se mueve
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Detenemos la flecha clavándola
        if (rb != null)
        {
            rb.isKinematic = true; // la "pega" en el objeto
            rb.linearVelocity = Vector3.zero;
        }

        // Si golpea a un enemigo
        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(10); // daño configurable
        }

        // Destruir después de 1 segundos
        Destroy(gameObject, 1f);
    }
}
