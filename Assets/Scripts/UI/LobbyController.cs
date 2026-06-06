// Assets/Scripts/UI/LobbyController.cs
//
// Drives the *lobby* portion of the main menu, today's scope only:
//   • Host a Steam lobby (Create Lobby button).
//   • Invite a friend via the Steam overlay friend picker (Invite button).
//   • Show the joined players' Steam names in the lobby panel.
//   • Host-only Start Game button → broadcasts a StartGameMessage so EVERY
//     machine (host + clients) prints a Debug.LogError that the host started.
//
// All UI references are exposed as inspector variables — wire them in the
// Inspector, no hard-coded paths. This script only handles the lobby; the rest
// of the flow (level loading, spawning, pause, etc.) is designed in
// "Lobby, level loading design.md" and is intentionally NOT implemented yet.

using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ScaryGame.Steam;
using ScaryGame.Networking;

namespace ScaryGame.UI
{
    public class LobbyController : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("The main-menu panel (Create Lobby / Settings / Quit). Hidden while in a lobby.")]
        [SerializeField] GameObject mainMenuPanel;
        [Tooltip("The lobby panel (player list, Invite, Start). Hidden until a lobby is hosted/joined.")]
        [SerializeField] GameObject lobbyPanel;

        [Header("Buttons")]
        [Tooltip("Main-menu button that hosts a new lobby.")]
        [SerializeField] Button createLobbyButton;
        [Tooltip("Lobby button that opens the Steam overlay friend picker to invite someone.")]
        [SerializeField] Button inviteButton;
        [Tooltip("Lobby button shown to the HOST only. Broadcasts the start signal.")]
        [SerializeField] Button startGameButton;
        [Tooltip("Optional. Lobby button that leaves the lobby and returns to the main menu.")]
        [SerializeField] Button leaveLobbyButton;

        [Header("Player list")]
        [Tooltip("One text slot per seat (4 for the 4-player design). Filled with Steam names; unused slots show the empty label.")]
        [SerializeField] TextMeshProUGUI[] playerNameSlots;
        [Tooltip("Text shown in an unoccupied player slot.")]
        [SerializeField] string emptySlotLabel = "—";

        [Header("Input blocker")]
        [Tooltip("A full-screen invisible Image (Raycast Target ON) on top of everything. " +
                 "Shown while a lobby host/join is in flight so the player can't spam buttons; " +
                 "auto-hidden on success, failure, cancel, or timeout.")]
        [SerializeField] GameObject inputBlocker;

        [Header("Optional status / gating")]
        [Tooltip("Optional. Shows player count / error messages.")]
        [SerializeField] TextMeshProUGUI statusText;
        [Tooltip("Host's Start button is only enabled at or above this many players. Set to 1 to test solo.")]
        [SerializeField] int minPlayersToStart = 2;

        void Awake()
        {
            // Wire button clicks in code so the only manual setup is dragging references.
            if (createLobbyButton) createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
            if (inviteButton)      inviteButton.onClick.AddListener(OnInviteClicked);
            if (startGameButton)   startGameButton.onClick.AddListener(OnStartGameClicked);
            if (leaveLobbyButton)  leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
        }

        void Start()
        {
            // Listen for the start signal on every machine (host's local client included).
            // ReplaceHandler avoids a duplicate-registration warning if the scene reloads.
            NetworkClient.ReplaceHandler<StartGameMessage>(OnStartGameReceived, false);

            if (SteamLobby.Instance != null)
            {
                SteamLobby.Instance.OnLobbyHosted        += HandleLobbyHosted;
                SteamLobby.Instance.OnLobbyJoined        += HandleLobbyJoined;
                SteamLobby.Instance.OnLobbyLeft          += HandleLobbyLeft;
                SteamLobby.Instance.OnLobbyMembersChanged += RefreshPlayerList;
                SteamLobby.Instance.OnLobbyError         += HandleLobbyError;
                SteamLobby.Instance.OnBusyChanged        += HandleBusyChanged;
            }
            else
            {
                Debug.LogError("[LobbyController] No SteamLobby.Instance found. Add a SteamLobby (and SteamManager + NetworkManager) to the scene.", this);
            }

            if (inputBlocker) inputBlocker.SetActive(false);
            ShowMainMenu();
        }

        void OnDestroy()
        {
            if (SteamLobby.Instance != null)
            {
                SteamLobby.Instance.OnLobbyHosted        -= HandleLobbyHosted;
                SteamLobby.Instance.OnLobbyJoined        -= HandleLobbyJoined;
                SteamLobby.Instance.OnLobbyLeft          -= HandleLobbyLeft;
                SteamLobby.Instance.OnLobbyMembersChanged -= RefreshPlayerList;
                SteamLobby.Instance.OnLobbyError         -= HandleLobbyError;
                SteamLobby.Instance.OnBusyChanged        -= HandleBusyChanged;
            }
        }

        // ── Button callbacks ──────────────────────────────────────────────────

        void OnCreateLobbyClicked()
        {
            if (SteamLobby.Instance == null) return;
            SetStatus("Creating lobby…");
            SteamLobby.Instance.HostLobby();
        }

        void OnInviteClicked()
        {
            SteamLobby.Instance?.OpenInviteOverlay();
        }

        void OnStartGameClicked()
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[LobbyController] Only the host can start the game.", this);
                return;
            }
            // Server → all clients (host's local client included). Each receiver logs the error.
            NetworkServer.SendToAll(new StartGameMessage());
        }

        void OnLeaveLobbyClicked()
        {
            SteamLobby.Instance?.LeaveLobby();
        }

        // ── SteamLobby event handlers ────────────────────────────────────────

        void HandleLobbyHosted() => ShowLobby(isHost: true);
        void HandleLobbyJoined() => ShowLobby(isHost: false);
        void HandleLobbyLeft()   => ShowMainMenu();
        void HandleLobbyError(string msg) => SetStatus(msg);

        // Show/hide the full-screen input-blocker while a host/join is in flight.
        void HandleBusyChanged(bool busy)
        {
            if (inputBlocker) inputBlocker.SetActive(busy);
        }

        // ── Network message handler (runs on every client) ───────────────────

        void OnStartGameReceived(StartGameMessage msg)
        {
            Debug.LogError("[Lobby] The host has clicked START GAME!");
        }

        // ── View state ───────────────────────────────────────────────────────

        void ShowMainMenu()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
            if (lobbyPanel)    lobbyPanel.SetActive(false);
            if (inputBlocker)  inputBlocker.SetActive(false);
        }

        void ShowLobby(bool isHost)
        {
            // Mirror clears client message handlers on Shutdown (leaving a lobby), so
            // re-register here — this runs right after StartHost/StartClient connects.
            NetworkClient.ReplaceHandler<StartGameMessage>(OnStartGameReceived, false);

            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (lobbyPanel)    lobbyPanel.SetActive(true);

            // Start is host-only; clients don't even see it.
            if (startGameButton) startGameButton.gameObject.SetActive(isHost);

            RefreshPlayerList();
        }

        void RefreshPlayerList()
        {
            if (SteamLobby.Instance == null) return;

            var members = SteamLobby.Instance.GetMembers();

            if (playerNameSlots != null)
            {
                for (int i = 0; i < playerNameSlots.Length; i++)
                {
                    if (playerNameSlots[i] == null) continue;
                    if (i < members.Count)
                        playerNameSlots[i].text = members[i].isHost ? $"{members[i].name}  (Host)" : members[i].name;
                    else
                        playerNameSlots[i].text = emptySlotLabel;
                }
            }

            int count = members.Count;
            int max   = playerNameSlots != null ? playerNameSlots.Length : count;
            SetStatus($"{count}/{max} players");

            // Host can only start once enough players are in.
            if (startGameButton && NetworkServer.active)
                startGameButton.interactable = count >= minPlayersToStart;
        }

        void SetStatus(string msg)
        {
            if (statusText) statusText.text = msg;
        }
    }
}
