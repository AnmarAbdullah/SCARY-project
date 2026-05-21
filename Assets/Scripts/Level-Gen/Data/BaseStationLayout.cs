using System;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    public enum BaseStationExit
    {
        Forward,
        Left,
        Right,
        ForwardLeft,
        ForwardRight,
        LeftRight,
        ForwardLeftRight
    }

    [Serializable]
    public class BaseStationVariant
    {
        [Tooltip("The base station tile prefab for this exit layout.")]
        public TileDefinition tile;

        [Tooltip("Which exits this variant has (relative to the spawn direction: Forward = deeper into the map, Left/Right = sideways).")]
        public BaseStationExit exits;

        [Tooltip("Selection weight. Higher = more likely to be chosen.")]
        public float weight = 1f;
    }
}
