# Development Roadmap

What remains before shipping to Steam, roughly ordered by priority and dependency.

## Phase 1: Core Gameplay Loop (Must Have)

### Enemy AI - Make It Scary
- [ ] Wire `TheWalker_final.fbx` model to the enemy prefab (currently not attached)
- [ ] Add NavMeshAgent to enemy, bake NavMesh at runtime after level generation (use `NavMeshSurface.BuildNavMesh()`)
- [ ] Implement real PatrolState: pick random NavMesh points within radius, walk to them
- [ ] Implement player detection in ChaseState: find nearest player via overlap sphere or line-of-sight raycast, replace hardcoded `target` reference
- [ ] Add kill mechanic: on collision/proximity with player -> player dies
- [ ] Network the enemy: server controls AI, NetworkTransform syncs position to clients
- [ ] Add animations (idle, walk, run, attack) -- needs animator controller and clips from the 3D artist

### Power Core Collection & Extraction
- [ ] Create a `PowerCore` item/interactable that players can pick up (reuse or extend the existing Item system)
- [ ] Track collected cores per-team (server-authoritative counter via SyncVar)
- [ ] Base Station becomes the extraction point: when all 3 cores delivered, trigger win
- [ ] UI: show core count (e.g., "2/3 Cores Collected")

### Death & Game Over
- [ ] Player health system (or instant-kill on enemy contact)
- [ ] Death state: ragdoll or fade to black, spectate other players
- [ ] Game over when all players dead
- [ ] Win screen when cores extracted
- [ ] Return to lobby/menu after game ends

## Phase 2: Multiplayer Infrastructure

### Session System
- [ ] Decide: Steam lobbies vs. direct IP connect vs. relay
- [ ] Build a simple "Create Game" / "Join Game" flow
- [ ] In-game waiting area before level generates (players load into a holding zone)
- [ ] Host presses "Start" -> seed rolls -> level generates -> players teleport to spawn
- [ ] Player ready-up (optional but nice)

### Steam SDK
- [ ] Add Steamworks.NET (or Facepunch.Steamworks) to the project
- [ ] Replace Mirror's default transport with SteamTransport (Steam P2P networking)
- [ ] Steam authentication (initialize Steam API on launch)
- [ ] Steam lobby creation and browsing
- [ ] Steam invite system (invite friends from Steam overlay)
- [ ] Steam app ID registration (requires a Steamworks developer account + $100 fee)

### Proximity Voice Chat
- [ ] Configure Dissonance for proximity: use spatial blend with distance rolloff
- [ ] Test with multiple players at varying distances
- [ ] Consider push-to-talk vs. always-on (push-to-talk is simpler and less annoying)

## Phase 3: UI & Menus

### Main Menu
- [ ] Menu scene with: Play, Settings, Quit
- [ ] "Play" opens session browser or host/join options
- [ ] Background ambiance (dark forest scene or animated background)

### Settings Menu
- [ ] Audio: master volume, SFX, music, voice chat volume
- [ ] Video: resolution, quality preset, fullscreen toggle, V-Sync
- [ ] Controls: mouse sensitivity, key rebinding (stretch goal)
- [ ] Save/load settings to PlayerPrefs or JSON file

### In-Game HUD
- [ ] Core count display
- [ ] Proximity indicator or compass pointing to cores (optional)
- [ ] Player status indicators (alive/dead)
- [ ] Pause menu (resume, settings, disconnect)

## Phase 4: Polish & Content

### Player Visuals
- [ ] 3rd-person player model (what other players see) -- from the 3D artist
- [ ] Attach model to player prefab with proper animator
- [ ] Sync animations over network (Mirror's NetworkAnimator)

### Level Gen Production Seeds
- [ ] Add post-generation validation: ensure all cores are reachable, paths aren't too short/long
- [ ] Curate a seed pool or improve the rule set so random seeds consistently produce good maps
- [ ] Consider map size tuning (20x20 may need adjustment based on playtesting)

### Audio & Atmosphere
- [ ] Ambient forest sounds (wind, distant sounds, horror stingers)
- [ ] Enemy audio (footsteps, growl/screech when chasing, ambient hum near monster base)
- [ ] Music: tension track that ramps with proximity to enemy or core count

### Items & Mechanics
- [ ] Flashlight (probably the most important item -- dark forest + horror)
- [ ] Sprint stamina (optional, adds tension)

## Phase 5: Steam Release

- [ ] Steam store page (screenshots, description, trailer)
- [ ] Steam build upload via SteamPipe
- [ ] Achievements (optional but adds perceived value cheaply)
- [ ] Cloud saves for settings (optional)
- [ ] Testing: 4-player sessions, edge cases (host disconnect, late join, etc.)
- [ ] Launch!

## Design Principles

- **Keep it simple.** Two devs. Ship fast. Cut scope ruthlessly.
- **Minimum viable horror:** dark forest + scary robot + proximity audio + limited visibility. That's enough.
- **Don't over-engineer.** If a system works, ship it. Optimize later if the game gets traction.
- **Procedural gen is the replayability.** Different map each game = reason to replay.
