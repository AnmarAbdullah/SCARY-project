# Steam Cloud Integration Strategy

**Status**: Planning phase. Steamworks SDK not yet integrated. This document outlines the architecture and implementation plan.

## Overview

Steam Cloud will handle all player data persistence:
- **Settings**: Gameplay, video, and audio preferences (already structured in SettingsData.cs)
- **Save Data**: 3 save slots for game progress (TBD on content)
- **Progress Tracking**: General stats and progress across sessions

No local database or custom save system needed — leverage Steam Cloud's robust sync and backup.

## Architecture

```
Steam Cloud (Server)
    ├── Settings/
    │   └── SettingsData.json       # Synced from SettingsManager
    │
    └── SaveSlots/
        ├── SaveSlot_0.json         # Save game 1
        ├── SaveSlot_1.json         # Save game 2
        └── SaveSlot_2.json         # Save game 3
```

## Components

### 1. Settings Persistence

**Existing System**: `Assets/Scripts/UI/Settings/SettingsManager.cs` (ScriptableObject-based)

**Steam Integration**:
- On game startup: `SteamManager.LoadSettingsFromCloud()` → deserialize → apply to SettingsData
- On settings change: `SteamManager.SaveSettingToCloud()` → serialize SettingsData → Steam Cloud
- Fallback: If Steam unavailable, use local ScriptableObject (already implemented)

**File**: `Settings/SettingsData.json` (UTF-8, ~2KB)

**Structure** (mirrors existing SettingsData.cs):
```json
{
  "gameplaySettings": {
    "mouseSensitivity": 1.0,
    "fieldOfView": 90
  },
  "videoSettings": {
    "displayMode": "Fullscreen",
    "resolution": "1920x1080",
    "fpsLimit": 120,
    "graphicsQuality": "High",
    "vSync": true,
    "brightness": 1.0,
    "gamma": 1.0
  },
  "audioSettings": {
    "masterVolume": 0.8,
    "effectsVolume": 1.0,
    "voiceChatVolume": 0.9,
    "dissonanceVoiceMode": "OpenMic"
  }
}
```

### 2. Save Data (3 Slots)

**Structure**: Each save slot contains game progress data.

**Files**:
- `SaveSlots/SaveSlot_0.json`
- `SaveSlots/SaveSlot_1.json`
- `SaveSlots/SaveSlot_2.json`

Each ~10-50KB (TBD after gameplay implementation)

**Planned Content** (to be refined):
```json
{
  "sessionId": "uuid-here",
  "timestamp": 1609459200,
  "playerName": "PlayerOne",
  "campaign": {
    "levelReached": 2,
    "levelsCompleted": [1],
    "audioLogsFound": ["lvl1_creator_01"]
  },
  "progress": {
    "campaignsCompleted": 1,
    "gamesPlayed": 12
  }
}
```

**TBD**: 
- What goes into current vs. lifetime progress?
- Do players resume mid-campaign at the last completed level, or is each playthrough a single run?
- Per-session or persistent tracking?

### 3. Progress Tracking

**Lifetime Stats** (automatically synced):
- Total games played
- Campaigns completed (all 7 levels)
- Highest level reached
- Audio logs / recordings found
- Play time (hours)

**Implementation**: Incremental updates to a `Stats.json` file when milestones are reached (game end, core collected, etc.).

## Implementation Checklist

### Phase 0: Prep (This Week)
- [ ] Add Steamworks.NET or Facepunch.Steamworks to manifest
- [ ] Create `SteamManager.cs` singleton with basic Steam API initialization
- [ ] Create `SteamCloudManager.cs` to handle all cloud I/O
- [ ] Define JSON schema for Settings, SaveSlots, Stats

### Phase 1: Settings Sync
- [ ] Implement `SteamCloudManager.LoadSettings()` and `SaveSettings()`
- [ ] Hook into SettingsPanelUIController: on "Apply", call `SteamCloudManager.SaveSettings()`
- [ ] On game startup, load from cloud; fallback to local if unavailable
- [ ] Test: change a setting, close/reopen game, verify persistence

### Phase 2: Save Slots
- [ ] Implement `SteamCloudManager.LoadSaveSlot(int slot)` and `SaveToSlot(int slot, SaveData)`
- [ ] Create `SaveData.cs` serializable class with progress fields
- [ ] Wire game-end logic: `OnGameEnd()` → serialize to active slot → `SaveToSlot()`
- [ ] Build a "Load Game" menu to pick slot and load
- [ ] Test: win a game, save to slot 0, restart, load slot 0, verify state

### Phase 3: Progress Tracking
- [ ] Implement `SteamCloudManager.IncrementStat(string statName, int value)`
- [ ] Hook into game flow: `OnCoreCollected()` → increment, `OnGameEnd()` → increment wins/plays
- [ ] Create a simple stats display in pause menu or main menu

### Phase 4: Robust Sync & Conflict Resolution
- [ ] Implement retry logic for failed uploads (queue + retry on next app tick)
- [ ] Add timestamp comparison for conflict detection (cloud vs. local)
- [ ] Implement "cloud wins" or "last-write-wins" strategy (decide based on game state)
- [ ] Add player notification: "Syncing settings..." spinner or similar

## Key Decisions

### No Local-First Strategy
- **Why**: For a shipped game, cloud is authoritative. Settings changes sync immediately.
- **Exception**: If steam API init fails, fall back to local ScriptableObject (already exists), but flag to user.

### 3 Save Slots (Not Continuous Autosave)
- **Why**: Simpler, lower bandwidth. Players can reload from checkpoint.
- **Alternative**: Autosave to cloud every 5min — decide after playtesting.

### Settings Format: JSON
- **Why**: Human-readable, easier debug, mirrors existing system.
- **Alternative**: Binary protobuf — if bandwidth becomes a concern, switch later.

### No Encryption Client-Side
- **Why**: Steam Cloud encrypts in transit + at rest on Valve's servers.
- **Why Not**: Adds complexity. Only needed if storing secrets (which we don't).

## Integration Points (As Features Are Built)

### When Building Level Progression / Win-Loss System
- On completing a level → update `levelReached` / `levelsCompleted` in the active slot
- On campaign complete (Level 7) → `OnGameEnd(bool won, SaveData data)` → `SteamCloudManager.SaveToSlot()` + increment stats

### When Building Main Menu
- Add "Load Game" button → call `SteamCloudManager.LoadSaveSlot(slotIndex)`
- Add "Settings" button → already wired to SettingsManager

### When Building the Ghost AI
- Difficulty profile (per-level ghost scaling) can be recorded in SaveData if relevant to progress
- **Hardcore mode is never persisted.** It is a single continuous run with no save slot, resume, or checkpoint — do not write Hardcore state to SaveData (see [HardCore-Mode.md](HardCore-Mode.md) and [Lobby, level loading design.md](Lobby,%20level%20loading%20design.md))

### When Adding Lobby/Session System
- Store which level the party is on for resume support
- Consider storing player steam IDs for stats attribution

## Dependencies

| Dependency | Version | Notes |
|---|---|---|
| Steamworks.NET or Facepunch.Steamworks | Latest | Add to Packages/manifest.json |
| Steam App ID | TBD | Requires Steamworks dev account ($100 fee), register early |
| Mirror SteamTransport | Built-in to Mirror | Switch from KCP to Steam P2P when Steam init succeeds |

## File Size Limits

Steam Cloud default quota: **1GB per app**. Current design:
- Settings: 2KB
- Save slot: 50KB × 3 = 150KB
- Stats: 10KB
- **Total**: ~165KB (well under quota)

No concern even with 1000 players' worth of data.

## Testing Strategy

1. **Offline Mode**: Disable Steam Init, verify fallback to local saves
2. **Cloud Sync**: Enable Steam, make changes, verify appear on another machine
3. **Conflict**: Modify cloud + local simultaneously, verify resolution strategy
4. **Large Payload**: Add verbose logging to save game → test 500KB+ edge cases
5. **Network Failure**: Simulate upload failure, verify retry + queue behavior

## Future Enhancements (Post-Ship)

- [ ] Cloud backups (auto-backup to 5th slot nightly)
- [ ] Leaderboard: Win count, play time, etc. (Steam Leaderboards API)
- [ ] Achievements: Unlock on cloud (Steam Achievements API)
- [ ] Cross-platform sync: If expanding to console, extend cloud schema
- [ ] Data analytics: Track aggregate stats (map popularity, average win time)

## Links & Resources

- [Steamworks.NET Docs](https://steamworks.github.io/)
- [Facepunch.Steamworks Docs](https://wiki.facepunch.com/steamworks/)
- [Steam Cloud Documentation](https://partner.steamgames.com/doc/features/cloudsave)
- [Mirror SteamTransport](https://mirror-networking.com/docs/Articles/Transports/SteamTransport.html)
- [Steamworks App ID Registration](https://partner.steamgames.com/documentation/example)
