using UnityEngine;
using ScaryGame.Noise;

namespace ScaryGame.Enemy
{
    [CreateAssetMenu(menuName = "SCARY/Ghost/Ghost Config", fileName = "GhostConfig")]
    public class GhostConfig : ScriptableObject
    {
        [Header("Movement Speeds")]
        public float chaseCoreNoiseSpeed = 5.5f;
        public float chaseDeviceNoiseSpeed = 5.0f;
        public float chaseFootstepSpeed = 4.5f;
        public float chasePlayerSpeed = 6.0f;
        public float searchLastKnownSpeed = 4.0f;
        public float returnToBaseSpeed = 3.0f;
        public float roamSpeed = 2.5f;

        [Header("Vision")]
        public float coneAngleDeg = 70f;
        public float coneRange = 18f;
        public LayerMask coneRaycastMask = ~0;
        public float losCheckInterval = 0.15f;

        [Header("Jumpscare")]
        public float jumpscareTriggerDistance = 2.5f;
        public float jumpscareLeapDuration = 0.5f;
        public float jumpscareCollideDistance = 1.0f;

        [Header("Hearing")]
        public float defaultFootstepHearRadius = 12f;
        public float defaultCoreHearRadius = 40f;
        public float defaultDeviceHearRadius = 30f;
        public float voiceChatHearRadius = 15f;
        public float noiseFreshnessSeconds = 4f;

        [Header("Noise Priority Weights (higher = more interesting)")]
        public float priorityFootstep = 1f;
        public float priorityCorePickup = 5f;
        public float priorityNoiseDevice = 3f;
        public float priorityVoiceChat = 2f;
        public float priorityCustom = 1f;

        [Header("Roam-Around-Point")]
        public float roamRadius = 12f;
        public float roamMinRadius = 3f;
        public float roamWaypointDwell = 1.5f;
        public float roamDuration = 12f;

        [Header("Idle / Base")]
        public Vector3 baseStationOffset = Vector3.zero;

        [Header("Search Last Known")]
        public float losPatienceSeconds = 4f;

        [Header("Laser Ability")]
        public float laserMinSecondsBetween = 25f;
        public float laserMaxSecondsBetween = 60f;
        [Range(0f, 1f)] public float laserActivationChance = 0.25f;
        public float laserFreezeDuration = 2f;
        public float laserMaxAllowedSpeed = 0.2f;

        [Header("Toggles (Future Use)")]
        public bool idleRoamsMap;

        public float GetSpeedForNoise(NoiseType type)
        {
            switch (type)
            {
                case NoiseType.CorePickup:  return chaseCoreNoiseSpeed;
                case NoiseType.NoiseDevice: return chaseDeviceNoiseSpeed;
                case NoiseType.VoiceChat:   return chaseFootstepSpeed;
                case NoiseType.Footstep:    return chaseFootstepSpeed;
                default:                    return roamSpeed;
            }
        }

        public float GetPriorityForNoise(NoiseType type)
        {
            switch (type)
            {
                case NoiseType.Footstep:    return priorityFootstep;
                case NoiseType.CorePickup:  return priorityCorePickup;
                case NoiseType.NoiseDevice: return priorityNoiseDevice;
                case NoiseType.VoiceChat:   return priorityVoiceChat;
                default:                    return priorityCustom;
            }
        }

        public float GetHearingRadiusForNoise(NoiseType type)
        {
            switch (type)
            {
                case NoiseType.Footstep:    return defaultFootstepHearRadius;
                case NoiseType.CorePickup:  return defaultCoreHearRadius;
                case NoiseType.NoiseDevice: return defaultDeviceHearRadius;
                case NoiseType.VoiceChat:   return voiceChatHearRadius;
                default:                    return defaultFootstepHearRadius;
            }
        }
    }
}
