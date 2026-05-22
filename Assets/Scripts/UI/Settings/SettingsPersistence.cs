using System.IO;
using UnityEngine;

namespace SCARY.UI.Settings
{
    public static class SettingsPersistence
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "SettingsData.json";

        public static string FolderPath => Path.Combine(Application.persistentDataPath, SETTINGS_FOLDER);
        public static string FilePath => Path.Combine(FolderPath, SETTINGS_FILE);

        public static bool FileExists() => File.Exists(FilePath);

        public static void Save(SettingsData data)
        {
            if (data == null)
            {
                Debug.LogWarning("SettingsPersistence.Save: data is null.");
                return;
            }

            try
            {
                if (!Directory.Exists(FolderPath))
                    Directory.CreateDirectory(FolderPath);

                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SettingsPersistence.Save failed: {e.Message}");
            }
        }

        public static bool Load(SettingsData target)
        {
            if (target == null)
            {
                Debug.LogWarning("SettingsPersistence.Load: target is null.");
                return false;
            }

            if (!FileExists())
                return false;

            try
            {
                string json = File.ReadAllText(FilePath);
                JsonUtility.FromJsonOverwrite(json, target);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SettingsPersistence.Load failed: {e.Message}");
                return false;
            }
        }

        public static void DeleteSaveFile()
        {
            if (FileExists())
                File.Delete(FilePath);
        }
    }
}
