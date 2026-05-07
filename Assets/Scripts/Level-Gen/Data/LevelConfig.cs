using System.Collections.Generic;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Level Config", fileName = "LevelConfig_New")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Map Size")]
        [Tooltip("Number of cells along X and Z. Map is grid.x by grid.y tiles.")]
        public Vector2Int gridSize = new Vector2Int(20, 20);

        [Tooltip("World units per tile. All tile prefabs should fit this footprint.")]
        public float tileSize = 10f;

        [Header("Seed")]
        [Tooltip("If true, every generation uses 'fixedSeed'. Useful for testing.")]
        public bool useFixedSeed = false;
        public uint fixedSeed = 12345;

        [Header("Required Center Tile")]
        [Tooltip("Always placed at grid center. Typically the Monster Base.")]
        public TileDefinition centerTile;

        [Header("Tile Pool")]
        [Tooltip("Required tiles (minCount > 0) are placed first, then terrain fill from this list.")]
        public List<TileDefinition> tiles = new List<TileDefinition>();

        [Tooltip("Used when no other tile fits an empty cell. Should have no rules and category=Terrain.")]
        public TileDefinition fallbackTerrain;

        [Header("Roads")]
        [Tooltip("Bundle of road shape variants (straight, curve, T, cross, end-cap). The generator auto-picks shape + rotation per cell from neighbors.")]
        public RoadTileSet roadTileSet;

        [Tooltip("Master switch for the road carving phase.")]
        public bool carveRoads = true;

        [Header("Road Network — what connects to what")]
        [Range(0f, 1f)]
        [Tooltip("0 = pure tree (every redundant path is skipped — clean but no branches). 1 = full mesh (every candidate pair is carved — dense). 0.2-0.4 = mostly tree with some extra branches that loop back. Sweet spot for organic networks.")]
        public float roadBranchiness = 0.25f;

        [Tooltip("Carve a path from every Spawn to every PowerCore.")]
        public bool connectSpawnsToCores = true;

        [Tooltip("Carve a path from every Spawn to every Objective. Off by default — spawns reach objectives THROUGH cores, which keeps the road network clean.")]
        public bool connectSpawnsToObjectives = false;

        [Tooltip("Carve a path from every PowerCore to every Objective.")]
        public bool connectCoresToObjectives = true;

        [Tooltip("Carve a path between every pair of PowerCores. Off by default — turn on for redundant ring networks.")]
        public bool connectCoresToCores = false;

        [Tooltip("Carve a path between every pair of Objectives.")]
        public bool connectObjectivesToObjectives = false;

        [Tooltip("Number of extra random connections between any two endpoints (Spawn/Core/Objective). Adds redundant cycles for variety.")]
        public int extraRandomConnections = 0;

        [Header("Road Shape — how curvy")]
        [Range(0f, 1f)]
        [Tooltip("0 = straight Manhattan paths. 1 = paths take dramatic curving detours via random waypoints. Default 0.25 keeps things readable.")]
        public float pathWindiness = 0.25f;

        [Tooltip("Categories that road carving must route AROUND (paths cannot enter cells of these categories' adjacent ring). MonsterBase is a sensible default.")]
        public List<TileCategory> roadAvoidCategories = new List<TileCategory> { TileCategory.MonsterBase };

        [Header("Generation Limits")]
        [Tooltip("How many random cell picks to try when placing a required tile before giving up.")]
        public int maxPlacementAttempts = 200;

        [Tooltip("How many full-generation retries with new seeds before erroring.")]
        public int maxRetries = 3;
    }
}
