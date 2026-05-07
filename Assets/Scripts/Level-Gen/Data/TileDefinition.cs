using System.Collections.Generic;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Tile Definition", fileName = "Tile_New")]
    public class TileDefinition : ScriptableObject
    {
        [Tooltip("Optional id for debug logs and DistanceFromTile rules.")]
        public string id;

        [Tooltip("Prefab spawned for each cell occupied by this tile.")]
        public GameObject prefab;

        [Tooltip("High-level category. Rules filter on this. Use Custom for unique tile types.")]
        public TileCategory category = TileCategory.Terrain;

        [Tooltip("Free-form tag for finer filtering, e.g., 'HealingChamber'. Optional.")]
        public string customTag;

        [Header("Counts")]
        [Tooltip("Generator must place at least this many. If it can't, generation retries with a new seed.")]
        public int minCount = 0;

        [Tooltip("Generator will not place more than this many.")]
        public int maxCount = 999;

        [Tooltip("Probability weight when filling terrain. Higher = more common.")]
        public float weight = 1f;

        [Header("Placement Rules")]
        [Tooltip("All rules must pass for a cell to be valid. Drag rule assets here.")]
        public List<PlacementRule> rules = new List<PlacementRule>();

        [Header("Rotation")]
        [Tooltip("If true, prefab gets a random 90-degree rotation around Y when spawned.")]
        public bool randomYRotationStep90 = false;
    }
}
