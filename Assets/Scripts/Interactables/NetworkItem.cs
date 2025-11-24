using UnityEngine;
using Fusion;
using System.Linq;

public class NetworkItem : NetworkBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Item";
    public Sprite itemIcon;
    public Item.ItemType itemType = Item.ItemType.Weapon;

    [Header("Pickup Settings")]
    public float pickupRadius = 2f;
    public LayerMask playerLayer;

    [Networked] public NetworkBool IsPickedUp { get; set; }
    [Networked] public TickTimer PickupDelayTimer { get; set; }

    private Collider itemCollider;
    private Renderer[] itemRenderers;

    public override void Spawned()
    {
        base.Spawned();

        itemCollider = GetComponent<Collider>();
        itemRenderers = GetComponentsInChildren<Renderer>();

        if (HasStateAuthority)
        {
            PickupDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
        }

        if (IsPickedUp)
        {
            HideItem();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;
        if (!PickupDelayTimer.ExpiredOrNotRunning(Runner)) return;
        if (IsPickedUp) return;

        NetworkPlayer player = other.GetComponent<NetworkPlayer>();
        if (player == null) player = other.GetComponentInParent<NetworkPlayer>();
        if (player == null) return;

        TryPickup(player);
    }

    private void TryPickup(NetworkPlayer player)
    {
        if (!HasStateAuthority) return;
        if (IsPickedUp) return;

        // Marcar como recogido
        IsPickedUp = true;

        // SOLUCIÓN DIRECTA: El servidor modifica DIRECTAMENTE el inventario
        AddItemToPlayerInventory(player);

        // Ocultar item
        HideItem();
        RPC_NotifyPickup();
    }

    // ✅ MÉTODO NUEVO: El servidor modifica DIRECTAMENTE el inventario
    private void AddItemToPlayerInventory(NetworkPlayer player)
    {
        NetworkInventorySystem inventory = player.GetComponent<NetworkInventorySystem>();
        if (inventory == null) return;

        // Buscar slot existente para apilar
        for (int i = 0; i < 6; i++)
        {
            var slot = inventory.InventorySlots[i];
            if (!slot.IsEmpty && slot.itemName.ToString() == itemName && slot.quantity < 99)
            {
                // Apilar item
                slot.quantity++;
                inventory.InventorySlots.Set(i, slot);
                inventory.RPC_NotifyItemAdded(i, slot.quantity);
                return;
            }
        }

        // Buscar slot vacío
        for (int i = 0; i < 6; i++)
        {
            var slot = inventory.InventorySlots[i];
            if (slot.IsEmpty)
            {
                // Nuevo item
                var newSlot = new NetworkInventorySystem.NetworkInventorySlot
                {
                    itemName = itemName,
                    quantity = 1,
                    itemTypeIndex = (int)itemType
                };
                inventory.InventorySlots.Set(i, newSlot);
                inventory.RPC_NotifyItemAdded(i, 1);
                return;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPickup(RpcInfo info = default)
    {
        HideItem();
    }

    private void HideItem()
    {
        if (itemCollider != null) itemCollider.enabled = false;
        if (itemRenderers != null)
        {
            foreach (Renderer rend in itemRenderers)
                if (rend != null) rend.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}