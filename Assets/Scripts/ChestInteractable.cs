using UnityEngine;

public class ChestInteractable : MonoBehaviour
{
    [Header("Chest Settings")]
    public Transform lidPivot; // ChestV3_Top
    public float openAngle = -60f;
    public float openSpeed = 2f;

    [Header("Loot")]
    public GameObject[] possibleLoot;

    private bool isOpen = false;
    private bool isOpening = false;
    private float currentRotation = 0f;
    private Vector3 originalRotation; // Guardar rotación original

    void Start()
    {
        // Guardar la rotación original de la tapa
        if (lidPivot != null)
        {
            originalRotation = lidPivot.localEulerAngles;
            Debug.Log($"Rotación original de la tapa: {originalRotation}");
        }
        else
        {
            Debug.LogError("Lid Pivot no está asignado!");
        }

        // Verificar collider
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Debug.Log($" Collider: {collider.name}, IsTrigger: {collider.isTrigger}");
        }
    }

    void Update()
    {
        if (isOpening)
        {
            // Animación suave de apertura
            currentRotation = Mathf.Lerp(currentRotation, openAngle, openSpeed * Time.deltaTime);

            // Aplicar rotación RELATIVA a la original
            lidPivot.localRotation = Quaternion.Euler(
                originalRotation.x + currentRotation,
                originalRotation.y,
                originalRotation.z
            );

            if (Mathf.Abs(currentRotation - openAngle) < 1f)
            {
                isOpening = false;
                SpawnLoot();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isOpen && other.CompareTag("Player"))
        {
            Debug.Log("Jugador detectado - abriendo cofre!");
            OpenChest();
        }
    }

    public void OpenChest()
    {
        if (isOpen) return;

        isOpen = true;
        isOpening = true;
        Debug.Log("Cofre abierto!");
    }

    void SpawnLoot()
    {
        if (possibleLoot.Length > 0)
        {
            GameObject lootToSpawn = possibleLoot[Random.Range(0, possibleLoot.Length)];
            Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;
            Instantiate(lootToSpawn, spawnPosition, Quaternion.identity);
            Debug.Log("Loot generado!");
        }
        else
        {
            Debug.Log("Cofre vacío - agrega items en el Inspector");
        }
    }
}