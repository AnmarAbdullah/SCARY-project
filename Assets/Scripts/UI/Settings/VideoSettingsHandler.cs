using UnityEngine;

namespace SCARY.UI.Settings
{
    public class VideoSettingsHandler : MonoBehaviour, ISettingsHandler
    {
        private SettingsData.VideoSettings pendingChanges;
        private SettingsData.VideoSettings appliedSettings;
        private Resolution[] availableResolutions;

        private void Start()
        {
            availableResolutions = Screen.resolutions;
        }

        public void LoadSettings(SettingsData data)
        {
            pendingChanges = new SettingsData.VideoSettings
            {
                displayMode = data.video.displayMode,
                resolutionIndex = data.video.resolutionIndex,
                fpsLimit = data.video.fpsLimit,
                graphicsQuality = data.video.graphicsQuality,
                vsyncEnabled = data.video.vsyncEnabled,
                brightness = data.video.brightness,
                gamma = data.video.gamma
            };
            appliedSettings = new SettingsData.VideoSettings
            {
                displayMode = data.video.displayMode,
                resolutionIndex = data.video.resolutionIndex,
                fpsLimit = data.video.fpsLimit,
                graphicsQuality = data.video.graphicsQuality,
                vsyncEnabled = data.video.vsyncEnabled,
                brightness = data.video.brightness,
                gamma = data.video.gamma
            };
        }

        public void SetDisplayMode(int mode)
        {
            pendingChanges.displayMode = mode;
        }

        public void SetResolution(int index)
        {
            if (index >= 0 && index < availableResolutions.Length)
                pendingChanges.resolutionIndex = index;
        }

        public void SetFPSLimit(int fps)
        {
            pendingChanges.fpsLimit = Mathf.Max(30, fps);
        }

        public void SetGraphicsQuality(int quality)
        {
            pendingChanges.graphicsQuality = Mathf.Clamp(quality, 0, 2);
        }

        public void SetVSyncEnabled(bool enabled)
        {
            pendingChanges.vsyncEnabled = enabled;
        }

        public void SetBrightness(float value)
        {
            pendingChanges.brightness = Mathf.Clamp01(value);
        }

        public void SetGamma(float value)
        {
            pendingChanges.gamma = Mathf.Clamp(value, 0.5f, 2.5f);
        }

        public void ApplySettings(SettingsData data)
        {
            // Update the data
            data.video.displayMode = pendingChanges.displayMode;
            data.video.resolutionIndex = pendingChanges.resolutionIndex;
            data.video.fpsLimit = pendingChanges.fpsLimit;
            data.video.graphicsQuality = pendingChanges.graphicsQuality;
            data.video.vsyncEnabled = pendingChanges.vsyncEnabled;
            data.video.brightness = pendingChanges.brightness;
            data.video.gamma = pendingChanges.gamma;

            // Apply display mode
            FullScreenMode mode = (FullScreenMode)pendingChanges.displayMode;
            if (pendingChanges.resolutionIndex < availableResolutions.Length)
            {
                Resolution res = availableResolutions[pendingChanges.resolutionIndex];
                Screen.SetResolution(res.width, res.height, mode);
            }

            // Apply FPS limit
            Application.targetFrameRate = pendingChanges.fpsLimit;

            // Apply graphics quality
            QualitySettings.SetQualityLevel(pendingChanges.graphicsQuality);

            // Apply V-Sync
            QualitySettings.vSyncCount = pendingChanges.vsyncEnabled ? 1 : 0;

            // Apply brightness/gamma (stored in data, UI controller will update the post-processing)
            appliedSettings = new SettingsData.VideoSettings
            {
                displayMode = pendingChanges.displayMode,
                resolutionIndex = pendingChanges.resolutionIndex,
                fpsLimit = pendingChanges.fpsLimit,
                graphicsQuality = pendingChanges.graphicsQuality,
                vsyncEnabled = pendingChanges.vsyncEnabled,
                brightness = pendingChanges.brightness,
                gamma = pendingChanges.gamma
            };
        }

        public void RevertPendingChanges()
        {
            pendingChanges = new SettingsData.VideoSettings
            {
                displayMode = appliedSettings.displayMode,
                resolutionIndex = appliedSettings.resolutionIndex,
                fpsLimit = appliedSettings.fpsLimit,
                graphicsQuality = appliedSettings.graphicsQuality,
                vsyncEnabled = appliedSettings.vsyncEnabled,
                brightness = appliedSettings.brightness,
                gamma = appliedSettings.gamma
            };
        }

        public int GetDisplayMode() => pendingChanges.displayMode;
        public int GetResolutionIndex() => pendingChanges.resolutionIndex;
        public int GetFPSLimit() => pendingChanges.fpsLimit;
        public int GetGraphicsQuality() => pendingChanges.graphicsQuality;
        public bool GetVSyncEnabled() => pendingChanges.vsyncEnabled;
        public float GetBrightness() => pendingChanges.brightness;
        public float GetGamma() => pendingChanges.gamma;

        public Resolution[] GetAvailableResolutions() => availableResolutions;
        public string GetResolutionString(int index) =>
            index < availableResolutions.Length
                ? $"{availableResolutions[index].width}x{availableResolutions[index].height}"
                : "Unknown";
    }
}
