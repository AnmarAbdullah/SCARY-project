using UnityEngine;

namespace TimeFracture.Camera
{
    public class HeadBob : MonoBehaviour
    {
        [Header("Bob Speeds")]
        public float walkBobSpeed = 10f;
        public float sprintBobSpeed = 16f;
        public float crouchBobSpeed = 7f;

        [Header("Positional Amplitude")]
        public float walkBobAmountY = 0.040f;
        public float sprintBobAmountY = 0.065f;
        public float crouchBobAmountY = 0.020f;
        public float walkBobAmountX = 0.020f;
        public float sprintBobAmountX = 0.035f;
        public float crouchBobAmountX = 0.010f;

        [Header("Roll (Z Rotation)")]
        public float walkRollAmount = 1.5f;
        public float sprintRollAmount = 2.5f;
        public float crouchRollAmount = 0.8f;

        [Header("Smoothing")]
        [Range(1f, 20f)] public float bobSmoothing = 14f;
        [Tooltip("How quickly the bob fades out when you stop. Lower = slower fade.")]
        [Range(0.5f, 10f)] public float fadeOutSpeed = 2f;

        [Header("Air Drift")]
        public float airPitchAmount = 2f;
        public float airRollAmount = 1f;
        [Range(1f, 15f)] public float airDriftSmoothing = 3f;
        public float velocityScale = 8f;

        private float _bobTimer;
        private float _amplitude = 0f;
        private float _amplitudeVelocity;
        private float _lastSpeed;
        private float _lastAmtY, _lastAmtX, _lastRoll;

        private Vector3 _currentPosVelocity;
        private float _currentRollVelocity;
        private float _airPitch, _airRoll;
        private float _airPitchVelocity, _airRollVelocity;

        public void Tick(bool isMoving, bool isSprinting, bool isCrouching, bool isGrounded, Vector3 velocityLocal)
        {
            // Fade amplitude smoothly — prevents the snap
            float targetAmplitude = (isGrounded && isMoving) ? 1f : 0f;
            _amplitude = Mathf.SmoothDamp(_amplitude, targetAmplitude, ref _amplitudeVelocity, 1f / fadeOutSpeed);

            // Store last bob params so fade-out keeps using them
            if (isGrounded && isMoving)
            {
                _lastSpeed = isSprinting ? sprintBobSpeed : isCrouching ? crouchBobSpeed : walkBobSpeed;
                _lastAmtY = isSprinting ? sprintBobAmountY : isCrouching ? crouchBobAmountY : walkBobAmountY;
                _lastAmtX = isSprinting ? sprintBobAmountX : isCrouching ? crouchBobAmountX : walkBobAmountX;
                _lastRoll = isSprinting ? sprintRollAmount : isCrouching ? crouchRollAmount : walkRollAmount;
            }

            // Sine wave keeps running during fade-out (no snap)
            if (_amplitude > 0.001f)
            {
                _bobTimer += Time.deltaTime * _lastSpeed;
            }
            else
            {
                _bobTimer = 0f;
                _amplitude = 0f;
            }

            float sin = Mathf.Sin(_bobTimer);

            // Amplitude multiplier makes everything fade smoothly
            Vector3 targetPos = new Vector3(
                sin * _lastAmtX * _amplitude,
                Mathf.Abs(sin) * _lastAmtY * _amplitude,
                0f);
            float targetRoll = sin * -_lastRoll * _amplitude;

            // Air drift
            if (isGrounded)
            {
                _airPitch = Mathf.SmoothDamp(_airPitch, 0f, ref _airPitchVelocity, 1f / airDriftSmoothing);
                _airRoll = Mathf.SmoothDamp(_airRoll, 0f, ref _airRollVelocity, 1f / airDriftSmoothing);
            }
            else
            {
                float forwardVel = velocityLocal.z / velocityScale;
                float rightVel = velocityLocal.x / velocityScale;
                _airPitch = Mathf.SmoothDamp(_airPitch, -forwardVel * airPitchAmount, ref _airPitchVelocity, 1f / airDriftSmoothing);
                _airRoll = Mathf.SmoothDamp(_airRoll, -rightVel * airRollAmount, ref _airRollVelocity, 1f / airDriftSmoothing);
            }

            // Apply position
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition, targetPos,
                ref _currentPosVelocity, 1f / bobSmoothing);

            // Apply rotation (roll + air drift)
            float currentRoll = transform.localEulerAngles.z;
            if (currentRoll > 180f) currentRoll -= 360f;
            float smoothedRoll = Mathf.SmoothDamp(currentRoll, targetRoll + _airRoll,
                ref _currentRollVelocity, 1f / bobSmoothing);

            transform.localEulerAngles = new Vector3(_airPitch, 0f, smoothedRoll);
        }

        public void Tick(bool isMoving, bool isSprinting, bool isCrouching)
            => Tick(isMoving, isSprinting, isCrouching, true, Vector3.zero);
    }
}