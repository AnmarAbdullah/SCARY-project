using UnityEngine;

namespace SCARY.UI.Settings
{
    [CreateAssetMenu(fileName = "SettingsData", menuName = "SCARY/Settings/Settings Data")]
    public class SettingsData : ScriptableObject
    {
        [System.Serializable]
        public class GameplaySettings
        {
            public float mouseSensitivity = 1f;
            public float fov = 60f;
        }

        [System.Serializable]
        public class VideoSettings
        {
            public int displayMode = 0; // 0: Windowed, 1: Fullscreen, 2: Borderless
            public int resolutionIndex = 0;
            public int fpsLimit = 60;
            public int graphicsQuality = 1; // 0: Low, 1: Medium, 2: High
            public bool vsyncEnabled = true;
            public float brightness = 1f;
            public float gamma = 1f;
        }

        [System.Serializable]
        public class AudioSettings
        {
            public float masterVolume = 1f;
            public float menuMusicVolume = 0.7f;
            public float micInputVolume = 1f;
            public float othersMicOutputVolume = 1f;
            public int micInputType = 0; // Index into available devices
            public int voiceChatType = 0; // 0: Push-to-Talk, 1: Open Mic
        }

        public GameplaySettings gameplay = new GameplaySettings();
        public VideoSettings video = new VideoSettings();
        public AudioSettings audio = new AudioSettings();

        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            gameplay = new GameplaySettings();
            video = new VideoSettings();
            audio = new AudioSettings();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
