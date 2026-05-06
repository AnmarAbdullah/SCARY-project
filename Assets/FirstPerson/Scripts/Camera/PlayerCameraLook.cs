using UnityEngine;
using TimeFracture.Interfaces;
using TimeFracture.Player;

namespace TimeFracture.Camera
{
    public class PlayerCameraLook : MonoBehaviour, ILookable
    {
        [Header("References")]
        public Transform cameraHolder;
        public Transform playerBody;
        public PlayerMovement playerMovement;

        [Header("Look Settings")]
        [Range(1f, 20f)] public float sensitivity = 2f;
        [Range(60f, 90f)] public float pitchLimit = 85f;

        [Header("Sway — Movement Tilt")]
        [Tooltip("Pitch tilt — moving forward tilts camera back.")]
        public float swayForwardBackTilt = 3f;
        [Tooltip("Roll tilt — strafing tilts camera opposite direction.")]
        public float swaySideTilt = 2.5f;
        [Range(1f, 20f)] public float swaySpeed = 6f;
        [Range(1f, 20f)] public float swayReturn = 8f;

        [Header("Momentum Overshoot — On Stop")]
        [Tooltip("How far the camera overshoots past zero when stopping. Higher = more dramatic.")]
        public float overshootAmount = 1.5f;
        [Tooltip("How fast the overshoot settles back to zero. Lower = longer wobble.")]
        [Range(1f, 20f)] public float overshootDamping = 5f;
        [Tooltip("How much of the previous sway velocity carries into the overshoot.")]
        [Range(0f, 1f)] public float overshootInheritance = 0.6f;

        // ── Private ───────────────────────────────────────────────────────
        private float _pitch;
        private float _yaw;

        // Sway
        private float _swayPitch;
        private float _swayRoll;
        private float _swayPitchVel;
        private float _swayRollVel;

        // Overshoot — separate layer added on top of sway
        private float _overshootPitch;
        private float _overshootRoll;
        private float _overshootPitchVel;
        private float _overshootRollVel;

        // Track previous sway to detect stop moment
        private float _prevSwayPitch;
        private float _prevSwayRoll;
        private bool _wasMoving;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _yaw = playerBody.eulerAngles.y;
        }

        private void Update() { }

        public void Look(Vector2 input)
        {
            _yaw += input.x;
            _pitch -= input.y;
            _pitch = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);

            playerBody.rotation = Quaternion.Euler(0f, _yaw, 0f);

            UpdateSway();

            // CameraHolder is a root object — must use world rotation including yaw
            cameraHolder.rotation = Quaternion.Euler(
                _pitch + _swayPitch + _overshootPitch,
                _yaw,
                _swayRoll + _overshootRoll);
        }

        public void SetSensitivity(float value) => sensitivity = value;

        private void UpdateSway()
        {
            if (playerMovement == null) return;

            Vector3 localVel = playerMovement.transform.InverseTransformDirection(playerMovement.Velocity);
            float fwd = Mathf.Clamp(localVel.z / 9f, -1f, 1f);
            float right = Mathf.Clamp(localVel.x / 9f, -1f, 1f);
            bool isMoving = playerMovement.IsMoving;

            // ── Detect stop moment ────────────────────────────────────────
            if (_wasMoving && !isMoving)
            {
                // Player just stopped — kick overshoot in the SAME direction as sway was going
                // so it carries through zero and comes back (like a pendulum)
                _overshootPitchVel = _swayPitch * overshootInheritance * overshootAmount;
                _overshootRollVel = _swayRoll * overshootInheritance * overshootAmount;
            }
            _wasMoving = isMoving;

            // ── Sway ──────────────────────────────────────────────────────
            float targetPitch = isMoving ? -fwd * swayForwardBackTilt : 0f;
            float targetRoll = isMoving ? -right * swaySideTilt : 0f;
            float smooth = 1f / (isMoving ? swaySpeed : swayReturn);

            _prevSwayPitch = _swayPitch;
            _prevSwayRoll = _swayRoll;

            _swayPitch = Mathf.SmoothDamp(_swayPitch, targetPitch, ref _swayPitchVel, smooth);
            _swayRoll = Mathf.SmoothDamp(_swayRoll, targetRoll, ref _swayRollVel, smooth);

            // ── Overshoot spring — decays back to zero on its own ─────────
            float dampTime = 1f / overshootDamping;
            _overshootPitch = Mathf.SmoothDamp(_overshootPitch, 0f, ref _overshootPitchVel, dampTime);
            _overshootRoll = Mathf.SmoothDamp(_overshootRoll, 0f, ref _overshootRollVel, dampTime);
        }
    }
}