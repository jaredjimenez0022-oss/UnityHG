using UnityEngine;
using Fusion;
using Scripts;

/// <summary>
/// Script puente que conecta el sistema de red (NetworkPlayer) con los scripts locales existentes
/// Este script es OPCIONAL - solo úsalo si quieres mantener compatibilidad con ambos sistemas
/// </summary>
public class NetworkPlayerBridge : NetworkBehaviour
{
    [Header("Referencias a Scripts Locales")]
    [SerializeField] private FirstPersonMovement firstPersonMovement;
    [SerializeField] private PlayerController playerController;
    
    [Header("Referencias a Scripts de Red")]
    [SerializeField] private NetworkPlayer networkPlayer;
    
    [Header("Configuración")]
    [Tooltip("Si es true, desactiva los componentes locales cuando se usa el sistema de red")]
    [SerializeField] private bool disableLocalComponentsInNetworkMode = true;

    private void Awake()
    {
        // Auto-encontrar componentes si no están asignados
        if (firstPersonMovement == null)
            firstPersonMovement = GetComponent<FirstPersonMovement>();
        
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        
        if (networkPlayer == null)
            networkPlayer = GetComponent<NetworkPlayer>();
    }

    public override void Spawned()
    {
        // Este método se llama cuando el objeto es spawneado en la red
        
        if (disableLocalComponentsInNetworkMode)
        {
            // Opción 1: Desactivar completamente los scripts locales en modo red
            // (El sistema de red maneja todo)
            if (firstPersonMovement != null)
                firstPersonMovement.enabled = false;
            
            if (playerController != null)
                playerController.enabled = false;
            
            Debug.Log($"[NetworkPlayerBridge] Scripts locales desactivados para Player {Object.InputAuthority.PlayerId}");
        }
        else
        {
            // Opción 2: Mantener scripts locales activos
            // (Ya tienen la validación de HasInputAuthority, así que solo funcionarán para el jugador local)
            Debug.Log($"[NetworkPlayerBridge] Scripts locales permanecen activos (con validación de autoridad)");
        }
    }

    /// <summary>
    /// Llama a este método si quieres forzar el uso del sistema local en lugar del de red
    /// </summary>
    public void EnableLocalMode()
    {
        if (firstPersonMovement != null)
            firstPersonMovement.enabled = true;
        
        if (playerController != null)
            playerController.enabled = true;
        
        if (networkPlayer != null)
            networkPlayer.enabled = false;
        
        Debug.Log("[NetworkPlayerBridge] Modo local activado");
    }

    /// <summary>
    /// Llama a este método para usar el sistema de red
    /// </summary>
    public void EnableNetworkMode()
    {
        if (firstPersonMovement != null)
            firstPersonMovement.enabled = false;
        
        if (playerController != null)
            playerController.enabled = false;
        
        if (networkPlayer != null)
            networkPlayer.enabled = true;
        
        Debug.Log("[NetworkPlayerBridge] Modo red activado");
    }
}
