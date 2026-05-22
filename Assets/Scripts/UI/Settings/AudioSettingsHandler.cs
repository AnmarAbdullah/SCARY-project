using UnityEngine;
using Dissonance;

namespace SCARY.UI.Settings
{
    public class AudioSettingsHandler : MonoBehaviour, ISettingsHandler
    {
        private SettingsData.AudioSettings pendingChanges;
        private SettingsData.AudioSettings appliedSettings;
        private DissonanceComms dissonanceComms;
        private string[] micDeviceNames;

        private void Start()
        {
            dissonanceComms = FindObjectOfType<DissonanceComms>();
            RefreshMicDevices();
        }

        public void LoadSettings(SettingsData data)
        {
            pendingChanges = CopyAudio(data.audio);
            appliedSettings = CopyAudio(data.audio);
        }

        public void SetMasterVolume(float value)
        {
            pendingChanges.masterVolume = Mathf.Clamp01(value);
        }

        public void SetMenuMusicVolume(float value)
        {
            pendingChanges.menuMusicVolume = Mathf.Clamp01(value);
        }

        public void SetMicInputVolume(float value)
        {
            pendingChanges.micInputVolume = Mathf.Clamp01(value);
        }

        public void SetOthersMicOutputVolume(float value)
        {
            pendingChanges.othersMicOutputVolume = Mathf.Clamp01(value);
        }

        public void SetMicInputType(int deviceIndex)
        {
            if (deviceIndex >= 0 && deviceIndex < Microphone.devices.Length)
            {
                pendingChanges.micInputType = deviceIndex;
            }
        }

        public void SetVoiceChatType(int type)
        {
            pendingChanges.voiceChatType = Mathf.Clamp(type, 0, 1);
        }

        public void ApplySettings(SettingsData data)
        {
            data.audio.masterVolume = pendingChanges.masterVolume;
            data.audio.menuMusicVolume = pendingChanges.menuMusicVolume;
            data.audio.micInputVolume = pendingChanges.micInputVolume;
            data.audio.othersMicOutputVolume = pendingChanges.othersMicOutputVolume;
            data.audio.micInputType = pendingChanges.micInputType;
            data.audio.voiceChatType = pendingChanges.voiceChatType;

            AudioListener.volume = pendingChanges.masterVolume;
            ApplyMicMute();
            ApplyOthersMicOutputVolume();
            ApplyMicInputType();
            ApplyVoiceChatType();

            appliedSettings = CopyAudio(data.audio);
        }

        public void RevertPendingChanges()
        {
            pendingChanges = new SettingsData.AudioSettings
            {
                masterVolume = appliedSettings.masterVolume,
                menuMusicVolume = appliedSettings.menuMusicVolume,
                micInputVolume = appliedSettings.micInputVolume,
                othersMicOutputVolume = appliedSettings.othersMicOutputVolume,
                micInputType = appliedSettings.micInputType,
                voiceChatType = appliedSettings.voiceChatType
            };
        }

        // Dissonance has no direct mic gain API. Treat 0 as muted; non-zero as unmuted.
        // Wire to an AudioMixer or custom IMicrophoneSubscriber later for true gain control.
        private void ApplyMicMute()
        {
            if (dissonanceComms != null)
            {
                dissonanceComms.IsMuted = pendingChanges.micInputVolume <= 0.001f;
            }
        }

        private void ApplyOthersMicOutputVolume()
        {
            if (dissonanceComms != null)
            {
                dissonanceComms.RemoteVoiceVolume = pendingChanges.othersMicOutputVolume;
            }
        }

        private void ApplyMicInputType()
        {
            if (dissonanceComms != null && pendingChanges.micInputType < Microphone.devices.Length)
            {
                dissonanceComms.MicrophoneName = Microphone.devices[pendingChanges.micInputType];
            }
        }

        private void ApplyVoiceChatType()
        {
            var trigger = FindObjectOfType<VoiceBroadcastTrigger>();
            if (trigger != null)
            {
                trigger.Mode = pendingChanges.voiceChatType == 0
                    ? CommActivationMode.PushToTalk
                    : CommActivationMode.VoiceActivation;
            }
        }

        private void RefreshMicDevices()
        {
            micDeviceNames = Microphone.devices;
        }

        private SettingsData.AudioSettings CopyAudio(SettingsData.AudioSettings source)
        {
            return new SettingsData.AudioSettings
            {
                masterVolume = source.masterVolume,
                menuMusicVolume = source.menuMusicVolume,
                micInputVolume = source.micInputVolume,
                othersMicOutputVolume = source.othersMicOutputVolume,
                micInputType = source.micInputType,
                voiceChatType = source.voiceChatType
            };
        }

        public float GetMasterVolume() => pendingChanges.masterVolume;
        public float GetMenuMusicVolume() => pendingChanges.menuMusicVolume;
        public float GetMicInputVolume() => pendingChanges.micInputVolume;
        public float GetOthersMicOutputVolume() => pendingChanges.othersMicOutputVolume;
        public int GetMicInputType() => pendingChanges.micInputType;
        public int GetVoiceChatType() => pendingChanges.voiceChatType;

        public string[] GetMicDeviceNames() => micDeviceNames;
        public string GetMicDeviceName(int index) =>
            index >= 0 && index < micDeviceNames.Length ? micDeviceNames[index] : "Default";
    }
}
