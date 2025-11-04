using Fusion;
using UnityEngine;

namespace Scripts.SafeZone
{
    [RequireComponent(typeof(PlayerCombat))]
    public class SafeZoneDamage : NetworkBehaviour
    {
        [SerializeField] private int damagePerTick = 2;
        [SerializeField] private float damageInterval = 1f;

        private PlayerCombat playerCombat;
        private SafeZoneController safeZoneController;
        private TickTimer damageTimer;

        public override void Spawned()
        {
            playerCombat = GetComponent<PlayerCombat>();
            
            // Find the SafeZoneController in the scene
            safeZoneController = FindFirstObjectByType<SafeZoneController>();
            
            if (safeZoneController == null)
            {
                Debug.LogWarning("SafeZoneDamage: No SafeZoneController found in scene");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || safeZoneController == null || playerCombat == null)
                return;

            if (!playerCombat.IsAlive())
                return;

            if (IsOutsideSafeZone())
            {
                // Apply damage at intervals when outside
                if (damageTimer.ExpiredOrNotRunning(Runner))
                {
                    playerCombat.TakeDamage(damagePerTick);
                    damageTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
                    
                    Debug.Log($"Player taking {damagePerTick} damage from safe zone");
                }
            }
            else
            {
                // Reset timer when inside safe zone
                damageTimer = TickTimer.None;
            }
        }

        private bool IsOutsideSafeZone()
        {
            Vector3 playerPosition = transform.position;
            Vector3 zoneCenter = safeZoneController.CurrentCenter;
            float zoneRadius = safeZoneController.CurrentRadius;

            // Calculate horizontal distance (ignore Y axis)
            Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.z);
            Vector2 zoneCenter2D = new Vector2(zoneCenter.x, zoneCenter.z);

            float distance = Vector2.Distance(playerPos2D, zoneCenter2D);

            return distance > zoneRadius;
        }

        // Public method to check if player is currently outside (useful for UI warnings)
        public bool IsCurrentlyOutside()
        {
            if (safeZoneController == null)
                return false;
                
            return IsOutsideSafeZone();
        }

        // Public method to get distance from safe zone edge (negative = inside, positive = outside)
        public float GetDistanceFromEdge()
        {
            if (safeZoneController == null)
                return 0f;

            Vector3 playerPosition = transform.position;
            Vector3 zoneCenter = safeZoneController.CurrentCenter;
            float zoneRadius = safeZoneController.CurrentRadius;

            Vector2 playerPos2D = new Vector2(playerPosition.x, playerPosition.z);
            Vector2 zoneCenter2D = new Vector2(zoneCenter.x, zoneCenter.z);

            float distance = Vector2.Distance(playerPos2D, zoneCenter2D);

            return distance - zoneRadius;
        }
    }
}