using Mirror;
using UnityEngine;
using ScaryGame.Noise;

namespace ScaryGame.Items
{
    /// <summary>
    /// Distractor device stub. Pressing the debug key (G) on the local player
    /// activates the nearest one and emits a single loud NoiseDevice event.
    /// Real inventory wiring is a later task.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class NoiseDevice : NetworkBehaviour
    {
        [SerializeField] private float hearingRadius = 30f;
        [SerializeField] private float cooldownSeconds = 5f;
        [SerializeField] private AudioClip activationSfx;

        [SyncVar] private double _nextEligibleNetTime;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
        }

        [Command(requiresAuthority = false)]
        public void CmdActivate(NetworkConnectionToClient sender = null)
        {
            if (NetworkTime.time < _nextEligibleNetTime) return;
            _nextEligibleNetTime = NetworkTime.time + cooldownSeconds;

            NoiseEventBus.Emit(new NoiseEvent(
                position: transform.position,
                type: NoiseType.NoiseDevice,
                intensity: 1f,
                hearingRadius: hearingRadius,
                source: gameObject));

            RpcPlayActivationSfx();
        }

        [ClientRpc]
        private void RpcPlayActivationSfx()
        {
            if (_source != null && activationSfx != null)
                _source.PlayOneShot(activationSfx);
        }

#if UNITY_EDITOR
        // Debug-only: pressing G on the local client triggers the nearest device.
        private void Update()
        {
            if (!isClient) return;
            if (!Input.GetKeyDown(KeyCode.G)) return;
            if (NetworkClient.localPlayer == null) return;
            if (Vector3.Distance(NetworkClient.localPlayer.transform.position, transform.position) > 3f) return;

            CmdActivate();
        }
#endif
    }
}
