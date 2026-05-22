# Settings System Architecture

## File Structure

```
Assets/Scripts/UI/Settings/
├── README.md                          # Full documentation
├── SETUP_CHECKLIST.md                 # Step-by-step setup guide
├── ARCHITECTURE.md                    # This file
├── SettingsData.cs                    # ScriptableObject for persisting settings
├── ISettingsHandler.cs                # Interface for all handlers
├── SettingsManager.cs                 # Singleton coordinator
├── GameplaySettingsHandler.cs         # Gameplay settings logic
├── VideoSettingsHandler.cs            # Video settings logic
├── AudioSettingsHandler.cs            # Audio settings logic (Dissonance integrated)
└── SettingsPanelUIController.cs       # Main UI controller for dependency injection
```

Updated files:
- `Assets/Scripts/Player/FPSController.cs` - Added SetMouseSensitivity() and SetFOV() methods

## Component Relationships

```
SettingsManager (Singleton)
├── Manages: SettingsData (ScriptableObject)
├── Coordinates: GameplaySettingsHandler
│                VideoSettingsHandler
│                AudioSettingsHandler
│
└── SettingsPanelUIController
    ├── Injects: UI References (Sliders, Toggles, Dropdowns, Buttons)
    ├── Calls: Handler methods when UI changes
    ├── Calls: SettingsManager.ApplySettings() on Apply button
    └── Calls: SettingsManager.RevertSettings() on Cancel button
```

## Data Flow: Setting a Value

```
1. Player moves Slider (e.g., MouseSensitivity)
   ↓
2. SettingsPanelUIController.mouseSlider.onValueChanged fires
   ↓
3. gameplayHandler.SetMouseSensitivity(value) called
   ↓
4. Handler stores value in pendingChanges variable
   ↓
5. SettingsPanelUIController updates display text
```

## Data Flow: Applying Settings

```
1. Player clicks "Apply" button
   ↓
2. SettingsPanelUIController.ApplySettings() called
   ↓
3. SettingsManager.ApplySettings() called
   ↓
4. For each handler:
   - handler.ApplySettings(data) called
   - Handler applies to game (e.g., fpsController.SetFOV())
   - Handler updates SettingsData values
   - Handler copies pending → applied values
   ↓
5. SettingsManager.SaveSettings() called
   ↓
6. SettingsData asset is marked dirty and saved
```

## Data Flow: Reverting Changes

```
1. Player clicks "Cancel" button
   ↓
2. SettingsPanelUIController.RevertSettings() called
   ↓
3. SettingsManager.RevertSettings() called
   ↓
4. For each handler:
   - handler.RevertPendingChanges() called
   - Handler copies applied → pending (discards changes)
   ↓
5. SettingsPanelUIController.LoadCurrentSettings() called
   ↓
6. UI sliders/toggles are reset to applied values
```

## State Model

Each handler maintains two states:

```
┌─────────────────────────────────────┐
│      APPLIED SETTINGS               │  ← Currently active in game
│   (Last time user clicked Apply)    │
└─────────────────────────────────────┘
            ↓ ↑
    Load/Save/Revert
            ↓ ↑
┌─────────────────────────────────────┐
│     PENDING SETTINGS                │  ← User is editing
│  (Changes not yet applied)          │
└─────────────────────────────────────┘
            ↓
      User clicks Apply
            ↓
     Copies to Applied
```

## Handler Responsibilities

### GameplaySettingsHandler
```
- Stores: mouseSensitivity, fov (pending & applied)
- Applies to: FPSController
- Methods: SetMouseSensitivity(), SetFOV()
           ApplySettings() calls FPSController.SetFOV()
```

### VideoSettingsHandler
```
- Stores: displayMode, resolutionIndex, fpsLimit, 
          graphicsQuality, vsyncEnabled, brightness, gamma
          
- Applies to: 
  - Screen.SetResolution() for display settings
  - Application.targetFrameRate for FPS
  - QualitySettings for graphics and V-sync
  - SettingsData stores brightness/gamma for post-processing
  
- Methods: SetDisplayMode(), SetResolution(), SetFPSLimit(),
           SetGraphicsQuality(), SetVSyncEnabled(),
           SetBrightness(), SetGamma()
```

### AudioSettingsHandler
```
- Stores: masterVolume, menuMusicVolume, micInputVolume,
          othersMicOutputVolume, micInputType, voiceChatType
          
- Applies to:
  - AudioListener.volume for master volume
  - DissonanceComms for mic volumes
  - DissonanceComms for mic device selection
  - VoiceActivationInput for push-to-talk mode
  
- Methods: SetMasterVolume(), SetMenuMusicVolume(),
           SetMicInputVolume(), SetOthersMicOutputVolume(),
           SetMicInputType(), SetVoiceChatType()
```

## SettingsData Structure

```csharp
SettingsData (ScriptableObject)
├── GameplaySettings
│   ├── mouseSensitivity (float)
│   └── fov (float)
├── VideoSettings
│   ├── displayMode (int: 0=Windowed, 1=Fullscreen, 2=Borderless)
│   ├── resolutionIndex (int)
│   ├── fpsLimit (int)
│   ├── graphicsQuality (int: 0=Low, 1=Med, 2=High)
│   ├── vsyncEnabled (bool)
│   ├── brightness (float)
│   └── gamma (float)
└── AudioSettings
    ├── masterVolume (float)
    ├── menuMusicVolume (float)
    ├── micInputVolume (float)
    ├── othersMicOutputVolume (float)
    ├── micInputType (int: device index)
    └── voiceChatType (int: 0=Push-to-Talk, 1=Open Mic)
```

## UI Injection Pattern

SettingsPanelUIController uses constructor injection via inspector:

```csharp
[SerializeField] private SettingsManager settingsManager;
[SerializeField] private GameplaySettingsHandler gameplayHandler;
[SerializeField] private VideoSettingsHandler videoHandler;
[SerializeField] private AudioSettingsHandler audioHandler;
[SerializeField] private Slider mouseSensitivitySlider;
// ... more UI elements
```

**Why this pattern?**
- Coworker builds the UI scene without writing code
- We attach the script and drag references in inspector
- No tight coupling between script and scene
- Easy to test and refactor

## Integration Points

### FPSController Integration
```
FPSController.cs
├── SetMouseSensitivity(float) ← Called by GameplaySettingsHandler
└── SetFOV(float) ← Called by GameplaySettingsHandler
```

### Dissonance Integration
```
DissonanceComms (built-in Dissonance component)
├── MicrophoneVolume ← Set by AudioSettingsHandler
├── PlaybackVolume ← Set by AudioSettingsHandler
├── MicrophoneName ← Set by AudioSettingsHandler
└── VoiceActivationInput component
    └── enabled ← Toggled by AudioSettingsHandler for push-to-talk
```

## Extension Points

To add new settings:

1. **Add to SettingsData:**
   ```csharp
   public class VideoSettings
   {
       // ... existing fields
       public float newSetting = 0.5f; // Add here
   }
   ```

2. **Add to Handler:**
   ```csharp
   public void SetNewSetting(float value) { /* ... */ }
   
   public void ApplySettings(SettingsData data)
   {
       // ... existing code
       data.video.newSetting = pendingChanges.newSetting;
       // Apply to game
   }
   ```

3. **Add to UI Controller:**
   ```csharp
   [SerializeField] private Slider newSettingSlider;
   
   private void SetupVideoUI()
   {
       // ... existing code
       newSettingSlider.onValueChanged.AddListener(v => 
           videoHandler.SetNewSetting(v));
   }
   
   private void LoadCurrentSettings()
   {
       // ... existing code
       newSettingSlider.value = videoHandler.GetNewSetting();
   }
   ```

## Persistence Strategy

Settings are persisted via the SettingsData ScriptableObject:

```
Game Session 1
└── User sets Mouse Sensitivity to 1.5
    └── Clicks Apply
        └── SettingsData.gameplay.mouseSensitivity = 1.5
            └── Asset marked dirty and saved to disk

Game Session 2
└── SettingsManager loads SettingsData
    └── SettingsData.gameplay.mouseSensitivity = 1.5 (restored)
        └── Handlers load the saved value
            └── UI shows 1.5 on startup
```

No JSON/PlayerPrefs needed - ScriptableObject handles all persistence.

## Key Design Decisions

1. **Pending vs Applied State**: Separates "what the user is editing" from "what's active in game"
2. **Handler Pattern**: Each category has its own handler → easy to maintain and extend
3. **UI Injection**: Coworker builds UI without touching code
4. **Singleton SettingsManager**: One place to coordinate all settings
5. **No Direct UI-to-Game**: UI never directly modifies game - goes through handlers
6. **ScriptableObject Persistence**: Simple, reliable, works in editor and builds

## Testing Checklist

- [ ] Load game → settings match saved values
- [ ] Change slider → display updates (before Apply)
- [ ] Click Apply → setting applies to game
- [ ] Click Cancel → reverts to last applied value
- [ ] Restart game → setting persists
- [ ] All three handlers work together
