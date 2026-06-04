using System.Collections.Generic;
using UnityEngine;
using ScaryGame.Noise;
using ScaryGame.Players;
using TimeFracture.Player;

namespace ScaryGame.Enemy
{
    /// <summary>
    /// Server-only. Maintains a small ring of recent noises filtered by
    /// per-type hearing radius, runs cone-of-vision LOS checks, and tracks
    /// last-known positions for each player.
    /// </summary>
    public class GhostPerception : MonoBehaviour
    {
        private const int NoiseBufferSize = 8;

        private readonly NoiseEvent[] _noises = new NoiseEvent[NoiseBufferSize];
        private int _noiseHead;

        private readonly List<PlayerController> _aliveBuffer = new List<PlayerController>();

        private GhostConfig _config;
        private GhostDifficultyProfile _difficulty;

        private float _nextLosCheck;

        public PlayerController VisiblePlayer { get; private set; }

        public struct LastKnown
        {
            public Vector3 position;
            public float time;
            public bool valid;
        }

        private readonly Dictionary<PlayerController, LastKnown> _lastKnown =
            new Dictionary<PlayerController, LastKnown>();

        public bool TryGetMostRecentLastKnown(out Vector3 pos, out float age, out PlayerController player)
        {
            pos = default;
            age = float.MaxValue;
            player = null;
            float newest = -1f;
            foreach (var kv in _lastKnown)
            {
                if (!kv.Value.valid) continue;
                if (kv.Value.time > newest)
                {
                    newest = kv.Value.time;
                    pos = kv.Value.position;
                    player = kv.Key;
                }
            }
            if (newest < 0f) return false;
            age = Time.time - newest;
            return true;
        }

        public void Initialize(GhostConfig config, GhostDifficultyProfile difficulty)
        {
            _config = config;
            _difficulty = difficulty;
            NoiseEventBus.OnNoise += OnNoise;
        }

        private void OnDestroy()
        {
            NoiseEventBus.OnNoise -= OnNoise;
        }

        private void OnNoise(NoiseEvent e)
        {
            if (_config == null) return;

            float hearingMul = _difficulty != null ? _difficulty.hearingRadiusMultiplier : 1f;
            float radius = e.hearingRadius * hearingMul;
            if ((transform.position - e.position).sqrMagnitude > radius * radius) return;

            _noises[_noiseHead] = e;
            _noiseHead = (_noiseHead + 1) % NoiseBufferSize;
        }

        public bool TryGetMostRelevantNoise(out NoiseEvent best)
        {
            best = default;
            if (_config == null) return false;

            float now = Time.time;
            float bestScore = -1f;
            bool found = false;

            for (int i = 0; i < NoiseBufferSize; i++)
            {
                var n = _noises[i];
                if (n.source == null && n.timestamp <= 0f) continue;

                float age = now - n.timestamp;
                if (age > _config.noiseFreshnessSeconds) continue;

                float dist = Vector3.Distance(transform.position, n.position);
                float prox = Mathf.Clamp01(1f - (dist / Mathf.Max(0.01f, n.hearingRadius)));
                float recency = Mathf.Clamp01(1f - (age / _config.noiseFreshnessSeconds));
                float priority = _config.GetPriorityForNoise(n.type);

                float score = priority * (0.6f * recency + 0.4f * prox);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = n;
                    found = true;
                }
            }
            return found;
        }

        public void ConsumeNoise(NoiseEvent n)
        {
            for (int i = 0; i < NoiseBufferSize; i++)
            {
                if (_noises[i].timestamp == n.timestamp && _noises[i].position == n.position)
                {
                    _noises[i] = default;
                    return;
                }
            }
        }

        /// <summary>Called by GhostBrain each tick.</summary>
        public void UpdatePerception()
        {
            if (_config == null) return;
            if (Time.time < _nextLosCheck) return;
            _nextLosCheck = Time.time + _config.losCheckInterval;

            PlayerRegistry.GetAlive(_aliveBuffer);

            float visionRange = _config.coneRange *
                (_difficulty != null ? _difficulty.visionRangeMultiplier : 1f);
            float halfAngle = _config.coneAngleDeg * 0.5f;

            Vector3 origin = transform.position + Vector3.up * 1.6f;
            Vector3 forward = transform.forward;

            PlayerController closestVisible = null;
            float closestSqr = float.MaxValue;

            for (int i = 0; i < _aliveBuffer.Count; i++)
            {
                var p = _aliveBuffer[i];
                if (p == null) continue;

                Vector3 toP = p.transform.position - transform.position;
                float sqr = toP.sqrMagnitude;
                if (sqr > visionRange * visionRange) continue;

                Vector3 flat = new Vector3(toP.x, 0f, toP.z);
                if (flat.sqrMagnitude < 0.0001f) continue;

                float angle = Vector3.Angle(new Vector3(forward.x, 0f, forward.z), flat);
                if (angle > halfAngle) continue;

                Vector3 target = p.transform.position + Vector3.up * 1.2f;
                Vector3 dir = (target - origin).normalized;
                float dist = Vector3.Distance(origin, target);

                if (Physics.Raycast(origin, dir, out var hit, dist, _config.coneRaycastMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == null || hit.collider.transform.root != p.transform.root)
                        continue;
                }

                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closestVisible = p;
                }
            }

            if (closestVisible != null)
            {
                _lastKnown[closestVisible] = new LastKnown
                {
                    position = closestVisible.transform.position,
                    time = Time.time,
                    valid = true
                };
            }
            else if (VisiblePlayer != null)
            {
                // just lost sight — keep the existing last-known entry
            }

            VisiblePlayer = closestVisible;
        }

        public bool HasFreshLastKnown(out Vector3 pos, out PlayerController player)
        {
            pos = default;
            player = null;
            if (_config == null) return false;

            float patience = _config.losPatienceSeconds *
                (_difficulty != null ? _difficulty.losPatienceMultiplier : 1f);

            if (!TryGetMostRecentLastKnown(out pos, out float age, out player)) return false;
            return age <= patience;
        }
    }
}
