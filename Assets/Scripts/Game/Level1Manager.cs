using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using ScaryGame.Noise;

namespace ScaryGame.Game
{
    /// <summary>
    /// Server-authoritative manager for Level 1 (Forest — signal towers).
    ///
    /// Responsibilities:
    ///   * Spawns the satellite towers onto designer-authored slot transforms,
    ///     each at a UNIQUE slot so two never share a location.
    ///   * Detects when an individual satellite is finished (signal -> 0%).
    ///   * Detects when ALL satellites are finished (the level objective).
    ///
    /// Each spawn slot is a full Transform (position AND rotation). Because the
    /// tower's far-off "computer" canvas is a fixed local offset inside the
    /// prefab, instantiating at a slot's position+rotation moves the whole rig
    /// as one unit — so a slot that's been authored to fit cleanly in-editor
    /// always fits at runtime, no matter which slots get chosen.
    ///
    /// Future hooks (sound on 1/2 done, voice line on all done, ghost-attracting
    /// noise) are scaffolded below and marked with TODO so they're trivial to
    /// fill in once the audio/voice assets exist.
    /// </summary>
    public class Level1Manager : NetworkBehaviour
    {
        [Header("Satellite Spawning")]
        [Tooltip("Networked satellite tower prefab. Must have a NetworkIdentity + Satelite component, " +
                 "and be registered as a spawnable prefab on the NetworkManager.")]
        [SerializeField] private GameObject satellitePrefab;

        [Tooltip("Designer-placed candidate slots (position + rotation). Author each one in-editor so the " +
                 "full tower rig — wheel AND computer canvas — fits cleanly with no terrain/tree clipping. " +
                 "Must contain at least 'satelliteCount' entries; the manager picks unique slots at random.")]
        [SerializeField] private Transform[] spawnSlots;

        [Tooltip("How many satellites to spawn this playthrough.")]
        [SerializeField, Min(1)] private int satelliteCount = 3;

        [Header("Ghost-Attracting Noise (future)")]
        [Tooltip("If true, completing a satellite emits a NoiseEvent so the Ghost is drawn toward it. " +
                 "Leave off until ghost tuning is ready.")]
        [SerializeField] private bool emitGhostNoiseOnComplete = false;
        [SerializeField] private float completeNoiseIntensity = 1f;
        [SerializeField] private float completeNoiseHearingRadius = 25f;

        [Header("Audio (future — wire when clips exist)")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Played each time a satellite is finished (e.g. a progress chime).")]
        [SerializeField] private AudioClip satelliteCompletedSfx;
        [Tooltip("Played / spoken once when ALL satellites are finished (Speaker Lady line, etc.).")]
        [SerializeField] private AudioClip allCompletedVoiceLine;

        // --- Runtime state (server) ---
        private readonly List<Satelite> _satellites = new List<Satelite>();
        // Per-satellite completion delegate, kept so we can unsubscribe cleanly.
        private readonly Dictionary<Satelite, Action> _completionHandlers = new Dictionary<Satelite, Action>();

        /// <summary>Number of satellites finished so far. Synced so client UI can show progress.</summary>
        [SyncVar] private int _completedCount;
        public int CompletedCount => _completedCount;

        public int TotalCount => _satellites.Count;
        public bool AllComplete => _satellites.Count > 0 && _completedCount >= _satellites.Count;

        /// <summary>Fires on the server each time a satellite finishes; arg is the new completed count.</summary>
        public event Action<int> OnSatelliteCompleted;
        /// <summary>Fires on the server once when every satellite is finished.</summary>
        public event Action OnAllSatellitesCompleted;

        private bool _allCompleteFired;
        
        [SerializeField] GateButton gateButton;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnSatellites();
        }

        public override void OnStopServer()
        {
            // Unsubscribe so completion callbacks don't fire after teardown.
            foreach (KeyValuePair<Satelite, Action> entry in _completionHandlers)
            {
                if (entry.Key != null)
                    entry.Key.OnCompleted -= entry.Value;
            }
            _completionHandlers.Clear();
            _satellites.Clear();
            base.OnStopServer();
        }

        [Server]
        private void SpawnSatellites()
        {
            if (satellitePrefab == null)
            {
                Debug.LogError("[Level1Manager] No satellite prefab assigned.", this);
                return;
            }

            if (spawnSlots == null || spawnSlots.Length < satelliteCount)
            {
                Debug.LogError($"[Level1Manager] Need at least {satelliteCount} spawn slots, " +
                               $"but only {spawnSlots?.Length ?? 0} are assigned.", this);
                return;
            }

            foreach (int slotIndex in PickUniqueSlots(satelliteCount, spawnSlots.Length))
            {
                Transform slot = spawnSlots[slotIndex];
                if (slot == null)
                {
                    Debug.LogWarning($"[Level1Manager] Spawn slot {slotIndex} is null; skipping.", this);
                    continue;
                }

                GameObject instance = Instantiate(satellitePrefab, slot.position, slot.rotation);
                NetworkServer.Spawn(instance);

                Satelite satellite = instance.GetComponentInChildren<Satelite>();
                if (satellite == null)
                {
                    Debug.LogError("[Level1Manager] Satellite prefab has no Satelite component.", instance);
                    continue;
                }

                // Capture this specific satellite so its completion reports itself
                // (accurate noise origin / SFX position for the Ghost).
                Satelite captured = satellite;
                Action handler = () => HandleSatelliteCompleted(captured);
                captured.OnCompleted += handler;

                _satellites.Add(captured);
                _completionHandlers[captured] = handler;
            }
        }

        /// <summary>
        /// Fisher-Yates partial shuffle: yields 'count' distinct slot indices in
        /// [0, slotCount), so no two satellites ever land on the same slot.
        /// </summary>
        private static IEnumerable<int> PickUniqueSlots(int count, int slotCount)
        {
            int[] indices = new int[slotCount];
            for (int i = 0; i < slotCount; i++)
                indices[i] = i;

            for (int i = 0; i < count; i++)
            {
                int swap = UnityEngine.Random.Range(i, slotCount);
                (indices[i], indices[swap]) = (indices[swap], indices[i]);
                yield return indices[i];
            }
        }

        [Server]
        private void HandleSatelliteCompleted(Satelite satellite)
        {
            _completedCount++;

            // --- Per-satellite hooks (1 done, 2 done, ...) ---
            // TODO: progress chime / Speaker Lady "one down" line.
            PlaySfx(satelliteCompletedSfx);

            // TODO: one (or more) completions should make noise that lures the Ghost.
            if (emitGhostNoiseOnComplete)
                EmitGhostNoise(satellite);

            OnSatelliteCompleted?.Invoke(_completedCount);

            if (AllComplete && !_allCompleteFired)
            {
                _allCompleteFired = true;
                HandleAllSatellitesCompleted();
            }
        }

        [Server]
        private void HandleAllSatellitesCompleted()
        {
            // --- All-done hooks ---
            // TODO: Speaker Lady voice line, then unlock the gate to Level 2
            //       (players still need to find the cabin fuse + regroup at the gate).
            PlaySfx(allCompletedVoiceLine);
            
            //Remove electricity particle effect on button... and make it isInteractable
            
            gateButton.MakeButtonInteractable();
            
            OnAllSatellitesCompleted?.Invoke();
        }

        [Server]
        private void EmitGhostNoise(Satelite source)
        {
            Vector3 position = source != null ? source.transform.position : transform.position;

            NoiseEventBus.Emit(new NoiseEvent(
                position,
                NoiseType.Custom,
                completeNoiseIntensity,
                completeNoiseHearingRadius,
                source != null ? source.gameObject : gameObject));
        }

        
        private void PlaySfx(AudioClip clip)
        {
            // Placeholder until audio is wired. Networked playback (ClientRpc) can
            // be added here later so all players hear it.
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
