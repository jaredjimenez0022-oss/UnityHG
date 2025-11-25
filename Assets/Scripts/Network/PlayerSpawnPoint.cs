using UnityEngine;

namespace Scripts.Network
{
    /// <summary>
    /// Marks a spawn point location for players in the game scene.
    /// Place these around the island at ground level where players should spawn.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private float gizmoRadius = 1f;

        private void OnDrawGizmos()
        {
            // Draw a green sphere so spawn points are visible in the editor
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius);

            // Draw a wire sphere outline
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);

            // Draw an arrow showing forward direction
            Gizmos.color = Color.blue;
            Vector3 direction = transform.forward * 2f;
            Gizmos.DrawLine(transform.position, transform.position + direction);
            Gizmos.DrawSphere(transform.position + direction, 0.3f);
        }

        private void OnDrawGizmosSelected()
        {
            // Highlight selected spawn point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius + 0.2f);
        }
    }
}
