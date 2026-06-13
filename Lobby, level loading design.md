# SCARY-project — Lobby, Level-Loading & Pause Flow Design

---

## 📌 IMPLEMENTATION STATUS — read this first (last updated 2026-06-05)

> **Read order for a new session:** this status block → the design sections below (they are the *plan*, mostly not built yet). The block tells you what is real in code today vs. what is still just designed.

**TL;DR — what exists today:** a working **host-a-lobby + Steam-overlay-invite + live player list** slice, **PLUS the level-load spine (built 2026-06-06):** host Start now loads `Level 1 Test` via `ServerChangeScene`, every client follows, and each player spawns/repositions correctly into the level. Players are **persistent** (Approach A — one body per connection, `DontDestroyOnLoad`, repositioned per scene, not respawned) and have a server-driven **Menu vs Gameplay mode** (no camera/movement/cursor-lock in the lobby; full control once a level loads). Still **not built**: save slots, Find-Game browser, the real `LevelManager` (objective/win/fail/player-left), loading screen, pause, reject-late-joiners. See "Implemented this session" below.

### Progress checklist

| Feature | Status | Notes |
|---|---|---|
| Host a FriendsOnly lobby | ✅ Done | `SteamLobby.HostLobby()` → `StartHost()` |
| Invite via Steam overlay friend picker | ✅ Done | `SteamLobby.OpenInviteOverlay()` |
| Join via Steam invite / "Join Game" | ✅ Done | `OnGameLobbyJoinRequested` → `JoinLobby()` |
| Live player list (Steam names, Host tag) | ✅ Done | `OnLobbyMembersChanged` + `GetMembers()` |
| Host-only Start button | ✅ Done | now calls `ScaryNetworkManager.ServerStartGame()` → `ServerChangeScene` (no longer a log stub). Legacy `StartGameMessage` path is now dead/unused |
| Clean lobby-switch (leave old before joining new) | ✅ Done | fixes "joiner kept Start button" |
| Input-blocker while host/join in flight | ✅ Done | `IsBusy` + `OnBusyChanged`, with 15s timeout backstop |
| Host leaves → all kicked to menu, no migration | ✅ Done | idempotent `Teardown()` + client disconnect handling |
| Save-slot select menu | ❌ Not built | designed below |
| Public/Hardcore lobby options | ❌ Not built | `HostLobby()` is FriendsOnly-only right now |
| Find-Game / public lobby browser | ❌ Not built | `RequestLobbyList` not implemented |
| Join-lock-on-start (reject late joiners) | ❌ Not built | needs `OnServerConnect` reject when `gameStarted`/full |
| `ScaryNetworkManager` subclass | ✅ Done (minimal) | session flag + spawn + scene-load + reposition. Save slots / reject-on-start deferred |
| Level loading (host Start → `ServerChangeScene`) | ✅ Done | loads `level1SceneName` (inspector field); all clients follow |
| Player spawning (persistent, Approach A) | ✅ Done | `OnServerAddPlayer` spawns parked-in-lobby; `OnServerSceneChanged` teleports to spawn points |
| Menu vs Gameplay control mode | ✅ Done | `_controlEnabled` SyncVar on `PlayerController`; server-driven |
| `SpawnPointRegistry` (stand-spots + spawn points) | ✅ Done | one per scene; `GetSpot(index)` |
| Character M/F switch | ✅ Done (stub) | `PlayerAppearance` synced gender; buttons in `LobbyController`; logs only (no models yet) |
| `LevelManager` (objective/win/fail/player-left) | ❌ Not built | designed below; this slice only loads + spawns |
| Loading screen, pause menu, fail/wipe flows | ❌ Not built | designed below |

### Files in this slice
- **`Assets/Scripts/Steam/SteamLobby.cs`** *(modified)* — the lobby brain. Public surface below.
- **`Assets/Scripts/Networking/LobbyNetMessages.cs`** *(new)* — `struct StartGameMessage : NetworkMessage`.
- **`Assets/Scripts/UI/LobbyController.cs`** *(new)* — UI driver; **all UI refs are inspector fields** (no hard-coded paths). Swaps menu↔lobby panels, renders the roster, toggles the input-blocker, broadcasts/receives Start. Adds its own button `onClick` listeners in `Awake` — **do not also wire OnClick in the Inspector** (double-fire).

### Implemented this session — level-load spine (2026-06-06)
**New files:**
- **`Assets/Scripts/Networking/ScaryNetworkManager.cs`** — `NetworkManager` subclass. `ServerStartGame()` (host-only) → `ServerChangeScene(level1SceneName)`. `OnServerAddPlayer` spawns a persistent player parked at a lobby stand-spot in **Menu mode**. `OnServerSceneChanged` (gated by a `_gameStarted` flag) teleports every connection's player to a spawn point and flips it to **Gameplay mode**. `level1SceneName` is an inspector field.
- **`Assets/Scripts/Networking/SpawnPointRegistry.cs`** — per-scene list of placed transforms; static `Instance`; `GetSpot(index)` (wraps + falls back). Used for both lobby stand-spots (FrontEnd) and level spawn points.
- **`Assets/Scripts/Player/PlayerAppearance.cs`** — networked player gender (`PlayerGender` enum, SyncVar + `CmdSetGender`). Mesh swap is a TODO (no models yet); the hook currently just logs.

**Modified files:**
- **`Assets/FirstPerson/Scripts/Player/PlayerController.cs`** — added the `_controlEnabled` SyncVar (Menu/Gameplay), `ServerSetControl`, networked `ServerTeleport`, and `DontDestroyOnLoad`. `OnStartLocalPlayer` now *applies the current mode* instead of forcing control on; `LateUpdate` is gated so there's no look/move in Menu mode (its guard had been commented out — that was the lobby camera/movement bug).
- **`Assets/FirstPerson/Scripts/Player/PlayerMovement.cs`** — added Rigidbody-safe `Teleport()` (snap + kill momentum).
- **`Assets/Scripts/UI/LobbyController.cs`** — Start now calls `ServerStartGame()`; added M/F button fields → `PlayerAppearance.CmdSetGender`.

**Key implementation facts (don't re-derive these):**
- **`Player.prefab` runs the advanced `PlayerController`/`PlayerMovement`/`PlayerCameraLook`/`PlayerInputHandler` stack — NOT `FPSController`.** (The old CLAUDE.md table had this backwards; corrected.) Gate/teleport on `PlayerController`.
- **One NetworkManager only**, `DontDestroyOnLoad`, persists across scenes. The level scene must **not** contain its own. `autoCreatePlayer = ON`, `onlineScene = empty`, Player prefab assigned.
- **Players are `DontDestroyOnLoad`** so they survive `ServerChangeScene` (Mirror destroys+respawns by default — we keep+reposition).
- `ServerTeleport` applies on server AND via `ClientRpc`, so it works regardless of the `NetworkTransformReliable` authority direction (the prefab has one).
- `minPlayersToStart` is set to **1** for solo testing.

**Editor wiring this depends on:** `ScaryNetworkManager` as the active manager (autoCreatePlayer ON, Player prefab, `level1SceneName`, scenes in Build Settings); a `SpawnPointRegistry` in FrontEnd (stand-spots) and in each level (spawn points); a **Menu Camera** in FrontEnd (player cameras are off in Menu mode); `PlayerAppearance` on `Player.prefab`; M/F buttons wired in `LobbyController`.

**Known non-blocker:** Dissonance logs `Cannot find DissonanceComms` in the menu (a player voice trigger with no `DissonanceComms` in that scene) — harmless spam until voice is set up in FrontEnd.

### `SteamLobby` public surface (the API to build on)
- **Actions:** `HostLobby()`, `JoinLobby(ulong lobbyId)`, `LeaveLobby()`, `OpenInviteOverlay()`, `InviteFriend(ulong id)` (direct-ID path, legacy).
- **State:** `Instance` (singleton, DontDestroyOnLoad), `InLobby`, `IsHost` (`==NetworkServer.active`), `IsBusy`, `CurrentLobbyID`, `GetMembers()` → `List<LobbyMember{steamId,name,isHost}>`, `GetMemberCount()`.
- **Events:** `OnLobbyHosted`, `OnLobbyJoined` (fires when Mirror actually *connects*, not just on Steam enter), `OnLobbyLeft`, `OnLobbyMembersChanged`, `OnBusyChanged(bool)`, `OnLobbyError(string)`.

### Lifecycle gotchas / invariants (don't regress these)
- **One teardown path:** `Teardown(bool fireLeftEvent)` is the *only* place we leave a lobby + stop Mirror. It clears the Steam lobby **first** so `InLobby==false` — that flag is how `HandleClientDisconnected` distinguishes an intentional leave from an unexpected host-drop. Keep that ordering.
- **Mirror wipes statics on `Shutdown()`:** `NetworkClient.OnConnectedEvent`, `OnDisconnectedEvent`, and all message `handlers` are nulled/cleared. So they are **re-hooked on every host/join** (`RehookClientEvents()` in `SteamLobby`, and `ReplaceHandler<StartGameMessage>` in `LobbyController.ShowLobby`). Any new message handler must follow the same pattern or it dies after the first leave.
- **Re-hook `OnConnected/DisconnectedEvent` AFTER `StartHost`/`StartClient`, never before.** Mirror's `StartHost`/`StartClient` → `RegisterClientMessages()` **assigns** (`=`, not `+=`) `NetworkClient.OnConnectedEvent`/`OnDisconnectedEvent` to its own internal handlers, so anything subscribed *before* `Start*` is silently clobbered. `RehookClientEvents()` must therefore run *after* `nm.StartClient()`/`nm.StartHost()`. (This was the "joiner spawns but his screen never leaves the menu, then he gets kicked ~7–10 s later" bug: `HandleClientConnected` was wiped, so `OnLobbyJoined` never fired and `IsBusy` was never cleared, so the 15 s busy-timeout tore the connection down. Transport connect is async, so `OnConnectedEvent` can't fire during the synchronous `Start*` call — hooking right after is safe.)
- **Start gating:** the Start button is host-only via `NetworkServer.active`. This is also what makes Steam owner-migration harmless (a migrated client is never a Mirror server).
- **Busy = blocker:** never leave `IsBusy` true without a matching clear. Every exit path (success / fail / cancel / timeout) calls `SetBusy(false)`. The timeout coroutine (`operationTimeout`, default 15s) is the backstop.

### Scene wiring (menu scene = `Assets/MainMenu.unity`; currently only `MainMenuController` is present)
Add these GameObjects + components, then assign `LobbyController` fields:
1. **`SteamManager`** (component: `SteamManager`).
2. **`NetworkManager`** (components: Mirror `NetworkManager` + `FizzySteamworks`; set transport, MaxConnections=4, **autoCreatePlayer = OFF** for now).
3. **`SteamLobby`** (component: `SteamLobby`).
4. **`LobbyController`** on the canvas — assign: `mainMenuPanel`, `lobbyPanel`, `createLobbyButton`, `inviteButton`, `startGameButton`, `leaveLobbyButton`(opt), `playerNameSlots[]` (the 4 children of the `players names` object), **`inputBlocker`** (full-screen invisible Image, Raycast Target ON, last sibling so it's on top, starts inactive), `statusText`(opt), `minPlayersToStart` (set to 1 to test solo).
- `steam_appid.txt = 480` (Spacewar) so Steam init works in-editor. Steam client must be running; overlay enabled for invites.
- Remove/ignore the legacy `SteamLobbyTester` (H/I/L keys) so it doesn't host alongside the UI.

### Where to pick up next (suggested order, per design below)
1. ~~Promote to `ScaryNetworkManager`~~ ✅ done (minimal). Still to add: `offlineScene=FrontEnd`, `OnServerConnect` reject-on-start, `OnServerDisconnect` → `OnPlayerLeft` + abort-below-2.
2. ~~Start → `ServerChangeScene`~~ ✅ done. ~~`SpawnPointRegistry` + player spawning~~ ✅ done.
3. **`LevelManager`** (per-level objective/win → next level, fail/wipe, player-left) — the next real piece. Today the manager only loads + spawns; nothing detects objectives or advances levels yet. (Level 1's `Level1Manager` exists separately for the satellites.)
4. `LoadingScreenManager` (sync'd start), then `PauseMenuController`.
5. Lobby extras: public/Hardcore options, Find-Game browser, join-lock-on-start, save slots.
6. Phase 2 lobby visuals: local-player-centered Fortnite layout + real M/F meshes (currently `PlayerAppearance` is a logging stub).

---

## Context

The game is a 7-level linear 4-player co-op horror title (Unity, Mirror Networking, FizzySteamworks transport, Steam). Today the project has the *pieces* but no glue: `SteamLobby` (host/join a FriendsOnly Steam lobby, DontDestroyOnLoad), `SteamManager`, a DontDestroyOnLoad `SettingsManager`, two player prefabs, an `Interactable` puzzle base class, and a `MainMenuController` with empty button stubs. There is **no** custom NetworkManager subclass, **no** scene-progression flow, **no** player spawn logic, **no** per-level manager, and **no** pause menu.

This document designs the end-to-end run: **Main Menu → Lobby (public/private, invite, host-start) → synchronized level loading with a loading screen → per-level objective/fail management → pause/quit handling**, reusing the existing Steam + Settings + Interactable systems. The goal is the simplest robust flow that ships fast.

### Decisions captured from the user
- **Start level:** The host's selected **save slot** drives the session. A mid-game save resumes at the furthest level reached **but that level restarts fresh**. Finishing the game once unlocks **level-select** for the host. **Hardcore** mode (lobby toggle) always starts at Level 1.
- **Failure / wipe (all players downed):** **Normal mode** → reload the current level (loading screen → re-load scene → respawn). **Hardcore mode** → kick everyone to Main Menu and the run resets to Level 1.
- **Hardcore = one continuous run, no saving.** Hardcore progress is **never persisted** — there is no save slot, no resume, no checkpoint. The whole campaign (Levels 1–7) must be completed in a **single session without dying**. The run **resets to Level 1** if **all players get downed (wipe)** or if **the host leaves** (server teardown). There is no second chance and no mid-run continue — closing the game or losing the host means starting over from Level 1. (Mode is also **locked at game start** and cannot be toggled mid-run.)
- **Late join:** **Locked on start.** Once Level 1 loads, the lobby is unjoinable; to add someone you quit to Main Menu.
- **Mid-level disconnect:** Levels are authored to be completable by **2–3** players. The per-level `LevelManager` listens for a *player-left* event and switches its win conditions to the remaining count. Levels with no co-op puzzle simply ignore the event.
- **Pause:** Local overlay only — the world keeps running (co-op can't freeze time).

---

## Scene model

| Scene | Mirror role | Contents |
|---|---|---|
| `FrontEnd` | **offlineScene** (and `onlineScene` left **empty**) | One scene, swapped **UI panels**: Main-Menu panel (Create Lobby, Find Game, Settings, Quit) and Lobby panel (player list, mode/level readout, host-only **Start** button, invite). Persistent managers live here at boot. |
| `Level_1` … `Level_7` | loaded via `ServerChangeScene` | Gameplay. Each has one `LevelManager`, a `SpawnPointRegistry`, objectives (Interactables), ghost, and an exit gate. |

**Why menu and lobby share one scene:** they're just two UI panels — hosting/joining only swaps panels, no scene load, no flicker, one less scene to maintain. Managers already persist via DontDestroyOnLoad. We set `onlineScene = empty` so Mirror does **not** reload on connect; the first real scene change is `ServerChangeScene(Level_1)` at game start.

We still keep the free "return to menu" behavior: with `offlineScene = FrontEnd`, any `StopHost`/`StopClient` reloads `FrontEnd` (defaulting to the Main-Menu panel). That gives "host quits → everyone kicked back" and "client quits → that client returns to menu" with no custom RPCs.

**Loading screen is NOT a scene.** We do *not* keep a loading scene always running or load levels additively — that fights Mirror's built-in scene sync and adds complexity. Instead a **persistent `LoadingScreenManager` canvas overlay** (DontDestroyOnLoad) covers the screen during every `ServerChangeScene` single-load transition. Simpler, Mirror-idiomatic, and it doubles later as the place to play Speaker-Lady between-level story text.

---

## Persistent managers (DontDestroyOnLoad singletons, created in MainMenu)

1. **`ScaryNetworkManager : NetworkManager`** *(new — the core orchestrator)*
   - Transport = **FizzySteamworks**; `maxConnections = 4`; `offlineScene = FrontEnd`, `onlineScene = empty` (no scene change on connect — panels swap instead).
   - Session state: `GameMode mode` (Normal/Hardcore), `int currentLevelIndex`, `bool gameStarted`, selected save slot.
   - `OnServerConnect`: **reject** connections when `gameStarted` (enforces lock-on-start) or when full.
   - **Player spawning:** spawn one **persistent player object per connection**, kept across scene changes; reposition it at each level's spawn point on scene load (details below). In the Lobby scene the avatar is parked/disabled.
   - `ServerStartGame()`: re-validate 2–4 players, set `gameStarted = true`, make the Steam lobby unjoinable, then `ServerChangeScene(LevelSceneName(currentLevelIndex))`.
   - `OnServerSceneChanged`: locate the new scene's `SpawnPointRegistry`, assign each player a spawn slot, hand control to the new scene's `LevelManager`.
   - `OnServerDisconnect`: remove player; raise **`OnPlayerLeft`** for the active `LevelManager`; if remaining `< 2`, abort session to Main Menu (1 player can't satisfy the 2–3 design).
   - `OnClientChangeScene` / `OnClientSceneChanged`: show / (request-) hide the loading screen.

2. **`SteamLobby`** *(extend existing)*
   - Add `HostLobby(bool isPublic, bool hardcore, int saveSlot)` — choose `ELobbyType` Public vs FriendsOnly; store `mode`, `level`, host name as lobby data.
   - Add **lobby browser**: `RequestLobbyList()` + `LobbyMatchList_t` callback → return joinable public lobbies to the Find Game UI; `JoinLobby(CSteamID)`.
   - On game start, set the lobby joinable flag off (`SetLobbyJoinable(false)`); restore on return to menu.
   - Keep existing `InviteFriend`, `LeaveLobby`, events.

3. **`SteamManager`** — unchanged (Steamworks init).

4. **`SettingsManager`** — unchanged; the pause menu reuses its `SettingsPanelUIController` (pending/applied flow already exists).

5. **`LoadingScreenManager`** *(new)* — DontDestroyOnLoad canvas; `Show()`, `Hide()`, optional status/story text. Shown on transition start, hidden only after **all players report scene-ready** (synchronized start).

6. **Save/progression** *(new, small)* — wraps the existing `SettingsPersistence` JSON pattern: a `SaveSlot` holds `furthestLevel` + flags (`gameCompletedOnce` → unlocks level-select). Host's slot is authoritative for the session. On **Normal**-mode level completion, write the host slot's `furthestLevel`. Hardcore does not persist mid-run. (Whether each client also advances their personal slot is deferred — host-authoritative for now.)

---

## End-to-end flow

### 1. Boot → FrontEnd (Main-Menu panel)
`FrontEnd` contains the persistent-managers bootstrap (or they self-create) and both UI panels (Main-Menu shown by default, Lobby hidden). `MainMenuController` buttons are wired:

- **Create Lobby** → opens a **Save-Slot Select menu first** (the host's slot drives the whole session, so it's chosen before the lobby exists):
  1. **Save-slot screen** — shows the 3 slots (furthest level reached, hardcore flag, empty/continue state) read via `SaveManager`. Host picks a slot.
     - New/empty slot → starts at Level 1.
     - Mid-game slot → resumes at its furthest level (that level reloads fresh).
     - If `gameCompletedOnce` on that slot → a **level-select** appears so the host can pick any unlocked level.
  2. **Lobby-options screen** — public/private toggle + hardcore toggle (Hardcore forces Level 1 and hides level-select).
  3. Confirm → `SteamLobby.HostLobby(isPublic, hardcore, saveSlot)`. On `OnLobbyHosted`, `StartHost()` runs (already in `SteamLobby`); the UI swaps to the **Lobby panel** (no scene change), carrying the chosen slot + mode + starting level in session state.
- **Find Game** → `SteamLobby.RequestLobbyList()` → list public lobbies → `JoinLobby` → `StartClient()` → on connect, swap to the **Lobby panel**. (Joiners pick **no** save slot — they adopt the host's session.)
- **Settings** → existing `SettingsPanelUIController`. **Quit** → existing `OnQuit`.

### 2. Lobby panel (still in FrontEnd)
- A `LobbyUIController` reads the connected players (steam names/avatars) and renders the list, plus mode + starting-level readout.
- **Start** button is **host-only** and `interactable` only when `2 ≤ players ≤ 4`. Click → `ScaryNetworkManager.ServerStartGame()`.
- Invite button → `SteamLobby.InviteFriend(...)` (works for both public and private).

### 3. Start → synchronized level load
1. Host clicks Start → server locks lobby, `ServerChangeScene(Level_X)`.
2. Every client's `OnClientChangeScene` fires → **LoadingScreenManager.Show()**.
3. Each client finishes loading → `OnClientSceneChanged` → signals scene-ready to server.
4. Server waits for **all** ready → tells the new `LevelManager` to **BeginLevel** and broadcasts hide-loading.
5. `BeginLevel`: players snapped to spawn points, control enabled, Speaker-Lady intro, ghost activated. Loading screen hides on all clients **simultaneously** — no one starts early.

### 4. During a level — `LevelManager` (one per level scene, NetworkBehaviour)
- Holds the level's required objectives (existing `Interactable`s — they already track `isDone`), `levelIndex`, `minPlayers`, and an `isCoop` flag.
- **Win:** when all required objectives are `isDone`, open the exit gate / fire completion → `ServerChangeScene(next)`; if Level 7 → final cutscene → return to Main Menu + mark `gameCompletedOnce`.
- **Fail (wipe):** watches player downed/dead state (`PlayerController._isDowned` already exists). When all are downed → Normal: loading screen → reload same scene → respawn; Hardcore: `StopHost`-driven return to Main Menu, run resets to Level 1.
- **Player-left adaptation:** subscribes to `ScaryNetworkManager.OnPlayerLeft`; co-op levels re-evaluate win conditions for the remaining count (2–3). Non-co-op levels ignore it.

### 5. Pause (ESC) — local overlay, world keeps running
- A `PauseMenuController` (in each level / persistent) toggles a local canvas on ESC. **No `Time.timeScale = 0`** — multiplayer can't freeze; the ghost keeps hunting. While open: unlock cursor, suspend local look/input only.
- Buttons: **Resume**, **Settings** (reuse `SettingsPanelUIController`; live FOV/sensitivity apply already works), **Quit to Menu**.
- **Quit to Menu** → `SteamLobby.LeaveLobby()` (already calls the right `StopHost`/`StopClient`/`StopServer`). Mirror then auto-loads `offlineScene = FrontEnd` (Main-Menu panel). **Host quitting tears down the server → every client disconnects → all auto-return to FrontEnd.** Clean kick-everyone with zero extra code.

---

## Player-spawning detail (the one non-obvious mechanic)

Recommended: **one persistent player object per connection, repositioned per level.** Mirror preserves a connection's player object across `ServerChangeScene`, so we spawn it once on connect, park it (disabled visuals/control) in the Lobby, and on each `OnServerSceneChanged` assign it the next free slot from that scene's `SpawnPointRegistry` and snap its transform (server sets position; clients reset via the existing NetworkTransform). Spawn points are simple tagged transforms placed in each level scene.

*Alternative considered:* Mirror's `NetworkRoomManager` room-player/game-player swap. Rejected — it would fight the existing `SteamLobby` ownership of lobby membership and add a prefab-swap layer we don't need.

---

## Edge cases the design explicitly handles

1. **Can't pause a co-op world** — pause is a local overlay; game keeps running (stated above).
2. **Players load at different speeds** — loading screen stays up until *all* report scene-ready; synchronized BeginLevel.
3. **Clean quit vs Alt-F4 / crash** — both route through `OnServerDisconnect` / Mirror's `offlineScene`; no special-casing.
4. **Host leaves** — no host migration (Mirror); server teardown returns everyone to Main Menu. Intended.
5. **Drop below 2 players** — abort session to Main Menu (below the authored 2–3 floor).
6. **Late join attempts after start** — `OnServerConnect` rejects + Steam lobby set unjoinable.
7. **Mirror connect fails after joining the Steam lobby** (NAT/relay) — add a join timeout → error → leave lobby + Main Menu toast.
8. **Quit during the loading screen** — disconnect handled mid-transition; loading screen torn down with scene return.
9. **Cursor lock transitions** — explicit states: menus/pause = unlocked, gameplay = locked.
10. **Player leaves between Start-click and scene load** — re-validate count at `ServerStartGame` and at BeginLevel.
11. **Hardcore toggle** — locked once the game starts; wipe → Main Menu + run reset.
12. **Level 7 completion** — final cutscene → Main Menu, set `gameCompletedOnce` (unlocks level-select).
13. **Mid-game save resume** — start at furthest level but reload it fresh (per decision).

---

## Files to create / modify

**New scripts**
- `Assets/Scripts/Networking/ScaryNetworkManager.cs` — NetworkManager subclass (orchestrator above).
- `Assets/Scripts/Networking/SpawnPointRegistry.cs` + `SpawnPoint.cs` — per-scene spawn slots.
- `Assets/Scripts/Game/LevelManager.cs` — per-level objective/fail/player-left logic.
- `Assets/Scripts/Game/GameMode.cs` — Normal/Hardcore enum + session config.
- `Assets/Scripts/Save/SaveSlot.cs` + `SaveManager.cs` — reuse `SettingsPersistence` JSON pattern.
- `Assets/Scripts/UI/LobbyUIController.cs` — player list, host-start gating, invite.
- `Assets/Scripts/UI/LoadingScreenManager.cs` — persistent loading overlay.
- `Assets/Scripts/UI/PauseMenuController.cs` — ESC overlay, Resume/Settings/Quit.

**Modify**
- `Assets/Scripts/Steam/SteamLobby.cs` — public/private host, lobby browser (`RequestLobbyList`/`JoinLobby`), joinable toggle, mode/level lobby data.
- `Assets/Scripts/UI/MainMenuController.cs` — implement `OnFindGame`/`OnCreateGame`/`OnSettings`; add the **Save-Slot Select** sub-menu and the **Lobby-Options** (public/private + hardcore) sub-menu that feed `SteamLobby.HostLobby(...)`.
- Reuse as-is: `SettingsManager` / `SettingsPanelUIController`, `Interactable` (objectives), `PlayerController._isDowned` (wipe detection), `PlayerRegistry`.

**Scenes / build settings**
- Add `FrontEnd`, `Level_1..7` to Build Settings (index 0 = FrontEnd).
- Each level scene: `LevelManager` + `SpawnPointRegistry` + objectives + ghost + exit gate.
- Configure `ScaryNetworkManager`: transport, offline/online scenes, max 4.

**Deliverable doc:** also drop this design into the repo (e.g. `Assets/Scripts/Networking/GAME_FLOW.md`) for the 2-person team.

---

## Verification

- **Lobby + gating:** Host a lobby, confirm Start is disabled at 1 player, enabled at 2–4. Join via invite (private) and via Find Game (public). Confirm lobby becomes unjoinable after Start.
- **Synchronized load:** Two+ clients; artificially slow one's load — confirm loading screen stays up for everyone until all are in, then all start together.
- **Spawning:** Confirm each player spawns at a distinct spawn point each level; positions reset on level change and on level-restart.
- **Objective → progression:** Complete a level's Interactables → gate opens → next level loads through the loading screen.
- **Fail flows:** Force all players downed in Normal → same level reloads; toggle Hardcore → wipe returns all to Main Menu.
- **Disconnect:** Drop one client mid co-op level → `OnPlayerLeft` fires, conditions adapt to remaining count; drop below 2 → session aborts to menu.
- **Pause/quit:** ESC opens overlay while ghost keeps moving; client Quit → that client to menu, others continue; host Quit → all return to menu. Settings changed in pause apply live.
- Run in-editor with Mirror's multi-instance (ParrelSync / build + editor) for 2–4 player checks.
