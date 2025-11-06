using Fusion;
using UnityEngine;

namespace Scripts.SafeZone
{
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(HealthRegeneration))]
    public class SafeZoneDamage : NetworkBehaviour
    {
        [SerializeField] private int damagePerTick = 2;
        [SerializeField] private float damageInterval = 1f;

        private PlayerHealth playerHealth;
        private HealthRegeneration healthRegeneration;
        private SafeZoneController safeZoneController;
        private TickTimer damageTimer;

        public override void Spawned()
        {
            playerHealth = GetComponent<PlayerHealth>();
            healthRegeneration = GetComponent<HealthRegeneration>();
            
            // Find the SafeZoneController in the scene
            safeZoneController = FindFirstObjectByType<SafeZoneController>();
            
            if (safeZoneController == null)
            {
                Debug.LogWarning("SafeZoneDamage: No SafeZoneController found in scene");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || safeZoneController == null || playerHealth == null)
                return;

            if (playerHealth.IsDead)
                return;

            if (IsOutsideSafeZone())
            {
                // Apply damage at intervals when outside (bypasses shield, goes directly to health)
                if (damageTimer.ExpiredOrNotRunning(Runner))
                {
                    playerHealth.TakeDamage(damagePerTick);

                    // Notify health regeneration component to reset its timer
                    if (healthRegeneration != null)
                    {
                        healthRegeneration.OnDamageTaken();
                    }

                    damageTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);

                    Debug.Log($"Player taking {damagePerTick} storm damage (bypasses shield)");
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