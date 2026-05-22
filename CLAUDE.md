# SCARY-project

## What Is This

A Slender-inspired **multiplayer horror game** (4 players max) built in Unity. Players spawn together, explore a **procedurally generated forest map**, collect **3 power cores** scattered across the map, and extract before a terrifying robot enemy kills them. Target platform is **Steam** (PC).

## Team

- **Two developers.** One programmer, one 3D artist (cousins).
- Goal: ship to Steam as fast as possible. Keep everything simple. Fame is a bonus, not the objective.

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity (HDRP 14.0.12) |
| Networking | Mirror (source in Assets/Mirror/) |
| Voice Chat | Dissonance Voice Chat (plugin in Assets/Plugins/Dissonance/) |
| Voice Transport | MirrorIgnorance (Dissonance + Mirror bridge, in Assets/Dissonance/Integrations/) |
| NavMesh | Unity AI Navigation 1.1.7 |
| Rendering | High Definition Render Pipeline |
| Steam | **NOT YET INTEGRATED** - See [STEAM_CLOUD.md](STEAM_CLOUD.md) for integration plan |
| Steam Cloud | **PLANNED** - Settings, 3 save slots, progress tracking |
| Steam Networking | **PLANNED** - Will replace Mirror KCP with Steam P2P transport |

## Game Loop (Design Intent)

1. Players join a lobby/session (not yet built)
2. Host starts the game -> server picks a seed -> level generates on all clients identically
3. Players spawn at the **Base Station** (bottom-center of map)
4. **Monster Base** is always at the **center** of the map
5. **3 Power Cores** are scattered around the map, connected by roads
6. Players collect all 3 cores -> extract at Base Station -> win
7. The robot enemy hunts players throughout (kills on contact)

**Current idea for session flow:** Players join and are already in the game scene (a waiting area). When the host starts, the level generates and teleports everyone in. Keeps it simple -- no separate lobby scene.

## Project Structure (What Matters)

```
Assets/
  Scripts/
    Level-Gen/          # Procedural map generation (fully implemented)
      Core/             # LevelGenerator, LevelGrid, NetworkLevelSync, TileInstance, GenerationContext
      Data/             # LevelConfig, TileDefinition, TileCategory, RoadTileSet, BaseStationLayout, TerrainPaintProfile
      Rules/            # PlacementRule (abstract), DistanceFromTileRule, DistanceFromCategoryRule, EdgeBufferRule, ExclusiveZoneRule, WithinRangeRule
      Props/            # PropScatterer, PropEntry
      Editor/           # LevelGeneratorGizmos (debug viz)
      TerrainPainter.cs # Paints Unity terrain textures under placed tiles
    Enemy/              # StateManager + State machine (Idle, Patrol, Chase)
    Player/             # FPSController (simple), PlayerItems (inventory)
    Items/              # Item (abstract), NetworkItemPickup, TestItem
  FirstPerson/
    Scripts/
      Player/           # PlayerController (advanced, networked), PlayerMovement (physics-based)
      Camera/           # PlayerCameraLook, HeadBob, CameraIdleSway, CameraFollow
      Audio/            # FootstepProfile, FootstepTrigger, PlayerAudioController, SprintBreathing, SurfaceIdentifier
      Input/            # PlayerInputHandler
      Interfaces/       # IMovable, IAudioPlayer, IInteractable, ILookable
  Prefabs/              # Player.prefab, FPS_Player.prefab
  ProceduralGeneration/
    Prefabs/            # Core, MonsterBase, BaseStation, Spawn, Roads (Straight/Curve/Cross/T/End), Tree
  Objects/
    Characters/         # TheWalker_final.fbx (enemy model -- exists but not wired to AI)
  Scenes/               # Scene1, devScene, LevelGen_tiles, levelGen_Test
  Dissonance/           # Mirror+Dissonance integration bridge
  Mirror/               # Mirror networking library + examples
```

## Namespaces

- `ScaryGame.LevelGen` -- all procedural generation code
- `TimeFracture.Player` -- advanced player controller
- `TimeFracture.Camera` -- camera systems (head bob, sway, look)
- `TimeFracture.Audio` -- footstep/breathing audio
- `TimeFracture.Input` -- input handling
- `TimeFracture.Interfaces` -- shared interfaces (IMovable, etc.)
- Root namespace -- Enemy states, Items, FPSController, TerrainPainter

## Key Architecture Decisions

- **Deterministic procedural generation**: Server rolls a seed, syncs via SyncVar. All clients run the same generator with the same seed = identical maps without transmitting grid data.
- **Server-authoritative items**: Pickup validated server-side (range check, identity check). SyncVar + RPC for state propagation.
- **Two player controller systems exist**: `FPSController` (simple, CharacterController-based) and `PlayerController`+`PlayerMovement` (advanced, Rigidbody-based with crouch, head bob, sprint breathing). The advanced one in `TimeFracture.Player` is the primary one being used.
- **Enemy AI uses a state machine**: `StateManager` orchestrates `State` subclasses (Idle -> Patrol -> Chase). Currently skeleton logic only.
- **ScriptableObject-driven configuration**: LevelConfig, TileDefinition, RoadTileSet, FootstepProfile, TerrainPaintProfile are all SO assets for designer-friendly iteration.

## What's Done

- Procedural level generation (5-phase pipeline, rule-based tile placement, road auto-tiling, prop scattering, terrain painting)
- Network seed sync (Mirror SyncVar)
- Advanced first-person controller (walk/sprint/crouch/jump, physics-based, head bob, camera sway)
- Footstep audio system (per-surface, per-movement-state)
- Sprint breathing audio
- Networked item pickup system (server-authoritative)
- Enemy state machine framework (Idle/Patrol/Chase states exist)
- Dissonance voice chat integrated with Mirror transport
- VoiceBroadcastTrigger toggling on PlayerController (enabled for remote players)
- TheWalker enemy model (FBX exists in Assets/Objects/Characters/)
- Road tile prefabs (Straight, Curve, Cross, T-junction, End cap)

## What's NOT Done (Remaining Work)

### Critical for Ship
- **Main Menu UI** -- no menu scene, no play/settings/quit buttons
- **Settings Menu** -- no audio/video/controls settings screen (settings system exists; needs UI wiring + Cloud sync)
- **Session/Lobby System** -- no way to find games, invite players, or start a match
- **Steam SDK Integration** -- Steamworks not installed, no Steam auth, no Steam lobby/matchmaking (see [STEAM_CLOUD.md](STEAM_CLOUD.md))
- **Win/Lose Conditions** -- no game-over trigger, no extraction mechanic, no victory screen
- **Power Core Interaction** -- cores exist as tiles but no pickup/deliver mechanic linking them to the Base Station
- **Enemy AI Behavior** -- PatrolState has no movement (just a timer), ChaseState has a hardcoded target reference, no player detection/awareness system
- **Enemy Model Integration** -- TheWalker FBX exists but is not on the enemy prefab or wired to animations
- **Player Models** -- no 3rd-person player model (other players see nothing or a capsule)
- **Proximity Voice Chat** -- Dissonance is integrated but not configured for distance-based falloff
- **Production Seeds** -- generation currently uses random seeds; need curated seed list or seed validation for consistent quality
- **NavMesh for Procedural Maps** -- enemy uses NavMeshAgent but NavMesh needs to be baked at runtime after generation
- **Death/Respawn System** -- no player health, no kill mechanic from enemy

### Nice to Have
- Loading screen during generation
- Flashlight item (classic horror staple)
- Sound cues / ambient horror audio
- Minimap or compass
- Post-processing horror effects (film grain, vignette ramp on danger)
- Anti-cheat basics

## How to Work on This Project

### Adding a new system
1. Keep scripts in the appropriate `Assets/Scripts/` subfolder or `Assets/FirstPerson/Scripts/` if it's player-related
2. Use the existing namespace conventions (`ScaryGame.LevelGen`, `TimeFracture.*`)
3. For networked behavior, inherit from `Mirror.NetworkBehaviour`
4. Use `[Command]` for client-to-server, `[ClientRpc]` for server-to-all-clients, `[SyncVar]` for auto-synced state

### Modifying the level generator
- All configuration lives in `LevelConfig` ScriptableObject assets
- To add a new tile type: create a `TileDefinition` SO, add placement rules (SO assets), add it to the config's tile list
- The 5 generation phases run in order: Center -> BaseStation -> Required Tiles -> Roads -> Terrain Fill
- See [LEVELGEN.md](LEVELGEN.md) for the full system breakdown

### Testing
- `devScene` and `levelGen_Test` are test scenes
- LevelGenerator has `generateOnStart` flag for offline testing (no networking needed)
- Mirror's NetworkManager can run Host mode for single-machine multiplayer testing

## Important File Locations

| What | Where |
|---|---|
| Level generation config | ScriptableObject assets (create via SCARY/LevelGen/Level Config menu) |
| Player prefab (advanced) | `Assets/Prefabs/FPS_Player.prefab` |
| Player prefab (simple) | `Assets/Prefabs/Player.prefab` |
| Enemy model | `Assets/Objects/Characters/The Walker/TheWalker_final.fbx` |
| Road prefabs | `Assets/ProceduralGeneration/Prefabs/Roads/` |
| Core/MonsterBase prefabs | `Assets/ProceduralGeneration/Prefabs/` |
| Dissonance integration | `Assets/Dissonance/Integrations/MirrorIgnorance/` |
| Footstep profiles | ScriptableObject assets (create via Assets > Create > ScriptableObject) |
| Package manifest | `Packages/manifest.json` |
