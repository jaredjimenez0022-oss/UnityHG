using System;
using Fusion;
using UnityEngine;

namespace Scripts.SafeZone
{
    [RequireComponent(typeof(NetworkObject))]
    public class SafeZoneController : NetworkBehaviour
    {
        [Serializable]
        public struct SafeZonePhase
        {
            [Min(0f)]
            public float durationSeconds;

            [Min(0f)]
            public float targetRadius;
        }

        [Serializable]
        private struct LineSettings
        {
            public Color color;

            [Min(0f)]
            public float width;

            public float heightOffset;
        }

        [SerializeField] private float initialRadius = 60f;
        [SerializeField] private bool randomizeCenter = true;
        [SerializeField] private SafeZonePhase[] phases =
        {
            new SafeZonePhase { durationSeconds = 60f, targetRadius = 40f },
            new SafeZonePhase { durationSeconds = 60f, targetRadius = 25f },
            new SafeZonePhase { durationSeconds = 60f, targetRadius = 10f }
        };
        [SerializeField] private SafeZoneVisual visual;
        [SerializeField] private LineSettings lineSettings = new LineSettings
        {
            color = new Color(0.2f, 0.6f, 1f, 1f),
            width = 0.35f,
            heightOffset = 0.1f
        };

        [Networked] public Vector2 ZoneCenter { get; private set; }
        [Networked] public float ZoneRadius { get; private set; }
        [Networked] private int ActivePhaseIndex { get; set; }
        [Networked] private TickTimer PhaseTimer { get; set; }

        private float phaseDuration;
        private float phaseStartRadius;
        private float phaseTargetRadius;
        private Vector2 phaseStartCenter;
        private Vector2 phaseTargetCenter;
        private float cachedGroundHeight;

        public Vector3 CurrentCenter => new Vector3(ZoneCenter.x, cachedGroundHeight, ZoneCenter.y);
        public float CurrentRadius => ZoneRadius;
        public bool HasCompletedAllPhases => phases == null || phases.Length == 0 || (ActivePhaseIndex >= phases.Length - 1 && !PhaseTimer.IsRunning);

        private void Awake()
        {
            CacheGroundHeight();
            if (visual == null)
            {
                visual = GetComponentInChildren<SafeZoneVisual>(true);
            }
        }

        public override void Spawned()
        {
            base.Spawned();

            if (Runner.IsServer)
            {
                ResetZoneState();
            }

            ConfigureVisual();
        }

        public override void FixedUpdateNetwork()
        {
            if (Runner.IsServer)
            {
                RunServerSimulation();
            }
        }

        public override void Render()
        {
            base.Render();
            UpdateVisual();
        }

        private void RunServerSimulation()
        {
            if (!PhaseTimer.IsRunning)
            {
                if (NeedsNextPhase())
                {
                    BeginNextPhase();
                }
                else
                {
                    FinalizeCurrentState();
                }

                return;
            }

            AdvancePhase();

            if (PhaseTimer.Expired(Runner))
            {
                CompletePhase();
            }
        }

        private void ResetZoneState()
        {
            ZoneCenter = new Vector2(transform.position.x, transform.position.z);
            ZoneRadius = Mathf.Max(0f, initialRadius);

            phaseStartCenter = ZoneCenter;
            phaseTargetCenter = ZoneCenter;
            phaseStartRadius = ZoneRadius;
            phaseTargetRadius = ZoneRadius;
            phaseDuration = 0f;

            PhaseTimer = TickTimer.None;
            ActivePhaseIndex = -1;
        }

        private bool NeedsNextPhase()
        {
            return phases != null && ActivePhaseIndex + 1 < phases.Length;
        }

        private void BeginNextPhase()
        {
            ActivePhaseIndex++;

            if (phases == null || phases.Length == 0)
            {
                CompletePhase();
                return;
            }

            SafeZonePhase phase = phases[ActivePhaseIndex];
            phaseStartRadius = ZoneRadius;
            phaseStartCenter = ZoneCenter;

            phaseTargetRadius = Mathf.Clamp(phase.targetRadius, 0f, phaseStartRadius);
            phaseTargetCenter = randomizeCenter
                ? GetRandomContainedCenter(phaseStartCenter, phaseStartRadius, phaseTargetRadius)
                : phaseStartCenter;

            phaseDuration = Mathf.Max(phase.durationSeconds, 0f);

            if (phaseDuration <= Mathf.Epsilon || Mathf.Approximately(phaseStartRadius, phaseTargetRadius))
            {
                CompletePhase();
                return;
            }

            PhaseTimer = TickTimer.CreateFromSeconds(Runner, phaseDuration);
        }

        private void AdvancePhase()
        {
            float remaining = PhaseTimer.RemainingTime(Runner) ?? 0f;
            float elapsed = Mathf.Max(0f, phaseDuration - remaining);
            float normalized = phaseDuration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / phaseDuration);

            ZoneRadius = Mathf.Lerp(phaseStartRadius, phaseTargetRadius, normalized);
            ZoneCenter = Vector2.Lerp(phaseStartCenter, phaseTargetCenter, normalized);
        }

        private void CompletePhase()
        {
            ZoneRadius = phaseTargetRadius;
            ZoneCenter = phaseTargetCenter;

            phaseStartRadius = ZoneRadius;
            phaseStartCenter = ZoneCenter;

            PhaseTimer = TickTimer.None;
        }

        private void FinalizeCurrentState()
        {
            ZoneRadius = phaseTargetRadius;
            ZoneCenter = phaseTargetCenter;
        }

        private Vector2 GetRandomContainedCenter(Vector2 currentCenter, float currentRadius, float targetRadius)
        {
            float maxOffset = Mathf.Max(0f, currentRadius - targetRadius);
            if (maxOffset <= Mathf.Epsilon)
            {
                return currentCenter;
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle * maxOffset;
            return currentCenter + offset;
        }

        private void UpdateVisual()
        {
            // In edit mode (or before the NetworkObject is spawned), fall back to the
            // transform position and initial radius so OnValidate can run safely.
            Vector3 worldCenter;
            float radius;

            if (!Application.isPlaying || Object == null)
            {
                Vector3 fallback = transform.position;
                worldCenter = new Vector3(fallback.x, cachedGroundHeight, fallback.z);
                radius = Mathf.Max(0f, initialRadius);
            }
            else
            {
                worldCenter = new Vector3(ZoneCenter.x, cachedGroundHeight, ZoneCenter.y);
                radius = ZoneRadius;
            }

            transform.position = worldCenter;

            if (visual != null)
            {
                visual.Draw(worldCenter, radius, lineSettings.heightOffset);
            }
        }

        private void ConfigureVisual()
        {
            if (visual == null)
            {
                return;
            }

            visual.Configure(lineSettings.color, lineSettings.width);
            UpdateVisual();
        }

        private void CacheGroundHeight()
        {
            cachedGroundHeight = transform.position.y;
        }

        private void OnValidate()
        {
            CacheGroundHeight();

            if (visual == null)
            {
                visual = GetComponentInChildren<SafeZoneVisual>();
            }

            if (phases == null)
            {
                phases = Array.Empty<SafeZonePhase>();
            }

            for (int i = 0; i < phases.Length; i++)
            {
                phases[i].durationSeconds = Mathf.Max(0f, phases[i].durationSeconds);
                phases[i].targetRadius = Mathf.Max(0f, phases[i].targetRadius);
            }

            ConfigureVisual();

            if (!Application.isPlaying && visual != null)
            {
                visual.Draw(transform.position, Mathf.Max(0f, initialRadius), lineSettings.heightOffset);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (phases == null || phases.Length == 0)
            {
                return;
            }

            Vector3 center = transform.position;
            Color baseColor = lineSettings.color;

            // Draw initial radius
            Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.6f);
            DrawCircleGizmo(center, initialRadius);

            // Draw each phase target radius with decreasing alpha
            for (int i = 0; i < phases.Length; i++)
            {
                float alpha = Mathf.Lerp(0.5f, 0.2f, (float)i / phases.Length);
                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                DrawCircleGizmo(center, phases[i].targetRadius);
            }
        }

        private void DrawCircleGizmo(Vector3 center, float radius)
        {
            if (radius <= 0f)
            {
                return;
            }

            int segments = 64;
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}
