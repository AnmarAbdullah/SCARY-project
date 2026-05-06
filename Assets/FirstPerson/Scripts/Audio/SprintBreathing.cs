using UnityEngine;

namespace TimeFracture.Audio
{
    /// <summary>
    /// Handles sprint breathing on a dedicated AudioSource.
    /// Fades in when sprinting starts, fades out when stopped.
    /// Plays inhale/exhale in sequence with randomised timing.
    /// </summary>
    public class SprintBreathing : MonoBehaviour
    {
        [Header("Clips")]
        [Tooltip("Short inhale clips — played at the start of each breath cycle.")]
        public AudioClip[] inhaleClips;

        [Tooltip("Longer exhale clips — played after the inhale.")]
        public AudioClip[] exhaleClips;

        [Header("Timing")]
        [Range(0.3f, 2f)]
        public float inhaleDelay  = 0.4f;   // gap before exhale after inhale fires
        [Range(0.5f, 3f)]
        public float breathCycle  = 1.8f;   // seconds between full breath cycles
        [Range(0f, 0.3f)]
        public float cycleVariance = 0.2f;

        [Header("Volume")]
        [Range(0f, 1f)] public float targetVolume  = 0.6f;
        [Range(1f, 10f)] public float fadeSpeed    = 4f;

        // ── Private ───────────────────────────────────────────────────────
        private AudioSource _source;
        private bool        _active;
        private float       _nextBreathTime;
        private bool        _waitingForExhale;
        private float       _exhaleTime;

        private void Awake()
        {
            _source        = gameObject.AddComponent<AudioSource>();
            _source.loop   = false;
            _source.volume = 0f;
            _source.spatialBlend = 0f; // 2D — heard in headphones only
        }

        private void Update()
        {
            // Fade volume
            float target = _active ? targetVolume : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, Time.deltaTime * fadeSpeed);

            if (!_active) return;

            // Breath cycle sequencer
            if (!_waitingForExhale && Time.time >= _nextBreathTime)
            {
                PlayRandom(inhaleClips);
                _waitingForExhale = true;
                _exhaleTime       = Time.time + inhaleDelay;
            }

            if (_waitingForExhale && Time.time >= _exhaleTime)
            {
                PlayRandom(exhaleClips);
                _waitingForExhale = false;
                _nextBreathTime   = Time.time + breathCycle + Random.Range(-cycleVariance, cycleVariance);
            }
        }

        public void SetActive(bool active)
        {
            if (active && !_active)
                _nextBreathTime = Time.time + 0.3f; // small delay before first breath

            _active = active;
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            _source.PlayOneShot(clips[Random.Range(0, clips.Length)], 1f);
        }
    }
}
