using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Network
{
    /// <summary>
    /// Manages player spawn points in the game scene.
    /// Populate the spawnPoints list in the inspector with your spawn point transforms.
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour
    {
        [Header("Spawn Points")]
        [Tooltip("Drag all your PlayerSpawnPoint transforms here from the scene")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        [Header("Spawn Settings")]
        [Tooltip("If true, players will face the spawn point's forward direction")]
        [SerializeField] private bool useSpawnRotation = true;

        private int lastSpawnIndex = -1;

        /// <summary>
        /// Get a spawn position using round-robin selection.
        /// Falls back to world origin if no spawn points are configured.
        /// </summary>
        public Vector3 GetSpawnPosition(int playerId)
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning("[PlayerSpawnManager] No spawn points configured! Falling back to origin.");
                return Vector3.zero;
            }

            // Remove any null entries
            spawnPoints.RemoveAll(sp => sp == null);

            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning("[PlayerSpawnManager] All spawn points are null! Falling back to origin.");
                return Vector3.zero;
            }

            // Round-robin selection
            lastSpawnIndex = (lastSpawnIndex + 1) % spawnPoints.Count;
            return spawnPoints[lastSpawnIndex].position;
        }

        /// <summary>
        /// Get spawn rotation for the last selected spawn point.
        /// </summary>
        public Quaternion GetSpawnRotation()
        {
            if (!useSpawnRotation || spawnPoints == null || spawnPoints.Count == 0 || lastSpawnIndex < 0)
            {
                return Quaternion.identity;
            }

            return spawnPoints[lastSpawnIndex].rotation;
        }

        /// <summary>
        /// Get a random spawn point instead of round-robin.
        /// </summary>
        public Vector3 GetRandomSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogWarning("[PlayerSpawnManager] No spawn points configured! Falling back to origin.");
                return Vector3.zero;
            }

            spawnPoints.RemoveAll(sp => sp == null);

            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning("[PlayerSpawnManager] All spawn points are null! Falling back to origin.");
                return Vector3.zero;
            }

            lastSpawnIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[lastSpawnIndex].position;
        }

        private void OnDrawGizmos()
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                return;
            }

            // Draw connections between spawn points to visualize the round-robin order
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] == null)
                {
                    continue;
                }

                int nextIndex = (i + 1) % spawnPoints.Count;
                if (spawnPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(spawnPoints[i].position + Vector3.up * 2f,
                                    spawnPoints[nextIndex].position + Vector3.up * 2f);
                }
            }
        }
    }
}
