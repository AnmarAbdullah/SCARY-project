using UnityEngine;

namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Terrain Paint Profile", fileName = "TerrainPaintProfile_New")]
    public class TerrainPaintProfile : ScriptableObject
    {
        [Tooltip("Which terrain layer index to paint.")]
        public int textureIndex = 1;

        [Range(0f, 1f)]
        public float strength = 1f;

        [Header("Corner Rounding")]
        [Range(0f, 1f)]
        public float cornerRadius = 0f;

        [Header("Noise")]
        public Texture2D noiseTexture;
        [Range(0f, 1f)] public float noiseInfluence = 0f;
        [Range(0.1f, 5f)] public float noiseContrast = 1f;

        [Header("Fade")]
        [Range(0f, 1f)] public float fade = 0f;

        [Header("Connectivity")]
        [Tooltip("When enabled, fade/noise/corner effects only apply to the left and right (X) sides. Forward and back (Z) edges paint at full strength so adjacent tiles connect seamlessly.")]
        public bool sidesOnly = false;
    }
}
