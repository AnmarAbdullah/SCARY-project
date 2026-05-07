using System.Collections.Generic;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Rules/Exclusive Zone", fileName = "Rule_ExclusiveZone")]
    public class ExclusiveZoneRule : PlacementRule
    {
        [Tooltip("Cell is rejected if any tile with one of these categories sits within 'radius' (inclusive).")]
        public List<TileCategory> excludedCategories = new List<TileCategory>();

        [Tooltip("Chebyshev radius. 1 = directly adjacent cells excluded.")]
        public int radius = 1;

        public override bool CanPlace(Vector2Int cell, TileDefinition tile, GenerationContext ctx)
        {
            foreach (var cat in excludedCategories)
            {
                foreach (var c in ctx.grid.CellsByCategory(cat))
                {
                    if (ctx.grid.ChebyshevDistance(cell, c) <= radius) return false;
                }
            }
            return true;
        }
    }
}
