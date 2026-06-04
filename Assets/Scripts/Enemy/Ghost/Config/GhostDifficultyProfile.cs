using UnityEngine;

namespace ScaryGame.Enemy
{
    [CreateAssetMenu(menuName = "SCARY/Ghost/Difficulty Profile", fileName = "GhostDifficulty")]
    public class GhostDifficultyProfile : ScriptableObject
    {
        [Tooltip("Multiplier on every movement speed.")]
        public float speedMultiplier = 1f;

        [Tooltip("Multiplier on every hearing radius.")]
        public float hearingRadiusMultiplier = 1f;

        [Tooltip("Multiplier on cone vision range.")]
        public float visionRangeMultiplier = 1f;

        [Tooltip("Multiplier on laser ability frequency (inverse: applied as 1/x to min/max delay).")]
        public float laserFrequencyMultiplier = 1f;

        [Tooltip("Multiplier on how long she'll keep searching last known position.")]
        public float losPatienceMultiplier = 1f;
    }
}
