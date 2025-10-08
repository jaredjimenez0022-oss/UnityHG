using UnityEngine;

public class Bow : MonoBehaviour, IWeapon
{
    public GameObject arrowPrefab; // Prefab de la flecha
    public Transform shootPoint;   // Punto de salida de la flecha
    public float shootForce = 20f; // Fuerza de disparo

    public void Use()
    {
        if (arrowPrefab == null || shootPoint == null)
        {
            Debug.LogWarning("⚠️ Falta asignar arrowPrefab o shootPoint en el Inspector.");
            return;
        }

        // Instanciamos la flecha usando la rotación del prefab + la dirección del shootPoint
        GameObject arrowObj = GameObject.Instantiate(arrowPrefab, shootPoint.position, shootPoint.rotation);
        Rigidbody rb = arrowObj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(shootPoint.forward * shootForce, ForceMode.Impulse);
        }

        Debug.Log("🏹 Flecha disparada!");
    }
}
