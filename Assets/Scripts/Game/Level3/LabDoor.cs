using Mirror;
using UnityEngine;

namespace ScaryGame.Game
{
    /// <summary>
    /// A lab room door opened by the matching KeycardItem.
    /// Set labIndex to 1, 2, or 3 to match the corresponding keycard.
    /// </summary>
    public class LabDoor : NetworkBehaviour
    {
        [Tooltip("1, 2, or 3. Must match the KeycardItem that opens this door.")]
        public int labIndex;

        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string openTrigger = "Open";

        [SyncVar] private bool _isOpen;
        public bool IsOpen => _isOpen;

        [Server]
        public void ServerOpen()
        {
            if (_isOpen) return;
            _isOpen = true;
            RpcOpen();
        }

        [ClientRpc]
        private void RpcOpen()
        {
            if (doorAnimator != null)
                doorAnimator.SetTrigger(openTrigger);
        }
    }
}
