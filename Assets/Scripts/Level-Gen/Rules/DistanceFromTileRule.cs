using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Rules/Distance From Tile", fileName = "Rule_DistFromTile")]
    public class DistanceFromTileRule : PlacementRule
    {
        [Tooltip("Specific TileDefinition this rule references.")]
        public TileDefinition referenceTile;

        public int minDistance = 0;
        public int maxDistance = 999;

        public override bool CanPlace(Vector2Int cell, TileDefinition tile, GenerationContext ctx)
        {
            if (referenceTile == null) return true;
            bool foundAny = false;
            bool foundWithinMax = false;
            foreach (var c in ctx.grid.CellsByDefinition(referenceTile))
            {
                foundAny = true;
                int d = ctx.grid.ChebyshevDistance(cell, c);
                if (d < minDistance) return false;
                if (d <= maxDistance) foundWithinMax = true;
            }
            if (foundAny && maxDistance < 999 && !foundWithinMax) return false;
            return true;
        }
    }
}
