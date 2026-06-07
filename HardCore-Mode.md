# Hardcore Mode — Implementation Plan

## Context

The lobby already plans a **Hardcore toggle** (see `Lobby, level loading design.md` — stored as `GameMode mode` in session state on the not-yet-built `ScaryNetworkManager`; Hardcore always starts at Level 1, and a wipe in Hardcore kicks everyone to Main Menu and resets the run). What's missing is how that mode flag actually changes a level once it loads.

We want Hardcore to:
1. Make **minor environment changes** (extra fog, removed lights, extra props).
2. Change puzzle **amounts** (e.g. 4 satellite towers instead of 3).
3. Make puzzles require **multiple runs** instead of one (e.g. the lever sequence solved 3× in a row instead of once).

The build target is "ship to Steam fast" with a 2-person team, so the guiding principle is: **no content duplication, all driven by one flag.**

## The big decisions (answers to the user's questions)

- **One scene per level — NOT two.** Duplicating each level into a Normal scene + Hardcore scene would double the art, NavMesh bakes, prefab wiring, and every future bugfix. A 2-dev team can't afford that. Instead, every level has **one scene that contains all content** (including the hardcore-only towers/props), and the active `GameMode` toggles what's on and how puzzles behave at level start.
- **One `LevelManager` per scene — NOT two managers.** The single `LevelManager` reads the session `GameMode` on `BeginLevel` and applies the mode to every puzzle and environment object in that scene.
- **Settings live as inspector fields on each puzzle — NOT a separate ScriptableObject.** Each objective/puzzle component exposes its own `normalRounds`, `hardcoreRounds`, and `hardcoreOnly` flag, set right on the object in the scene. Maximally visual and "set as much as you want" per object, no ID-matching indirection. (The existing `GhostDifficultyProfile` SO already covers ghost-aggression scaling separately, so no new SO is needed here.)
- **Multi-round puzzles repeat the same puzzle** N times (no per-round randomization) — simplest to build and verify.

## How it works (the model)

```
Lobby Hardcore toggle ──> ScaryNetworkManager.mode (GameMode, session state)
                                     │  (read on the server when the level scene loads)
                                     ▼
                           LevelManager.BeginLevel(mode)
                                     │
              ┌──────────────────────┼───────────────────────────┐
              ▼                      ▼                            ▼
   For each IModeAware in     Toggle HardcoreToggle      Build required-objective
   scene: ApplyMode(mode)     objects (env + extra       list = objectives that are
   → sets roundsRequired,     puzzles) on/off            ACTIVE after ApplyMode
     active state                                        (excludes disabled hardcore-only)
```

## Pieces to build

### 1. `GameMode` enum + session state (already in lobby plan)
- `Assets/Scripts/Game/GameMode.cs` — `enum GameMode { Normal, Hardcore }`.
- Stored on `ScaryNetworkManager` (per lobby design). The Hardcore lobby toggle sets it before `ServerStartGame()`. `LevelManager` reads it on the server at `BeginLevel`.

### 2. Make `Interactable` round-aware (core change)
File: `Assets/Scripts/Interactables/Interactable.cs`

Add multi-run support so any puzzle can require N solves before it's truly done:
- New fields: `[SerializeField] int normalRounds = 1; [SerializeField] int hardcoreRounds = 1; [SerializeField] bool hardcoreOnly = false;`
- New SyncVars: `int roundsRequired = 1; int roundsDone;`
- New `ApplyMode(GameMode mode)` (server): sets `roundsRequired = mode == Hardcore ? hardcoreRounds : normalRounds`; if `hardcoreOnly && mode != Hardcore`, deactivate the object (and report itself as not-required to `LevelManager`).
- Change `CompleteInteraction()`: increment `roundsDone`; if `roundsDone < roundsRequired` → fire a new `protected virtual void ResetForNextRound()` (re-arm: clears `isDone`/`isOccupied` so it can be solved again) and a `OnRoundCompleted` event; only when `roundsDone >= roundsRequired` set `isDone = true` and fire the existing `OnCompleted`.
- Keep `isDone` semantics intact for all existing consumers (it still means "fully finished"), so `LevelManager`/gate logic is unchanged.

### 3. Adapt `Satelite` for re-arming
File: `Assets/Scripts/Interactables/Satelite.cs`
- Override `ResetForNextRound()` to reset `signalPower = maxSignalPower` and `isOccupied = false` so the same tower can be drained again for the next round. (No angle randomization — "repeat the same" per the decision.)
- No other changes; its `CompleteInteraction()` call at `signalPower <= 0` now naturally feeds the round counter.

### 4. `HardcoreToggle` component (environment + amount changes)
File: `Assets/Scripts/Game/HardcoreToggle.cs` (new, small)
- A marker `NetworkBehaviour` (or plain component toggled by the server) placed on any object that should exist **only** in Hardcore: the 4th tower, extra fog volume, a disabled light, extra props.
- `LevelManager` collects these on the server at `BeginLevel` and `SetActive(mode == Hardcore)`, syncing the active state to clients (SyncVar bool applied in `OnStartClient`, or a single `RpcSetActive` at BeginLevel). For purely cosmetic local props a non-networked variant is fine, but anything affecting gameplay (the extra tower) must be server-driven + synced for fairness.
- This is the single mechanism for both "minor environment changes" and "extra puzzle pieces" — designers just drop the component on whatever should be Hardcore-only.

### 5. `LevelManager` mode application (part of the lobby-plan LevelManager)
File: `Assets/Scripts/Game/LevelManager.cs` (new — already in lobby design; this plan adds the mode hook)
- On `BeginLevel` (server), read `mode` from `ScaryNetworkManager`.
- Find all `Interactable`s (and `HardcoreToggle`s) registered for this level (inspector list or `FindObjectsOfType` filtered to this scene).
- Call `ApplyMode(mode)` on each, toggle the `HardcoreToggle` objects.
- Build the **required-objective list** from objectives that remain **active** (a deactivated hardcore-only tower is simply not required in Normal; the 4th tower IS required in Hardcore). Win = all required objectives `isDone`.
- Multi-round puzzles (lever sequence) plug in the same way: their controller exposes `normalRounds`/`hardcoreRounds` and reports `isDone` only after the last round.

### 6. Future lever-sequence puzzle (Level 2) — built round-aware from day one
When the 4-lever sequential puzzle is built, model it as a `SequencePuzzle` controller (managing 4 lever `Interactable`s) that already carries `normalRounds`/`hardcoreRounds`. Each full correct sequence = one round; a wrong lever fires a `NoiseEvent` (existing `NoiseEventBus`). It reports `isDone` only after `roundsRequired` full sequences. No special-casing — it's just another round-aware objective the `LevelManager` applies mode to.

## What this gives the designers

- **Amount changes:** drop a `HardcoreToggle` on the extra tower/relay. Done.
- **Multi-run changes:** set `hardcoreRounds = 3` on the puzzle. Done.
- **Environment changes:** drop a `HardcoreToggle` on fog/lights/props. Done.
- All from one Hardcore lobby toggle, one scene per level, one manager per scene.

## Files

**New**
- `Assets/Scripts/Game/GameMode.cs`
- `Assets/Scripts/Game/HardcoreToggle.cs`
- `Assets/Scripts/Game/LevelManager.cs` (shared with lobby plan; this plan defines its mode hook)

**Modify**
- `Assets/Scripts/Interactables/Interactable.cs` — round/mode fields, `ApplyMode`, round-aware `CompleteInteraction`, `ResetForNextRound`.
- `Assets/Scripts/Interactables/Satelite.cs` — override `ResetForNextRound`.
- `ScaryNetworkManager` (when built per lobby plan) — expose `GameMode mode` to `LevelManager`.

**No new scenes. No duplicated levels. No second manager.**

## Verification

- **Normal run:** Host with Hardcore OFF, load Level 1 → only 3 towers active/required, no hardcore props, each tower drains once → gate opens.
- **Hardcore run:** Host with Hardcore ON → 4th tower + fog/props appear, all 4 towers required, and a tower configured with `hardcoreRounds = 2` must be drained twice before it reads `isDone`.
- **Sync check:** With 2 editor/build instances, confirm the 4th tower and env toggles appear/disappear identically on host and client, and round progress (signalPower reset between rounds) stays in sync.
- **Regression:** Confirm a puzzle left at default (`normalRounds = hardcoreRounds = 1`, `hardcoreOnly = false`) behaves exactly as today in both modes — `isDone` fires once, gate logic unchanged.
- Test in-editor with Mirror multi-instance (ParrelSync / build + editor) as the lobby doc prescribes.
