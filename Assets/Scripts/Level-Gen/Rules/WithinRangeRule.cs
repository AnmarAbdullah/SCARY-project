using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Rules/Within Range", fileName = "Rule_WithinRange")]
    public class WithinRangeRule : PlacementRule
    {
        [Tooltip("Cell must sit within 'maxDistance' of at least one tile of this category.")]
        public TileCategory category = TileCategory.PowerCore;

        public int maxDistance = 5;

        [Tooltip("If true, also rejected when no tiles of that category exist yet.")]
        public bool requireAtLeastOne = true;

        public override bool CanPlace(Vector2Int cell, TileDefinition tile, GenerationContext ctx)
        {
            bool foundAny = false;
            foreach (var c in ctx.grid.CellsByCategory(category))
            {
                foundAny = true;
                if (ctx.grid.ChebyshevDistance(cell, c) <= maxDistance) return true;
            }
            if (!foundAny && !requireAtLeastOne) return true;
            return false;
        }
    }
}
