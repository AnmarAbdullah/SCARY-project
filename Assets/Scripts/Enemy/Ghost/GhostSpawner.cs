using Mirror;
using UnityEngine;
using ScaryGame.LevelGen;
using ScaryGame.LevelGen.Runtime;

namespace ScaryGame.Enemy
{
    /// <summary>
    /// Server-only. Waits for both LevelGenerator.OnLevelGenerated and the
    /// runtime NavMesh bake to complete, then instantiates the Ghost at the
    /// Monster Base center.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class GhostSpawner : NetworkBehaviour
    {
        [SerializeField] private LevelGenerator generator;
        [SerializeField] private RuntimeNavMeshBaker navMeshBaker;
        [SerializeField] private GameObject ghostPrefab;
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        private bool _levelReady;
        private bool _navMeshReady;
        private bool _spawned;
        private LevelGrid _lastGrid;

        private void Awake()
        {
            if (generator == null) generator = FindObjectOfType<LevelGenerator>();
            if (navMeshBaker == null) navMeshBaker = FindObjectOfType<RuntimeNavMeshBaker>();
        }

        public override void OnStartServer()
        {
            if (generator != null) generator.OnLevelGenerated += OnLevelGenerated;
            if (navMeshBaker != null) navMeshBaker.NavMeshReady += OnNavMeshReady;
        }

        public override void OnStopServer()
        {
            if (generator != null) generator.OnLevelGenerated -= OnLevelGenerated;
            if (navMeshBaker != null) navMeshBaker.NavMeshReady -= OnNavMeshReady;
        }

        private void OnLevelGenerated(LevelGrid grid, uint seed)
        {
            _lastGrid = grid;
            _levelReady = true;
            TrySpawn();
        }

        private void OnNavMeshReady()
        {
            _navMeshReady = true;
            TrySpawn();
        }

        [Server]
        private void TrySpawn()
        {
            if (_spawned || !_levelReady || !_navMeshReady) return;
            if (ghostPrefab == null || generator == null || _lastGrid == null) return;

            float ts = generator.config.tileSize;
            float halfW = (_lastGrid.width - 1) * 0.5f;
            float halfH = (_lastGrid.height - 1) * 0.5f;
            Vector2Int center = _lastGrid.Center;
            Vector3 worldCenter = new Vector3(
                (center.x - halfW) * ts,
                generator.config.tileYOffset,
                (center.y - halfH) * ts);

            Vector3 spawnPos = worldCenter + spawnOffset;
            GameObject go = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
            if (go.TryGetComponent<Ghost>(out var ghost))
                ghost.ServerInitialize(worldCenter);

            NetworkServer.Spawn(go);
            _spawned = true;
        }
    }
}
