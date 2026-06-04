# Power Core System — Implementation Plan

> ## ⚠️ DEPRECATED objective (June 2026)
>
> The "collect 3 power cores and deliver them to the Base Station to escape" objective is **no longer the game**. SCARY-project is now a **linear, hand-built 7-level campaign** with per-level objectives (signal towers, lever puzzles, etc.) — see [CurrentLevels.md](CurrentLevels.md) and [CLAUDE.md](CLAUDE.md). Procedural generation and power-core collection are **cancelled**.
>
> **This document is retained for its reusable mechanics, not its objective.** The following patterns are still valuable and should be reused for level objectives / progression:
> - **Hold-E-to-channel interaction** (player stationary + vulnerable while activating) → reuse for towers, levers, generators, gates.
> - **Server-authoritative interaction zones** (trigger + `CmdBegin/CmdCancel`) → reuse for any objective interactable.
> - **In-game HUD** showing progress → reuse for per-level objective progress.
> - **Win/end via VideoPlayer cinematic then scene load** → reuse for the campaign ending / between-level transitions.
>
> Ignore the specific "core/Base Station/extraction" nouns below — map them onto the current level objectives.

## Context (historical)

This plan was written when the team's goal was to collect **3 Power Cores** scattered in a procedurally generated forest and deliver them to the **Base Station** to escape. The robot enemy hunts them in the meantime.

A lot of the foundation is already built:

- **Cores spawn and can be picked up** — `Core.prefab` exists, scattered by the level generator, picked up via `NetworkItemPickup` (range check, SyncVar ownership, noise event on pickup).
- **State container exists** — `CoreGameState` already has `NotifyCorePickedUp` and `NotifyCoreExtracted`, with a SyncVar `_coresExtracted` counter.
- **Base Station is placed** — `BaseStation.prefab` spawns at a known grid spot (bottom-center) with roads carved to every core.

**What's missing:** the actual delivery mechanic, the win condition, and the in-game HUD that shows progress. This plan adds those three pieces in the smallest amount of code possible.

The user chose:
- **Hold E to deposit** a carried core (adds tension — player is stationary and vulnerable while channeling).
- **Cinematic mp4** on win (a VideoPlayer plays the escape scene, then returns to menu).

---

## What we're building (in plain terms)

1. A **trigger zone** on the Base Station that knows when a player is inside it.
2. A **"hold E to deposit"** flow when the player is in the zone and carrying a core.
3. A **HUD** showing `X/3 Cores Extracted`.
4. A **win check** that fires when the 3rd core is delivered, plays the mp4, then returns to the main menu.

---

## Step 1 — Extraction trigger on the Base Station

**File:** create `Assets/Scripts/Game/CoreExtractionZone.cs` (new, ~80 lines).

A `NetworkBehaviour` placed on a child of `BaseStation.prefab` with a trigger `Collider` (sphere, ~5 m radius).

Responsibilities (server-authoritative — same pattern as `NetworkItemPickup`):

- `OnTriggerEnter` / `OnTriggerExit` track which players are in the zone (server only).
- Exposes `CmdBeginDeposit(NetworkIdentity player)` and `CmdCancelDeposit(NetworkIdentity player)` for the client to call.
- On deposit success: find the player's currently-held core (the child under their `Items` transform that has a `CoreItemMarker`), call `CoreGameState.Instance.NotifyCoreExtracted(core.netIdentity)`, then `NetworkServer.Destroy(core.gameObject)`.

**Prefab change:** add this component + a trigger collider to a new child object of `BaseStation.prefab` (e.g. `ExtractionZone`).

---

## Step 2 — Hold-E deposit flow on the player

**File:** edit `Assets/Scripts/Player/PlayerItems.cs`.

Add a small state block (no new file — keep it co-located with the rest of player interaction):

- Cache the nearest `CoreExtractionZone` whenever the player enters/exits it (use `OnTriggerEnter/Exit` on the player or a small child trigger probe — whichever fits cleaner with the existing collider layout).
- In `Update` on the local player:
  - If in a zone AND holding a core AND `E` is held down → start a timer (e.g. 2 s).
  - Show a UI prompt + radial fill ("Hold E to deposit… 1.3 s").
  - On timer complete → `extractionZone.CmdBeginDeposit(netIdentity)`; reset.
  - If `E` is released or the player leaves the zone → cancel and reset timer.

**Helper to add:** a `bool IsHoldingCore()` check on `PlayerItems` — iterate `Items` children, return true if any has `CoreItemMarker`.

Reuse the existing `pickupPrompt` GameObject style for the on-screen "Hold E" UI, or add a sibling `depositPrompt` GameObject — simpler than building a new system.

---

## Step 3 — In-game HUD

**File:** create `Assets/Scripts/UI/GameHUDController.cs` (new, ~30 lines) + a new `GameHUD.prefab` Canvas.

- A single TextMeshPro field showing `"{CoreGameState.Instance.CoresExtracted} / 3 Cores Extracted"`.
- Polls `CoreGameState.Instance.CoresExtracted` in `Update` (cheap; SyncVar already propagates the value to clients).
- Canvas is auto-spawned when the local player joins — easiest is to instantiate it from `PlayerItems.OnStartLocalPlayer` or from a small `LocalPlayerHUDSpawner` component on the player prefab.

Match the existing UI namespace pattern (`ScaryGame.UI.Settings`) — put this under `ScaryGame.UI.Game` or similar.

---

## Step 4 — Win condition + cinematic

**File:** edit `Assets/Scripts/Game/CoreGameState.cs`.

In `NotifyCoreExtracted`, after the increment:

```csharp
if (_coresExtracted >= 3)
    RpcPlayWinCinematic();
```

Add:

```csharp
[ClientRpc]
private void RpcPlayWinCinematic() { /* tell each client to play the cinematic */ }
```

**Cinematic playback:** create `Assets/Scripts/Game/WinCinematicController.cs` (new, ~40 lines).

- A `MonoBehaviour` placed in the game scene (or auto-spawned with HUD).
- Holds a reference to a Unity `VideoPlayer` and a fullscreen `RawImage` target.
- `Play()` enables the canvas, starts the `VideoPlayer`, listens for `loopPointReached`.
- On video end → `SceneManager.LoadScene("MainMenu")` (or whatever the menu scene is named).

**Asset:** drop the mp4 into `Assets/Video/win_cinematic.mp4` and reference it on the `VideoPlayer`. (The user will supply the mp4.)

---

## Step 5 — Wire `CoreGameState` into the scene

Confirm `CoreGameState` is present in the gameplay scene with a `NetworkIdentity` and is spawned by the server (Mirror requires `NetworkServer.Spawn` for runtime-spawned NetworkBehaviours, OR place it in the scene with a NetworkIdentity and it will be server-authoritative automatically). If it's not already in the scene, add it to the bootstrap scene that hosts spawns.

---

## Files touched (summary)

**New:**
- `Assets/Scripts/Game/CoreExtractionZone.cs`
- `Assets/Scripts/Game/WinCinematicController.cs`
- `Assets/Scripts/UI/GameHUDController.cs`
- `Assets/Prefabs/UI/GameHUD.prefab` (Canvas with the counter + cinematic RawImage)
- `Assets/Video/win_cinematic.mp4` (user-supplied)

**Edited:**
- `Assets/Scripts/Player/PlayerItems.cs` — hold-E flow + `IsHoldingCore()` helper + HUD spawn
- `Assets/Scripts/Game/CoreGameState.cs` — fire win RPC at 3 cores
- `Assets/ProceduralGeneration/Prefabs/BaseStations/BaseStation.prefab` — add `ExtractionZone` child with trigger collider + `CoreExtractionZone` component

---

## Verification (how to test it works)

**Single-player smoke test (host only):**
1. Open the gameplay scene, press Play as Host.
2. Walk to a core, press E — pickup prompt should show, core attaches to player, HUD stays `0/3`.
3. Walk to the Base Station extraction zone — "Hold E to deposit" prompt should appear.
4. Hold E — radial/timer fills. Release early → cancels. Hold full duration → core disappears, HUD ticks to `1/3`, the noise/visual matches existing pickup feedback.
5. Repeat for cores 2 and 3 — on the 3rd deposit, the win cinematic plays and the menu loads.

**4-player multiplayer test (the real one):**
1. Host + 3 clients connect via Mirror.
2. Each client picks up a different core. HUD on all clients shows the same `_coresInPlay` situation (verify via debug log if needed).
3. Each player deposits their core in turn — HUD updates on all clients simultaneously (SyncVar on `_coresExtracted` does this for free).
4. On the 3rd deposit, **every client** plays the cinematic (RPC) and returns to the menu.

**Edge cases to spot-check:**
- Two players in the zone at once, both holding cores — both should be able to deposit independently.
- Player leaves zone mid-channel — deposit cancels, no core lost.
- Player dies (once Phase 1 death is in) — held core should drop / be recoverable (out of scope for this plan, but note it).
- Host disconnects mid-cinematic — clients should fall back to menu gracefully (acceptable for now if it just disconnects them).

---

## Out of scope (intentionally)

- Dropping a core on death — depends on the death system, which is a separate roadmap item.
- Visual "core slot" animation on the Base Station (lights up as cores are deposited) — polish for later.
- Stats tracking (cores collected counter for Steam) — wired in when Steam Cloud lands.
- A real win/lose state machine — for now, win = scene reload to menu. A proper state manager can come once death + game-over are also being built.
