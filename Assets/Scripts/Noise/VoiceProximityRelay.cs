using Dissonance;
using Mirror;
using UnityEngine;

namespace ScaryGame.Noise
{
    /// <summary>
    /// Sits on each player. While the player is transmitting voice, the server
    /// emits a periodic VoiceChat noise at the player's position. The Ghost
    /// hears this — proximity audio falloff for other players is a separate task.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(VoiceBroadcastTrigger))]
    public class VoiceProximityRelay : NetworkBehaviour
    {
        [SerializeField] private float emitInterval = 0.5f;
        [SerializeField] private float hearingRadius = 15f;

        private VoiceBroadcastTrigger _trigger;
        private float _nextEmitTime;

        private void Awake()
        {
            _trigger = GetComponent<VoiceBroadcastTrigger>();
        }

        [ServerCallback]
        private void Update()
        {
            if (_trigger == null) return;
            if (!_trigger.IsTransmitting) return;
            if (Time.time < _nextEmitTime) return;

            _nextEmitTime = Time.time + emitInterval;
            NoiseEventBus.Emit(new NoiseEvent(
                transform.position,
                NoiseType.VoiceChat,
                intensity: 1f,
                hearingRadius: hearingRadius,
                source: gameObject));
        }
    }
}
