using UnityEngine;

namespace ScaryGame.LevelGen
{
    public abstract class PlacementRule : ScriptableObject
    {
        public abstract bool CanPlace(Vector2Int cell, TileDefinition tile, GenerationContext ctx);
    }
}
