using System;
using Unity.AI.Navigation;
using UnityEngine;
using ScaryGame.LevelGen;

namespace ScaryGame.LevelGen.Runtime
{
    /// <summary>
    /// Listens to LevelGenerator.OnLevelGenerated, builds the NavMeshSurface,
    /// then raises NavMeshReady. Place on (or next to) the LevelGenerator.
    /// One per scene.
    /// </summary>
    [RequireComponent(typeof(LevelGenerator))]
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        [SerializeField] private NavMeshSurface surface;
        [SerializeField] private LevelGenerator generator;

        public event Action NavMeshReady;
        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (generator == null) generator = GetComponent<LevelGenerator>();
            if (surface == null) surface = GetComponentInChildren<NavMeshSurface>();
        }

        private void OnEnable()
        {
            if (generator != null)
                generator.OnLevelGenerated += HandleGenerated;
        }

        private void OnDisable()
        {
            if (generator != null)
                generator.OnLevelGenerated -= HandleGenerated;
        }

        private void HandleGenerated(LevelGrid grid, uint seed)
        {
            if (surface == null)
            {
                Debug.LogError("[RuntimeNavMeshBaker] No NavMeshSurface assigned.", this);
                return;
            }

            surface.BuildNavMesh();
            IsReady = true;
            NavMeshReady?.Invoke();
        }
    }
}
