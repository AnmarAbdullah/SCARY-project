using UnityEngine;
using TimeFracture.Camera;
using TimeFracture.Input;

namespace TimeFracture.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Sub-system References")]
        public PlayerCameraLook cameraLook;
        public HeadBob headBob;
        public CameraIdleSway idleSway;

        private PlayerMovement _movement;
        private PlayerInputHandler _input;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _input = GetComponent<PlayerInputHandler>();
        }

        private void LateUpdate()
        {
            // Freeze everything while reading a note
            cameraLook.SetSensitivity(_input.mouseSensitivity);
            cameraLook.Look(_input.LookInput);

            _movement.Move(_input.MoveInput, _input.SprintHeld);

            if (_input.JumpPressed)
                _movement.Jump();

            _movement.Crouch(_input.CrouchHeld);

            // Head bob
            if (headBob != null)
            {
                Vector3 localVel = transform.InverseTransformDirection(_movement.Velocity);
                headBob.Tick(
                    _movement.IsMoving,
                    _movement.IsSprinting,
                    _movement.IsCrouching,
                    _movement.IsGrounded,
                    localVel);
            }

            if (idleSway != null)
                idleSway.enabled = !_movement.IsMoving;
        }
    }
}