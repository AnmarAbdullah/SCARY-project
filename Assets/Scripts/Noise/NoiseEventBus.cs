using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScaryGame.Noise
{
    /// <summary>
    /// Single server-side bus where every noise emitter publishes. The Ghost's
    /// perception is the consumer. Server-only — Emit is a no-op on clients.
    /// </summary>
    public static class NoiseEventBus
    {
        public static event Action<NoiseEvent> OnNoise;

        private static bool _sceneHookInstalled;

        public static void Emit(NoiseEvent e)
        {
            if (!NetworkServer.active) return;
            EnsureSceneHook();
            OnNoise?.Invoke(e);
        }

        private static void EnsureSceneHook()
        {
            if (_sceneHookInstalled) return;
            _sceneHookInstalled = true;
            SceneManager.sceneUnloaded += _ => OnNoise = null;
        }
    }
}
