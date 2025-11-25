using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

/// <summary>
/// Gestiona la instanciación y configuración automática de UI para NetworkPlayer
/// Se ejecuta solo para el jugador local (HasInputAuthority)
/// Attachar a: NetworkPlayer prefab
/// </summary>
public class NetworkUIManager : NetworkBehaviour
{
    [Header("UI Prefab References")]
    [SerializeField] private GameObject inventoryCanvasPrefab;
    [SerializeField] private GameObject staminaCanvasPrefab;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private GameObject inventoryCanvasInstance;
    private GameObject staminaCanvasInstance;

    public override void Spawned()
    {
        base.Spawned();

        // Solo configurar UI para jugador local
        if (!HasInputAuthority)
        {
            Log("[NetworkUIManager] Jugador remoto - no se configura UI");
            return;
        }

        Log("[NetworkUIManager] Jugador local detectado - configurando UI...");

        // Validar que los prefabs estén asignados
        if (!ValidatePrefabs())
        {
            LogError("[NetworkUIManager] ERROR: Prefabs no asignados en Inspector");
            return;
        }

        // Verificar que exista EventSystem para UI clicks
        EnsureEventSystem();

        // Instanciar y configurar UI
        SetupInventoryUI();
        SetupStaminaUI();

        Log("[NetworkUIManager] Configuración de UI completada");
    }

    /// <summary>
    /// Verifica que exista un EventSystem en la escena para detectar clicks en UI
    /// </summary>
    private void EnsureEventSystem()
    {
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Log("[NetworkUIManager] No hay EventSystem en la escena - creando uno automáticamente...");

            // Crear EventSystem automáticamente
            var eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // Agregar InputModule para el nuevo Input System
            var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            Log("[NetworkUIManager] ✓ EventSystem creado correctamente con InputSystemUIInputModule");
            Debug.Log($"[NetworkUIManager] EventSystem GameObject: {eventSystemObj.name}, EventSystem: {eventSystem != null}, InputModule: {inputModule != null}");
        }
        else
        {
            Log($"[NetworkUIManager] ✓ EventSystem encontrado: {eventSystem.gameObject.name}");

            // Verificar si tiene el InputModule correcto
            var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null)
            {
                LogError("[NetworkUIManager] WARNING: EventSystem existe pero no tiene InputSystemUIInputModule - agregándolo...");
                inputModule = eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Log("[NetworkUIManager] ✓ InputSystemUIInputModule agregado al EventSystem existente");
            }
        }
    }

    private bool ValidatePrefabs()
    {
        bool valid = true;

        if (inventoryCanvasPrefab == null)
        {
            LogError("[NetworkUIManager] ERROR: inventoryCanvasPrefab no asignado");
            valid = false;
        }

        if (staminaCanvasPrefab == null)
        {
            LogError("[NetworkUIManager] ERROR: staminaCanvasPrefab no asignado");
            valid = false;
        }

        return valid;
    }

    #region Inventory UI Setup

    private void SetupInventoryUI()
    {
        Log("[NetworkUIManager] Configurando Inventory UI...");

        // Instanciar InventoryCanvas
        inventoryCanvasInstance = Instantiate(inventoryCanvasPrefab, transform);
        inventoryCanvasInstance.name = "InventoryCanvas";
        Log($"[NetworkUIManager] InventoryCanvas instanciado: {inventoryCanvasInstance.name}");

        // Buscar NetworkInventorySystem en el jugador
        NetworkInventorySystem inventorySystem = GetComponent<NetworkInventorySystem>();
        if (inventorySystem == null)
        {
            LogError("[NetworkUIManager] ERROR: NetworkInventorySystem no encontrado en Player");
            return;
        }

        // Buscar InventoryPanel
        Transform inventoryPanelTransform = inventoryCanvasInstance.transform.Find("InventoryPanel");
        if (inventoryPanelTransform == null)
        {
            LogError("[NetworkUIManager] ERROR: InventoryPanel no encontrado en InventoryCanvas");
            return;
        }
        GameObject inventoryPanel = inventoryPanelTransform.gameObject;
        Log($"[NetworkUIManager] InventoryPanel encontrado: {inventoryPanel.name}");

        // Buscar los 6 slots
        Image[] slotImages = new Image[6];
        TextMeshProUGUI[] slotQuantityTexts = new TextMeshProUGUI[6];

        for (int i = 0; i < 6; i++)
        {
            string slotName = $"Slot{i + 1}";
            Transform slotTransform = inventoryPanel.transform.Find(slotName);

            if (slotTransform == null)
            {
                LogError($"[NetworkUIManager] ERROR: {slotName} no encontrado en InventoryPanel");
                continue;
            }

            // Obtener Image del slot
            Image slotImage = slotTransform.GetComponent<Image>();
            if (slotImage == null)
            {
                LogError($"[NetworkUIManager] ERROR: Image component no encontrado en {slotName}");
                continue;
            }
            slotImages[i] = slotImage;

            // Obtener QuantityText (hijo del slot)
            Transform quantityTextTransform = slotTransform.Find("QuantityText");
            if (quantityTextTransform == null)
            {
                LogError($"[NetworkUIManager] ERROR: QuantityText no encontrado en {slotName}");
                continue;
            }

            TextMeshProUGUI quantityText = quantityTextTransform.GetComponent<TextMeshProUGUI>();
            if (quantityText == null)
            {
                LogError($"[NetworkUIManager] ERROR: TextMeshProUGUI component no encontrado en QuantityText de {slotName}");
                continue;
            }
            slotQuantityTexts[i] = quantityText;

            Log($"[NetworkUIManager] {slotName} configurado correctamente");
        }

        // Configurar NetworkInventorySystem con las referencias
        inventorySystem.SetUIReferences(inventoryPanel, slotImages, slotQuantityTexts);
        Log("[NetworkUIManager] NetworkInventorySystem configurado con referencias UI");

        // Configurar botones de slots para doble-click
        ConfigureSlotButtons(inventoryPanel, inventorySystem);
    }

    /// <summary>
    /// Configura los botones de cada slot para detectar clicks y llamar a OnSlotClicked
    /// </summary>
    private void ConfigureSlotButtons(GameObject inventoryPanel, NetworkInventorySystem inventorySystem)
    {
        Log("[NetworkUIManager] Configurando botones de slots para doble-click...");

        for (int i = 0; i < 6; i++)
        {
            string slotName = $"Slot{i + 1}";
            Transform slotTransform = inventoryPanel.transform.Find(slotName);

            if (slotTransform == null)
            {
                LogError($"[NetworkUIManager] ERROR: {slotName} no encontrado para configurar botón");
                continue;
            }

            // Obtener o agregar componente Button
            Button slotButton = slotTransform.GetComponent<Button>();
            if (slotButton == null)
            {
                LogError($"[NetworkUIManager] ERROR: Button component no encontrado en {slotName}");
                continue;
            }

            // Limpiar listeners previos (por si acaso)
            slotButton.onClick.RemoveAllListeners();

            // Capturar el índice en una variable local para el closure
            int slotIndex = i;

            // Agregar listener que llama a OnSlotClicked con el índice correcto
            slotButton.onClick.AddListener(() =>
            {
                inventorySystem.OnSlotClicked(slotIndex);
            });

            Log($"[NetworkUIManager] Botón configurado para {slotName} (índice {slotIndex})");
        }

        Log("[NetworkUIManager] Todos los botones de slots configurados correctamente");
    }

    #endregion

    #region Stamina UI Setup

    private void SetupStaminaUI()
    {
        Log("[NetworkUIManager] Configurando Stamina UI...");

        // Instanciar StaminaCanvas
        staminaCanvasInstance = Instantiate(staminaCanvasPrefab, transform);
        staminaCanvasInstance.name = "StaminaCanvas";
        Log($"[NetworkUIManager] StaminaCanvas instanciado: {staminaCanvasInstance.name}");

        // Buscar NetworkStaminaSystem en el jugador
        NetworkStaminaSystem staminaSystem = GetComponent<NetworkStaminaSystem>();
        if (staminaSystem == null)
        {
            LogError("[NetworkUIManager] ERROR: NetworkStaminaSystem no encontrado en Player");
            return;
        }

        // Buscar StaminaBar (Slider)
        Transform staminaBarTransform = staminaCanvasInstance.transform.Find("StaminaBar");
        if (staminaBarTransform == null)
        {
            LogError("[NetworkUIManager] ERROR: StaminaBar no encontrado en StaminaCanvas");
            return;
        }

        Slider staminaBar = staminaBarTransform.GetComponent<Slider>();
        if (staminaBar == null)
        {
            LogError("[NetworkUIManager] ERROR: Slider component no encontrado en StaminaBar");
            return;
        }
        Log($"[NetworkUIManager] StaminaBar (Slider) encontrado");

        // Buscar Fill Image (StaminaBar/Fill Area/Fill)
        Transform fillAreaTransform = staminaBarTransform.Find("Fill Area");
        if (fillAreaTransform == null)
        {
            LogError("[NetworkUIManager] ERROR: Fill Area no encontrado en StaminaBar");
            return;
        }

        Transform fillTransform = fillAreaTransform.Find("Fill");
        if (fillTransform == null)
        {
            LogError("[NetworkUIManager] ERROR: Fill no encontrado en Fill Area");
            return;
        }

        Image staminaFill = fillTransform.GetComponent<Image>();
        if (staminaFill == null)
        {
            LogError("[NetworkUIManager] ERROR: Image component no encontrado en Fill");
            return;
        }
        Log($"[NetworkUIManager] StaminaFill (Image) encontrado");

        // Configurar NetworkStaminaSystem con las referencias
        staminaSystem.SetUIReferences(staminaCanvasInstance, staminaBar, staminaFill);
        Log("[NetworkUIManager] NetworkStaminaSystem configurado con referencias UI");
    }

    #endregion

    #region Logging Helpers

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar instancias cuando el jugador se destruya
        if (inventoryCanvasInstance != null)
        {
            Destroy(inventoryCanvasInstance);
        }

        if (staminaCanvasInstance != null)
        {
            Destroy(staminaCanvasInstance);
        }
    }
}
