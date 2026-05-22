using UnityEditor;
using UnityEngine;

namespace SCARY.UI.Settings.EditorTools
{
    [CustomEditor(typeof(SettingsManager))]
    public class SettingsManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SettingsManager manager = (SettingsManager)target;
            SettingsData data = manager.GetSettingsData();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Dev Tools", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                $"Save file: {SettingsPersistence.FilePath}\n" +
                $"Exists: {(SettingsPersistence.FileExists() ? "Yes" : "No (created on first Apply)")}",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(data == null))
            {
                if (GUILayout.Button("Save to JSON Now"))
                {
                    SettingsPersistence.Save(data);
                    Debug.Log($"[Settings] Saved to: {SettingsPersistence.FilePath}");
                }

                if (GUILayout.Button("Reload from JSON"))
                {
                    if (SettingsPersistence.Load(data))
                    {
                        EditorUtility.SetDirty(data);
                        Debug.Log("[Settings] Reloaded from disk.");
                    }
                    else
                    {
                        Debug.Log("[Settings] No save file found.");
                    }
                }
            }

            if (GUILayout.Button("Open Save File Location"))
            {
                string folder = SettingsPersistence.FolderPath;
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);
                EditorUtility.RevealInFinder(folder);
            }

            using (new EditorGUI.DisabledScope(!SettingsPersistence.FileExists()))
            {
                if (GUILayout.Button("Delete Save File"))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Delete Save File?",
                        "This permanently deletes the JSON save. Next launch will use default settings from the SettingsData asset.",
                        "Delete",
                        "Cancel");

                    if (confirm)
                    {
                        SettingsPersistence.DeleteSaveFile();
                        Debug.Log("[Settings] Save file deleted.");
                    }
                }
            }

            using (new EditorGUI.DisabledScope(data == null))
            {
                if (GUILayout.Button("Reset SettingsData to Defaults"))
                {
                    data.ResetToDefaults();
                    EditorUtility.SetDirty(data);
                    Debug.Log("[Settings] ScriptableObject reset to defaults.");
                }
            }
        }
    }
}
