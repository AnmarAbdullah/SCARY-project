using Dissonance;
using Mirror;
using UnityEngine;
using ScaryGame.Players;
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

        [Header("Downed (stub — full revive system later)")]
        [SerializeField] private CanvasGroup downedOverlay;

        [SyncVar(hook = nameof(OnDownedChanged))]
        private bool _isDowned;
        public bool IsDowned => _isDowned;

        // Menu (false) vs Gameplay (true) — the SERVER flips this. Starts false so a
        // player spawned into the lobby stands still: no camera, no input, free cursor.
        [SyncVar(hook = nameof(OnControlEnabledChanged))]
        private bool _controlEnabled;

        private PlayerMovement _movement;
        private PlayerInputHandler _input;
        private UnityEngine.Camera[] _cameras;
        private AudioListener[] _audioListeners;
        private CameraFollow[] _cameraFollowers;

        public PlayerMovement Movement => _movement;

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

            // Approach A: persist across scene loads (lobby -> level), repositioned not respawned.
            DontDestroyOnLoad(gameObject);
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

        public override void OnStartServer()
        {
            PlayerRegistry.Register(this);
        }

        public override void OnStopServer()
        {
            PlayerRegistry.Unregister(this);
        }

        [Server]
        public void ServerDown()
        {
            if (_isDowned) return;
            _isDowned = true;
        }

        [Server]
        public void ServerRevive()
        {
            if (!_isDowned) return;
            _isDowned = false;
        }

        private void OnDownedChanged(bool oldVal, bool newVal)
        {
            if (newVal && isLocalPlayer)
            {
                if (_input != null) _input.enabled = false;
                if (_movement != null) _movement.enabled = false;
            }
            else if (!newVal && isLocalPlayer)
            {
                if (_input != null) _input.enabled = true;
                if (_movement != null) _movement.enabled = true;
            }

            if (downedOverlay != null && isLocalPlayer)
                downedOverlay.alpha = newVal ? 1f : 0f;
        }

        public override void OnStartLocalPlayer()
        {
            // Apply whatever mode the server has us in (Menu when first spawned into
            // the lobby, Gameplay once a level is loaded). Does NOT force control on.
            ApplyControlState(_controlEnabled);
        }

        public override void OnStopLocalPlayer()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ── Menu / Gameplay control (server-driven) ──────────────────────────

        /// <summary>SERVER switch: Menu mode (false) or Gameplay mode (true).</summary>
        [Server]
        public void ServerSetControl(bool active) => _controlEnabled = active;

        private void OnControlEnabledChanged(bool _, bool active)
        {
            if (isLocalPlayer)
                ApplyControlState(active);
        }

        private void ApplyControlState(bool active)
        {
            SetLocalPlayerState(active);
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }

        // Snap to a pose. Applied on server AND mirrored to clients, so it works
        // regardless of the NetworkTransform's authority direction.
        [Server]
        public void ServerTeleport(Vector3 position, Quaternion rotation)
        {
            ApplyTeleport(position, rotation);
            RpcTeleport(position, rotation);
        }

        [ClientRpc]
        private void RpcTeleport(Vector3 position, Quaternion rotation)
        {
            ApplyTeleport(position, rotation);
        }

        private void ApplyTeleport(Vector3 position, Quaternion rotation)
        {
            if (_movement != null)
                _movement.Teleport(position, rotation);
            else
                transform.SetPositionAndRotation(position, rotation);
        }

        private void LateUpdate()
        {
            if (!HasLocalControl) return;

            // In a networked session, only drive look/movement once the server grants
            // control (Gameplay mode). In the lobby (Menu mode) the player stands still.
            if ((NetworkClient.active || NetworkServer.active) && !_controlEnabled)
                return;

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
