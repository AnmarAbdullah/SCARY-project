using Dissonance;
using Mirror;
using UnityEngine;
using TimeFracture.Camera;
using TimeFracture.Input;

namespace TimeFracture.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Sub-system References")]
        public PlayerCameraLook cameraLook;
        public HeadBob headBob;
        public CameraIdleSway idleSway;

        private PlayerMovement _movement;
        private PlayerInputHandler _input;
        private UnityEngine.Camera[] _cameras;
        private AudioListener[] _audioListeners;
        private CameraFollow[] _cameraFollowers;

        private bool HasLocalControl => isLocalPlayer || (!NetworkClient.active && !NetworkServer.active);

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _input = GetComponent<PlayerInputHandler>();
            _cameras = GetComponentsInChildren<UnityEngine.Camera>(true);
            _audioListeners = GetComponentsInChildren<AudioListener>(true);
            _cameraFollowers = GetComponentsInChildren<CameraFollow>(true);

            if (cameraLook == null)
                cameraLook = GetComponent<PlayerCameraLook>();

            if (headBob == null)
                headBob = GetComponentInChildren<HeadBob>(true);

            if (idleSway == null)
                idleSway = GetComponentInChildren<CameraIdleSway>(true);

            if (cameraLook != null)
            {
                if (cameraLook.playerBody == null)
                    cameraLook.playerBody = transform;

                if (cameraLook.playerMovement == null)
                    cameraLook.playerMovement = _movement;
            }

            if (idleSway != null && idleSway.playerMovement == null)
                idleSway.playerMovement = _movement;
            
            GetComponent<VoiceBroadcastTrigger>().enabled = !isLocalPlayer;
        }
        

        private void Start()
        {
            if (!NetworkClient.active && !NetworkServer.active)
                SetLocalPlayerState(true);
        }

        public override void OnStartClient()
        {
            SetLocalPlayerState(false);
            GetComponent<VoiceBroadcastTrigger>().enabled = isLocalPlayer;
        }

        public override void OnStartLocalPlayer()
        {
            SetLocalPlayerState(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnStopLocalPlayer()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            //if (!HasLocalControl) return;
            // here
            // Freeze everything while reading a note
            if (cameraLook != null)
            {
                cameraLook.SetSensitivity(_input.mouseSensitivity);
                cameraLook.Look(_input.LookInput);
            }

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

        private void SetLocalPlayerState(bool active)
        {
            if (_input != null)
                _input.enabled = active;

            if (_movement != null)
                _movement.enabled = active;

            if (cameraLook != null)
                cameraLook.enabled = active;

            if (headBob != null)
                headBob.enabled = active;

            if (idleSway != null)
                idleSway.enabled = active;

            foreach (CameraFollow cameraFollower in _cameraFollowers)
                cameraFollower.enabled = active;

            foreach (UnityEngine.Camera playerCamera in _cameras)
                playerCamera.enabled = active;

            foreach (AudioListener audioListener in _audioListeners)
                audioListener.enabled = active;
        }
    }
}
