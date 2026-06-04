# Ghost AI Implementation Plan

> ## Adaptation note (June 2026) — behavior unchanged, plumbing retargeted
>
> The game pivoted to a **linear, hand-built 7-level campaign** (procedural generation **CANCELLED** — see [CLAUDE.md](CLAUDE.md)). **The Ghost's behavior in this plan is kept exactly as designed** (noise bus → perception → state machine → mover; idle/investigate/roam/chase/search/jumpscare/laser). Only the *environment plumbing* changes:
>
> - **NavMesh:** baked **in-editor per hand-built level** — no runtime bake after generation. (§8 below is now an editor step, not a runtime baker.)
> - **Spawn:** the Ghost spawns at a **designer-placed spawn point** in each level scene — not computed from a grid center. (§9 below.)
> - **Idle/home anchor:** her resting/return position is a **designer-placed anchor** per level, not the procedural "MonsterBase center."
> - **Noise emitters:** footsteps and voice chat are unchanged. The "core pickup" emitter generalizes to **level-objective interactions** (e.g. wrong lever, tower noise) — any objective interactable can emit a `NoiseEvent`.
> - **"Core in play" → "objective active":** the stay-out-vs-return logic is unchanged in spirit; it is now driven by a per-level **objective-active flag** the level sets, instead of a power-core counter. (§7 below.)
>
> Everything else in this document stands as written.

## Context

The Ghost is the antagonist of SCARY (4-player co-op horror, Unity HDRP + Mirror + Dissonance). She hunts players as they solve each hand-built level's objective. Today the project has a `StateManager`/`State` skeleton at `Assets/Scripts/Enemy/` that does nothing — no movement, no detection, no noise handling. There is no player-downed state, no distractor item, and no proximity voice chat yet. This plan builds the Ghost end-to-end on top of those gaps, with heavy designer-facing tuning (ScriptableObjects), because difficulty, hardcore mode, and per-level scaling all need to be dialled later.

Core design idea: **everything the Ghost "hears" is a `NoiseEvent`** — footsteps, level-objective interactions, distractor devices, voice chat. One bus, one consumer (the Ghost's perception), many emitters. All Ghost AI runs server-only; clients see her via `NetworkTransform` and a `NetworkAnimator`.

User-confirmed decisions:
- Replace the existing `StateManager`/`IdleState`/`PatrolState`/`ChaseState` entirely
- Build a minimal stub `NoiseDevice` item so end-to-end testing works
- Use NavMesh **baked in-editor per hand-built level** (originally a runtime bake after procedural generation — no longer needed)
- Add a minimal `Down()`/`IsDowned` stub on `PlayerController`; full revive system later

---

## Architecture Overview

```
NoiseEventBus (static)  <—— emitters: FootstepTrigger, ObjectiveInteractable, NoiseDevice, VoiceProximityRelay
        │
        ▼
GhostPerception ───── tracks recent noises, runs cone-of-vision raycasts, tracks last-known player pos
        │
        ▼
GhostBrain (state machine) ─── chooses state by priority; configurable via GhostConfig SO + DifficultyProfile SO
        │
        ▼
GhostMover (NavMeshAgent wrapper) ─── set destination, set speed for current behavior
        │
        ▼
GhostAnimator (stub) + GhostAudio (stub) + NetworkAnimator + NetworkTransform
```

All scripts live under `Assets/Scripts/Enemy/Ghost/` in namespace `ScaryGame.Enemy`.

---

## File-by-file Plan

### 1. Noise system — shared infrastructure

**New: `Assets/Scripts/Noise/NoiseEvent.cs`**
- `enum NoiseType { Footstep, ObjectiveNoise, NoiseDevice, VoiceChat, Custom }`
- `struct NoiseEvent { Vector3 position; NoiseType type; float intensity; float hearingRadius; GameObject source; float timestamp; }`

**New: `Assets/Scripts/Noise/NoiseEventBus.cs`**
- Static `event Action<NoiseEvent> OnNoise`
- `static void Emit(NoiseEvent e)` — server-side only; the Ghost's perception is the only subscriber for now
- Cleared on scene unload

**Wire emitters:**
- **Footsteps** — in `Assets/FirstPerson/Scripts/Audio/FootstepTrigger.cs` (Tick → just before `_audioPlayer?.PlayFootstep`): if `NetworkServer.active`, call `NoiseEventBus.Emit(...)`. Hearing radius/intensity comes from a small `FootstepNoiseProfile` field (walk/sprint/crouch values mirroring the volume curve already there). The trigger runs on every client today, so guard with `isLocalPlayer` + a `[Command]` to relay the emit server-side, OR add a dedicated server-side `PlayerNoiseRelay` that watches the local `FootstepTrigger` and sends the noise via command. Recommended: local player owns the relay → `CmdEmitFootstepNoise(pos, state)`.
- **Objective interactions** — level-objective interactables (e.g. a wrong lever in Level 2, a signal-tower noise burst in Level 1, or any item pickup) emit a `NoiseType.ObjectiveNoise` event server-side when they fire. The original plan emitted `NoiseType.CorePickup` from `NetworkItemPickup.CmdPickup`; that same hook now generalizes — any objective interactable calls `NoiseEventBus.Emit(...)` from its server-side activation. (Power cores no longer exist; the emission pattern is identical.)
- **NoiseDevice** — see section 4.
- **VoiceChat** — see section 5.

### 2. Ghost configuration ScriptableObjects

**New: `Assets/Scripts/Enemy/Ghost/Config/GhostConfig.cs`** — base tuning, one asset per difficulty baseline.
Fields, all designer-editable:
- **Movement speeds** — `chaseObjectiveNoiseSpeed`, `chaseDeviceNoiseSpeed`, `chaseFootstepSpeed`, `chasePlayerSpeed`, `searchLastKnownSpeed`, `returnToBaseSpeed`, `roamSpeed`
- **Vision** — `coneAngleDeg`, `coneRange`, `coneRaycastMask` (LayerMask for LOS occluders), `losCheckInterval`
- **Jumpscare** — `jumpscareTriggerDistance`, `jumpscareLeapDuration`, `jumpscareCollideDistance`
- **Hearing** — `defaultFootstepHearRadius`, `defaultObjectiveHearRadius`, `defaultDeviceHearRadius`, `voiceChatHearRadius`, `noiseFreshnessSeconds` (ignore older events), `noisePriority` (per-type weight)
- **Roam-around-point** — `roamRadius`, `roamMinRadius`, `roamWaypointDwell`, `roamDuration` (how long to roam before reverting)
- **Idle/home** — `homeAnchorOffset` (local offset from the level's designer-placed home anchor for her resting pose)
- **Laser ability** — `laserMinSecondsBetween`, `laserMaxSecondsBetween`, `laserActivationChance`, `laserFreezeDuration` (2s), `laserMaxAllowedSpeed`
- **Always-roam toggle** — `idleRoamsMap` (future use; off by default)

**New: `Assets/Scripts/Enemy/Ghost/Config/GhostDifficultyProfile.cs`** — overlays multipliers on `GhostConfig`. Used for hardcore mode + per-level scaling.
- `float speedMultiplier`, `float hearingRadiusMultiplier`, `float visionRangeMultiplier`, `float laserFrequencyMultiplier`, `float losPatience` (how long before she gives up search)
- `GhostBrain` reads `effectiveValue = config.x * profile.multiplier` at state Enter().

### 3. The Ghost itself

**New: `Assets/Scripts/Enemy/Ghost/Ghost.cs`** — root `NetworkBehaviour` on the Ghost prefab. Owns:
- `NavMeshAgent agent` (required)
- `GhostConfig config`, `GhostDifficultyProfile difficulty` (assignable)
- `GhostPerception perception`
- `GhostBrain brain`
- `GhostMover mover`
- `GhostAnimator animatorAdapter`, `GhostAudio audioAdapter`
- `NetworkAnimator` + `NetworkTransform` (Mirror components on the prefab)
- Server-only `Update()` runs `brain.Tick()`. Clients run nothing.

**New: `Assets/Scripts/Enemy/Ghost/GhostPerception.cs`** (server-only)
- Subscribes to `NoiseEventBus.OnNoise`. Stores them in a small ring buffer (e.g. last 8), filtered by per-type hearing radius from config.
- `bool TryGetMostRelevantNoise(out NoiseEvent e)` — picks by `priority * recency * proximity`.
- Cone-of-vision check at `losCheckInterval`: iterates alive players (via `PlayerRegistry`, see §6), angle check, raycast LOS check. Outputs `VisiblePlayer` (closest visible) and updates `lastKnownPosition`/`lastKnownTime` per-player when a previously visible player breaks LOS.

**New: `Assets/Scripts/Enemy/Ghost/GhostMover.cs`**
- Thin wrapper around `NavMeshAgent`. `void GoTo(Vector3 pos, float speed)`. `bool ReachedDestination()` with stoppingDistance tolerance. `void Stop()`. Used by every state — no state touches the agent directly.

**New: `Assets/Scripts/Enemy/Ghost/GhostAnimator.cs`** — stub adapter. Today it just sets parameters like `Speed`, `IsChasing`, triggers `Idle`, `Jumpscare`, `LaserCharge` on an `Animator` ref (may be empty until cousin delivers rig). Names are constants so it's painless to wire later.

**New: `Assets/Scripts/Enemy/Ghost/GhostAudio.cs`** — stub adapter with `AudioSource` + one-shot SFX per behavior (`PlayChase()`, `PlayDetect()`, `PlayRoam()`, `PlayJumpscareScream()`, `PlayLaserCharge()`). Empty clips by default; assign later.

### 4. State machine

**New: `Assets/Scripts/Enemy/Ghost/GhostBrain.cs`** — replaces `StateManager`. Holds all state instances, runs `Tick()` each server frame:
1. `perception.Update()` — refreshes vision + LOS
2. Pick **target state** via priority resolver (see below)
3. If different from current, `Exit()` → `Enter()`
4. `currentState.Run(dt)`

**States** (each a plain class implementing `IGhostState { Enter(); Run(dt); Exit(); }` — no MonoBehaviour, no `[RequireComponent]`):

1. **`GhostIdleAtBase`** — sits at her designer-placed **home anchor** for the level (from `GhostSpawner.homeAnchor`). Triggers `Idle` anim. No movement. Exits when something pulls her out. ("Base" here just means her home anchor, not a procedural MonsterBase.)
2. **`GhostInvestigateNoise`** — `mover.GoTo(noisePos, speedForType)`. Speed picked from config based on `NoiseType`. While moving, fresher higher-priority noise can re-target. When `ReachedDestination()`, transition to `GhostRoamAroundPoint(noisePos)`.
3. **`GhostRoamAroundPoint(center)`** — picks random NavMesh points in `[roamMinRadius, roamRadius]` around `center`; walks to one, dwells `roamWaypointDwell`, picks next. Exits after `roamDuration` or when a new higher-priority trigger fires.
4. **`GhostChasePlayer(player)`** — `mover.GoTo(player.position, chasePlayerSpeed)` every tick. While the player is in cone+LOS, stays. If LOS lost, hand off to `GhostSearchLastKnown(lastKnownPos)`. If distance ≤ `jumpscareTriggerDistance` AND still visible, transition to `GhostJumpscare`.
5. **`GhostSearchLastKnown(lastKnownPos)`** — `mover.GoTo(lastKnownPos, searchLastKnownSpeed)`. On arrival, transition to `GhostRoamAroundPoint(lastKnownPos)`. If she re-sees the player at any point, back to chase.
6. **`GhostReturnToBase`** — `mover.GoTo(homeAnchor.position, returnToBaseSpeed)`. On arrival → `GhostIdleAtBase`. Interruptible by higher priorities (vision, fresh noise).
7. **`GhostJumpscare(player)`** — disables NavMeshAgent, drives a leap (lerp transform from current to player over `jumpscareLeapDuration`), triggers `Jumpscare` anim + scream SFX. When `distance ≤ jumpscareCollideDistance` OR leap timer ends, call `player.ServerDown()` (see §6). After: re-enable agent, transition to whatever brain decides next (likely chase another player or return to base).
8. **`GhostLaserAbility`** — sets a `freezeWindow` flag on a `LaserGameState` SyncVar broadcast to all clients (so client HUD can show "DON'T MOVE"). For `laserFreezeDuration` seconds, brain monitors each non-downed player's speed; any player whose speed exceeds `laserMaxAllowedSpeed` → instant `ServerDown()`. After window: brain returns to normal priority resolution. Ability becomes eligible only once the level's objective is active / sufficiently underway (a per-level gating flag on `LevelObjectiveState`, see §7), throttled by `laserMin/MaxSecondsBetween` + low `laserActivationChance` per check.

**Transition priority** (highest first), evaluated each tick from the top:
1. Active jumpscare or laser ability → cannot be interrupted
2. Jumpscare trigger met (visible + close) → `GhostJumpscare`
3. Laser ability eligible + roll succeeds → `GhostLaserAbility`
4. Player visible in cone → `GhostChasePlayer`
5. Just lost sight of a player (lastKnown < N seconds old) → `GhostSearchLastKnown`
6. Fresh relevant noise above threshold → `GhostInvestigateNoise`
7. Currently roaming around a point and not yet timed out → stay
8. Objective currently active (`LevelObjectiveState.objectiveActive`) → stay out / continue last action / re-roam from current
9. No objective active → `GhostReturnToBase` → `GhostIdleAtBase`

### 5. Voice chat as noise

**New: `Assets/Scripts/Noise/VoiceProximityRelay.cs`** — sits on each player prefab.
- On server only, polls the player's `VoiceBroadcastTrigger.IsTransmitting` + (optionally) `CapturePipelineManager.Amplitude` (or for remote players, `BaseVoicePlayback.IsSpeaking`/amplitude).
- While transmitting, emits a `NoiseType.VoiceChat` event roughly every N ms at the player position with `hearingRadius = config.voiceChatHearRadius` (controlled separately so designers can tune voice-chat hearing independently of footstep hearing).
- Because Dissonance proximity isn't actually configured yet, this relay is the simplest way to give the Ghost "voice ears" without depending on Dissonance audio source distance. The proximity falloff for *players hearing each other* is a separate concern flagged in `NETWORKING.md` — leave that to its own task; **what's added here is purely Ghost-side hearing**.

### 6. Player-side support

**New: `Assets/Scripts/Player/PlayerRegistry.cs`** — static `List<PlayerController> AlivePlayers`. Players add themselves in `OnStartServer`, remove in `OnStopServer`. `Ghost` uses this instead of `FindObjectsOfType` every tick.

**Edit: `Assets/FirstPerson/Scripts/Player/PlayerController.cs`** — add minimal downed stub:
- `[SyncVar(hook = nameof(OnDownedChanged))] bool isDowned`
- `[Server] public void ServerDown()` → set `isDowned = true`
- Hook disables `PlayerMovement` input + shows a simple "you've been downed" screen tint (placeholder — full UI later)
- `PlayerRegistry` excludes downed players from `AlivePlayers` (or exposes `AliveOnly` filter)

**Edit: `Assets/FirstPerson/Scripts/Audio/FootstepTrigger.cs`** — see §1. Add a `[SerializeField] FootstepNoiseEmitter _noiseEmitter` and call it in `Tick()`. The emitter component itself is the new networked relay.

### 7. "Is an objective active?" state container

**New: `Assets/Scripts/Game/LevelObjectiveState.cs`** — server-side, one per level scene. Tracks:
- `bool objectiveActive` (SyncVar) — true while players are actively engaging the level's objective (e.g. a tower is mid-redirect, the lever puzzle is in progress). Used by `GhostBrain` to decide return-to-anchor vs stay-out.
- `bool objectiveComplete` (SyncVar) — set when the level's objective is satisfied (gate opens).
- Public server API: `void SetObjectiveActive(bool active)`, `void NotifyObjectiveComplete()`.
- Level-objective interactables call `SetObjectiveActive(true/false)` as they start/stop being engaged, and `NotifyObjectiveComplete()` when the level is solved. This replaces the old power-core "in play / extracted" counter — the Ghost just queries `objectiveActive` to decide whether to keep prowling or return to her home anchor.

### 8. NavMesh (baked in-editor, per level)

No runtime baker is needed anymore. Each hand-built level scene has a baked **NavMesh** (Unity AI Navigation 1.1.7, already in the project per `CLAUDE.md`):
- Add a `NavMeshSurface` to the level scene and **bake it in-editor** as part of authoring the level.
- The Ghost's `NavMeshAgent` paths on that pre-baked mesh — there is nothing to bake at runtime.
- (The original plan baked at runtime after procedural generation; since levels are now authored, baking happens at edit time.)

### 9. Ghost spawning

**New: `Assets/Scripts/Enemy/Ghost/GhostSpawner.cs`** — `NetworkBehaviour`, server-only.
- Lives in each level scene with a serialized **`Transform ghostSpawnPoint`** (a designer-placed empty in the scene). Optionally a serialized **`Transform homeAnchor`** for her idle/return position (defaults to the spawn point).
- In `OnStartServer` (after the scene + NavMesh are ready), `NetworkServer.Instantiate(ghostPrefab, ghostSpawnPoint.position, ghostSpawnPoint.rotation)`.
- Ghost prefab path: `Assets/Prefabs/Ghost.prefab` (new, capsule for now per user; swap to TheWalker later).

### 10. Minimal NoiseDevice (distractor stub)

**New: `Assets/Scripts/Items/NoiseDevice.cs` + `Assets/Prefabs/NoiseDevice.prefab`**
- `NetworkBehaviour`. `[Command] CmdActivate()` → server emits a single `NoiseType.NoiseDevice` event at the device's world position with `hearingRadius = config.defaultDeviceHearRadius`, plays a stub SFX, optional cooldown.
- For now wire activation to a temporary debug key in-editor (e.g. `Input.GetKeyDown(KeyCode.G)` on local player) so behavior is testable. Real inventory wiring later.

---

## Key Reuse — Things NOT to reinvent

- **Spawn & home positions**: use the designer-placed `Transform`s in the level scene (`GhostSpawner.ghostSpawnPoint` / `homeAnchor`). No grid-to-world math — levels are authored, so positions are just scene transforms.
- **NavMesh**: bake it in-editor per level (`NavMeshSurface`). Do not write a runtime baker.
- **Mirror patterns**: follow `NetworkItemPickup` for `[Command]`/`[ClientRpc]`/`SyncVar` usage. Use `NetworkTransform` + `NetworkAnimator` rather than custom syncing.
- **`PlayerController.isLocalPlayer`** patterns: already used for camera/voice toggles — same pattern for the footstep noise relay (only local player relays its own footstep, server validates and emits).
- **Settings system**: `GhostConfig` and `GhostDifficultyProfile` are pure ScriptableObjects, not part of the existing `SettingsManager`. Hardcore mode toggle, when added, just swaps which `GhostDifficultyProfile` SO is active.

---

## Files To Delete (after replacement is in)

- `Assets/Scripts/Enemy/StateManager.cs`
- `Assets/Scripts/Enemy/State.cs` (the base class)
- `Assets/Scripts/Enemy/IdleState.cs`
- `Assets/Scripts/Enemy/PatrolState.cs`
- `Assets/Scripts/Enemy/ChaseState.cs`

(Nothing else references these — confirmed during exploration.)

---

## Verification

End-to-end test in a hand-built level scene (or a dev/test scene) with a baked `NavMeshSurface`, a placed `GhostSpawner` (spawn point + home anchor), a `LevelObjectiveState`, and Mirror Host mode.

1. **Spawn & NavMesh.** Launch host. Confirm the level's `NavMeshSurface` is baked (visible in Navigation window). Ghost spawns at the designer-placed spawn point in Idle.
2. **Idle stays put.** No noises → Ghost remains at her home anchor, plays Idle anim trigger (no clip yet but parameter set).
3. **Footstep noise.** Sprint near her — she leaves home, heads toward sprint origin at `chaseFootstepSpeed`, reaches it, roams around it within `roamRadius` for `roamDuration`, then returns home (no objective active).
4. **Cone of vision.** Walk in front of her → she chases at `chasePlayerSpeed`. Duck behind a wall → she stops seeing you (raycast LOS), transitions to SearchLastKnown, arrives at last seen position, roams.
5. **Jumpscare.** Stand still in her cone. When `distance ≤ jumpscareTriggerDistance`, she disables agent and leaps to you; `ServerDown()` fires; your input dies and the screen tints. Verify on a second client that you see her leap via `NetworkTransform`.
6. **Objective noise.** Trigger an objective interactable (e.g. a stub wrong-lever or the `NoiseDevice`). Ghost reacts to an `ObjectiveNoise` event at that position at `chaseObjectiveNoiseSpeed`.
7. **Stay-out vs return.** With `LevelObjectiveState.objectiveActive = true`, hide far away from the Ghost's chase target — she should keep roaming the last known area, NOT return home. Set `objectiveActive = false` (debug button) → confirm she returns home.
8. **NoiseDevice.** Drop the device prefab in scene, press the debug activate key → Ghost investigates at `chaseDeviceNoiseSpeed`.
9. **Voice chat hearing.** With two clients in Host mode, hold push-to-talk on the remote — Ghost should treat your position as a noise source within `voiceChatHearRadius`. Independently tunable.
10. **Laser ability.** Set `laserActivationChance = 1` and `laserMinSecondsBetween = 0` in the config for testing. Set the level's laser-eligibility flag (debug) → verify the ability can fire: HUD flag goes up via SyncVar, players who keep moving past `laserMaxAllowedSpeed` are downed; players who stop survive.
11. **Difficulty profile.** Swap to a "Hardcore" `GhostDifficultyProfile` with 2× speed/hearing → confirm all values scale at next state Enter().

No automated tests (this is Unity gameplay code); verification is in-editor with the live scene.

---

## Out of Scope (Explicit)

- Full per-level objective mechanics (towers, levers, gate logic) — this plan only builds the `LevelObjectiveState` flag the Ghost queries
- Inventory/HUD wiring for the NoiseDevice
- Full down/revive teammate-help system
- Dissonance proximity for *players hearing each other* (separate task)
- TheWalker animations/clips (stub adapter ready; clips wire later)
- Hardcore mode UI toggle (the SO swap mechanism is ready; the menu wiring is its own task)
- Always-roam map mode (flag exists in config for future)
