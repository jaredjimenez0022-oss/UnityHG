using UnityEngine;
using Fusion;
using System.Linq;

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
    [Networked] public TickTimer PickupDelayTimer { get; set; }

    private Collider itemCollider;
    private Renderer[] itemRenderers;
    private Vector3 lastPosition;
    private int positionLogCounter = 0;

    public override void Spawned()
    {
        base.Spawned();

        itemCollider = GetComponent<Collider>();
        itemRenderers = GetComponentsInChildren<Renderer>();

        // Inicializar delay de pickup en el servidor
        if (HasStateAuthority)
        {
            PickupDelayTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
            Debug.Log($"[NetworkItem.Spawned] SERVER - Item {itemName} spawneado con 0.5s de delay para pickup");
        }

        Debug.Log($"[NetworkItem.Spawned] Item {itemName} spawneado - Position: {transform.position}, IsPickedUp: {IsPickedUp}, Renderers: {itemRenderers.Length}");

        // Si el item ya fue recogido antes de que este cliente se conectara
        if (IsPickedUp)
        {
            Debug.LogWarning($"[NetworkItem.Spawned] Item {itemName} ya estaba marcado como recogido - ocultando");
            HideItem();
        }
        else
        {
            Debug.Log($"[NetworkItem.Spawned] Item {itemName} visible - Renderers activos: {itemRenderers.Count(r => r.enabled)}");
        }

        lastPosition = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        // Solo trackear en el servidor y solo durante los primeros 3 segundos
        if (HasStateAuthority && !IsPickedUp)
        {
            positionLogCounter++;

            // Log cada 30 ticks (aproximadamente cada 0.5 segundos)
            if (positionLogCounter % 30 == 0)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                float distanceMoved = Vector3.Distance(transform.position, lastPosition);

                Debug.Log($"[NetworkItem] {itemName} - Pos: {transform.position}, Velocidad: {(rb != null ? rb.velocity.magnitude.ToString("F2") : "N/A")}, Distancia movida: {distanceMoved:F2}m");

                lastPosition = transform.position;
            }

            // Después de 180 ticks (3 segundos), verificar si el item está bajo tierra
            if (positionLogCounter == 180)
            {
                if (transform.position.y < -5f)
                {
                    Debug.LogError($"[NetworkItem] {itemName} CAYÓ BAJO TIERRA a Y={transform.position.y} - PROBLEMA DE COLISIÓN!");
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[NetworkItem] OnTriggerEnter detectado - Objeto: {other.gameObject.name}, HasStateAuthority: {HasStateAuthority}");

        // Solo procesar en el servidor
        if (!HasStateAuthority) return;

        // Verificar delay de pickup (para dar tiempo a ver el item caer)
        if (!PickupDelayTimer.ExpiredOrNotRunning(Runner))
        {
            float remainingTime = (float)PickupDelayTimer.RemainingTime(Runner);
            Debug.Log($"[NetworkItem] Item {itemName} aun en delay de pickup - {remainingTime:F2}s restantes");
            return;
        }

        // Si ya fue recogido, ignorar
        if (IsPickedUp)
        {
            Debug.Log($"[NetworkItem] Item {itemName} ya fue recogido - ignorando colision");
            return;
        }

        // Buscar NetworkPlayer en el objeto o en su padre (para casos donde el trigger está en un hijo)
        NetworkPlayer player = other.GetComponent<NetworkPlayer>();
        if (player == null)
        {
            player = other.GetComponentInParent<NetworkPlayer>();
        }

        Debug.Log($"[NetworkItem] NetworkPlayer encontrado: {(player != null ? "SI" : "NO")}");

        if (player != null)
        {
            Debug.Log($"[NetworkItem] Jugador {player.Object.InputAuthority} tocó item {itemName} - delay expirado, permitiendo pickup");
            TryPickup(player);
        }
    }

    /// <summary>
    /// Intenta que el jugador recoja el item
    /// Solo se ejecuta en el servidor
    /// </summary>
    private void TryPickup(NetworkPlayer player)
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning($"[NetworkItem] TryPickup llamado sin StateAuthority");
            return;
        }

        if (IsPickedUp)
        {
            Debug.LogWarning($"[NetworkItem] Item {itemName} ya fue recogido");
            return;
        }

        // Obtener el sistema de inventario del jugador
        NetworkInventorySystem inventory = player.GetComponent<NetworkInventorySystem>();

        if (inventory == null)
        {
            Debug.LogError($"[Server] Jugador {player.Object.InputAuthority} no tiene NetworkInventorySystem");
            return;
        }

        if (inventory.IsInventoryFull())
        {
            Debug.LogWarning($"[Server] Inventario de {player.Object.InputAuthority} está lleno - no se puede recoger {itemName}");
            return;
        }

        Debug.Log($"[Server] Intentando añadir {itemName} al inventario de {player.Object.InputAuthority}");

        // Marcar como recogido
        IsPickedUp = true;

        // Notificar al cliente del jugador para que añada el item a su inventario
        RPC_NotifyPickup(player.Object.InputAuthority);

        Debug.Log($"[Server] {itemName} recogido exitosamente por {player.Object.InputAuthority}");

        // Ocultar el item para todos
        HideItem();

        // Opcional: Despawnear después de un delay
        // Runner.Despawn(Object, 2f);
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
