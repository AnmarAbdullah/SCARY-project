using System;
using Mirror;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace ScaryGame.Steam
{
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

#if !DISABLESTEAMWORKS
        Callback<LobbyCreated_t> _lobbyCreated;
        Callback<GameLobbyJoinRequested_t> _joinRequested;
        Callback<LobbyEnter_t> _lobbyEntered;
#endif

        public CSteamID CurrentLobbyID { get; private set; }
        public bool InLobby => CurrentLobbyID.IsValid() && CurrentLobbyID.m_SteamID != 0;

        public event Action OnLobbyHosted;
        public event Action OnLobbyJoined;
        public event Action OnLobbyLeft;
        public event Action<string> OnLobbyError;

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

            _lobbyCreated  = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            _joinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            _lobbyEntered  = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
#endif
        }

        void OnDestroy()
        {
#if !DISABLESTEAMWORKS
            _lobbyCreated?.Dispose();
            _joinRequested?.Dispose();
            _lobbyEntered?.Dispose();
#endif
            if (Instance == this) Instance = null;
        }

        // Called by host UI (or test script). Creates a Steam lobby; Mirror host starts in the callback.
        public void HostLobby()
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized) { Report("Steam not initialized"); return; }
            if (InLobby) { Report("Already in a lobby"); return; }

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

        // Leaves the Steam lobby and tears down Mirror (host or client, whichever is active).
        public void LeaveLobby()
        {
#if !DISABLESTEAMWORKS
            if (InLobby)
            {
                SteamMatchmaking.LeaveLobby(CurrentLobbyID);
                CurrentLobbyID = CSteamID.Nil;
            }
#endif
            var nm = NetworkManager.singleton;
            if (nm != null)
            {
                if (NetworkServer.active && NetworkClient.isConnected) nm.StopHost();
                else if (NetworkClient.isConnected) nm.StopClient();
                else if (NetworkServer.active) nm.StopServer();
            }
            OnLobbyLeft?.Invoke();
        }

#if !DISABLESTEAMWORKS
        void OnLobbyCreated(LobbyCreated_t cb)
        {
            if (cb.m_eResult != EResult.k_EResultOK)
            {
                Report($"Lobby create failed: {cb.m_eResult}");
                return;
            }

            CurrentLobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            // Store host's SteamID in lobby data so joiners know who to connect Mirror to.
            SteamMatchmaking.SetLobbyData(CurrentLobbyID, HostAddressKey, SteamUser.GetSteamID().ToString());

            Debug.Log($"[SteamLobby] Lobby created. ID={CurrentLobbyID.m_SteamID} HostSteamID={SteamUser.GetSteamID()}", this);

            var nm = NetworkManager.singleton;
            if (nm == null) { Report("No NetworkManager in scene"); return; }

            nm.StartHost();
            OnLobbyHosted?.Invoke();
        }

        // Fires when a friend clicks "Join Game" or accepts an invite from the Steam overlay.
        void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t cb)
        {
            SteamMatchmaking.JoinLobby(cb.m_steamIDLobby);
        }

        // Fires for everyone who enters the lobby — both host and joiners.
        void OnLobbyEntered(LobbyEnter_t cb)
        {
            CurrentLobbyID = new CSteamID(cb.m_ulSteamIDLobby);

            // Host already called StartHost in OnLobbyCreated. Only non-host clients connect here.
            if (NetworkServer.active) return;

            string hostAddress = SteamMatchmaking.GetLobbyData(CurrentLobbyID, HostAddressKey);
            if (string.IsNullOrEmpty(hostAddress)) { Report("Lobby missing host address"); return; }

            var nm = NetworkManager.singleton;
            if (nm == null) { Report("No NetworkManager in scene"); return; }

            Debug.Log($"[SteamLobby] Lobby entered. ID={CurrentLobbyID.m_SteamID} HostSteamID={hostAddress}", this);

            nm.networkAddress = hostAddress;   // FizzySteamworks reads this as host's SteamID
            nm.StartClient();
            OnLobbyJoined?.Invoke();
        }
#endif

        void Report(string msg)
        {
            Debug.LogWarning($"[SteamLobby] {msg}", this);
            OnLobbyError?.Invoke(msg);
        }
    }
}
