using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Sistema de generación de cofres sincronizado en red
/// Attachar a: GameObject vacío en GameScene
/// Solo el servidor spawneará los cofres
/// </summary>
public class NetworkChestSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef chestPrefab;

    [Header("Centro del Mapa (100% spawn rate)")]
    [SerializeField] private Vector3 centerPosition = Vector3.zero;
    [SerializeField] private float centerRadius = 20f;
    [SerializeField] private int minChestsCenter = 2;
    [SerializeField] private int maxChestsCenter = 4;

    [Header("Periferia del Mapa (70% spawn rate)")]
    [SerializeField] private Vector3 peripheryCenter = Vector3.zero;
    [SerializeField] private float peripheryInnerRadius = 30f;
    [SerializeField] private float peripheryOuterRadius = 80f;
    [SerializeField] private float peripherySpawnRate = 0.7f;
    [SerializeField] private int minChestsPeriphery = 3;
    [SerializeField] private int maxChestsPeriphery = 6;

    [Header("Configuración General")]
    [SerializeField] private float minDistanceBetweenChests = 15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool visualizeSpawnAreas = true;

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<NetworkObject> spawnedChests = new List<NetworkObject>();

    public override void Spawned()
    {
        base.Spawned();

        // Solo el servidor genera los cofres
        if (HasStateAuthority)
        {
            SpawnChests();
        }
    }

    private void SpawnChests()
    {
        if (!HasStateAuthority) return;

        // Limpiar listas
        spawnedPositions.Clear();
        spawnedChests.Clear();

        // Generar cofres en el centro (100% spawn rate)
        int centerCount = Random.Range(minChestsCenter, maxChestsCenter + 1);

        for (int i = 0; i < centerCount; i++)
        {
            Vector3 spawnPos = GenerateRandomPointInCircle(centerPosition, centerRadius);
            if (IsValidSpawnPoint(spawnPos))
            {
                SpawnChestAtPosition(spawnPos);
            }
        }

        // Generar cofres en la periferia (70% spawn rate)
        int peripheryAttempts = Random.Range(minChestsPeriphery, maxChestsPeriphery + 1);

        for (int i = 0; i < peripheryAttempts; i++)
        {
            // Verificar spawn rate (70%)
            if (Random.value > peripherySpawnRate)
            {
                continue;
            }

            Vector3 spawnPos = GenerateRandomPointInAnnulus(
                peripheryCenter,
                peripheryInnerRadius,
                peripheryOuterRadius
            );

            if (IsValidSpawnPoint(spawnPos))
            {
                SpawnChestAtPosition(spawnPos);
            }
        }
    }

    private void SpawnChestAtPosition(Vector3 position)
    {
        if (!HasStateAuthority) return;

        // Spawn del cofre en red
        NetworkObject chest = Runner.Spawn(
            chestPrefab,
            position,
            Quaternion.identity
        );

        if (chest != null)
        {
            spawnedChests.Add(chest);
            spawnedPositions.Add(position);
        }
    }

    private Vector3 GenerateRandomPointInCircle(Vector3 center, float radius)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(0f, radius);

        float x = center.x + distance * Mathf.Cos(angle);
        float z = center.z + distance * Mathf.Sin(angle);

        float y = GetGroundHeight(new Vector3(x, center.y + 100f, z));

        return new Vector3(x, y, z);
    }

    private Vector3 GenerateRandomPointInAnnulus(Vector3 center, float innerRadius, float outerRadius)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(innerRadius, outerRadius);

        float x = center.x + distance * Mathf.Cos(angle);
        float z = center.z + distance * Mathf.Sin(angle);

        float y = GetGroundHeight(new Vector3(x, center.y + 100f, z));

        return new Vector3(x, y, z);
    }

    private float GetGroundHeight(Vector3 position)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 200f, groundLayer))
        {
            return hit.point.y + 0.1f; // Pequeño offset para que no esté dentro del suelo
        }

        return position.y;
    }

    private bool IsValidSpawnPoint(Vector3 position)
    {
        // Verificar distancia mínima con otros cofres
        foreach (Vector3 spawnedPos in spawnedPositions)
        {
            if (Vector3.Distance(position, spawnedPos) < minDistanceBetweenChests)
            {
                return false;
            }
        }

        // Verificar que no esté bajo el nivel del agua
        if (position.y < 0)
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!visualizeSpawnAreas) return;

        // Área central (verde)
        Gizmos.color = Color.green;
        DrawCircle(centerPosition, centerRadius, 30);

        // Área de periferia (amarillo)
        Gizmos.color = Color.yellow;
        DrawCircle(peripheryCenter, peripheryInnerRadius, 30);
        DrawCircle(peripheryCenter, peripheryOuterRadius, 30);

        // Cofres spawneados (rojo)
        Gizmos.color = Color.red;
        foreach (Vector3 pos in spawnedPositions)
        {
            Gizmos.DrawWireSphere(pos, minDistanceBetweenChests / 2);
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + new Vector3(radius, 0, 0);

        for (int i = 0; i <= segments; i++)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                radius * Mathf.Cos(rad),
                0,
                radius * Mathf.Sin(rad)
            );

            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
            angle += angleStep;
        }
    }
}
