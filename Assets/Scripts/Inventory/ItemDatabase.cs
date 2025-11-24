using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base de datos centralizada de items para sincronización de sprites en red
/// Crear asset: Assets → Create → Game Data → Item Database
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game Data/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemData
    {
        public string itemName;
        public Sprite icon;
        public Item.ItemType itemType;
    }

    [Header("Item Registry")]
    public List<ItemData> items = new List<ItemData>();

    // Singleton para acceso global
    private static ItemDatabase instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                // Cargar desde Resources
                instance = Resources.Load<ItemDatabase>("ItemDatabase");

                if (instance == null)
                {
                    Debug.LogError("[ItemDatabase] No se encontró ItemDatabase en Resources/. " +
                        "Crea uno en: Assets/Resources/ItemDatabase.asset");
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Obtener sprite de un item por nombre
    /// </summary>
    public Sprite GetIcon(string itemName)
    {
        foreach (var item in items)
        {
            if (item.itemName == itemName)
            {
                return item.icon;
            }
        }

        Debug.LogWarning($"[ItemDatabase] No se encontró sprite para item: {itemName}");
        return null;
    }

    /// <summary>
    /// Obtener tipo de item por nombre
    /// </summary>
    public Item.ItemType GetItemType(string itemName)
    {
        foreach (var item in items)
        {
            if (item.itemName == itemName)
            {
                return item.itemType;
            }
        }

        return Item.ItemType.Weapon; // Default
    }

    /// <summary>
    /// Verificar si un item existe en la base de datos
    /// </summary>
    public bool ItemExists(string itemName)
    {
        foreach (var item in items)
        {
            if (item.itemName == itemName)
            {
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Auto-poblar la base de datos escaneando todos los prefabs de NetworkItem
    /// </summary>
    [ContextMenu("Auto-Populate from NetworkItem Prefabs")]
    private void AutoPopulateFromPrefabs()
    {
        items.Clear();

        // Buscar todos los prefabs con NetworkItem
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                NetworkItem networkItem = prefab.GetComponent<NetworkItem>();
                if (networkItem != null && !string.IsNullOrEmpty(networkItem.itemName))
                {
                    // Verificar que no exista ya
                    bool exists = false;
                    foreach (var existing in items)
                    {
                        if (existing.itemName == networkItem.itemName)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        items.Add(new ItemData
                        {
                            itemName = networkItem.itemName,
                            icon = networkItem.itemIcon,
                            itemType = networkItem.itemType
                        });
                        Debug.Log($"[ItemDatabase] Item agregado: {networkItem.itemName}");
                    }
                }
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[ItemDatabase] Auto-población completada. Total items: {items.Count}");
    }
#endif
}
