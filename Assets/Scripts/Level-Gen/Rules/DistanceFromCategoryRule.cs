using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Rules/Distance From Category", fileName = "Rule_DistFromCategory")]
    public class DistanceFromCategoryRule : PlacementRule
    {
        [Tooltip("The category this rule cares about.")]
        public TileCategory category = TileCategory.MonsterBase;

        [Tooltip("Cell must be at least this many tiles away from EVERY tile of that category.")]
        public int minDistance = 0;

        [Tooltip("Cell must be at most this many tiles away from at least one tile of that category. Set to 999 for 'no max'.")]
        public int maxDistance = 999;

        [Tooltip("If true, the cell is rejected when no tiles of that category exist yet (forces ordering).")]
        public bool requireAtLeastOne = false;

        public override bool CanPlace(Vector2Int cell, TileDefinition tile, GenerationContext ctx)
        {
            bool foundAny = false;
            bool foundWithinMax = false;
            foreach (var c in ctx.grid.CellsByCategory(category))
            {
                foundAny = true;
                int d = ctx.grid.ChebyshevDistance(cell, c);
                if (d < minDistance) return false;
                if (d <= maxDistance) foundWithinMax = true;
            }
            if (requireAtLeastOne && !foundAny) return false;
            if (foundAny && maxDistance < 999 && !foundWithinMax) return false;
            return true;
        }
    }
}
