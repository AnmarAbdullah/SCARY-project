using Mirror;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    /// <summary>
    /// Pair this with LevelGenerator on the same GameObject (which also needs a NetworkIdentity).
    /// Server rolls a seed in OnStartServer and writes it to a SyncVar; clients run the same
    /// generator with the same seed, so every client sees an identical map.
    ///
    /// Networked gameplay actors (monster, etc.) should NOT be spawned by the LevelGenerator.
    /// Listen to LevelGenerator.OnLevelGenerated and have a server-side spawner walk the grid
    /// (e.g., look up PowerCore category cells) and call NetworkServer.Spawn there.
    /// </summary>
    [RequireComponent(typeof(LevelGenerator))]
    public class NetworkLevelSync : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnSeedChanged))]
        private uint syncedSeed;

        private LevelGenerator generator;

        void Awake()
        {
            generator = GetComponent<LevelGenerator>();
            // Don't auto-generate in Start; we wait for the seed.
            generator.generateOnStart = false;
        }

        public override void OnStartServer()
        {
            uint seed = generator.config != null && generator.config.useFixedSeed
                ? generator.config.fixedSeed
                : (uint)Random.Range(1, int.MaxValue);
            syncedSeed = seed;
            generator.GenerateFromSeed(seed);
        }

        public override void OnStartClient()
        {
            if (isServer) return; // host already generated in OnStartServer
            if (syncedSeed != 0) generator.GenerateFromSeed(syncedSeed);
        }

        void OnSeedChanged(uint oldSeed, uint newSeed)
        {
            if (isServer) return;
            if (newSeed == 0) return;
            generator.GenerateFromSeed(newSeed);
        }
    }
}
