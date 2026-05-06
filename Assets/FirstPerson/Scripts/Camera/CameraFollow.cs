using UnityEngine;
using TimeFracture.Player;

namespace TimeFracture.Camera
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("References")]
        public Transform      target;
        public PlayerMovement playerMovement;

        [Header("Height")]
        public float standHeight  = 1.6f;
        public float crouchHeight = 0.75f;

        [Header("Smoothing")]
        [Range(1f, 20f)]   public float crouchSmoothing = 8f;
        [Range(10f, 100f)] public float followSpeed     = 30f;

        private float _currentHeight;

        private void Awake()
        {
            _currentHeight = standHeight;
            if (target != null)
                transform.position = target.position + Vector3.up * _currentHeight;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float targetHeight = (playerMovement != null && playerMovement.IsCrouching)
                               ? crouchHeight : standHeight;
            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, crouchSmoothing * Time.deltaTime);
            Vector3 targetPos = target.position + Vector3.up * _currentHeight;
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        }
    }
}
