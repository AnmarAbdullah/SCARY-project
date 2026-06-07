using UnityEngine;

namespace ScaryGame.Networking
{
    /// <summary>
    /// A per-scene list of placed transforms used as spawn / stand positions.
    /// Drop one in a scene, drag your empty GameObjects into <see cref="points"/>,
    /// and the active scene's registry is reachable via <see cref="Instance"/>.
    ///
    /// Used in two places by <see cref="ScaryNetworkManager"/>:
    ///   * In the menu/lobby scene — "stand here" spots for parked players.
    ///   * In each level scene — the player spawn points.
    ///
    /// Single-scene loading means only one registry is active at a time, so a
    /// simple static Instance is enough (no cross-scene collisions).
    /// </summary>
    public class SpawnPointRegistry : MonoBehaviour
    {
        public static SpawnPointRegistry Instance { get; private set; }

        [Tooltip("The placed empty GameObjects to spawn/stand players on. " +
                 "Order matters — player 0 uses element 0, player 1 element 1, etc.")]
        [SerializeField] private Transform[] points;

        public int Count => points != null ? points.Length : 0;

        private void Awake()
        {
            // Last-loaded registry wins; on single-scene loads there's only ever one.
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Returns the spot for the given player index. Wraps if there are more
        /// players than spots (with a warning), and falls back to this object's
        /// own transform if no points are assigned.
        /// </summary>
        public Transform GetSpot(int index)
        {
            if (points == null || points.Length == 0)
            {
                Debug.LogWarning($"[SpawnPointRegistry] No points assigned on '{name}'; using its own transform.", this);
                return transform;
            }

            if (index >= points.Length)
            {
                Debug.LogWarning($"[SpawnPointRegistry] Player index {index} exceeds {points.Length} spots on '{name}'; wrapping.", this);
                index %= points.Length;
            }

            Transform spot = points[index];
            return spot != null ? spot : transform;
        }
    }
}
