using Mirror;
using UnityEngine;
using TimeFracture.Audio;

namespace ScaryGame.Noise
{
    /// <summary>
    /// Local player owns this. It calls CmdEmitFootstepNoise on the server when
    /// the local FootstepTrigger fires. Server validates basic sanity and
    /// publishes onto the NoiseEventBus.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class PlayerNoiseRelay : NetworkBehaviour
    {
        [Header("Footstep hearing radius by movement state")]
        public float walkHearRadius = 8f;
        public float sprintHearRadius = 16f;
        public float crouchHearRadius = 3f;

        public void LocalReportFootstep(FootstepState state)
        {
            if (!isLocalPlayer) return;
            CmdEmitFootstepNoise(transform.position, (int)state);
        }

        [Command]
        private void CmdEmitFootstepNoise(Vector3 pos, int state)
        {
            // Loose sanity check — owner's transform should be near reported pos.
            if ((transform.position - pos).sqrMagnitude > 25f) return;

            FootstepState s = (FootstepState)state;
            float radius = s == FootstepState.Sprint ? sprintHearRadius
                         : s == FootstepState.Crouch ? crouchHearRadius
                         : walkHearRadius;
            float intensity = s == FootstepState.Sprint ? 1.0f
                            : s == FootstepState.Crouch ? 0.3f
                            : 0.6f;

            NoiseEventBus.Emit(new NoiseEvent(
                position: transform.position,
                type: NoiseType.Footstep,
                intensity: intensity,
                hearingRadius: radius,
                source: gameObject));
        }
    }
}
