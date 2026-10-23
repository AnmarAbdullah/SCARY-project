using System;
using Mirror;
using TimeFracture.Player;
using UnityEngine;

namespace ScaryGame.Game
{
    public enum ShapeType { Triangle = 0, Circle = 1, Diamond = 2, Square = 3 }

    /// <summary>
    /// Passive trigger zone — detects when a player stands on / leaves it.
    /// Needs a Trigger Collider on this GameObject.
    /// Lab1SimonSaysPuzzle subscribes to OnOccupancyChanged.
    /// </summary>
    public class PressurePlate : NetworkBehaviour
    {
        [Tooltip("The shape assigned to this plate and its matching button.")]
        public ShapeType shapeType;

        [SyncVar] private bool _isOccupied;
        [SyncVar] private uint _occupantNetId;

        public bool IsOccupied => _isOccupied;
        public uint OccupantNetId => _occupantNetId;

        /// <summary>Server-only. Fires when a player steps on or off.</summary>
        public event Action<PressurePlate, bool> OnOccupancyChanged;

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active || _isOccupied) return;

            NetworkIdentity netId = other.GetComponentInParent<NetworkIdentity>();
            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (netId == null || pc == null) return;

            _isOccupied = true;
            _occupantNetId = netId.netId;
            OnOccupancyChanged?.Invoke(this, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!NetworkServer.active || !_isOccupied) return;

            NetworkIdentity netId = other.GetComponentInParent<NetworkIdentity>();
            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (netId == null || pc == null) return;
            if (netId.netId != _occupantNetId) return;

            _isOccupied = false;
            _occupantNetId = 0;
            OnOccupancyChanged?.Invoke(this, false);
        }

    }
}
