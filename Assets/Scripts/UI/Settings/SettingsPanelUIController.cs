using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SCARY.UI.Settings
{
    public class SettingsPanelUIController : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private SettingsManager settingsManager;
        [SerializeField] private GameplaySettingsHandler gameplayHandler;
        [SerializeField] private VideoSettingsHandler videoHandler;
        [SerializeField] private AudioSettingsHandler audioHandler;

        [Header("Gameplay UI")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Text mouseSensitivityValue;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private Text fovValue;

        [Header("Video UI")]
        [SerializeField] private Dropdown displayModeDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Slider fpsLimitSlider;
        [SerializeField] private Text fpsLimitValue;
        [SerializeField] private Dropdown graphicsQualityDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private Text brightnessValue;
        [SerializeField] private Slider gammaSlider;
        [SerializeField] private Text gammaValue;

        [Header("Audio UI")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeValue;
        [SerializeField] private Slider menuMusicVolumeSlider;
        [SerializeField] private Text menuMusicVolumeValue;
        [SerializeField] private Slider micInputVolumeSlider;
        [SerializeField] private Text micInputVolumeValue;
        [SerializeField] private Slider othersMicVolumeSlider;
        [SerializeField] private Text othersMicVolumeValue;
        [SerializeField] private Dropdown micInputTypeDropdown;
        [SerializeField] private Dropdown voiceChatTypeDropdown;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;

        private void OnEnable()
        {
            if (settingsManager == null)
                settingsManager = SettingsManager.Instance;

            if (settingsManager == null)
            {
                Debug.LogError("SettingsManager not found!");
                return;
            }

            settingsManager.RegisterHandler(gameplayHandler);
            settingsManager.RegisterHandler(videoHandler);
            settingsManager.RegisterHandler(audioHandler);

            InitializeUI();
            LoadCurrentSettings();
        }

        private void OnDisable()
        {
            if (settingsManager != null)
            {
                settingsManager.UnregisterHandler(gameplayHandler);
                settingsManager.UnregisterHandler(videoHandler);
                settingsManager.UnregisterHandler(audioHandler);
            }
        }

        private void InitializeUI()
        {
            SetupGameplayUI();
            SetupVideoUI();
            SetupAudioUI();
            SetupButtonListeners();
        }

        private void SetupGameplayUI()
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = 0.1f;
                mouseSensitivitySlider.maxValue = 3f;
                mouseSensitivitySlider.onValueChanged.AddListener(value =>
                {
                    gameplayHandler.SetMouseSensitivity(value);
                    UpdateMouseSensitivityDisplay(value);
                });
            }

            if (fovSlider != null)
            {
                fovSlider.minValue = 40f;
                fovSlider.maxValue = 110f;
                fovSlider.onValueChanged.AddListener(value =>
                {
                    gameplayHandler.SetFOV(value);
                    UpdateFOVDisplay(value);
                });
            }
        }

        private void SetupVideoUI()
        {
            if (displayModeDropdown != null)
            {
                displayModeDropdown.ClearOptions();
                displayModeDropdown.AddOptions(new List<string> { "Windowed", "Fullscreen", "Borderless" });
                displayModeDropdown.onValueChanged.AddListener(value =>
                {
                    videoHandler.SetDisplayMode(value);
                });
            }

            if (resolutionDropdown != null)
            {
                UpdateResolutionDropdown();
                resolutionDropdown.onValueChanged.AddListener(value =>
                {
                    videoHandler.SetResolution(value);
                });
            }

            if (fpsLimitSlider != null)
            {
                fpsLimitSlider.minValue = 30f;
                fpsLimitSlider.maxValue = 240f;
                fpsLimitSlider.wholeNumbers = true;
                fpsLimitSlider.onValueChanged.AddListener(value =>
                {
                    videoHandler.SetFPSLimit((int)value);
                    UpdateFPSLimitDisplay((int)value);
                });
            }

            if (graphicsQualityDropdown != null)
            {
                graphicsQualityDropdown.ClearOptions();
                graphicsQualityDropdown.AddOptions(new List<string> { "Low", "Medium", "High" });
                graphicsQualityDropdown.onValueChanged.AddListener(value =>
                {
                    videoHandler.SetGraphicsQuality(value);
                });
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.onValueChanged.AddListener(value =>
                {
                    videoHandler.SetVSyncEnabled(value);
                });
            }

            if (brightnessSlider != null)
            {
                brightnessSlider.minValue = 0.3f;
                brightnessSlider.maxValue = 2f;
                brightnessSlider.onValueChanged.AddListener(value =>
                {
                    videoHandler.SetBrightness(value);
                    UpdateBrightnessDisplay(value);
                });
            }

            if (gammaSlider != null)
            {
                gammaSlider.minValue = 0.5f;
                gammaSlider.maxValue = 2.5f;
                gammaSlider.onValueChanged.AddListener(value =>
                {
                    videoHandler.SetGamma(value);
                    UpdateGammaDisplay(value);
                });
            }
        }

        private void SetupAudioUI()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
                masterVolumeSlider.onValueChanged.AddListener(value =>
                {
                    audioHandler.SetMasterVolume(value);
                    UpdateMasterVolumeDisplay(value);
                });
            }

            if (menuMusicVolumeSlider != null)
            {
                menuMusicVolumeSlider.minValue = 0f;
                menuMusicVolumeSlider.maxValue = 1f;
                menuMusicVolumeSlider.onValueChanged.AddListener(value =>
                {
                    audioHandler.SetMenuMusicVolume(value);
                    UpdateMenuMusicVolumeDisplay(value);
                });
            }

            if (micInputVolumeSlider != null)
            {
                micInputVolumeSlider.minValue = 0f;
                micInputVolumeSlider.maxValue = 1f;
                micInputVolumeSlider.onValueChanged.AddListener(value =>
                {
                    audioHandler.SetMicInputVolume(value);
                    UpdateMicInputVolumeDisplay(value);
                });
            }

            if (othersMicVolumeSlider != null)
            {
                othersMicVolumeSlider.minValue = 0f;
                othersMicVolumeSlider.maxValue = 1f;
                othersMicVolumeSlider.onValueChanged.AddListener(value =>
                {
                    audioHandler.SetOthersMicOutputVolume(value);
                    UpdateOthersMicVolumeDisplay(value);
                });
            }

            if (micInputTypeDropdown != null)
            {
                UpdateMicDevicesDropdown();
                micInputTypeDropdown.onValueChanged.AddListener(value =>
                {
                    audioHandler.SetMicInputType(value);
                });
            }

            if (voiceChatTypeDropdown != null)
            {
                voiceChatTypeDropdown.ClearOptions();
                voiceChatTypeDropdown.AddOptions(new List<string> { "Push-to-Talk", "Open Mic" });
                voiceChatTypeDropdown.onValueChanged.AddListener(value =>
                {
                    audioHandler.SetVoiceChatType(value);
                });
            }
        }

        private void SetupButtonListeners()
        {
            if (applyButton != null)
                applyButton.onClick.AddListener(() => ApplySettings());

            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => RevertSettings());
        }

        private void LoadCurrentSettings()
        {
            settingsManager.LoadSettings();

            // Load Gameplay
            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.value = gameplayHandler.GetMouseSensitivity();
            if (fovSlider != null)
                fovSlider.value = gameplayHandler.GetFOV();

            // Load Video
            if (displayModeDropdown != null)
                displayModeDropdown.value = videoHandler.GetDisplayMode();
            if (resolutionDropdown != null)
                resolutionDropdown.value = videoHandler.GetResolutionIndex();
            if (fpsLimitSlider != null)
                fpsLimitSlider.value = videoHandler.GetFPSLimit();
            if (graphicsQualityDropdown != null)
                graphicsQualityDropdown.value = videoHandler.GetGraphicsQuality();
            if (vsyncToggle != null)
                vsyncToggle.isOn = videoHandler.GetVSyncEnabled();
            if (brightnessSlider != null)
                brightnessSlider.value = videoHandler.GetBrightness();
            if (gammaSlider != null)
                gammaSlider.value = videoHandler.GetGamma();

            // Load Audio
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = audioHandler.GetMasterVolume();
            if (menuMusicVolumeSlider != null)
                menuMusicVolumeSlider.value = audioHandler.GetMenuMusicVolume();
            if (micInputVolumeSlider != null)
                micInputVolumeSlider.value = audioHandler.GetMicInputVolume();
            if (othersMicVolumeSlider != null)
                othersMicVolumeSlider.value = audioHandler.GetOthersMicOutputVolume();
            if (micInputTypeDropdown != null)
                micInputTypeDropdown.value = audioHandler.GetMicInputType();
            if (voiceChatTypeDropdown != null)
                voiceChatTypeDropdown.value = audioHandler.GetVoiceChatType();
        }

        private void UpdateResolutionDropdown()
        {
            if (resolutionDropdown == null) return;

            resolutionDropdown.ClearOptions();
            Resolution[] resolutions = videoHandler.GetAvailableResolutions();
            List<string> options = new List<string>();

            foreach (var res in resolutions)
            {
                options.Add($"{res.width}x{res.height}");
            }

            resolutionDropdown.AddOptions(options);
        }

        private void UpdateMicDevicesDropdown()
        {
            if (micInputTypeDropdown == null) return;

            micInputTypeDropdown.ClearOptions();
            string[] devices = audioHandler.GetMicDeviceNames();
            List<string> options = new List<string>(devices.Length);

            foreach (var device in devices)
            {
                options.Add(device);
            }

            if (options.Count == 0)
                options.Add("No Microphones Found");

            micInputTypeDropdown.AddOptions(options);
        }

        private void UpdateMouseSensitivityDisplay(float value)
        {
            if (mouseSensitivityValue != null)
                mouseSensitivityValue.text = value.ToString("F2");
        }

        private void UpdateFOVDisplay(float value)
        {
            if (fovValue != null)
                fovValue.text = value.ToString("F0");
        }

        private void UpdateFPSLimitDisplay(int value)
        {
            if (fpsLimitValue != null)
                fpsLimitValue.text = value.ToString();
        }

        private void UpdateBrightnessDisplay(float value)
        {
            if (brightnessValue != null)
                brightnessValue.text = (value * 100).ToString("F0");
        }

        private void UpdateGammaDisplay(float value)
        {
            if (gammaValue != null)
                gammaValue.text = value.ToString("F2");
        }

        private void UpdateMasterVolumeDisplay(float value)
        {
            if (masterVolumeValue != null)
                masterVolumeValue.text = (value * 100).ToString("F0");
        }

        private void UpdateMenuMusicVolumeDisplay(float value)
        {
            if (menuMusicVolumeValue != null)
                menuMusicVolumeValue.text = (value * 100).ToString("F0");
        }

        private void UpdateMicInputVolumeDisplay(float value)
        {
            if (micInputVolumeValue != null)
                micInputVolumeValue.text = (value * 100).ToString("F0");
        }

        private void UpdateOthersMicVolumeDisplay(float value)
        {
            if (othersMicVolumeValue != null)
                othersMicVolumeValue.text = (value * 100).ToString("F0");
        }

        public void ApplySettings()
        {
            settingsManager.ApplySettings();
            Debug.Log("Settings applied!");
        }

        public void RevertSettings()
        {
            settingsManager.RevertSettings();
            LoadCurrentSettings();
            Debug.Log("Settings reverted!");
        }
    }
}
