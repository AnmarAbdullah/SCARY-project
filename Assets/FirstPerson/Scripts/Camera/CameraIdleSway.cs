using UnityEngine;
using TimeFracture.Player;

namespace TimeFracture.Camera
{
    /// <summary>
    /// Attach to the Camera GameObject (child of CameraHolder).
    /// Plays a subtle idle animation when the player isn't moving.
    /// Uses layered sine waves at different frequencies — never feels like a loop.
    /// Fades out smoothly when movement starts, fades back in when stopped.
    /// </summary>
    public class CameraIdleSway : MonoBehaviour
    {
        [Header("References")]
        public PlayerMovement playerMovement;

        [Header("Vertical Drift (Y position)")]
        [Tooltip("How far the camera floats up and down.")]
        public float verticalAmount = 0.003f;
        [Tooltip("Speed of the vertical drift cycle.")]
        public float verticalSpeed = 0.8f;

        [Header("Horizontal Drift (X position)")]
        [Tooltip("How far the camera drifts left and right.")]
        public float horizontalAmount = 0.002f;
        [Tooltip("Speed of the horizontal drift — slightly off from vertical so it feels organic.")]
        public float horizontalSpeed = 0.6f;

        [Header("Roll Sway (Z rotation)")]
        [Tooltip("Subtle roll tilt left and right.")]
        public float rollAmount = 0.15f;
        [Tooltip("Speed of the roll sway.")]
        public float rollSpeed = 0.5f;

        [Header("Micro Shake")]
        [Tooltip("Tiny high-frequency noise — barely visible, adds life.")]
        public float shakeAmount = 0.0008f;
        [Tooltip("Speed of the micro shake noise.")]
        public float shakeSpeed = 3f;

        [Header("Blend")]
        [Tooltip("How quickly idle sway fades in when stopped.")]
        [Range(1f, 10f)] public float fadeInSpeed = 2f;
        [Tooltip("How quickly idle sway fades out when moving.")]
        [Range(1f, 10f)] public float fadeOutSpeed = 4f;

        [Header("Toggle")]
        [Tooltip("Uncheck to disable idle sway. Fades out smoothly — never cuts.")]
        public bool enableIdleSway = true;

        // ── Private ───────────────────────────────────────────────────────
        private float _timer;
        private float _weight;       // 0 = no idle, 1 = full idle
        private float _weightVel;
        private float _noiseOffset;  // randomised per instance so two players never sync

        private void OnDisable()
        {
            // Smoothly reset position when disabled so there's no pop
            _weight = 0f;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        private void Awake()
        {
            // Random start offset so it never looks the same twice
            _noiseOffset = Random.Range(0f, 100f);
            _timer = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (playerMovement == null) return;

            // Blend weight — fades in when enabled, out when disabled
            float targetWeight = enableIdleSway ? 1f : 0f;
            float blendSpeed = targetWeight > _weight ? fadeInSpeed : fadeOutSpeed;
            _weight = Mathf.SmoothDamp(_weight, targetWeight, ref _weightVel, 1f / blendSpeed);

            if (_weight < 0.001f)
            {
                // Fully faded — reset position so bob doesn't snap on re-entry
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }

            _timer += Time.deltaTime;

            // ── Layered sines — each axis runs at a different frequency ───

            // Vertical: primary breathing motion
            float y = Mathf.Sin(_timer * verticalSpeed) * verticalAmount;

            // Horizontal: slightly out of phase with vertical
            float x = Mathf.Sin(_timer * horizontalSpeed + 1.3f) * horizontalAmount;

            // Second layer on vertical — slightly different frequency gives organic feel
            y += Mathf.Sin(_timer * verticalSpeed * 1.37f + 0.9f) * (verticalAmount * 0.4f);
            x += Mathf.Sin(_timer * horizontalSpeed * 1.21f + 2.1f) * (horizontalAmount * 0.3f);

            // Roll: slow gentle tilt
            float roll = Mathf.Sin(_timer * rollSpeed + 0.5f) * rollAmount;

            // Micro shake: Perlin noise for organic high-frequency noise
            float noiseX = (Mathf.PerlinNoise(_timer * shakeSpeed + _noiseOffset, 0f) - 0.5f) * shakeAmount;
            float noiseY = (Mathf.PerlinNoise(0f, _timer * shakeSpeed + _noiseOffset) - 0.5f) * shakeAmount;

            // Apply weight so it fades in/out smoothly
            transform.localPosition = new Vector3(
                (x + noiseX) * _weight,
                (y + noiseY) * _weight,
                0f);

            transform.localRotation = Quaternion.Euler(0f, 0f, roll * _weight);
        }
    }
}