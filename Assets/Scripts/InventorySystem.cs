using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InventorySystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Image[] slotImages;

    private List<GameObject> inventoryItems = new List<GameObject>();
    private bool isInventoryOpen = false;

    public static InventorySystem Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void Update()
    {
        //  SOLUCIÓN DIRECTA - Detectar TAB directamente
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);
            Debug.Log(isInventoryOpen ? " Inventario ABIERTO" : " Inventario CERRADO");
        }
        else
        {
            Debug.LogError(" InventoryPanel no está asignado");
        }

        // Pausar/despausar juego
        Time.timeScale = isInventoryOpen ? 0f : 1f;

        // Control del cursor
        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isInventoryOpen;
    }

    public bool AddItem(GameObject item)
    {
        if (inventoryItems.Count >= 6)
        {
            Debug.Log(" Inventario lleno!");
            return false;
        }

        inventoryItems.Add(item);
        UpdateInventoryUI();
        Debug.Log($" {item.name} agregado al inventario");
        return true;
    }

    void UpdateInventoryUI()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (i < inventoryItems.Count && inventoryItems[i] != null)
            {
                Item item = inventoryItems[i].GetComponent<Item>();
                if (item != null && item.itemIcon != null)
                {
                    slotImages[i].sprite = item.itemIcon;
                    slotImages[i].color = Color.white;
                }
                else
                {
                    // Si no tiene icono, poner color azul
                    slotImages[i].sprite = null;
                    slotImages[i].color = Color.blue;
                }
            }
            else
            {
                // Slot vacío
                slotImages[i].sprite = null;
                slotImages[i].color = new Color(1, 1, 1, 0.3f);
            }
        }
    }

    public bool IsInventoryFull()
    {
        return inventoryItems.Count >= 6;
    }
}