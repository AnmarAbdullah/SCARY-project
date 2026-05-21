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

        [Tooltip("Y position (height) offset for all spawned tiles. Adjust if tiles spawn above or below the terrain.")]
        public float tileYOffset = 0f;

        [Header("Seed")]
        [Tooltip("If true, every generation uses 'fixedSeed'. Useful for testing.")]
        public bool useFixedSeed = false;
        public uint fixedSeed = 12345;

        [Header("Enemy Base (Center Tile)")]
        [Tooltip("Always placed at grid center. The Monster Base.")]
        public TileDefinition centerTile;

        [Header("Base Station")]
        [Tooltip("List of base station variants with different exit directions. One is randomly chosen per generation. The base station is always placed at the center-bottom of the generated area, connecting to the spawn zone.")]
        public List<BaseStationVariant> baseStationVariants = new List<BaseStationVariant>();

        [Tooltip("How many cells above the bottom edge to place the base station. 0 = bottom row, 1 = one row up, etc.")]
        public int baseStationBottomOffset = 1;

        [Header("Tile Pool")]
        [Tooltip("Required tiles (minCount > 0) are placed first, then terrain fill from this list.")]
        public List<TileDefinition> tiles = new List<TileDefinition>();

        [Tooltip("Used when no other tile fits an empty cell. Should have no rules and category=Terrain.")]
        public TileDefinition fallbackTerrain;

        [Header("Roads")]
        [Tooltip("Bundle of road shape variants (straight, curve, T, cross). The generator auto-picks shape + rotation per cell from neighbors.")]
        public RoadTileSet roadTileSet;

        [Tooltip("Master switch for the road carving phase.")]
        public bool carveRoads = true;

        [Header("Road Routing (Base Station → Power Cores)")]
        [Range(0f, 1f)]
        [Tooltip("0 = pure tree (no redundant paths). 1 = full mesh (every possible path carved). 0.2-0.4 = mostly tree with some extra loops.")]
        public float roadBranchiness = 0.25f;

        [Range(0f, 1f)]
        [Tooltip("Chance that a branch will fork to reach multiple cores instead of chaining through them. 0 = no forks. 1 = always fork where possible.")]
        public float branchForkChance = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("0 = straight Manhattan paths. 1 = paths take dramatic curving detours via random waypoints.")]
        public float pathWindiness = 0.25f;

        [Tooltip("Categories that road carving must route AROUND. MonsterBase is a sensible default.")]
        public List<TileCategory> roadAvoidCategories = new List<TileCategory> { TileCategory.MonsterBase };

        [Header("Terrain Painting")]
        [Tooltip("Shared paint settings used by all TerrainPainter cubes. Individual cubes can override with their own profile.")]
        public TerrainPaintProfile terrainPaintProfile;

        [Header("Generation Limits")]
        [Tooltip("How many random cell picks to try when placing a required tile before giving up.")]
        public int maxPlacementAttempts = 200;

        [Tooltip("How many full-generation retries with new seeds before erroring.")]
        public int maxRetries = 3;
    }
}
