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
    private bool animationCompleted = false;

    public override void Spawned()
    {
        base.Spawned();

        if (lidPivot != null)
        {
            originalRotation = lidPivot.localEulerAngles;
            UpdateChestVisualsImmediate(); // Sincronizar estado inicial
        }
    }

    public override void Render()
    {
        // USAR RENDER() para animaciones visuales - más confiable que FixedUpdateNetwork
        if (IsOpen && !animationCompleted)
        {
            AnimateChestOpen();
        }
    }

    private void AnimateChestOpen()
    {
        if (lidPivot == null) return;

        // Animación suave de apertura
        currentRotation = Mathf.Lerp(currentRotation, openAngle, openSpeed * Time.deltaTime);

        lidPivot.localRotation = Quaternion.Euler(
            originalRotation.x + currentRotation,
            originalRotation.y,
            originalRotation.z
        );

        // Verificar si la animación está completa
        if (Mathf.Abs(currentRotation - openAngle) < 1f)
        {
            animationCompleted = true;
            // Asegurar posición final exacta
            lidPivot.localRotation = Quaternion.Euler(
                originalRotation.x + openAngle,
                originalRotation.y,
                originalRotation.z
            );
        }
    }

    private void UpdateChestVisualsImmediate()
    {
        if (lidPivot == null) return;

        if (IsOpen)
        {
            // Si está abierto, poner directamente en posición final
            currentRotation = openAngle;
            animationCompleted = true;
            lidPivot.localRotation = Quaternion.Euler(
                originalRotation.x + openAngle,
                originalRotation.y,
                originalRotation.z
            );
        }
        else
        {
            // Si está cerrado, posición inicial
            currentRotation = 0f;
            animationCompleted = false;
            lidPivot.localRotation = Quaternion.Euler(originalRotation);
        }
    }

    /// <summary>
    /// Intentar abrir el cofre (llamado por NetworkPlayer cuando presiona E)
    /// </summary>
    public void TryOpen(NetworkPlayer player)
    {
        if (IsOpen) return;

        if (!player.HasInputAuthority) return;

        // Verificar distancia
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance > interactionRadius) return;

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

        // Marcar como abierto
        IsOpen = true;
        animationCompleted = false; // Reiniciar animación

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
        // Reiniciar estado de animación
        animationCompleted = false;
        currentRotation = 0f;

        // Forzar actualización visual inmediata
        UpdateChestVisualsImmediate();
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

            // Spawn del item en red
            NetworkObject item = Runner.Spawn(
                lootPrefab,
                spawnPosition,
                Quaternion.identity,
                null,
                (runner, obj) =>
                {
                    // Configurar Rigidbody
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                }
            );
        }
    }

    // Visualización en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}