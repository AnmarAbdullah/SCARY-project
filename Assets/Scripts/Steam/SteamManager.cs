// Canonical SteamManager from Steamworks.NET (rlabrecque) — adapted for SCARY-project.
// This is the standard init/shutdown singleton. Do not put more than one in the scene.
// Reference: https://steamworks.github.io/installation/

using UnityEngine;
using System;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

[DisallowMultipleComponent]
public class SteamManager : MonoBehaviour
{
#if !DISABLESTEAMWORKS
    protected static bool s_EverInitialized = false;

    protected static SteamManager s_instance;
    protected static SteamManager Instance => s_instance;

    protected bool m_bInitialized = false;
    public static bool Initialized => Instance != null && Instance.m_bInitialized;

    protected SteamAPIWarningMessageHook_t m_SteamAPIWarningMessageHook;

    protected static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
    {
        Debug.LogWarning(pchDebugText);
    }

#if UNITY_2019_3_OR_NEWER
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitOnPlayMode()
    {
        s_EverInitialized = false;
        s_instance = null;
    }
#endif

    protected virtual void Awake()
    {
        if (s_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        s_instance = this;

        if (s_EverInitialized)
        {
            throw new Exception("Tried to Initialize the SteamAPI twice in one session.");
        }

        DontDestroyOnLoad(gameObject);

        if (!Packsize.Test())
        {
            Debug.LogError("[Steamworks.NET] Packsize Test returned false, native size doesn't match managed size.", this);
        }

        if (!DllCheck.Test())
        {
            Debug.LogError("[Steamworks.NET] DllCheck Test returned false, native DLL versions don't match wrapper.", this);
        }

        try
        {
            // Restart through Steam if launched outside it (only matters in shipped builds with a real App ID).
            if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
            {
                Application.Quit();
                return;
            }
        }
        catch (DllNotFoundException e)
        {
            Debug.LogError("[Steamworks.NET] Could not load steam_api.dll/so/dylib. Refer to README for details.\n" + e, this);
            Application.Quit();
            return;
        }

        m_bInitialized = SteamAPI.Init();
        if (!m_bInitialized)
        {
            Debug.LogError("[Steamworks.NET] SteamAPI_Init() failed. Is Steam running? Is steam_appid.txt present in the project root?", this);
            return;
        }

        s_EverInitialized = true;
    }

    protected virtual void OnEnable()
    {
        if (s_instance == null) s_instance = this;
        if (!m_bInitialized) return;
        if (m_SteamAPIWarningMessageHook == null)
        {
            m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
            SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
        }
    }

    protected virtual void OnDestroy()
    {
        if (s_instance != this) return;
        s_instance = null;
        if (!m_bInitialized) return;
        SteamAPI.Shutdown();
    }

    protected virtual void Update()
    {
        if (!m_bInitialized) return;
        SteamAPI.RunCallbacks();
    }
#else
    public static bool Initialized => false;
#endif
}
