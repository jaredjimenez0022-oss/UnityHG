using UnityEngine;
using Fusion;

/// <summary>
/// Item que puede ser recogido y sincronizado en red
/// Attachar a: Prefabs de items (Arrow, Knife, etc.)
/// IMPORTANTE: El prefab debe tener NetworkObject component
/// </summary>
public class NetworkItem : NetworkBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Item";
    public Sprite itemIcon;
    public Item.ItemType itemType = Item.ItemType.Weapon;

    [Header("Pickup Settings")]
    public float pickupRadius = 2f;
    public LayerMask playerLayer;

    // Estado del item (si fue recogido o no)
    [Networked] public NetworkBool IsPickedUp { get; set; }

    private Collider itemCollider;
    private Renderer[] itemRenderers;

    public override void Spawned()
    {
        base.Spawned();

        itemCollider = GetComponent<Collider>();
        itemRenderers = GetComponentsInChildren<Renderer>();

        // Si el item ya fue recogido antes de que este cliente se conectara
        if (IsPickedUp)
        {
            HideItem();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo procesar en el servidor
        if (!HasStateAuthority) return;

        // Si ya fue recogido, ignorar
        if (IsPickedUp) return;

        // Verificar si es un jugador
        if (other.CompareTag("Player"))
        {
            NetworkPlayer player = other.GetComponent<NetworkPlayer>();
            if (player != null && player.HasInputAuthority)
            {
                // Intentar añadir al inventario del jugador
                TryPickup(player);
            }
        }
    }

    /// <summary>
    /// Intenta que el jugador recoja el item
    /// Solo se ejecuta en el servidor
    /// </summary>
    private void TryPickup(NetworkPlayer player)
    {
        if (!HasStateAuthority) return;
        if (IsPickedUp) return;

        // Obtener el sistema de inventario del jugador
        NetworkInventorySystem inventory = player.GetComponent<NetworkInventorySystem>();

        if (inventory != null && !inventory.IsInventoryFull())
        {
            // Marcar como recogido
            IsPickedUp = true;

            // Notificar al cliente del jugador para que añada el item a su inventario
            RPC_NotifyPickup(player.Object.InputAuthority);

            Debug.Log($"[Server] {itemName} recogido por {player.Object.InputAuthority}");

            // Ocultar el item para todos
            HideItem();

            // Opcional: Despawnear después de un delay
            // Runner.Despawn(Object, 2f);
        }
    }

    /// <summary>
    /// RPC para notificar al cliente que debe añadir el item a su inventario
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPickup(PlayerRef playerWhoPickedUp, RpcInfo info = default)
    {
        // Solo el jugador que recogió el item lo añade a su inventario local
        NetworkPlayer localPlayer = FindLocalPlayer();
        if (localPlayer != null && localPlayer.Object.InputAuthority == playerWhoPickedUp)
        {
            NetworkInventorySystem inventory = localPlayer.GetComponent<NetworkInventorySystem>();
            if (inventory != null)
            {
                inventory.TryAddItem(itemName, itemType, itemIcon);
                Debug.Log($"[Client] {itemName} añadido a mi inventario");
            }
        }

        // Ocultar el item para todos los clientes
        HideItem();
    }

    private void HideItem()
    {
        if (itemCollider != null)
            itemCollider.enabled = false;

        if (itemRenderers != null)
        {
            foreach (Renderer rend in itemRenderers)
            {
                if (rend != null)
                    rend.enabled = false;
            }
        }
    }

    private NetworkPlayer FindLocalPlayer()
    {
        //foreach (NetworkPlayer player in FindObjectsOfType<NetworkPlayer>())
        foreach (NetworkPlayer player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (player.HasInputAuthority)
                return player;
        }
        return null;
    }

    // Visualización en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
