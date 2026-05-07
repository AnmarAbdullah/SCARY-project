using System;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    [Serializable]
    public class PropEntry
    {
        public GameObject prefab;

        [Tooltip("How many of this prop to scatter on the tile.")]
        public int countMin = 1;
        public int countMax = 3;

        [Tooltip("Minimum distance between props of this type (world units). 0 = no spacing.")]
        public float minSpacing = 0f;

        [Tooltip("If true, prop is rotated 0/90/180/270 randomly around Y. Else 0-360 continuous.")]
        public bool snapTo90 = false;

        [Tooltip("Random Y rotation enabled.")]
        public bool randomYRotation = true;

        [Tooltip("Random uniform scale range.")]
        public float scaleMin = 1f;
        public float scaleMax = 1f;

        [Tooltip("Padding from tile edges (world units) so props don't clip into neighbor tiles.")]
        public float edgePadding = 0.5f;
    }
}
