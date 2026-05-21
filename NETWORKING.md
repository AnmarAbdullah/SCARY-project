# Networking & Multiplayer

## Stack

- **Mirror** (source-included in `Assets/Mirror/`) -- handles connections, spawning, sync
- **Dissonance Voice Chat** (plugin in `Assets/Plugins/Dissonance/`) -- voice comms
- **MirrorIgnorance** (in `Assets/Dissonance/Integrations/MirrorIgnorance/`) -- bridges Dissonance to Mirror's transport layer

## How Networking is Used Today

### Level Generation Sync
`NetworkLevelSync` (`Assets/Scripts/Level-Gen/Core/NetworkLevelSync.cs`):
- Server generates seed in `OnStartServer()`, stores in `[SyncVar] syncedSeed`
- Clients receive seed via SyncVar hook `OnSeedChanged`, run `LevelGenerator.GenerateFromSeed(seed)`
- Host skips client-side generation (already did it in `OnStartServer`)
- Result: all clients have identical maps without transmitting tile data

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
- **Planned approach**: players join directly into the game scene (a holding area). Host clicks "Start" -> level generates -> players teleport to spawn. No separate lobby scene.

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

### Networked Enemy
The enemy state machine (`Assets/Scripts/Enemy/`) is purely local right now:
- `StateManager` runs on the enemy GameObject but has no `NetworkBehaviour`
- For multiplayer: enemy logic should run on the server only, with position/state synced to clients via SyncVars or NetworkTransform
- Kill detection needs to be server-authoritative

## Mirror Patterns Used

| Pattern | Where |
|---|---|
| `[SyncVar(hook)]` | NetworkLevelSync (seed), NetworkItemPickup (pickedBy) |
| `[Command]` | NetworkItemPickup.CmdPickup, PlayerItems.CmdSetActiveItem |
| `[ClientRpc]` | NetworkItemPickup.RpcOnPickedUp, PlayerItems.RpcSetActiveItem |
| `isLocalPlayer` checks | FPSController, PlayerController, PlayerItems |
| `NetworkIdentity` | On all networked prefabs |
| `requiresAuthority = false` | CmdPickup (any client can pick up any item) |
