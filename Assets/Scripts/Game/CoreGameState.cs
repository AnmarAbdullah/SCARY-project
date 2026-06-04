using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace ScaryGame.Game
{
    /// <summary>
    /// Server-side container tracking which cores are in play (picked up but
    /// not yet extracted) vs extracted. Ghost queries this to decide whether
    /// to return to base.
    ///
    /// The actual extraction trigger (player walks core into the Base Station)
    /// is wired in a future task; this class only exposes the state container.
    /// </summary>
    public class CoreGameState : NetworkBehaviour
    {
        public static CoreGameState Instance { get; private set; }

        [SyncVar] private int _coresExtracted;
        public int CoresExtracted => _coresExtracted;

        private readonly HashSet<NetworkIdentity> _coresInPlay = new HashSet<NetworkIdentity>();

        public int CoresInPlayCount => _coresInPlay.Count;
        public bool AnyCoreInPlay => _coresInPlay.Count > 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [Server]
        public void NotifyCorePickedUp(NetworkIdentity core)
        {
            if (core == null) return;
            _coresInPlay.Add(core);
        }

        [Server]
        public void NotifyCoreExtracted(NetworkIdentity core)
        {
            if (core == null) return;
            if (_coresInPlay.Remove(core))
                _coresExtracted++;
        }
    }
}
