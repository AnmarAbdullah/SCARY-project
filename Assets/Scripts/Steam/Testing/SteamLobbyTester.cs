// TEMPORARY TEST SCRIPT. Delete once a real main menu wires buttons to SteamLobby.Instance.
// Binds keyboard keys so you can host/invite/leave without UI.

using UnityEngine;

namespace ScaryGame.Steam.Testing
{
    public class SteamLobbyTester : MonoBehaviour
    {
        [SerializeField] KeyCode hostKey   = KeyCode.H;
        [SerializeField] KeyCode inviteKey = KeyCode.I;
        [SerializeField] KeyCode leaveKey  = KeyCode.L;

        [Header("Invite target")]
        [Tooltip("Cousin's 64-bit SteamID (the long number in their Steam profile URL, e.g. 76561198xxxxxxxxx). " +
                 "Find it: open friend's Steam profile in browser, look at the URL — if it's not numeric, go to steamid.io and paste the URL.")]
        [SerializeField] string friendSteamId64 = "";

        void Update()
        {
            if (SteamLobby.Instance == null) return;

            if (Input.GetKeyDown(hostKey))   SteamLobby.Instance.HostLobby();
            if (Input.GetKeyDown(leaveKey))  SteamLobby.Instance.LeaveLobby();
            if (Input.GetKeyDown(inviteKey)) TryInvite();
        }

        void TryInvite()
        {
            if (string.IsNullOrWhiteSpace(friendSteamId64))
            {
                Debug.LogWarning("[SteamLobbyTester] friendSteamId64 is empty. Set it in the inspector.", this);
                return;
            }
            if (!ulong.TryParse(friendSteamId64.Trim(), out ulong id))
            {
                Debug.LogWarning($"[SteamLobbyTester] '{friendSteamId64}' is not a valid 64-bit SteamID. Should be a 17-digit number like 76561198xxxxxxxxx.", this);
                return;
            }
            SteamLobby.Instance.InviteFriend(id);
        }
    }
}
