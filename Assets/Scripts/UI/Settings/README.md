# Settings System Documentation

## Overview
This is a modular, production-ready settings system for SCARY-project. It handles gameplay, video, and audio settings with a clean separation of concerns. Settings are persisted automatically and applied only when the player clicks the Apply button.

## Architecture

### Core Components

1. **SettingsData** - Scriptable Object that stores all settings
   - GameplaySettings: Mouse sensitivity, FOV
   - VideoSettings: Display mode, resolution, FPS limit, graphics quality, V-sync, brightness, gamma
   - AudioSettings: All volume levels, mic input type, voice chat type

2. **SettingsManager** - Singleton that coordinates all handlers
   - Loads/saves settings
   - Registers/unregisters handlers
   - Applies pending changes when Apply is clicked

3. **ISettingsHandler** - Interface that all handlers implement
   - LoadSettings() - Load current settings
   - ApplySettings() - Apply pending changes to the game
   - RevertPendingChanges() - Discard unsaved changes

4. **GameplaySettingsHandler** - Manages gameplay settings
   - Updates FPSController mouse sensitivity and FOV
   - Clamps values to valid ranges

5. **VideoSettingsHandler** - Manages video settings
   - Updates resolution, display mode, graphics quality
   - Applies FPS limit and V-sync
   - Stores brightness/gamma for post-processing

6. **AudioSettingsHandler** - Manages audio settings with Dissonance integration
   - Updates master volume, music volume
   - Manages microphone input/output volumes
   - Handles Dissonance voice chat settings (push-to-talk vs open mic)
   - Lists available microphone devices

7. **SettingsPanelUIController** - Main UI controller
   - Injects UI element references (sliders, dropdowns, toggles, etc.)
   - Syncs UI with pending changes
   - Handles Apply and Cancel buttons

## Setup Instructions

### Step 1: Create SettingsData Asset
1. In Unity, create a folder: `Assets/Resources/Settings/`
2. Right-click in the folder → Create → SCARY/Settings/Settings Data
3. Name it `SettingsData`

### Step 2: Create Settings Manager in Scene
1. Create an empty GameObject in your Main Menu scene
2. Name it `SettingsManager`
3. Add the `SettingsManager` component
4. Drag the `SettingsData` asset into the `Settings Data` field

### Step 3: Create Settings Handlers in Scene
1. Create three empty GameObjects as children of `SettingsManager`:
   - `GameplaySettingsHandler`
   - `VideoSettingsHandler`
   - `AudioSettingsHandler`
2. Add the corresponding handler components to each

### Step 4: Create Settings Panel GameObject
1. Create a Panel in your canvas (or load from coworker's design)
2. Name it `SettingsPanel`
3. Add the `SettingsPanelUIController` component
4. In the inspector, set:
   - SettingsManager → drag the SettingsManager GameObject
   - GameplayHandler → drag the GameplaySettingsHandler GameObject
   - VideoHandler → drag the VideoSettingsHandler GameObject
   - AudioHandler → drag the AudioSettingsHandler GameObject

### Step 5: Attach UI Elements
Your coworker will have created UI elements in the scene. Drag them into the corresponding fields:

**Gameplay Section:**
- Mouse Sensitivity Slider
- Mouse Sensitivity Value Text
- FOV Slider
- FOV Value Text

**Video Section:**
- Display Mode Dropdown
- Resolution Dropdown
- FPS Limit Slider
- FPS Limit Value Text
- Graphics Quality Dropdown
- V-Sync Toggle
- Brightness Slider
- Brightness Value Text
- Gamma Slider
- Gamma Value Text

**Audio Section:**
- Master Volume Slider
- Master Volume Value Text
- Menu Music Volume Slider
- Menu Music Volume Value Text
- Mic Input Volume Slider
- Mic Input Volume Value Text
- Others Mic Output Volume Slider
- Others Mic Output Volume Value Text
- Mic Input Type Dropdown
- Voice Chat Type Dropdown

**Buttons:**
- Apply Button
- Cancel Button (or "Back")

### Step 6: Update FPSController
The FPSController has been updated with:
- `playerCamera` field - drag your main camera here
- `defaultFOV` field - stores the default FOV
- `SetMouseSensitivity(float)` method
- `SetFOV(float)` method

Make sure to assign the player camera in the inspector.

## How It Works

### Player Changes Settings in Menu
1. Player moves sliders/toggles in the UI
2. `SettingsPanelUIController` calls handler methods (e.g., `SetMouseSensitivity()`)
3. Handlers store changes in pending variables
4. UI updates to show the current value

### Player Clicks Apply
1. `ApplySettings()` is called
2. `SettingsManager` calls `ApplySettings()` on each handler
3. Handlers apply changes to the game (FOV, volume, resolution, etc.)
4. Changes are persisted in `SettingsData`
5. Scene saves the updated data

### Player Clicks Cancel
1. `RevertSettings()` is called
2. `SettingsManager` calls `RevertPendingChanges()` on each handler
3. All pending changes are discarded
4. UI reloads to show last applied settings

## Persisting Settings

Settings are saved to the `SettingsData` ScriptableObject, which persists between play sessions. The `SettingsManager` automatically:
- Loads settings on startup
- Saves settings when Apply is clicked
- Preserves settings between play sessions

## Extending the System

To add new settings:

1. Add fields to the appropriate inner class in `SettingsData` (e.g., `AudioSettings`)
2. Add a public method in the handler to set the value
3. Add getter/setter logic in `ApplySettings()` and `LoadSettings()`
4. In `SettingsPanelUIController`:
   - Add the UI element field
   - Initialize it in the setup method
   - Load the current value in `LoadCurrentSettings()`
   - Handle the value change with a listener

## Integration Notes

- **Dissonance**: The `AudioSettingsHandler` assumes you have Dissonance installed. It calls methods on `DissonanceComms` for voice chat settings.
- **FPSController**: Make sure the `playerCamera` is assigned in the inspector for FOV changes to work.
- **Post-Processing**: Brightness and gamma are stored but not applied automatically. You'll need to connect them to post-processing volumes in your scene or a separate shader.
- **Voice Chat**: Voice activation input is controlled via the `VoiceChatType` setting (0 = Push-to-Talk, 1 = Open Mic).

## Example: Settings Menu Flow

```
1. Player opens Settings from Main Menu
2. SettingsPanelUIController.OnEnable() → Loads current settings
3. Player adjusts sliders/toggles
4. Handlers track pending changes
5. Player clicks "Apply"
   → ApplySettings() called
   → Handlers apply to game
   → Settings saved
6. Player clicks "Back" to Main Menu
```

## Troubleshooting

**Settings not persisting:**
- Make sure `SettingsData` is in `Assets/Resources/Settings/` folder

**Camera FOV not changing:**
- Verify `playerCamera` is assigned in FPSController

**Audio not working:**
- Check Dissonance is installed and `DissonanceComms` is in the scene
- Verify microphone devices are available on the system

**UI elements not updating:**
- Make sure all UI element references are assigned in the inspector
- Check that handlers are registered with SettingsManager
