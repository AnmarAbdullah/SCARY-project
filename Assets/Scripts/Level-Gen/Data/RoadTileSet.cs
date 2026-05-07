using UnityEngine;

namespace ScaryGame.LevelGen
{
    /// <summary>
    /// Bundle of road shape variants. The generator picks the right one for each
    /// road cell based on which of its 4 cardinal neighbors are also road cells.
    ///
    /// AUTHORING CONVENTIONS (must match in your prefabs):
    ///   - Tiles face the world's +Z axis as "north".
    ///   - Straight: authored running NORTH-SOUTH (along Z). Rotated 90 for E-W.
    ///   - Curve: authored connecting NORTH and EAST (an L from +Z to +X). Rotated for the 3 other corners.
    ///   - T-junction: authored with the bar running E-W and the stem pointing SOUTH (so the missing
    ///     connection is NORTH). Rotated for the 3 other orientations.
    ///   - Cross: 4-way intersection. No rotation needed.
    ///   - End cap (optional): authored OPENING NORTH (the road approaches from +Z and dead-ends here).
    ///
    /// All slots assigned to TileDefinitions with Category = Road.
    /// If you only have one road prefab so far, assign it to every slot — shapes will all look the
    /// same but rotations will still be applied, which is enough to see the system working.
    /// </summary>
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Road Tile Set", fileName = "RoadTileSet_New")]
    public class RoadTileSet : ScriptableObject
    {
        [Tooltip("Straight road authored running N-S along +Z.")]
        public TileDefinition straight;

        [Tooltip("90-degree curve authored connecting NORTH and EAST.")]
        public TileDefinition curve;

        [Tooltip("T-junction authored with the bar E-W and the stem pointing SOUTH (missing N).")]
        public TileDefinition tJunction;

        [Tooltip("Four-way intersection.")]
        public TileDefinition cross;

        [Tooltip("Optional dead-end stub authored opening NORTH. If null, 'straight' is used as a fallback.")]
        public TileDefinition endCap;

        [Tooltip("Optional tile for an isolated road cell with no neighbors. If null, 'straight' is used.")]
        public TileDefinition solo;

        public const int BIT_N = 1;
        public const int BIT_E = 2;
        public const int BIT_S = 4;
        public const int BIT_W = 8;

        /// <summary>
        /// Given a 4-bit neighbor mask (BIT_N | BIT_E | BIT_S | BIT_W), returns the right tile
        /// definition and the Y-rotation in degrees to apply when spawning it.
        /// </summary>
        public (TileDefinition def, float yaw) Resolve(int mask)
        {
            switch (mask)
            {
                case 0: return (solo != null ? solo : straight, 0f);

                case BIT_N: return (endCap != null ? endCap : straight, 0f);
                case BIT_E: return (endCap != null ? endCap : straight, 90f);
                case BIT_S: return (endCap != null ? endCap : straight, 180f);
                case BIT_W: return (endCap != null ? endCap : straight, 270f);

                case BIT_N | BIT_S: return (straight, 0f);
                case BIT_E | BIT_W: return (straight, 90f);

                case BIT_N | BIT_E: return (curve, 0f);
                case BIT_E | BIT_S: return (curve, 90f);
                case BIT_S | BIT_W: return (curve, 180f);
                case BIT_W | BIT_N: return (curve, 270f);

                case BIT_E | BIT_S | BIT_W: return (tJunction, 0f);    // missing N
                case BIT_N | BIT_S | BIT_W: return (tJunction, 90f);   // missing E
                case BIT_N | BIT_E | BIT_W: return (tJunction, 180f);  // missing S
                case BIT_N | BIT_E | BIT_S: return (tJunction, 270f);  // missing W

                case BIT_N | BIT_E | BIT_S | BIT_W: return (cross, 0f);

                default: return (straight, 0f);
            }
        }
    }
}
