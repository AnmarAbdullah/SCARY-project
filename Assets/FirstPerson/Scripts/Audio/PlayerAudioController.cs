using UnityEngine;
using TimeFracture.Interfaces;
using TimeFracture.Player;

namespace TimeFracture.Audio
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(FootstepTrigger))]
    [RequireComponent(typeof(SprintBreathing))]
    public class PlayerAudioController : MonoBehaviour, IAudioPlayer
    {
        // ... (Existing fields kept exactly as they were) ...
        [Header("Footstep Profile")]
        public FootstepProfile defaultProfile;

        [Header("Surface Detection")]
        public float surfaceRayLength = 1.5f;
        public LayerMask surfaceLayerMask = ~0;

        [Header("Jump & Land Clips")]
        public AudioClip[] jumpClips;
        public AudioClip[] landSoftClips;
        public AudioClip[] landHardClips;

        [Range(0f, 1f)] public float jumpVolume = 0.5f;
        [Range(0f, 1f)] public float landSoftVolume = 0.4f;
        [Range(0f, 1f)] public float landHardVolume = 0.7f;

        public float hardLandThreshold = 6f;

        [Header("Audio Sources")]
        public AudioSource footstepSource;
        public AudioSource actionSource;

        private PlayerMovement _movement;
        private FootstepTrigger _footstepTrigger;
        private SprintBreathing _breathing;
        private int _lastFootstepIndex = -1;
        private bool _wasGrounded;
        private SurfaceType _currentSurface = SurfaceType.Default;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _footstepTrigger = GetComponent<FootstepTrigger>();
            _breathing = GetComponent<SprintBreathing>();

            // Ensure sources exist
            if (footstepSource == null) SetupSource(ref footstepSource);
            if (actionSource == null) SetupSource(ref actionSource);
        }

        private void SetupSource(ref AudioSource source)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.loop = false;
        }

        private void Update()
        {
            DetectSurface();

            FootstepState state = _movement.IsCrouching ? FootstepState.Crouch
                                : _movement.IsSprinting ? FootstepState.Sprint
                                : FootstepState.Walk;

            _footstepTrigger.Tick(_movement.IsMoving, _movement.IsGrounded, state);
            _breathing.SetActive(_movement.IsSprinting);

            if (!_wasGrounded && _movement.IsGrounded)
                PlayLand(Mathf.Abs(_movement.Velocity.y));

            _wasGrounded = _movement.IsGrounded;
        }

        private void DetectSurface()
        {
            if (!_movement.IsGrounded) return;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down,
                                out RaycastHit hit, surfaceRayLength,
                                surfaceLayerMask, QueryTriggerInteraction.Ignore))
            {
                var identifier = hit.collider.GetComponent<SurfaceIdentifier>()
                              ?? hit.collider.GetComponentInParent<SurfaceIdentifier>();
                _currentSurface = identifier != null ? identifier.surfaceType : SurfaceType.Default;
            }
        }

        // ── NEW METHOD ──────────────────────────────────────────────────

        /// <summary>
        /// Plays an item pickup sound through the player's action audio source.
        /// </summary>
        public void PlayPickup(AudioClip clip, float volume)
        {
            if (clip == null || actionSource == null) return;
            actionSource.PlayOneShot(clip, volume);
        }

        // ── IAudioPlayer ──────────────────────────────────────────────────

        public void PlayFootstep(FootstepState state)
        {
            if (defaultProfile == null) return;
            AudioClip clip = defaultProfile.GetClip(_currentSurface, state, ref _lastFootstepIndex);
            float volume = defaultProfile.GetVolume(_currentSurface, state);
            float variance = defaultProfile.GetPitchVariance(_currentSurface);
            if (clip == null) return;
            footstepSource.pitch = 1f + Random.Range(-variance, variance);
            footstepSource.PlayOneShot(clip, volume);
        }

        public void PlayJump() => PlayRandom(actionSource, jumpClips, jumpVolume);

        public void PlayLand(float impactSpeed)
        {
            if (impactSpeed < 1f) return;
            bool hard = impactSpeed >= hardLandThreshold;
            AudioClip[] pool = hard ? landHardClips : landSoftClips;
            float volume = hard ? landHardVolume : landSoftVolume;
            PlayRandom(actionSource, pool, volume);
        }

        public void SetSprintBreathing(bool active) => _breathing.SetActive(active);

        private void PlayRandom(AudioSource source, AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0 || source == null) return;
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        }
    }
}