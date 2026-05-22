namespace SCARY.UI.Settings
{
    public interface ISettingsHandler
    {
        void LoadSettings(SettingsData data);
        void ApplySettings(SettingsData data);
        void RevertPendingChanges();
    }
}
