# Settings System Setup Checklist

Use this checklist when your coworker provides the UI scene with all the visual elements.

## Pre-Scene Setup

- [ ] Create `Assets/Resources/Settings/` folder
- [ ] Create SettingsData scriptable object and place it in Resources/Settings/

## Main Menu Scene Setup

### SettingsManager GameObject
- [ ] Create empty GameObject named `SettingsManager` in the scene
- [ ] Add `SettingsManager` component
- [ ] Assign `SettingsData` to the `Settings Data` field
- [ ] Create three child GameObjects:
  - [ ] `GameplaySettingsHandler`
  - [ ] `VideoSettingsHandler`
  - [ ] `AudioSettingsHandler`

### Settings Handlers Setup
For each handler GameObject:
- [ ] Add corresponding handler component (e.g., `GameplaySettingsHandler`)
- [ ] Keep them as children of `SettingsManager`

### Settings Panel UI Setup
- [ ] Create or find the Settings Panel from coworker's design
- [ ] Add `SettingsPanelUIController` component
- [ ] In inspector, assign:
  - [ ] `Settings Manager` → SettingsManager GameObject
  - [ ] `Gameplay Handler` → GameplaySettingsHandler GameObject
  - [ ] `Video Handler` → VideoSettingsHandler GameObject
  - [ ] `Audio Handler` → AudioSettingsHandler GameObject

### Gameplay UI Elements
- [ ] Assign `Mouse Sensitivity Slider` field
- [ ] Assign `Mouse Sensitivity Value` Text
- [ ] Assign `FOV Slider` field
- [ ] Assign `FOV Value` Text

### Video UI Elements
- [ ] Assign `Display Mode Dropdown`
- [ ] Assign `Resolution Dropdown`
- [ ] Assign `FPS Limit Slider`
- [ ] Assign `FPS Limit Value` Text
- [ ] Assign `Graphics Quality Dropdown`
- [ ] Assign `V Sync Toggle`
- [ ] Assign `Brightness Slider`
- [ ] Assign `Brightness Value` Text
- [ ] Assign `Gamma Slider`
- [ ] Assign `Gamma Value` Text

### Audio UI Elements
- [ ] Assign `Master Volume Slider`
- [ ] Assign `Master Volume Value` Text
- [ ] Assign `Menu Music Volume Slider`
- [ ] Assign `Menu Music Volume Value` Text
- [ ] Assign `Mic Input Volume Slider`
- [ ] Assign `Mic Input Volume Value` Text
- [ ] Assign `Others Mic Volume Slider`
- [ ] Assign `Others Mic Volume Value` Text
- [ ] Assign `Mic Input Type Dropdown`
- [ ] Assign `Voice Chat Type Dropdown`

### Button Setup
- [ ] Assign `Apply Button`
- [ ] Assign `Cancel Button`
- [ ] In the Apply Button's On Click event, add the SettingsPanelUIController and select `ApplySettings()`
- [ ] In the Cancel Button's On Click event, add the SettingsPanelUIController and select `RevertSettings()`

### Player Setup
- [ ] In FPSController, assign `Player Camera` field to the camera that renders the player view
- [ ] The `defaultFOV` will auto-populate to 60, but you can change it

## After Setup

- [ ] Test opening the settings panel
- [ ] Test moving a slider and clicking Apply (check if it saves)
- [ ] Test moving a slider and clicking Cancel (check if it reverts)
- [ ] Test that settings persist when you close and reopen the game

## UI Element Naming Convention

To make it easier to find elements in the scene, follow this naming:
```
SettingsPanel/
├── GameplaySection/
│   ├── MouseSensitivitySlider
│   ├── MouseSensitivityValue
│   ├── FOVSlider
│   └── FOVValue
├── VideoSection/
│   ├── DisplayModeDropdown
│   ├── ResolutionDropdown
│   ├── FPSLimitSlider
│   ├── FPSLimitValue
│   ├── GraphicsQualityDropdown
│   ├── VSyncToggle
│   ├── BrightnessSlider
│   ├── BrightnessValue
│   ├── GammaSlider
│   └── GammaValue
├── AudioSection/
│   ├── MasterVolumeSlider
│   ├── MasterVolumeValue
│   ├── MenuMusicVolumeSlider
│   ├── MenuMusicVolumeValue
│   ├── MicInputVolumeSlider
│   ├── MicInputVolumeValue
│   ├── OthersMicVolumeSlider
│   ├── OthersMicVolumeValue
│   ├── MicInputTypeDropdown
│   └── VoiceChatTypeDropdown
└── Buttons/
    ├── ApplyButton
    └── CancelButton
```

## Quick Test

After setup, run this test sequence:
1. Open the settings panel
2. Change mouse sensitivity slider
3. Click Apply
4. Close settings (go back to main menu)
5. Open settings again - mouse sensitivity should be at the new value
6. Change FOV slider
7. Click Cancel
8. Check FOV slider is back to previous value
9. Play the game - camera FOV and mouse sensitivity should be applied

## Notes for Coworker

When providing the UI scene:
- Name all UI elements clearly (use the naming convention above)
- Group elements by section (Gameplay, Video, Audio) for organization
- Make sure to add the buttons (Apply and Cancel) with visible labels
- Don't worry about functionality - we'll wire it up with On Click listeners
