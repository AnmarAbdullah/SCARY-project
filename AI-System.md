# Ghost AI Design

> **Redesigned October 2026.** The original ghost was built for the cancelled procedural "steal the power cores" game: she guarded a Monster Base and only hunted while a core was in play. That no longer fits a linear, story-driven 7-level campaign, so it has been replaced with something deliberately **simple**. This document is the single source of truth for her behavior.

## Who she is

A therapist/companion AI built by the Creator that went rogue. In every level she **roams on her own**. **Major sounds** draw her in. If she sees a player she chases them, and if she reaches them they go down.

## Decisions (confirmed with the team)

| Topic | Decision |
|---|---|
| Default behavior | **Always roaming.** No lair, no base. She wanders the level picking random spots. |
| What attracts her | **Only major sounds**: puzzle mistakes, level events meant to lure her, the noise device. Footsteps and voice chat do **not** attract her for now. |
| Catching a player | **Downed, team revives.** All players down → level restarts. |
| Across the 7 levels | **Gets stronger each level** via a per-level difficulty profile. |
| Laser "freeze" attack | **Removed.** |

---

## The loop

```
          ┌──────────────── nothing going on ───────────────┐
          ▼                                                  │
       ROAM ──major sound──► INVESTIGATE ──arrives──► SEARCH AREA
          │                       │                          │
          └──────── sees a player (from any state) ──────────┤
                                  ▼                          │
                               CHASE ──loses sight──► SEARCH LAST SEEN
                                  │
                           close enough
                                  ▼
                       CATCH (player downed) ──► RETREAT ──► ROAM
```

Checked every server tick, top to bottom. The first one that applies wins.

| # | State | When | What she does |
|---|---|---|---|
| 1 | **Catch** | A visible player is within catch distance | Lunges, jumpscare, **downs** the player. Can't be interrupted. |
| 2 | **Retreat** | Right after downing someone | Walks away for a few seconds (~8s), ignoring everything, so teammates can revive. No camping. |
| 3 | **Chase** | She can see a player | Runs at them. |
| 4 | **Search last seen** | She just lost sight of a player | Goes to where she last saw them, then searches around it. |
| 5 | **Investigate** | She heard a major sound | Rushes to it, then searches around it. |
| 6 | **Search area** | Arrived at a sound / last-seen spot | Checks random spots nearby for a while. |
| 7 | **Roam** | Nothing else going on | Wanders the level to random spots. Never idles. |

## Major sounds

Anything that should lure her calls **one shared helper** on the server:

```csharp
NoiseEventBus.EmitMajorNoise(position, source);
```

- One place controls how far major sounds carry (`GhostConfig.majorNoiseHearRadius`), so every puzzle mistake is equally loud. Puzzle scripts don't carry their own noise settings.
- She only reacts if she's within that radius (× the level's hearing multiplier).
- **Who sends it today:**
  - Level 1: `Level1Manager` when a tower is finished (optional toggle)
  - Level 3: each lab puzzle on a wrong input / timeout
  - Noise device item
  - **Level 2:** the wrong-lever noise is in the design but not coded yet. It just needs to call the helper.

## Sight

- A cone in front of her (angle + range), checked a few times per second, with a line-of-sight raycast so walls block it.
- Downed players are invisible to her.

---

## Per-level difficulty

Each level's `GhostSpawner` points to a `GhostDifficultyProfile` asset that multiplies the base numbers in `GhostConfig`:

| Setting | What it changes | Level 1 → Level 7 (example) |
|---|---|---|
| `speedMultiplier` | All her speeds | 0.8 → 1.3 |
| `hearingRadiusMultiplier` | How far major sounds reach her | 0.8 → 1.5 |
| `visionRangeMultiplier` | How far she sees | 0.8 → 1.3 |
| `searchPatienceMultiplier` | How long she keeps searching | 0.7 → 1.5 |
| `activationDelaySeconds` | Grace period at level start before she moves | 60s → 5s |

---

## Spawning

- Each level scene has a **`GhostSpawner`** (spawn point + ghost prefab + difficulty profile).
- When the level loads on the server, she spawns there, waits out the grace period, then roams.
- Each level scene needs a **baked NavMesh** (`NavMeshSurface`, baked in-editor). She can only walk where it's baked.
- No spawner in a scene = no ghost in that level.

## Downed and revive (player side)

- `PlayerController.ServerDown()` sets a synced `isDowned` flag. The ghost ignores downed players.
- Revive (teammate holds F on a downed player) and "all downed → restart level" are **not built yet**. They're the next player-side task.

---

## Scripts

All under `Assets/Scripts/Enemy/Ghost/`, namespace `ScaryGame.Enemy`.

| Script | Role |
|---|---|
| `Ghost` | Root `NetworkBehaviour` on the prefab; builds the brain on the server and ticks it. |
| `GhostBrain` | Picks the state each tick (table above). |
| `States/GhostStates` | Catch, Retreat, Chase, SearchLastSeen, Investigate, SearchArea, Roam. |
| `GhostPerception` | Ears (recent major sounds) + eyes (vision cone, last-seen positions). |
| `GhostMover` | Thin `NavMeshAgent` wrapper; the only thing that moves her. |
| `GhostAnimator` / `GhostAudio` | Adapters for animation + sounds. |
| `GhostSpawner` | Spawns her at the level's spawn point with the level's profile. |
| `Config/GhostConfig` | Base tuning numbers. |
| `Config/GhostDifficultyProfile` | Per-level multipliers. |

## Removed from the old design

- Laser freeze ability
- Monster Base / idle-at-base / return-to-base
- Power cores (`CoreGameState`, `CorePickup` noise, "core in play" logic)
- Spawner dependency on the procedural `LevelGenerator` / runtime NavMesh baker
- Per-noise-type speeds and priorities (only major sounds matter now)

## Maybe later

- Footsteps (sprinting) and voice chat as minor sounds. `PlayerNoiseRelay` and `VoiceProximityRelay` still exist for this, but are not on the player prefab.
- Roaming that drifts toward players' general area in later levels.
- Hardcore mode pointing every level at a harsher profile.

---

## How to test

In a level scene with a baked NavMesh and a `GhostSpawner`, run as Host:

1. She spawns, waits out the grace period, then wanders the level on her own.
2. Walk and talk near her (out of sight) → she doesn't react.
3. Make a puzzle mistake → she rushes to the puzzle if within range.
4. Step into her view → she chases. Break line of sight → she searches where she last saw you.
5. Let her reach you → jumpscare, you're downed, and she walks away.
6. Swap the level's difficulty profile → she gets faster / hears further.
