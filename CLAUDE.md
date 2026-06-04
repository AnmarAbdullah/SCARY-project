# SCARY-project

## What Is This

A **story-driven multiplayer horror game** (4 players co-op) built in Unity, shipping to **Steam** (PC). Players progress through a **linear 7-level campaign** (Day/Night 1 through 7), each a hand-built level with its own objective and puzzle. A rogue robot "ghost" hunts the players throughout. The game is a narrative experience, not a procedurally generated replayable roguelike.

> **IMPORTANT — Direction change (June 2026):** Procedural generation is **CANCELLED**. The game is no longer a Slender-style "collect 3 cores in a random map" survival game. It is now a **linear, hand-crafted, story-driven 7-level campaign**. Any docs/code referring to procedural map generation, seeds, power-core collection, or extraction reflect the **old** design and are being phased out. See [LEVELGEN.md](LEVELGEN.md) and [CurrentLevels.md](CurrentLevels.md).

## Team

- **Two developers.** One programmer, one 3D artist (cousins).
- Goal: ship to Steam as fast as possible. Keep everything simple. A good story/experience over replayability.

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity (HDRP 14.0.12) |
| Networking | Mirror (source in Assets/Mirror/) |
| Voice Chat | Dissonance Voice Chat (plugin in Assets/Plugins/Dissonance/) |
| Voice Transport | MirrorIgnorance (Dissonance + Mirror bridge, in Assets/Dissonance/Integrations/) |
| NavMesh | Unity AI Navigation 1.1.7 (baked per hand-built level) |
| Rendering | High Definition Render Pipeline |
| Steam | **NOT YET INTEGRATED** - See [STEAM_CLOUD.md](STEAM_CLOUD.md) for integration plan |
| Steam Cloud | **PLANNED** - Settings, 3 save slots, progress tracking |
| Steam Networking | **PLANNED** - Will replace Mirror KCP with Steam P2P transport |

## Game Flow (Design Intent)

1. Players set up / join a **lobby**, host waits until up to **4 players** are connected, then starts the game.
2. The game loads **Level 1** (a pre-built scene). All levels are hand-crafted and loaded by index — no runtime generation.
3. Players complete the level's objective (see [CurrentLevels.md](CurrentLevels.md)) while the **ghost** hunts them.
4. Completing the objective opens a **gate/door** to the next level. Players must regroup at the gate to advance.
5. Repeat through **Levels 1–7**. Level 7 ends the campaign.

A **Speaker Lady** voice guides players through objectives. **Recording audio** pickups scattered in levels play one-time story logs (often from the ghost's Creator). See [CurrentLevels.md](CurrentLevels.md) for level designs.

### The Story (high level)

A scientist built the robot ghost to be his personal **therapist/companion AI**. She became self-aware and went **rogue** (exact reason TBD in narrative design). The campaign reveals this story through the Speaker Lady's guidance and discoverable audio recordings from the Creator.

## Levels (Status)

- **Level 1 — Forest:** Find 3 satellite/signal towers, redirect each away from signal (relay fights back 100%→0%). Find a cabin with an audio log + fuse(s) used to open the gate to Level 2. All towers done + players regrouped at gate → advance. (Designed — see [CurrentLevels.md](CurrentLevels.md))
- **Level 2 — Cabin:** Restore power via a lever (wire on the floor guides players). Cabin lights up, contains an audio log + map + a note hinting the answer. Solve a **4-location / sequential-lever** puzzle in the correct order across all 4 players; a wrong input makes noise that attracts the ghost. Done correctly → a door/hatch opens to Level 3. (Designed — see [CurrentLevels.md](CurrentLevels.md))
- **Levels 3–7:** **PENDING DESIGN.** To be planned. Each should escalate puzzle complexity and ghost aggression and advance the story.

## Project Structure (What Matters)

```
Assets/
  Scripts/
    Enemy/Ghost/        # Ghost AI (NoiseEventBus consumer, perception, state machine) - see AI-System.md
    Noise/              # NoiseEvent + NoiseEventBus (shared "hearing" infrastructure)
    Player/             # FPSController, PlayerItems, PlayerInteract, PlayerRegistry
    Items/              # Item (abstract), NetworkItemPickup, NoiseDevice
    Interactables/      # Level-objective interactables (towers, levers, etc.)
    Game/               # Game/level state containers, win/progression logic
    Steam/              # Steam integration (planned)
    Level-Gen/          # DEPRECATED procedural generation (being phased out - do NOT build on this)
  FirstPerson/
    Scripts/
      Player/           # PlayerController (advanced, networked), PlayerMovement (physics-based)
      Camera/           # PlayerCameraLook, HeadBob, CameraIdleSway, CameraFollow
      Audio/            # FootstepProfile, FootstepTrigger, PlayerAudioController, SprintBreathing, SurfaceIdentifier
      Input/            # PlayerInputHandler
      Interfaces/       # IMovable, IAudioPlayer, ILookable
  Prefabs/
    Level 1/            # Hand-built Level 1 prefabs/props
  Objects/
    Characters/         # TheWalker_final.fbx (ghost model)
  Scenes/               # One scene per level (hand-built), plus dev/test scenes
  Dissonance/           # Mirror+Dissonance integration bridge
  Mirror/               # Mirror networking library + examples
```

## Namespaces

- `ScaryGame.Enemy` -- Ghost AI
- `TimeFracture.Player` -- advanced player controller
- `TimeFracture.Camera` -- camera systems (head bob, sway, look)
- `TimeFracture.Audio` -- footstep/breathing audio
- `TimeFracture.Input` -- input handling
- `TimeFracture.Interfaces` -- shared interfaces (IMovable, etc.)
- `ScaryGame.LevelGen` -- **DEPRECATED** procedural generation (do not extend)
- Root namespace -- Items, FPSController

## Key Architecture Decisions

- **Hand-built levels, loaded by index.** No runtime generation. Each level is its own scene/prefab set, with NavMesh baked in-editor. The server tells clients which level scene to load; everyone loads the same authored content.
- **Server-authoritative gameplay**: interactions, objective state, and the ghost all run on the server. SyncVar + Command/ClientRpc for state propagation (see [NETWORKING.md](NETWORKING.md)).
- **Ghost AI behavior is unchanged from its original plan** and runs server-only; clients see her via NetworkTransform/NetworkAnimator. Everything she "hears" flows through one `NoiseEventBus`. See [AI-System.md](AI-System.md).
- **Two player controller systems exist**: `FPSController` (simple) and `PlayerController`+`PlayerMovement` (advanced, primary). The advanced one in `TimeFracture.Player` is the one being used.
- **ScriptableObject-driven configuration** for tunable systems (ghost config, footstep profiles, etc.).

## What's Done

- Advanced first-person controller (walk/sprint/crouch/jump, physics-based, head bob, camera sway)
- Footstep audio system (per-surface, per-movement-state) and sprint breathing audio
- Networked item pickup system (server-authoritative)
- Ghost AI plan + scaffolding (NoiseEventBus, perception, state machine) — see [AI-System.md](AI-System.md)
- Dissonance voice chat integrated with Mirror transport
- TheWalker ghost model (FBX in Assets/Objects/Characters/)
- Settings system (gameplay/video/audio) — needs UI wiring + Steam Cloud sync
- Level 1 design + initial prefabs

## What's NOT Done (Remaining Work)

### Critical for Ship
- **Main Menu UI** -- no menu scene, no play/settings/quit buttons
- **Lobby / Session System** -- players set up a lobby, join the host (up to 4), host starts the game
- **Level loading/progression flow** -- load Level N scene, detect objective completion, open gate, advance to N+1
- **Level 1 & 2 objective mechanics** -- signal towers (relay redirect), cabin power + sequential-lever puzzle
- **Levels 3–7 design + build** -- still being planned
- **Speaker Lady dialogue system** -- triggered voice guidance per objective/level
- **Audio recording pickups** -- interactable one-shot story logs
- **Steam SDK Integration** -- Steamworks not installed (see [STEAM_CLOUD.md](STEAM_CLOUD.md))
- **Settings Menu UI** -- settings system exists; needs UI wiring + Cloud sync
- **Win/Lose & progression** -- level-fail handling, campaign-complete flow, return to menu
- **Ghost AI implementation** -- build out the plan in [AI-System.md](AI-System.md) (behavior stays as planned)
- **Ghost model integration** -- TheWalker FBX wired to the ghost prefab + animations
- **Player Models** -- 3rd-person model other players can see
- **Proximity Voice Chat** -- Dissonance integrated but not distance-based yet
- **Death/Down system** -- player downed state, kill from ghost

### Nice to Have
- Loading screen between levels
- Flashlight item (horror staple)
- Ambient horror audio / music that ramps with danger
- Post-processing horror effects (film grain, vignette on danger)

## How to Work on This Project

### Adding a new system
1. Keep scripts in the appropriate `Assets/Scripts/` subfolder or `Assets/FirstPerson/Scripts/` if player-related
2. Use existing namespace conventions (`ScaryGame.*`, `TimeFracture.*`)
3. For networked behavior, inherit from `Mirror.NetworkBehaviour`
4. Use `[Command]` for client-to-server, `[ClientRpc]` for server-to-all-clients, `[SyncVar]` for auto-synced state
5. **Do NOT build on the deprecated `Level-Gen` procedural system.** Levels are hand-built scenes.

### Building a level
- Each level is its own hand-authored scene/prefab set under `Assets/` (e.g. `Assets/Prefabs/Level 1/`)
- Bake the NavMesh for the level in-editor (the ghost uses NavMeshAgent)
- Place objective interactables, ghost spawn point(s), player spawn(s), the Speaker Lady triggers, and audio-recording pickups
- See [CurrentLevels.md](CurrentLevels.md) for per-level objective specs

### Testing
- Use a level scene directly, or a dev/test scene
- Mirror's NetworkManager can run Host mode for single-machine multiplayer testing

## Important File Locations

| What | Where |
|---|---|
| Per-level objective designs | [CurrentLevels.md](CurrentLevels.md) |
| Ghost AI plan | [AI-System.md](AI-System.md) |
| Player prefab (advanced) | `Assets/Prefabs/FPS_Player.prefab` |
| Player prefab (simple) | `Assets/Prefabs/Player.prefab` |
| Ghost model | `Assets/Objects/Characters/The Walker/TheWalker_final.fbx` |
| Level 1 prefabs | `Assets/Prefabs/Level 1/` |
| Dissonance integration | `Assets/Dissonance/Integrations/MirrorIgnorance/` |
| Package manifest | `Packages/manifest.json` |
