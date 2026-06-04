# Development Roadmap

What remains before shipping to Steam, roughly ordered by priority and dependency.

> **Design direction (June 2026):** Linear, hand-built **7-level story campaign**. Procedural generation is **CANCELLED** — there are no seeds, random maps, or power-core collection. See [CLAUDE.md](CLAUDE.md) and [CurrentLevels.md](CurrentLevels.md).

## Phase 1: Core Gameplay Loop (Must Have)

### Ghost AI - Make It Scary
Behavior is already specified (and is **unchanged** by the design pivot) — see [AI-System.md](AI-System.md). Remaining build work:
- [ ] Wire `TheWalker_final.fbx` model to the ghost prefab + animator (idle, walk, run, jumpscare) — needs clips from the 3D artist
- [ ] Build the `NoiseEventBus` → `GhostPerception` → `GhostBrain` → `GhostMover` pipeline
- [ ] Spawn the ghost at **designer-placed spawn points** per level (no longer computed from a grid center)
- [ ] Use **NavMesh baked in-editor per hand-built level** (no runtime bake needed anymore)
- [ ] Implement states: Idle, Investigate Noise, Roam, Chase, Search-Last-Known, Jumpscare, (Laser ability)
- [ ] Down/kill mechanic: on jumpscare contact → player downed (server-authoritative)
- [ ] Network the ghost: server controls AI, NetworkTransform/NetworkAnimator sync to clients

### Level Loading & Progression
- [ ] Level manager: load Level N scene, spawn players, advance to N+1 when the gate opens
- [ ] Objective-complete detection per level → unlock gate/door
- [ ] Require players to regroup at the gate to advance
- [ ] Campaign-complete flow after Level 7 → ending → return to menu

### Level Objective Mechanics
- [ ] **Level 1 (Forest):** 3 signal towers with relay redirect (100%→0%, relay fights back), direction monitor visual, cabin with audio log + fuse(s), gate opens when all towers done
- [ ] **Level 2 (Cabin):** restore power via lever (floor wire guides players), cabin lights + contains audio log/map/note, 4 relays each with named levers (Amplify/Ground/Suppress/Stabilize) pulled in correct order by all 4 players, wrong input makes noise, door/hatch opens on success
- [ ] **Levels 3–7:** **design pending**, then build

### Narrative Systems
- [ ] **Speaker Lady** dialogue system: triggered voice guidance per objective/level
- [ ] **Audio recording** pickups: interactable objects that play a one-time story log (Creator's logs, etc.)

### Death & Game Over
- [ ] Player downed state (input disabled, screen tint) + revive/help (TBD)
- [ ] Level-fail condition + handling (retry level vs. restart — TBD, see memory `level_progression`)
- [ ] Win/ending screen after Level 7
- [ ] Return to lobby/menu after game ends

## Phase 2: Multiplayer Infrastructure

### Lobby / Session System
- [ ] Lobby flow: host creates, players join until up to **4**, host starts the game
- [ ] Player list / ready-up
- [ ] On start → load Level 1 scene → spawn all players
- [ ] Decide transport: Steam lobbies vs. direct IP vs. relay

### Steam SDK & Cloud
See [STEAM_CLOUD.md](STEAM_CLOUD.md) for detailed architecture and checklist.
- [ ] Add Steamworks.NET (or Facepunch.Steamworks) to the project
- [ ] Create `SteamCloudManager.cs` singleton for all cloud I/O
- [ ] **Steam Cloud Settings**: wire `SettingsManager` to save/load via cloud (JSON)
- [ ] **Steam Cloud Save Slots**: 3 slots storing campaign progress (which level reached)
- [ ] **Steam Cloud Stats**: lifetime stats (games played, campaigns completed, play time)
- [ ] Replace Mirror's default transport with SteamTransport (Steam P2P)
- [ ] Steam authentication, lobby creation/browsing, invite system
- [ ] Steam app ID registration (Steamworks dev account + $100 fee)

### Proximity Voice Chat
- [ ] Configure Dissonance for proximity (spatial blend + distance rolloff)
- [ ] Test with multiple players at varying distances

## Phase 3: UI & Menus

### Main Menu
- [ ] Menu scene: Play, Settings, Quit
- [ ] "Play" → lobby (host/join)
- [ ] Background ambiance

### Settings Menu
- [ ] Audio: master, SFX, music, voice chat volume
- [ ] Video: resolution, quality preset, fullscreen, V-Sync
- [ ] Controls: mouse sensitivity, FOV (key rebinding stretch goal)
- [ ] Persist via Steam Cloud (see [STEAM_CLOUD.md](STEAM_CLOUD.md))

### In-Game HUD
- [ ] Objective/progress display (per level)
- [ ] Player status indicators (alive/downed)
- [ ] Subtitles for Speaker Lady / audio recordings
- [ ] Pause menu (resume, settings, disconnect)

## Phase 4: Polish & Content

### Player Visuals
- [ ] 3rd-person player model — from the 3D artist
- [ ] Attach model to player prefab with animator; sync via Mirror's NetworkAnimator

### Level Content & Atmosphere
- [ ] Build out art/props for each hand-built level
- [ ] Ambient audio per level (wind, hums, horror stingers)
- [ ] Ghost audio (footsteps, screech on chase)
- [ ] Music: tension track that ramps with ghost proximity

### Items & Mechanics
- [ ] Flashlight (likely the most important item)
- [ ] Sprint stamina (optional)

## Phase 5: Steam Release

- [ ] Steam store page (screenshots, description, trailer)
- [ ] Steam build upload via SteamPipe
- [ ] Achievements (optional, e.g. complete each level / the campaign)
- [ ] Testing: 4-player sessions, edge cases (host disconnect, late join, etc.)
- [ ] Launch!

## Design Principles

- **Keep it simple.** Two devs. Ship fast. Cut scope ruthlessly.
- **Minimum viable horror:** dark environments + scary robot ghost + proximity audio + limited visibility + tense co-op puzzles.
- **Story/experience over replayability.** Hand-built levels and narrative are the draw — not a different map every game.
- **Don't over-engineer.** If a system works, ship it.
