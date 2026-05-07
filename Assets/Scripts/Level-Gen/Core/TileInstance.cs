using UnityEngine;

namespace ScaryGame.LevelGen
{
    public class TileInstance
    {
        public TileDefinition definition;
        public Vector2Int cell;
        public GameObject spawned;

        // Used by the road auto-tiler so curves/Ts face the right way. When true,
        // InstantiateTiles uses 'rotationOverride' instead of any random Y-rotation.
        public bool useRotationOverride;
        public Quaternion rotationOverride = Quaternion.identity;

        public TileInstance(TileDefinition def, Vector2Int cell)
        {
            this.definition = def;
            this.cell = cell;
        }
    }
}
