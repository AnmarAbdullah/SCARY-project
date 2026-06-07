using Mirror;
using UnityEngine;
using TimeFracture.Player;

namespace ScaryGame.Networking
{
    /// <summary>
    /// The single, persistent NetworkManager for the whole game (Approach A:
    /// one manager + persistent players repositioned per scene).
    ///
    /// Today's scope (Phase 1 — the functional spine):
    ///   * Spawn one persistent player per connection, parked at a lobby
    ///     stand-spot, in MENU mode (no control).
    ///   * On host Start: load the level scene; Mirror pulls every client along.
    ///   * After the level loads: reposition each player onto a spawn point and
    ///     flip them into GAMEPLAY mode.
    ///
    /// Deferred (designed in "Lobby, level loading design.md"): reject-late-joiners,
    /// save slots, loading screen, fail/wipe, level-to-level progression triggers.
    /// </summary>
    public class ScaryNetworkManager : NetworkManager
    {
        [Header("SCARY — Session")]
        [Tooltip("Exact scene name (must also be in Build Settings) loaded when the host starts the game.")]
        [SerializeField] private string level1SceneName = "Level 1 Test";

        // Set true once the host deliberately starts the game, so the scene-changed
        // hook only repositions/enables players for real gameplay loads.
        private bool _gameStarted;

        /// <summary>Host-only: lock in the session and load the first level.</summary>
        [Server]
        public void ServerStartGame()
        {
            if (_gameStarted) return;
            if (numPlayers < 1)
            {
                Debug.LogWarning("[ScaryNetworkManager] Can't start with no players.");
                return;
            }

            _gameStarted = true;
            ServerChangeScene(level1SceneName);
        }

        // A connection joined and is asking for its player. Spawn the persistent
        // body parked at a lobby stand-spot, in Menu mode.
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Transform spot = SpawnPointRegistry.Instance != null
                ? SpawnPointRegistry.Instance.GetSpot(numPlayers)   // numPlayers = count BEFORE this add
                : null;

            GameObject player = spot != null
                ? Instantiate(playerPrefab, spot.position, spot.rotation)
                : Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, player);

            // Explicitly park in Menu mode (SyncVar default is already false, but be clear).
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.ServerSetControl(false);
        }

        // Called on the server after a ServerChangeScene load finishes. For a real
        // gameplay load, move everyone to spawn points and hand them control.
        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);

            if (!_gameStarted) return;   // ignore menu/boot scene loads

            SpawnPointRegistry registry = SpawnPointRegistry.Instance;
            if (registry == null)
                Debug.LogWarning($"[ScaryNetworkManager] No SpawnPointRegistry in '{sceneName}'; players will spawn at the prefab origin.");

            int i = 0;
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn == null || conn.identity == null) continue;

                GameObject player = conn.identity.gameObject;
                PlayerController controller = player.GetComponent<PlayerController>();

                if (registry != null)
                {
                    Transform spot = registry.GetSpot(i);
                    if (controller != null)
                        controller.ServerTeleport(spot.position, spot.rotation);
                    else
                        player.transform.SetPositionAndRotation(spot.position, spot.rotation);
                }

                // Hand the player control of their body now that they're in the level.
                if (controller != null) controller.ServerSetControl(true);

                i++;
            }
        }
    }
}
