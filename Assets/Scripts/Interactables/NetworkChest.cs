using UnityEngine;
using Fusion;
using System.Linq;

/// <summary>
/// Cofre interactuable sincronizado en red
/// Attachar a: Chest prefab (con NetworkObject component)
/// Se abre con tecla E cuando el jugador está cerca
/// </summary>
public class NetworkChest : NetworkBehaviour
{
    [Header("Chest Settings")]
    [SerializeField] private Transform lidPivot;
    [SerializeField] private float openAngle = -60f;
    [SerializeField] private float openSpeed = 2f;

    [Header("Loot")]
    [SerializeField] private NetworkPrefabRef[] possibleLoot;
    [SerializeField] private int minItems = 3;
    [SerializeField] private int maxItems = 4;

    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 3f;

    // Estado del cofre sincronizado
    [Networked] public NetworkBool IsOpen { get; set; }
    [Networked] public TickTimer OpenAnimationTimer { get; set; }

    private Vector3 originalRotation;
    private float currentRotation = 0f;

    public override void Spawned()
    {
        base.Spawned();

        if (lidPivot != null)
        {
            originalRotation = lidPivot.localEulerAngles;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Animar apertura (en todos los clientes)
        if (IsOpen && !OpenAnimationTimer.ExpiredOrNotRunning(Runner))
        {
            // Animación suave de apertura
            currentRotation = Mathf.Lerp(currentRotation, openAngle, openSpeed * Runner.DeltaTime);

            if (lidPivot != null)
            {
                lidPivot.localRotation = Quaternion.Euler(
                    originalRotation.x + currentRotation,
                    originalRotation.y,
                    originalRotation.z
                );
            }
        }
    }

    /// <summary>
    /// Intentar abrir el cofre (llamado por NetworkPlayer cuando presiona E)
    /// </summary>
    public void TryOpen(NetworkPlayer player)
    {
        if (IsOpen)
        {
            Debug.Log("[NetworkChest] Cofre ya está abierto");
            return;
        }

        if (!player.HasInputAuthority)
        {
            Debug.LogWarning("[NetworkChest] Solo el jugador local puede abrir cofres");
            return;
        }

        // Verificar distancia
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance > interactionRadius)
        {
            Debug.Log($"[NetworkChest] Demasiado lejos del cofre (distancia: {distance:F2}m)");
            return;
        }

        Debug.Log($"[NetworkChest] Jugador {player.Object.InputAuthority} abriendo cofre");

        // Enviar RPC al servidor para abrir el cofre
        RPC_OpenChest(player.Object.InputAuthority);
    }

    /// <summary>
    /// RPC para abrir el cofre (procesado por el servidor)
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_OpenChest(PlayerRef playerWhoOpened, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;
        if (IsOpen) return;

        Debug.Log($"[Server] Cofre abierto por {playerWhoOpened}");

        // Marcar como abierto
        IsOpen = true;

        // Iniciar timer de animación (2 segundos)
        OpenAnimationTimer = TickTimer.CreateFromSeconds(Runner, 2f);

        // Generar loot
        SpawnLoot();

        // Notificar a todos los clientes
        RPC_NotifyChestOpened();
    }

    /// <summary>
    /// RPC para notificar a todos que el cofre fue abierto
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyChestOpened(RpcInfo info = default)
    {
        Debug.Log("[Client] Cofre abierto - reproduciendo animación");
        // Aquí puedes reproducir sonidos, partículas, etc.
    }

    private void SpawnLoot()
    {
        if (!HasStateAuthority) return;

        if (possibleLoot == null || possibleLoot.Length == 0)
        {
            Debug.LogWarning("[Server] No hay loot configurado en el cofre");
            return;
        }

        int itemCount = Random.Range(minItems, maxItems + 1);
        Debug.Log($"[Server] Spawning {itemCount} items");

        for (int i = 0; i < itemCount; i++)
        {
            // Seleccionar item aleatorio
            NetworkPrefabRef lootPrefab = possibleLoot[Random.Range(0, possibleLoot.Length)];

            // Posición de spawn arriba del cofre
            float offsetX = Random.Range(-0.3f, 0.3f);
            float offsetZ = Random.Range(-0.3f, 0.3f);
            Vector3 spawnOffset = new Vector3(offsetX, 0.8f, offsetZ); // 80cm arriba

            Vector3 chestPosition = transform.position;
            Vector3 spawnPosition = chestPosition + spawnOffset;

            Debug.Log($"[Server] COFRE en posicion: {chestPosition}");
            Debug.Log($"[Server] Offset aplicado: {spawnOffset}");
            Debug.Log($"[Server] Item FINAL spawn position: {spawnPosition}");
            Debug.Log($"[Server] Distancia del cofre al spawn: {Vector3.Distance(chestPosition, spawnPosition):F2}m");

            // Spawn del item en red
            NetworkObject item = Runner.Spawn(
                lootPrefab,
                spawnPosition,
                Quaternion.identity,
                null,
                (runner, obj) =>
                {
                    // Configurar Rigidbody - la física se controla desde el prefab
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        // RESETEAR velocidad
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;

                        // Asegurar que la física esté activa
                        rb.isKinematic = false;
                        rb.useGravity = true;

                        Debug.Log($"[Server] Rigidbody reseteado - UseGravity: {rb.useGravity}, IsKinematic: {rb.isKinematic}");
                    }

                    Debug.Log($"[Server] Item spawneado en {spawnPosition}");
                }
            );

            if (item != null)
            {
                // VERIFICACIÓN CRÍTICA: ¿El item spawneó donde se esperaba?
                Vector3 actualPosition = item.transform.position;
                float distanceError = Vector3.Distance(spawnPosition, actualPosition);

                Debug.Log($"[Server] ===== VERIFICACIÓN DE SPAWN =====");
                Debug.Log($"[Server] Posición ESPERADA: {spawnPosition}");
                Debug.Log($"[Server] Posición REAL del item: {actualPosition}");
                Debug.Log($"[Server] Error de posición: {distanceError:F3}m");

                if (distanceError > 0.1f)
                {
                    Debug.LogError($"[Server] ALERTA: Item {item.name} NO spawneó en la posición esperada! Error: {distanceError:F2}m");
                }

                // Diagnóstico detallado del item spawneado
                NetworkItem networkItem = item.GetComponent<NetworkItem>();
                Rigidbody rb = item.GetComponent<Rigidbody>();
                Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                Collider[] colliders = item.GetComponents<Collider>();

                Debug.Log($"[Server] - NetworkItem: {(networkItem != null ? "SI" : "NO")}, IsPickedUp: {(networkItem != null ? networkItem.IsPickedUp.ToString() : "N/A")}");
                Debug.Log($"[Server] - Rigidbody: {(rb != null ? "SI" : "NO")}, UseGravity: {(rb != null ? rb.useGravity.ToString() : "N/A")}, IsKinematic: {(rb != null ? rb.isKinematic.ToString() : "N/A")}");
                Debug.Log($"[Server] - Renderers activos: {renderers.Count(r => r.enabled)}/{renderers.Length}");
                Debug.Log($"[Server] =====================================");
            }
            else
            {
                Debug.LogError($"[Server] Failed to spawn item: {lootPrefab}");
            }
        }
    }

    // Visualización en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
