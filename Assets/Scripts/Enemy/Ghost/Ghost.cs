using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace ScaryGame.Enemy
{
    /// <summary>
    /// Root NetworkBehaviour for the Ghost. All AI runs server-side. Clients see
    /// her via NetworkTransform + NetworkAnimator (added on the prefab) and read
    /// a single SyncVar for the laser "freeze" warning.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(GhostMover))]
    public class Ghost : NetworkBehaviour
    {
        [Header("Configuration")]
        public GhostConfig config;
        public GhostDifficultyProfile difficulty;

        [Header("Components")]
        [SerializeField] private GhostMover mover;
        [SerializeField] private GhostAnimator animatorAdapter;
        [SerializeField] private GhostAudio audioAdapter;

        [SyncVar] private bool _laserActive;
        public bool LaserActive => _laserActive;

        private GhostPerception _perception;
        private GhostBrain _brain;
        private Vector3 _basePosition;

        private void Awake()
        {
            if (mover == null) mover = GetComponent<GhostMover>();
            if (animatorAdapter == null) animatorAdapter = GetComponent<GhostAnimator>();
            if (audioAdapter == null) audioAdapter = GetComponent<GhostAudio>();
        }

        /// <summary>Set by the spawner before NetworkServer.Spawn.</summary>
        [Server]
        public void ServerInitialize(Vector3 basePosition)
        {
            _basePosition = basePosition;
        }

        public override void OnStartServer()
        {
            if (config == null)
            {
                Debug.LogError("[Ghost] GhostConfig is not assigned.", this);
                enabled = false;
                return;
            }

            if (_basePosition == Vector3.zero)
                _basePosition = transform.position;

            _perception = gameObject.AddComponent<GhostPerception>();
            _perception.Initialize(config, difficulty);

            _brain = new GhostBrain(
                this, config, difficulty,
                mover, _perception, animatorAdapter, audioAdapter,
                _basePosition);
            _brain.Start();
        }

        private void Update()
        {
            if (!NetworkServer.active) return;
            if (_brain == null) return;
            _brain.Tick(Time.deltaTime);
        }

        [Server]
        public void ServerSetLaserActive(bool on) => _laserActive = on;
    }
}
