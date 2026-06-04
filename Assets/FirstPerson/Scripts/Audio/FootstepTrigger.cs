using UnityEngine;
using ScaryGame.Noise;
using TimeFracture.Interfaces;

namespace TimeFracture.Audio
{
    /// <summary>
    /// Tracks distance walked and fires footstep events at the right intervals.
    /// Decoupled from the audio player — only raises events.
    /// </summary>
    public class FootstepTrigger : MonoBehaviour
    {
        [Header("Step Intervals (units between steps)")]
        public float walkStepInterval   = 2.3f;
        public float sprintStepInterval = 1.6f;
        public float crouchStepInterval = 3.0f;

        // ── Private ───────────────────────────────────────────────────────
        private float            _distanceTravelled;
        private Vector3          _lastPosition;
        private IAudioPlayer     _audioPlayer;
        private PlayerNoiseRelay _noiseRelay;

        private void Awake()
        {
            _audioPlayer  = GetComponent<IAudioPlayer>();
            _noiseRelay   = GetComponentInParent<PlayerNoiseRelay>();
            _lastPosition = transform.position;
        }

        /// <summary>Call every frame from PlayerSoundController.</summary>
        public void Tick(bool isMoving, bool isGrounded, FootstepState state)
        {
            if (!isMoving || !isGrounded)
            {
                _lastPosition = transform.position;
                return;
            }

            // Only count horizontal distance — ignore vertical (jumping, slopes)
            Vector3 current  = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 previous = new Vector3(_lastPosition.x,      0f, _lastPosition.z);
            _distanceTravelled += Vector3.Distance(current, previous);
            _lastPosition       = transform.position;

            float interval = state == FootstepState.Sprint ? sprintStepInterval
                           : state == FootstepState.Crouch ? crouchStepInterval
                           : walkStepInterval;

            if (_distanceTravelled >= interval)
            {
                _distanceTravelled = 0f;
                _audioPlayer?.PlayFootstep(state);
                _noiseRelay?.LocalReportFootstep(state);
            }
        }

        public void ResetDistance() => _distanceTravelled = 0f;
    }
}
