# Networking & Multiplayer

## Stack

- **Mirror** (source-included in `Assets/Mirror/`) -- handles connections, spawning, sync
- **Dissonance Voice Chat** (plugin in `Assets/Plugins/Dissonance/`) -- voice comms
- **MirrorIgnorance** (in `Assets/Dissonance/Integrations/MirrorIgnorance/`) -- bridges Dissonance to Mirror's transport layer

> **Note (June 2026):** Procedural generation is **CANCELLED**. Levels are hand-built scenes loaded by index, not generated from a synced seed. The old `NetworkLevelSync` seed-sync described in earlier versions of this doc is deprecated. See [CLAUDE.md](CLAUDE.md) and [LEVELGEN.md](LEVELGEN.md).

## How Networking is Used Today

### Level Loading / Scene Sync
Levels are authored scenes. The flow is standard Mirror scene management:
- Host/server decides the active level and uses Mirror's networked scene change (`NetworkManager.ServerChangeScene`) to load the same level scene on all clients
- Players are (re)spawned at the level's designer-placed spawn points after the scene loads
- Progression: when a level's objective is complete and the gate opens, the server changes scene to the next level (Level N → N+1)
- No seed/tile data is transmitted — everyone loads identical authored content

### Player Spawning
Two player prefabs exist:
- `Assets/Prefabs/Player.prefab` -- uses simple `FPSController` (CharacterController-based)
- `Assets/Prefabs/FPS_Player.prefab` -- uses advanced `PlayerController` + `PlayerMovement` (Rigidbody-based)

Both inherit `NetworkBehaviour`. On spawn:
- `OnStartClient()`: disable cameras/audio for non-local players
- `OnStartLocalPlayer()`: enable cameras, lock cursor, enable input

### Item Pickup
`NetworkItemPickup` (`Assets/Scripts/Items/NetworkItemPickup.cs`):
- `[Command(requiresAuthority = false)] CmdPickup()` -- any client can request pickup
- Server validates: not already picked, correct identity, within range (`serverPickupRange = 4`)
- `[SyncVar] _pickedBy` tracks who owns the item
- `[ClientRpc] RpcOnPickedUp()` syncs visual state to all clients
- Items reparent to the picker's "Items" child transform

### Voice Chat
`PlayerController` (`Assets/FirstPerson/Scripts/Player/PlayerController.cs`):
- Toggles `VoiceBroadcastTrigger` component: enabled for local player in `OnStartClient`, disabled in `Awake`
- **Current state**: voice works but is NOT proximity-based. All players hear each other regardless of distance.

## What's Missing

### Session/Lobby System
No lobby, matchmaking, or session management exists. Needs:
- A way for players to create/join games (host starts a Mirror server, others connect)
- Player list / ready-up before game start
- **Planned approach**: players set up / join a lobby and wait until up to 4 players are connected. Host clicks "Start" -> server changes scene to **Level 1** -> players spawn at the level's spawn points. Progression advances scene-by-scene through Levels 1–7.

### Steam Integration
Steamworks SDK is not in the project. Needs:
- `Steamworks.NET` or `Facepunch.Steamworks` package
- Steam authentication (player identity)
- Steam lobbies for matchmaking (or direct connect as fallback)
- Mirror has a `SteamTransport` -- use that instead of default KCP when on Steam

### Proximity Voice Chat
Dissonance supports spatial audio out of the box. To enable:
- Configure `VoiceBroadcastTrigger` with a **positional room** or use `Direct` mode with distance attenuation
- Set up a `VoiceReceiptTrigger` on the listener
- Configure `DissonanceComms` with spatial blend settings
- The MirrorIgnorance transport already handles the networking side

### Networked Ghost
The ghost AI (see [AI-System.md](AI-System.md)) runs **server-only**:
- All perception, state machine, and movement decisions happen on the server
- Position/animation synced to clients via `NetworkTransform` + `NetworkAnimator`
- Down/kill detection (jumpscare contact) is server-authoritative

## Mirror Patterns Used

| Pattern | Where |
|---|---|
| `[SyncVar(hook)]` | NetworkItemPickup (pickedBy), objective/progression state |
| `[Command]` | NetworkItemPickup.CmdPickup, PlayerItems.CmdSetActiveItem |
| `[ClientRpc]` | NetworkItemPickup.RpcOnPickedUp, PlayerItems.RpcSetActiveItem |
| `isLocalPlayer` checks | FPSController, PlayerController, PlayerItems |
| `NetworkIdentity` | On all networked prefabs |
| `requiresAuthority = false` | CmdPickup (any client can pick up any item) |
