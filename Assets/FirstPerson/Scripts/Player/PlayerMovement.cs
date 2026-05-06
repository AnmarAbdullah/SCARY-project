using UnityEngine;
using TimeFracture.Interfaces;
using TimeFracture.Audio;

namespace TimeFracture.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerMovement : MonoBehaviour, IMovable
    {
        [Header("Speed Settings")]
        public float walkSpeed   = 5f;
        public float sprintSpeed = 9f;
        public float crouchSpeed = 2.5f;

        [Header("Jump")]
        public float jumpForce = 6f;

        [Header("Ground Check")]
        public LayerMask groundMask = ~0;
        public float groundCheckRadius = 0.3f;
        public float groundCheckOffset = 0f;

        [Header("Crouch")]
        public float standHeight  = 1.8f;
        public float crouchHeight = 1.0f;
        public float crouchTransitionSpeed = 10f;

        [Header("Camera Crouch")]
        public Transform cameraHolder;
        public float standCameraY  = 1.6f;
        public float crouchCameraY = 0.8f;
        public float cameraCrouchSmoothing = 8f;

        [Header("Physics Feel")]
        public float gravityMultiplier = 2.5f;

        [Header("Abilities")]
        public bool canWalk   = true;
        public bool canSprint = true;
        public bool canJump   = true;
        public bool canCrouch = true;

        // ── Public state ──────────────────────────────────────────────────
        public bool    IsGrounded  { get; private set; }
        public bool    IsSprinting { get; private set; }
        public bool    IsCrouching { get; private set; }
        public bool    IsMoving    { get; private set; }
        public Vector3 Velocity    => _rb != null ? _rb.velocity : Vector3.zero;

        // ── Private ───────────────────────────────────────────────────────
        private Rigidbody            _rb;
        private CapsuleCollider      _col;
        private Vector3              _moveDir;
        private float                _targetCapsuleHeight;
        private PlayerAudioController _audio;

        // Jump flag — set from Update, consumed in FixedUpdate
        // Stays true across multiple FixedUpdate steps until it fires
        private bool  _jumpQueued;
        private float _jumpQueueTime;
        private float _lastJumpTime;
        private const float JUMP_BUFFER    = 0.15f;
        private const float JUMP_COOLDOWN  = 0.3f;

        private void Awake()
        {
            _rb  = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
            _rb.freezeRotation   = true;
            _rb.interpolation    = RigidbodyInterpolation.Interpolate;
            _targetCapsuleHeight = standHeight;
            _audio               = GetComponent<PlayerAudioController>();
        }

        private void FixedUpdate()
        {
            CheckGround();
            ApplyExtraGravity();
            ApplyMovement();
            UpdateCapsuleCrouch();
            TryJump();
        }

        private void Update()
        {
            UpdateCameraHeight();
        }

        // ── IMovable ──────────────────────────────────────────────────────

        public void Move(Vector2 input, bool isSprinting)
        {
            if (!canWalk) { _moveDir = Vector3.zero; IsMoving = false; IsSprinting = false; return; }
            IsSprinting = canSprint && isSprinting && !IsCrouching && input.magnitude > 0.1f;
            IsMoving    = input.magnitude > 0.05f;
            float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            _moveDir    = (transform.forward * input.y + transform.right * input.x).normalized * speed;
        }

        public void Jump()
        {
            if (!canJump) return;
            _jumpQueued    = true;
            _jumpQueueTime = Time.time;
        }

        public void Crouch(bool isCrouching)
        {
            if (isCrouching && !canCrouch) return;
            IsCrouching          = isCrouching;
            _targetCapsuleHeight = isCrouching ? crouchHeight : standHeight;
        }

        // ── Private helpers ───────────────────────────────────────────────

        private void TryJump()
        {
            if (!_jumpQueued) return;
            _jumpQueued = false; // always consume immediately

            if (IsGrounded && !IsCrouching)
            {
                _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                _audio?.PlayJump();
            }
        }

        private void ApplyMovement()
        {
            Vector3 vel = _rb.velocity;
            vel.x = _moveDir.x;
            vel.z = _moveDir.z;
            _rb.velocity = vel;
        }

        private void CheckGround()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            IsGrounded = Physics.SphereCast(origin, 0.3f, Vector3.down, out _,
                                            0.4f + groundCheckOffset,
                                            ~(1 << gameObject.layer),
                                            QueryTriggerInteraction.Ignore);
        }

        private void ApplyExtraGravity()
        {
            if (!IsGrounded)
                _rb.AddForce(Vector3.down * (9.81f * (gravityMultiplier - 1f)),
                             ForceMode.Acceleration);
        }

        private void UpdateCapsuleCrouch()
        {
            _col.height = Mathf.Lerp(_col.height, _targetCapsuleHeight,
                                     Time.fixedDeltaTime * crouchTransitionSpeed);
            _col.center = new Vector3(0f, _col.height * 0.5f, 0f);
        }

        private void UpdateCameraHeight()
        {
            if (cameraHolder == null) return;
            float   targetY = IsCrouching ? crouchCameraY : standCameraY;
            Vector3 pos     = cameraHolder.localPosition;
            pos.y           = Mathf.Lerp(pos.y, targetY, Time.deltaTime * cameraCrouchSmoothing);
            cameraHolder.localPosition = pos;
        }

        private bool CeilingBlocked()
        {
            int mask = groundMask & ~(1 << gameObject.layer);
            Vector3 top = transform.position + Vector3.up * (standHeight - groundCheckRadius);
            return Physics.CheckSphere(top, groundCheckRadius, mask,
                                       QueryTriggerInteraction.Ignore);
        }

        // ── Gizmos ────────────────────────────────────────────────────────

        [Header("Gizmo Appearance")]
        public Color gizmoFillColor    = new Color(0.2f, 0.8f, 1f, 0.15f);
        public Color gizmoOutlineColor = new Color(0.2f, 0.8f, 1f, 0.6f);

        private void OnDrawGizmos()
        {
            float height = _col != null ? _col.height : standHeight;
            float radius = _col != null ? _col.radius : 0.4f;
            DrawFilledCapsule(transform.position, height, radius);
            DrawWireCapsule(transform.position, height, radius);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + Vector3.down * (0.25f + groundCheckOffset));
            Gizmos.DrawWireSphere(origin + Vector3.down * (0.25f + groundCheckOffset), 0.05f);
        }

        private void DrawFilledCapsule(Vector3 basePos, float height, float radius)
        {
            Gizmos.color   = gizmoFillColor;
            Vector3 center = basePos + Vector3.up * (height * 0.5f);
            float   bodyH  = Mathf.Max(0f, height - radius * 2f);
            Gizmos.DrawSphere(center + Vector3.up   * (bodyH * 0.5f), radius);
            Gizmos.DrawSphere(center + Vector3.down * (bodyH * 0.5f), radius);
            int   steps = Mathf.Max(2, Mathf.RoundToInt(bodyH / (radius * 0.5f)));
            float stepH = bodyH / steps;
            for (int i = 0; i <= steps; i++)
                Gizmos.DrawSphere(center + Vector3.up * (-bodyH * 0.5f + stepH * i), radius);
        }

        private void DrawWireCapsule(Vector3 basePos, float height, float radius)
        {
            Gizmos.color   = gizmoOutlineColor;
            Vector3 center = basePos + Vector3.up * (height * 0.5f);
            float   bodyH  = Mathf.Max(0f, height - radius * 2f);
            Vector3 top    = center + Vector3.up   * (bodyH * 0.5f);
            Vector3 bottom = center + Vector3.down * (bodyH * 0.5f);
            Gizmos.DrawWireSphere(top,    radius);
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
            Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);
            Gizmos.DrawLine(top + Vector3.right   * radius, bottom + Vector3.right   * radius);
            Gizmos.DrawLine(top - Vector3.right   * radius, bottom - Vector3.right   * radius);
        }
    }
}