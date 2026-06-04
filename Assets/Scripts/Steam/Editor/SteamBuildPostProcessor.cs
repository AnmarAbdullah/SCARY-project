// Auto-copies steam_appid.txt and steam_api64.dll into the build output folder after every Windows build.
// Reason: UPM Git install of Steamworks.NET doesn't reliably mark steam_api64.dll as a native plugin,
// so Unity skips it during build. And steam_appid.txt isn't a Unity asset, so it's never copied.
// Without these next to the .exe, SteamAPI_Init() fails.

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScaryGame.Steam.EditorTools
{
    public class SteamBuildPostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64 &&
                report.summary.platform != BuildTarget.StandaloneWindows) return;

            string buildDir = Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrEmpty(buildDir)) return;

            CopyAppId(buildDir);
            CopySteamApiDll(buildDir, report.summary.platform == BuildTarget.StandaloneWindows64);
        }

        static void CopyAppId(string buildDir)
        {
            string src = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "steam_appid.txt");
            if (!File.Exists(src))
            {
                Debug.LogWarning("[SteamBuildPostProcessor] steam_appid.txt not found at project root. Build will fail to init Steam unless you create it.");
                return;
            }
            File.Copy(src, Path.Combine(buildDir, "steam_appid.txt"), overwrite: true);
            Debug.Log("[SteamBuildPostProcessor] Copied steam_appid.txt to build folder.");
        }

        static void CopySteamApiDll(string buildDir, bool x64)
        {
            string dllName = x64 ? "steam_api64.dll" : "steam_api.dll";
            string src = FindInPackageCache(dllName);
            if (src == null)
            {
                Debug.LogError($"[SteamBuildPostProcessor] Could not find {dllName} in Steamworks.NET package cache. Build will fail to init Steam.");
                return;
            }
            File.Copy(src, Path.Combine(buildDir, dllName), overwrite: true);
            Debug.Log($"[SteamBuildPostProcessor] Copied {dllName} to build folder.");
        }

        static string FindInPackageCache(string fileName)
        {
            string cacheDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "PackageCache");
            if (!Directory.Exists(cacheDir)) return null;
            foreach (var dir in Directory.GetDirectories(cacheDir, "com.rlabrecque.steamworks.net*"))
            {
                string candidate = Path.Combine(dir, "Plugins", fileName);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }
    }
}
