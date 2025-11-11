using UnityEngine;
using Fusion;

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
    [SerializeField] private int minItems = 1;
    [SerializeField] private int maxItems = 3;

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

            // Posición de spawn (arriba del cofre con pequeño offset aleatorio)
            Vector3 spawnOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                1.5f,
                Random.Range(-0.5f, 0.5f)
            );
            Vector3 spawnPosition = transform.position + spawnOffset;

            // Spawn del item en red
            NetworkObject item = Runner.Spawn(
                lootPrefab,
                spawnPosition,
                Quaternion.identity,
                null,
                (runner, obj) =>
                {
                    // Opcional: Aplicar fuerza hacia arriba para efecto de "saltar"
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(Vector3.up * 3f + Random.insideUnitSphere * 2f, ForceMode.Impulse);
                    }
                }
            );

            if (item != null)
            {
                Debug.Log($"[Server] Item spawneado: {item.name}");
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
