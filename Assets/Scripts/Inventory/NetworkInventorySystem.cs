
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Sistema de inventario sincronizado en red con Photon Fusion
/// Attachar a: NetworkPlayer prefab
/// </summary>
public class NetworkInventorySystem : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Image[] slotImages;
    [SerializeField] private TextMeshProUGUI[] slotQuantityTexts;

    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 6;
    [SerializeField] private int maxStackSize = 99;

    // Estructura de datos sincronizada para un slot de inventario
    private struct NetworkInventorySlot : INetworkStruct
    {
        public NetworkString<_16> itemName;
        public int quantity;
        public int itemTypeIndex;

        public bool IsEmpty => quantity <= 0;
    }

    // Array de slots sincronizado en red
    [Networked, Capacity(6)]
    private NetworkArray<NetworkInventorySlot> InventorySlots { get; }

    // Para detectar cambios en botones
    [Networked] private NetworkButtons previousButtons { get; set; }

    private bool isInventoryOpen = false;
    private Dictionary<string, Sprite> itemIconCache = new Dictionary<string, Sprite>();

    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
        {
            for (int i = 0; i < maxSlots; i++)
            {
                var emptySlot = new NetworkInventorySlot
                {
                    itemName = "",
                    quantity = 0,
                    itemTypeIndex = 0
                };
                InventorySlots.Set(i, emptySlot);
            }
        }

        if (HasInputAuthority)
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);

            LoadItemIconCache();
            UpdateInventoryUI();
        }
        else
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);
        }
    }

    public void InitializeLocalPlayer()
    {
        // Ya se inicializa en Spawned(), pero este método permite inicialización adicional
        if (!HasInputAuthority) return;

        Debug.Log("[NetworkInventorySystem] Inicializado para jugador local");

        // Asegurar que el panel está cerrado al inicio
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        if (GetInput(out NetworkInputData input))
        {
            // Detectar si el botón de inventario fue presionado usando GetPressed
            var pressed = input.buttons.GetPressed(previousButtons);

            if (pressed.IsSet(InputButtons.Inventory))
            {
                ToggleInventory();
            }

            // Actualizar botones previos
            previousButtons = input.buttons;
        }
    }

    public override void Render()
    {
        if (HasInputAuthority && isInventoryOpen)
        {
            UpdateInventoryUI();
        }
    }

    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
        }

        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isInventoryOpen;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddItem(NetworkString<_16> itemName, int itemTypeIndex, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;

        Debug.Log($"[Server] Añadiendo item: {itemName}");

        int existingSlot = FindItemSlot(itemName.ToString());

        if (existingSlot != -1)
        {
            var slot = InventorySlots[existingSlot];
            if (slot.quantity < maxStackSize)
            {
                slot.quantity++;
                InventorySlots.Set(existingSlot, slot);
                Debug.Log($"[Server] Item apilado. Nuevo total: x{slot.quantity}");
                RPC_NotifyItemAdded(existingSlot, slot.quantity);
                return;
            }
        }

        int emptySlot = FindEmptySlot();
        if (emptySlot != -1)
        {
            var newSlot = new NetworkInventorySlot
            {
                itemName = itemName,
                quantity = 1,
                itemTypeIndex = itemTypeIndex
            };
            InventorySlots.Set(emptySlot, newSlot);
            Debug.Log($"[Server] Item añadido a slot {emptySlot}");
            RPC_NotifyItemAdded(emptySlot, 1);
        }
        else
        {
            Debug.Log("[Server] Inventario lleno");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_NotifyItemAdded(int slotIndex, int newQuantity, RpcInfo info = default)
    {
        Debug.Log($"[Client] Item agregado confirmado - Slot {slotIndex}: x{newQuantity}");
        UpdateInventoryUI();
    }

    public void TryAddItem(string itemName, Item.ItemType itemType, Sprite icon)
    {
        if (!HasInputAuthority) return;

        if (!itemIconCache.ContainsKey(itemName))
        {
            itemIconCache[itemName] = icon;
        }

        NetworkString<_16> netItemName = itemName;
        RPC_AddItem(netItemName, (int)itemType);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RemoveItem(int slotIndex, int quantity, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;
        if (slotIndex < 0 || slotIndex >= maxSlots) return;

        var slot = InventorySlots[slotIndex];
        if (slot.IsEmpty) return;

        slot.quantity -= quantity;

        if (slot.quantity <= 0)
        {
            slot = new NetworkInventorySlot
            {
                itemName = "",
                quantity = 0,
                itemTypeIndex = 0
            };
        }

        InventorySlots.Set(slotIndex, slot);
        Debug.Log($"[Server] Removido {quantity} items del slot {slotIndex}");
    }

    private int FindItemSlot(string itemName)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            var slot = InventorySlots[i];
            if (!slot.IsEmpty && slot.itemName.ToString() == itemName)
            {
                return i;
            }
        }
        return -1;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (InventorySlots[i].IsEmpty)
            {
                return i;
            }
        }
        return -1;
    }

    private void UpdateInventoryUI()
    {
        if (!HasInputAuthority) return;

        for (int i = 0; i < slotImages.Length && i < maxSlots; i++)
        {
            var slot = InventorySlots[i];

            if (!slot.IsEmpty)
            {
                string itemName = slot.itemName.ToString();
                Sprite icon = itemIconCache.ContainsKey(itemName) ? itemIconCache[itemName] : null;

                slotImages[i].sprite = icon;
                slotImages[i].color = icon != null ? Color.white : Color.blue;

                if (slotQuantityTexts != null && i < slotQuantityTexts.Length && slotQuantityTexts[i] != null)
                {
                    slotQuantityTexts[i].text = slot.quantity.ToString();
                    slotQuantityTexts[i].gameObject.SetActive(true);
                }
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = new Color(1, 1, 1, 0.3f);

                if (slotQuantityTexts != null && i < slotQuantityTexts.Length && slotQuantityTexts[i] != null)
                {
                    slotQuantityTexts[i].text = "";
                    slotQuantityTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void LoadItemIconCache()
    {
        // Los iconos se cargan dinámicamente cuando se recoge el item
    }

    public bool IsInventoryFull()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (InventorySlots[i].IsEmpty)
                return false;
        }
        return true;
    }

    public bool HasItem(string itemName)
    {
        return FindItemSlot(itemName) != -1;
    }

    public int GetItemQuantity(string itemName)
    {
        int slot = FindItemSlot(itemName);
        if (slot != -1)
        {
            return InventorySlots[slot].quantity;
        }
        return 0;
    }
}
