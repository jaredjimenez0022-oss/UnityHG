using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Item";
    public Sprite itemIcon;
    public ItemType itemType = ItemType.Weapon;

    public enum ItemType
    {
        Weapon,
        Shield,
        Compass,
        Consumable
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Jugador tocó: {itemName}");

            // Intentar agregar al inventario
            if (InventorySystem.Instance != null && InventorySystem.Instance.AddItem(this.gameObject))
            {
                // Si se agregó exitosamente, esconder el objeto en escena
                this.gameObject.SetActive(false);
                Debug.Log($" {itemName} agregado al inventario");
            }
            else
            {
                Debug.Log("No se pudo agregar al inventario");
            }
        }
    }
}