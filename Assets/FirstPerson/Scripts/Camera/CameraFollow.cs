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
            ResolveReferences();

            _currentHeight = IsChildOfTarget()
                ? transform.localPosition.y
                : standHeight;

            if (target != null && !IsChildOfTarget())
                transform.position = target.position + Vector3.up * _currentHeight;
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (target == null) return;
            float targetHeight = (playerMovement != null && playerMovement.IsCrouching)
                               ? crouchHeight : standHeight;

            if (IsChildOfTarget())
            {
                if (playerMovement != null && playerMovement.cameraHolder == transform)
                    return;

                Vector3 localPos = transform.localPosition;
                localPos.x = Mathf.Lerp(localPos.x, 0f, followSpeed * Time.deltaTime);
                localPos.y = Mathf.Lerp(localPos.y, targetHeight, crouchSmoothing * Time.deltaTime);
                localPos.z = Mathf.Lerp(localPos.z, 0f, followSpeed * Time.deltaTime);
                transform.localPosition = localPos;
                return;
            }

            _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, crouchSmoothing * Time.deltaTime);
            Vector3 targetPos = target.position + Vector3.up * _currentHeight;
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        }

        private void ResolveReferences()
        {
            if (playerMovement == null)
                playerMovement = GetComponentInParent<PlayerMovement>();

            if (target == null && playerMovement != null)
                target = playerMovement.transform;

            if (playerMovement == null && target != null)
                playerMovement = target.GetComponent<PlayerMovement>();
        }

        private bool IsChildOfTarget()
        {
            return target != null && transform.IsChildOf(target);
        }
    }
}
