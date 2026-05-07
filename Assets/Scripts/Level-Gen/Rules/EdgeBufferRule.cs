using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Rules/Edge Buffer", fileName = "Rule_EdgeBuffer")]
    public class EdgeBufferRule : PlacementRule
    {
        [Tooltip("Cell must be at least this many tiles from any map edge.")]
        public int buffer = 1;

        public override bool CanPlace(Vector2Int cell, TileDefinition tile, GenerationContext ctx)
        {
            return cell.x >= buffer
                && cell.y >= buffer
                && cell.x < ctx.grid.width - buffer
                && cell.y < ctx.grid.height - buffer;
        }
    }
}
