using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace ScaryGame.Steam
{
    /// One entry in the lobby member list, surfaced to UI.
    public struct LobbyMember
    {
        public ulong steamId;
        public string name;
        public bool isHost;
    }

    /// Hosts/joins Steam lobbies and bridges them to Mirror's NetworkManager via FizzySteamworks.
    /// Singleton, DontDestroyOnLoad. UI binds to the public events; callers invoke HostLobby/LeaveLobby.
    [DisallowMultipleComponent]
    public class SteamLobby : MonoBehaviour
    {
        public static SteamLobby Instance { get; private set; }

        const string HostAddressKey = "HostAddress";

        [SerializeField, Tooltip("Max players in a lobby. Matches the 4-player game design.")]
        int maxPlayers = 4;

        [SerializeField, Tooltip("Lobby visibility. FriendsOnly is correct for Steam-invite flow.")]
        ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;

        [SerializeField, Tooltip("Seconds before a host/join attempt is force-aborted if Steam or Mirror never responds. " +
                                 "Backup so the UI input-blocker can never get stuck on forever.")]
        float operationTimeout = 15f;

#if !DISABLESTEAMWORKS
        Callback<LobbyCreated_t> _lobbyCreated;
        Callback<GameLobbyJoinRequested_t> _joinRequested;
        Callback<LobbyEnter_t> _lobbyEntered;
        Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
#endif

        public CSteamID CurrentLobbyID { get; private set; }
        public bool InLobby => CurrentLobbyID.IsValid() && CurrentLobbyID.m_SteamID != 0;

        /// True only on the machine that created the lobby (Mirror host).
        public bool IsHost => NetworkServer.active;

        public event Action OnLobbyHosted;
        public event Action OnLobbyJoined;
        public event Action OnLobbyLeft;
        public event Action<string> OnLobbyError;

        /// Fires whenever the lobby roster changes (someone joins/leaves) or we just entered a lobby.
        /// UI subscribes to this to re-render the player list.
        public event Action OnLobbyMembersChanged;

        /// Identifies which async operation is in flight (for timeout / error messages).
        public enum BusyReason { None, Hosting, Joining }

        /// True while a host/join is in flight. UI shows an input-blocker while busy.
        public bool IsBusy { get; private set; }
        BusyReason _busyReason = BusyReason.None;
        Coroutine _timeoutCo;

        /// Fires (true) when an async host/join starts and (false) when it finishes,
        /// fails, is canceled, or times out. UI toggles a full-screen input-blocker on this.
        public event Action<bool> OnBusyChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized)
            {
                Debug.LogError("[SteamLobby] SteamManager not initialized. Add SteamManager to the scene and ensure Steam client is running.", this);
                return;
            }

            _lobbyCreated    = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _joinRequested   = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            _lobbyEntered    = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
#endif
        }

        void OnDestroy()
        {
#if !DISABLESTEAMWORKS
            _lobbyCreated?.Dispose();
            _joinRequested?.Dispose();
            _lobbyEntered?.Dispose();
            _lobbyChatUpdate?.Dispose();
#endif
            NetworkClient.OnConnectedEvent    -= HandleClientConnected;
            NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
            if (Instance == this) Instance = null;
        }

        // Called by host UI. Creates a Steam lobby; Mirror host starts in the callback.
        public void HostLobby()
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized) { Report("Steam not initialized"); return; }
            if (IsBusy) { Report("Busy — another lobby operation is in progress."); return; }
            if (InLobby) { Report("Already in a lobby"); return; }

            BeginBusy(BusyReason.Hosting);
            SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
#endif
        }

        // Sends a Steam chat invite to a specific friend's SteamID (64-bit, the long number in their profile URL).
        // Friend gets a clickable Steam chat message; clicking it fires GameLobbyJoinRequested_t in their game.
        // This is the canonical invite path that works WITHOUT the Steam overlay being functional.
        public void InviteFriend(ulong friendSteamId64)
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized) { Report("Steam not initialized"); return; }
            if (!InLobby) { Report("Cannot invite — not in a lobby. Host first (press H)."); return; }
            if (friendSteamId64 == 0) { Report("Invalid friend SteamID (0). Set it in the SteamLobbyTester inspector."); return; }

            var friend = new CSteamID(friendSteamId64);
            bool ok = SteamMatchmaking.InviteUserToLobby(CurrentLobbyID, friend);
            if (ok) Debug.Log($"[SteamLobby] Invite sent to {friendSteamId64}", this);
            else    Report($"InviteUserToLobby returned false for {friendSteamId64}");
#endif
        }

        // Opens the Steam overlay's "invite a friend" picker scoped to the current lobby.
        // The player picks a friend from their list; Steam sends the lobby invite automatically.
        // Requires the Steam overlay to be enabled (Steam Settings → In-Game → Enable overlay).
        public void OpenInviteOverlay()
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized) { Report("Steam not initialized"); return; }
            if (!InLobby) { Report("Cannot invite — not in a lobby. Host first."); return; }

            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyID);
            Debug.Log("[SteamLobby] Opened Steam invite overlay for lobby " + CurrentLobbyID.m_SteamID, this);
#endif
        }

        // Returns the current lobby roster (steamId, persona name, host flag) for the UI player list.
        public List<LobbyMember> GetMembers()
        {
            var members = new List<LobbyMember>();
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized || !InLobby) return members;

            CSteamID owner = SteamMatchmaking.GetLobbyOwner(CurrentLobbyID);
            int count = SteamMatchmaking.GetNumLobbyMembers(CurrentLobbyID);
            for (int i = 0; i < count; i++)
            {
                CSteamID id = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobbyID, i);
                members.Add(new LobbyMember
                {
                    steamId = id.m_SteamID,
                    name    = SteamFriends.GetFriendPersonaName(id),
                    isHost  = id == owner
                });
            }
#endif
            return members;
        }

        public int GetMemberCount()
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized || !InLobby) return 0;
            return SteamMatchmaking.GetNumLobbyMembers(CurrentLobbyID);
#else
            return 0;
#endif
        }

        // Joins a friend's lobby. If we're already hosting/in a lobby, that one is fully
        // torn down first (we leave it as if we'd quit) so we never sit in two lobbies —
        // which is what gave a joining host the Start button before.
        public void JoinLobby(ulong lobbyId)
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized) { Report("Steam not initialized"); return; }
            if (lobbyId == 0) { Report("Invalid lobby id"); return; }
            if (IsBusy) { Report("Busy — finish the current lobby operation first."); return; }

            // Tear down our own lobby/session silently (no return-to-menu UI flicker)
            // before joining the new one.
            if (InLobby || NetworkServer.active || NetworkClient.active)
                Teardown(fireLeftEvent: false);

            BeginBusy(BusyReason.Joining);
            SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
#endif
        }

        // Leaves the Steam lobby and tears down Mirror (host or client). Public entry for the
        // Leave button / pause-quit. Host leaving stops the server → every client disconnects
        // and is returned to the menu by their own disconnect handler (no host migration).
        public void LeaveLobby()
        {
            Teardown(fireLeftEvent: true);
        }

        // Single idempotent teardown path used by LeaveLobby, JoinLobby, disconnect, and timeout.
        // Clears the Steam lobby FIRST (so InLobby == false), which is how the disconnect handler
        // tells an intentional leave from an unexpected host-drop.
        void Teardown(bool fireLeftEvent)
        {
            ClearTimeout();
#if !DISABLESTEAMWORKS
            if (InLobby)
            {
                SteamMatchmaking.LeaveLobby(CurrentLobbyID);
                CurrentLobbyID = CSteamID.Nil;
            }
#endif
            StopMirror();
            SetBusy(false);
            if (fireLeftEvent) OnLobbyLeft?.Invoke();
        }

        void StopMirror()
        {
            var nm = NetworkManager.singleton;
            if (nm == null) return;
            if (NetworkServer.active && NetworkClient.isConnected) nm.StopHost();
            else if (NetworkClient.active) nm.StopClient();
            else if (NetworkServer.active) nm.StopServer();
        }

#if !DISABLESTEAMWORKS
        void OnLobbyCreated(LobbyCreated_t cb)
        {
            if (cb.m_eResult != EResult.k_EResultOK)
            {
                SetBusy(false);
                Report($"Lobby create failed: {cb.m_eResult}");
                return;
            }

            CurrentLobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            // Store host's SteamID in lobby data so joiners know who to connect Mirror to.
            SteamMatchmaking.SetLobbyData(CurrentLobbyID, HostAddressKey, SteamUser.GetSteamID().ToString());

            Debug.Log($"[SteamLobby] Lobby created. ID={CurrentLobbyID.m_SteamID} HostSteamID={SteamUser.GetSteamID()}", this);

            var nm = NetworkManager.singleton;
            if (nm == null) { SetBusy(false); Report("No NetworkManager in scene"); return; }

            nm.StartHost();
            // Re-hook AFTER Start, not before. StartHost/StartClient call Mirror's
            // RegisterClientMessages(), which *assigns* (=) NetworkClient.OnConnectedEvent /
            // OnDisconnectedEvent to Mirror's own internal handlers — clobbering anything
            // subscribed earlier. Hooking here is what keeps our handlers alive.
            RehookClientEvents();

            // Host is ready immediately (its own local client). End the busy state.
            SetBusy(false);
            OnLobbyHosted?.Invoke();
            OnLobbyMembersChanged?.Invoke();
        }

        // Fires when a friend clicks "Join Game" or accepts an invite from the Steam overlay.
        // Route through JoinLobby so we leave any current lobby first.
        void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t cb)
        {
            JoinLobby(cb.m_steamIDLobby.m_SteamID);
        }

        // Fires for everyone who enters the lobby — both host and joiners.
        void OnLobbyEntered(LobbyEnter_t cb)
        {
            // Joining can fail (lobby full, doesn't exist, etc.).
            if (cb.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                SetBusy(false);
                Report($"Could not enter lobby (response {cb.m_EChatRoomEnterResponse}).");
                OnLobbyLeft?.Invoke();
                return;
            }

            CurrentLobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            // Refresh the roster for both host and joiners (host auto-enters its own lobby).
            OnLobbyMembersChanged?.Invoke();

            // Host already called StartHost in OnLobbyCreated. Only non-host clients connect here.
            if (NetworkServer.active) return;

            string hostAddress = SteamMatchmaking.GetLobbyData(CurrentLobbyID, HostAddressKey);
            if (string.IsNullOrEmpty(hostAddress))
            {
                Report("Lobby missing host address");
                Teardown(fireLeftEvent: true);
                return;
            }

            var nm = NetworkManager.singleton;
            if (nm == null) { Report("No NetworkManager in scene"); Teardown(fireLeftEvent: true); return; }

            Debug.Log($"[SteamLobby] Lobby entered. ID={CurrentLobbyID.m_SteamID} HostSteamID={hostAddress}", this);

            nm.networkAddress = hostAddress;   // FizzySteamworks reads this as host's SteamID
            nm.StartClient();
            // Re-hook AFTER StartClient, not before. StartClient calls Mirror's
            // RegisterClientMessages(), which *assigns* (=) NetworkClient.OnConnectedEvent /
            // OnDisconnectedEvent to Mirror's own internal handlers, wiping out anything
            // subscribed before it. The transport connects asynchronously, so OnConnectedEvent
            // cannot have fired during this synchronous call — hooking here is safe and is what
            // makes HandleClientConnected actually run → OnLobbyJoined (panel swap) + busy clear.
            // Without this ordering the joiner spawns but never leaves the menu, then the 15s
            // busy-timeout tears the connection down (the "kicked after ~10s" bug).
            RehookClientEvents();
            // NOTE: OnLobbyJoined is fired from HandleClientConnected once Mirror actually connects,
            // so the busy/blocker stays up until the connection succeeds or times out.
        }

        // Fires for every member when someone joins, leaves, disconnects, or is kicked/banned.
        void OnLobbyChatUpdate(LobbyChatUpdate_t cb)
        {
            if (cb.m_ulSteamIDLobby != CurrentLobbyID.m_SteamID) return;
            OnLobbyMembersChanged?.Invoke();
        }
#endif

        // ── Mirror client connection lifecycle (joiner side) ─────────────────────
        // Mirror nulls these events on Shutdown, so we re-hook them on every host/join.
        void RehookClientEvents()
        {
            NetworkClient.OnConnectedEvent    -= HandleClientConnected;
            NetworkClient.OnConnectedEvent    += HandleClientConnected;
            NetworkClient.OnDisconnectedEvent -= HandleClientDisconnected;
            NetworkClient.OnDisconnectedEvent += HandleClientDisconnected;
        }

        void HandleClientConnected()
        {
            // Host's own local client also fires this; the host already finished in OnLobbyCreated.
            if (NetworkServer.active) return;

            SetBusy(false);                 // connected → release the input-blocker
            OnLobbyJoined?.Invoke();
            OnLobbyMembersChanged?.Invoke();
        }

        void HandleClientDisconnected()
        {
            if (NetworkServer.active) return;  // host teardown handled by Teardown()
            if (!InLobby) return;              // we already left intentionally (Steam lobby cleared first)

            // Unexpected: host left/crashed, we were kicked, or the join never connected.
            bool wasJoining = IsBusy && _busyReason == BusyReason.Joining;
            Teardown(fireLeftEvent: false);
            Report(wasJoining ? "Could not connect to the host." : "The host left — returning to the menu.");
            OnLobbyLeft?.Invoke();
        }

        // ── Busy state + timeout backstop ────────────────────────────────────────
        void BeginBusy(BusyReason reason)
        {
            _busyReason = reason;
            SetBusy(true);
            ClearTimeout();
            _timeoutCo = StartCoroutine(TimeoutWatch(reason));
        }

        void SetBusy(bool busy)
        {
            if (!busy) { ClearTimeout(); _busyReason = BusyReason.None; }
            if (IsBusy == busy) return;
            IsBusy = busy;
            OnBusyChanged?.Invoke(busy);
        }

        void ClearTimeout()
        {
            if (_timeoutCo != null) { StopCoroutine(_timeoutCo); _timeoutCo = null; }
        }

        // Backstop: if Steam/Mirror never call back, force-abort so the blocker can't hang forever.
        IEnumerator TimeoutWatch(BusyReason reason)
        {
            yield return new WaitForSecondsRealtime(operationTimeout);
            _timeoutCo = null;
            if (!IsBusy) yield break;

            Debug.LogWarning($"[SteamLobby] {reason} timed out after {operationTimeout}s — aborting.", this);
            Teardown(fireLeftEvent: false);     // sets busy=false, clears any partial lobby/session
            Report(reason == BusyReason.Hosting ? "Hosting timed out." : "Joining timed out.");
            OnLobbyLeft?.Invoke();
        }

        void Report(string msg)
        {
            Debug.LogWarning($"[SteamLobby] {msg}", this);
            OnLobbyError?.Invoke(msg);
        }
    }
}
