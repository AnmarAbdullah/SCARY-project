using UnityEngine;

namespace SCARY.UI.Settings
{
    public class GameplaySettingsHandler : MonoBehaviour, ISettingsHandler
    {
        private SettingsData.GameplaySettings pendingChanges;
        private SettingsData.GameplaySettings appliedSettings;

        public void LoadSettings(SettingsData data)
        {
            pendingChanges = Copy(data.gameplay);
            appliedSettings = Copy(data.gameplay);
        }

        public void SetMouseSensitivity(float value)
        {
            pendingChanges.mouseSensitivity = value;
        }

        public void SetFOV(float value)
        {
            pendingChanges.fov = value;
        }

        public void ApplySettings(SettingsData data)
        {
            data.gameplay.mouseSensitivity = pendingChanges.mouseSensitivity;
            data.gameplay.fov = pendingChanges.fov;

            // Apply to the LOCAL player if one exists. In the main menu (no player
            // spawned yet), this no-ops -- the player will pull these values in
            // OnStartLocalPlayer when it spawns.
            FPSController localPlayer = FindLocalFPSController();
            if (localPlayer != null)
            {
                localPlayer.SetMouseSensitivity(pendingChanges.mouseSensitivity);
                localPlayer.SetFOV(pendingChanges.fov);
            }

            appliedSettings = Copy(pendingChanges);
        }

        public void RevertPendingChanges()
        {
            pendingChanges = Copy(appliedSettings);
        }

        private FPSController FindLocalFPSController()
        {
            FPSController[] all = FindObjectsOfType<FPSController>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].isLocalPlayer) return all[i];
            }
            return null;
        }

        private SettingsData.GameplaySettings Copy(SettingsData.GameplaySettings source)
        {
            return new SettingsData.GameplaySettings
            {
                mouseSensitivity = source.mouseSensitivity,
                fov = source.fov
            };
        }

        public float GetMouseSensitivity() => pendingChanges.mouseSensitivity;
        public float GetFOV() => pendingChanges.fov;
    }
}
