
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
    public struct NetworkInventorySlot : INetworkStruct
    {
        public NetworkString<_16> itemName;
        public int quantity;
        public int itemTypeIndex;

        public bool IsEmpty => quantity <= 0;
    }

    // Array de slots sincronizado en red
    [Networked, Capacity(6)]
    public NetworkArray<NetworkInventorySlot> InventorySlots { get; }

    // Para detectar cambios en botones
    [Networked] private NetworkButtons previousButtons { get; set; }

    public bool isInventoryOpen { get; private set; } = false;
    private Dictionary<string, Sprite> itemIconCache = new Dictionary<string, Sprite>();

    // Debug: contador para ver si algo se ejecuta en loop
    private int renderFrameCount = 0;
    private float lastDebugTime = 0f;

    // Para evitar que TAB se procese múltiples veces en el mismo frame Unity
    private bool tabProcessedThisFrame = false;
    private int lastFrameCount = -1;

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

        // Asegurar que el panel está cerrado al inicio
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;
        }
    }

    /// <summary>
    /// Configura las referencias UI dinámicamente (llamado por NetworkUIManager)
    /// </summary>
    public void SetUIReferences(GameObject panel, Image[] slotImgs, TextMeshProUGUI[] slotQtyTexts)
    {
        if (!HasInputAuthority) return;

        inventoryPanel = panel;
        slotImages = slotImgs;
        slotQuantityTexts = slotQtyTexts;
        maxStackSize = 99;

        // Asegurar que el panel comienza cerrado
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;
        }

        // Inicializar UI
        LoadItemIconCache();
        UpdateInventoryUI();
    }


    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        // Verificar cambios en el inventario y actualizar UI
        if (HasInventoryChanged())
        {
            UpdateInventoryUI();
        }

        if (GetInput(out NetworkInputData input))
        {
            // Actualizar botones previos
            previousButtons = input.buttons;
        }
    }
    private bool HasInventoryChanged()
    {
        // Verificar si algún slot cambió desde la última actualización
        for (int i = 0; i < maxSlots; i++)
        {
            var slot = InventorySlots[i];
            if (!slot.IsEmpty)
            {
                return true; // Hay items en el inventario
            }
        }
        return false;
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        // Resetear flag al inicio de cada frame Unity nuevo
        if (Time.frameCount != lastFrameCount)
        {
            tabProcessedThisFrame = false;
            lastFrameCount = Time.frameCount;
            renderFrameCount = 0;
        }

        renderFrameCount++;

        // SOLUCIÓN ALTERNATIVA: Detectar TAB directamente con Input System en Render()
        // Esto evita el AssertException de Fusion con InputButtons.Inventory
        // IMPORTANTE: Solo procesar UNA VEZ por frame Unity (Render() se llama múltiples veces)
        if (UnityEngine.InputSystem.Keyboard.current != null && !tabProcessedThisFrame)
        {
            if (UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Debug.Log($"[Inventory] TAB detectado en frame {Time.frameCount}, render call #{renderFrameCount}");
                ToggleInventory();
                tabProcessedThisFrame = true;
            }
        }

        // Actualizar UI si está abierto
        if (isInventoryOpen)
        {
            UpdateInventoryUI();

            // Logging diagnóstico cada segundo
            if (Time.time - lastDebugTime > 1f)
            {
                Debug.Log($"[Inventory] Render() llamado {renderFrameCount} veces en el último segundo. Cursor: visible={Cursor.visible}, lockState={Cursor.lockState}");
                lastDebugTime = Time.time;
            }

            // FORZAR cursor visible continuamente mientras el inventario esté abierto
            // Esto previene que otros scripts lo sobrescriban
            if (Cursor.lockState != CursorLockMode.None)
            {
                Debug.LogWarning($"[Inventory] Frame {Time.frameCount}: Cursor lockState incorrecto: {Cursor.lockState} - forzando None");
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Debug.LogWarning($"[Inventory] Frame {Time.frameCount}: Cursor invisible - forzando visible");
                Cursor.visible = true;
            }
        }
        else
        {
            // Cuando el inventario está cerrado, asegurar que el cursor esté bloqueado
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        Debug.Log($"[Inventory] ===== TOGGLE INVENTORY (Frame {Time.frameCount}) =====");
        Debug.Log($"[Inventory] Nuevo estado: isInventoryOpen = {isInventoryOpen}");

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
            Debug.Log($"[Inventory] Panel activado: {inventoryPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[Inventory] ERROR: inventoryPanel es NULL!");
        }

        // NO deshabilitar FusionInputProvider completamente porque rompe el menú de pausa
        // En su lugar, HandleLookRotation ya está verificando isInventoryOpen

        // Manejar cursor de forma más agresiva
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log($"[Inventory] Inventario ABIERTO - Cursor configurado: visible={Cursor.visible}, lockState={Cursor.lockState}");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log($"[Inventory] Inventario CERRADO - Cursor configurado: visible={Cursor.visible}, lockState={Cursor.lockState}");
        }

        Debug.Log($"[Inventory] ===== FIN TOGGLE =====");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddItem(NetworkString<_16> itemName, int itemTypeIndex, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;

        int existingSlot = FindItemSlot(itemName.ToString());

        if (existingSlot != -1)
        {
            var slot = InventorySlots[existingSlot];
            if (slot.quantity < maxStackSize)
            {
                slot.quantity++;
                InventorySlots.Set(existingSlot, slot);
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

            // NOTIFICAR A TODOS LOS CLIENTES
            RPC_NotifyItemAdded(emptySlot, 1);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NotifyItemAdded(int slotIndex, int newQuantity, RpcInfo info = default)
    {
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

        if (slotImages == null || slotQuantityTexts == null)
        {
            Debug.LogError($"[NetworkInventorySystem] Referencias UI nulas - Images: {slotImages == null}, Texts: {slotQuantityTexts == null}");
            return;
        }

        for (int i = 0; i < slotImages.Length && i < maxSlots; i++)
        {
            var slot = InventorySlots[i];

            if (!slot.IsEmpty)
            {
                string itemName = slot.itemName.ToString();

                // Obtener icono desde cache, NO desde ItemDatabase
                Sprite icon = itemIconCache.ContainsKey(itemName) ? itemIconCache[itemName] : null;

                slotImages[i].sprite = icon;
                slotImages[i].color = icon != null ? Color.white : Color.blue;

                if (i < slotQuantityTexts.Length && slotQuantityTexts[i] != null)
                {
                    slotQuantityTexts[i].text = slot.quantity > 1 ? slot.quantity.ToString() : "";
                    slotQuantityTexts[i].gameObject.SetActive(slot.quantity > 1);
                }
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = new Color(1, 1, 1, 0.3f);

                if (i < slotQuantityTexts.Length && slotQuantityTexts[i] != null)
                {
                    slotQuantityTexts[i].text = "";
                    slotQuantityTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void LoadItemIconCache()
    {
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("[NetworkInventorySystem] ItemDatabase.Instance es NULL");
            return;
        }

        itemIconCache.Clear();

        foreach (var item in ItemDatabase.Instance.items)
        {
            if (!itemIconCache.ContainsKey(item.itemName))
            {
                itemIconCache.Add(item.itemName, item.icon);
            }
        }
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

    #region Item Equipping System (Double-Click)

    // Variables para tracking de doble-click
    private int lastClickedSlot = -1;
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f; // 300ms para doble-click

    /// <summary>
    /// Llamado desde UI cuando se hace click en un slot del inventario
    /// Detecta doble-click y equipa items equipables (Sword, Bow, Spear)
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        if (!HasInputAuthority) return;
        if (slotIndex < 0 || slotIndex >= maxSlots) return;

        var slot = InventorySlots[slotIndex];
        if (slot.IsEmpty) return;

        // Detectar doble-click
        float currentTime = Time.time;
        bool isDoubleClick = (slotIndex == lastClickedSlot) &&
                            (currentTime - lastClickTime <= doubleClickThreshold);

        if (isDoubleClick)
        {
            // Doble-click detectado - intentar equipar
            string itemName = slot.itemName.ToString();

            if (IsEquippableItem(itemName))
            {
                Debug.Log($"[Inventory] Doble-click en {itemName} - Equipando...");
                TryEquipItem(slotIndex, itemName);
            }
            else
            {
                Debug.Log($"[Inventory] {itemName} no es equipable (solo Sword, Bow y Spear)");
            }

            // Reset tracking después de doble-click
            lastClickedSlot = -1;
            lastClickTime = 0f;
        }
        else
        {
            // Primer click - guardar para tracking
            lastClickedSlot = slotIndex;
            lastClickTime = currentTime;
        }
    }

    /// <summary>
    /// Verifica si un item es equipable (solo Sword, Bow y Spear tienen animaciones)
    /// </summary>
    private bool IsEquippableItem(string itemName)
    {
        // Normalizar nombre (remover comillas si existen)
        itemName = itemName.Trim().Trim('"');

        return itemName == "Sword" || itemName == "Bow" || itemName == "Spear";
    }

    /// <summary>
    /// Intenta equipar un item desde el inventario
    /// </summary>
    private void TryEquipItem(int slotIndex, string itemName)
    {
        if (!HasInputAuthority) return;

        // Obtener referencia al WeaponManager
        var weaponManager = GetComponent<Scripts.WeaponManagerNetwork>();
        if (weaponManager == null)
        {
            Debug.LogError("[Inventory] No se encontró WeaponManagerNetwork en el Player!");
            return;
        }

        // Normalizar nombre
        itemName = itemName.Trim().Trim('"');

        // Activar el arma en el WeaponManager
        weaponManager.ActiveWeapon(itemName);
        Debug.Log($"[Inventory] Arma '{itemName}' activada en WeaponManager");

        // Consumir 1 unidad del item vía RPC
        RPC_ConsumeEquippedItem(slotIndex);
    }

    /// <summary>
    /// RPC para consumir un item al equiparlo (servidor autoridad)
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ConsumeEquippedItem(int slotIndex, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;
        if (slotIndex < 0 || slotIndex >= maxSlots) return;

        var slot = InventorySlots[slotIndex];
        if (slot.IsEmpty) return;

        // Decrementar cantidad
        slot.quantity--;

        // Si se acabó, vaciar el slot
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

        // Notificar actualización de UI
        RPC_NotifyItemConsumed(slotIndex);
    }

    /// <summary>
    /// RPC para notificar que un item fue consumido (actualizar UI)
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyItemConsumed(int slotIndex, RpcInfo info = default)
    {
        UpdateInventoryUI();
        Debug.Log($"[Inventory] Item en slot {slotIndex} consumido");
    }

    #endregion
}
