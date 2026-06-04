using UnityEngine;

namespace ScaryGame.Noise
{
    public enum NoiseType
    {
        Footstep,
        CorePickup,
        NoiseDevice,
        VoiceChat,
        Custom
    }

    public struct NoiseEvent
    {
        public Vector3 position;
        public NoiseType type;
        public float intensity;
        public float hearingRadius;
        public GameObject source;
        public float timestamp;

        public NoiseEvent(Vector3 position, NoiseType type, float intensity, float hearingRadius, GameObject source)
        {
            this.position = position;
            this.type = type;
            this.intensity = intensity;
            this.hearingRadius = hearingRadius;
            this.source = source;
            this.timestamp = Time.time;
        }
    }
}
