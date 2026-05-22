using UnityEngine;
using System.Collections.Generic;

namespace SCARY.UI.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [SerializeField] private SettingsData settingsData;

        private List<ISettingsHandler> handlers = new List<ISettingsHandler>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (settingsData == null)
            {
                settingsData = Resources.Load<SettingsData>("Settings/SettingsData");
                if (settingsData == null)
                {
                    Debug.LogError("SettingsData not found! Create one at Assets/Resources/Settings/SettingsData.asset");
                    settingsData = ScriptableObject.CreateInstance<SettingsData>();
                }
            }

            // Load persisted JSON values into the in-memory ScriptableObject.
            // If no save file exists yet (first launch), the SO keeps its default values.
            SettingsPersistence.Load(settingsData);
        }

        private void Start()
        {
            LoadSettings();
        }

        public void RegisterHandler(ISettingsHandler handler)
        {
            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        public void UnregisterHandler(ISettingsHandler handler)
        {
            handlers.Remove(handler);
        }

        public void LoadSettings()
        {
            foreach (var handler in handlers)
            {
                handler.LoadSettings(settingsData);
            }
        }

        public void ApplySettings()
        {
            foreach (var handler in handlers)
            {
                handler.ApplySettings(settingsData);
            }
            SaveSettings();
        }

        public void RevertSettings()
        {
            foreach (var handler in handlers)
            {
                handler.RevertPendingChanges();
            }
        }

        public void SaveSettings()
        {
            SettingsPersistence.Save(settingsData);
        }

        public SettingsData GetSettingsData() => settingsData;
    }
}
