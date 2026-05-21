// Assets/Editor/AssetOrganizer.cs
// Run via: Tools > SCARY Project > Organize Project Assets
//
// ── What this does ───────────────────────────────────────────────────────────
//   Reorganizes all CUSTOM project assets into a clean, consistent folder
//   hierarchy using AssetDatabase.MoveAsset() — which preserves every GUID,
//   so no scene/prefab references break.
//
// ── What this NEVER touches ───────────────────────────────────────────────────
//   Mirror/  |  TextMesh Pro/  |  Unity Assets/  |  ScriptTemplates/
//   TutorialInfo/  |  Settings/HDRPDefaultResources/
// ─────────────────────────────────────────────────────────────────────────────

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AssetOrganizer
{
    // ── Entry point ───────────────────────────────────────────────────────────
    [MenuItem("Tools/SCARY Project/Organize Project Assets", false, 20)]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog(
            "Organize Project Assets",
            "This will reorganize all custom Assets into a clean folder structure.\n\n" +
            "• Third-party packages (Mirror, TextMesh Pro, Unity Assets) are NOT touched.\n" +
            "• All GUIDs are preserved — scene and prefab references stay intact.\n\n" +
            "Make a backup first. Continue?",
            "Yes, Organize", "Cancel")) return;

        AssetDatabase.StartAssetEditing();
        try
        {
            Step1_CreateTargetFolders();
            Step2_MoveFiles();
            Step3_DeleteEmptySourceFolders();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("[AssetOrganizer] Done! Check the Console for any warnings.");
        EditorUtility.DisplayDialog("Organize Complete",
            "Asset organization finished!\nCheck the Console for any skipped files.", "OK");
    }

    // ── Step 1 — Create every destination folder up-front ────────────────────
    private static void Step1_CreateTargetFolders()
    {
        // Audio
        EnsureFolder("Assets/Audio/Footsteps");
        EnsureFolder("Assets/Audio/Footsteps/Dirt");

        // Fonts
        EnsureFolder("Assets/Fonts");
        EnsureFolder("Assets/Fonts/Calibri");

        // Models
        EnsureFolder("Assets/Models");
        EnsureFolder("Assets/Models/Cable");

        // Prefabs
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/Player");

        // ScriptableObjects
        EnsureFolder("Assets/ScriptableObjects");
        EnsureFolder("Assets/ScriptableObjects/LevelGen");
        EnsureFolder("Assets/ScriptableObjects/Terrain");
        EnsureFolder("Assets/ScriptableObjects/VolumeProfiles");

        // Scripts — new LevelGen (no hyphen), Player sub-systems, Utilities
        EnsureFolder("Assets/Scripts/LevelGen");
        EnsureFolder("Assets/Scripts/LevelGen/Core");
        EnsureFolder("Assets/Scripts/LevelGen/Data");
        EnsureFolder("Assets/Scripts/LevelGen/Editor");
        EnsureFolder("Assets/Scripts/LevelGen/Props");
        EnsureFolder("Assets/Scripts/LevelGen/Rules");
        EnsureFolder("Assets/Scripts/Player");
        EnsureFolder("Assets/Scripts/Utilities");

        // Settings/Lighting (Settings/ already exists)
        EnsureFolder("Assets/Settings/Lighting");

        // Textures sub-folders
        EnsureFolder("Assets/Textures");
        EnsureFolder("Assets/Textures/Backgrounds");
        EnsureFolder("Assets/Textures/RenderTextures");
    }

    // ── Step 2 — Move everything ──────────────────────────────────────────────
    private static void Step2_MoveFiles()
    {
        // ── AUDIO ─────────────────────────────────────────────────────────────
        // "Dirt Footsteps" (spaces, wrong category name) → Footsteps/Dirt/
        MoveFolder("Assets/Audio/Dirt Footsteps", "Assets/Audio/Footsteps/Dirt");

        // ── FONTS ─────────────────────────────────────────────────────────────
        // "CalibriFont" root folder → Fonts/Calibri/
        MoveFolder("Assets/CalibriFont", "Assets/Fonts/Calibri");

        // ── MODELS ────────────────────────────────────────────────────────────
        // "Objects/PulsingCable_" (trailing underscore, wrong root) → Models/Cable/
        MoveFolder("Assets/Objects/PulsingCable_", "Assets/Models/Cable");

        // ── SCENES ────────────────────────────────────────────────────────────
        // Three loose scenes sitting at root
        MoveAsset("Assets/OutdoorsScene.unity", "Assets/Scenes/OutdoorsScene.unity");
        MoveAsset("Assets/Scene1.unity",         "Assets/Scenes/Scene1.unity");
        MoveAsset("Assets/Scene2.unity",         "Assets/Scenes/Scene2.unity");

        // ── SCRIPTABLE OBJECTS ────────────────────────────────────────────────
        // Terrain data sitting loose at root (bad names too)
        MoveAsset("Assets/New Terrain.asset",
                  "Assets/ScriptableObjects/Terrain/NewTerrain.asset");
        MoveAsset("Assets/New Terrain 1.asset",
                  "Assets/ScriptableObjects/Terrain/NewTerrain1.asset");
        MoveAsset("Assets/New Terrain 2.asset",
                  "Assets/ScriptableObjects/Terrain/NewTerrain2.asset");

        // ScriptableObject data files wrongly stored inside Scripts/Level-Gen/
        MoveFilesByExtension("Assets/Scripts/Level-Gen",
                             "Assets/ScriptableObjects/LevelGen", ".asset");
        MoveAsset("Assets/Scripts/Level-Gen/README.md",
                  "Assets/ScriptableObjects/LevelGen/README.md");

        // Volume profile (standalone "Profiles" folder at root is too vague)
        MoveAsset("Assets/Profiles/FP_Profile.asset",
                  "Assets/ScriptableObjects/VolumeProfiles/FP_Profile.asset");

        // ── SETTINGS ──────────────────────────────────────────────────────────
        // "Lighting Settings" (spaces in folder name) → Settings/Lighting/
        MoveFolder("Assets/Lighting Settings", "Assets/Settings/Lighting");

        // ── TEXTURES ──────────────────────────────────────────────────────────
        // "1 - 1920x1080.png" — meaningless name, wrong folder
        MoveAsset("Assets/Images/1 - 1920x1080.png",
                  "Assets/Textures/Backgrounds/MainMenu_Background.png");

        // Render texture at root
        MoveAsset("Assets/Render Texture.renderTexture",
                  "Assets/Textures/RenderTextures/RenderTexture.renderTexture");

        // ── VIDEOS ────────────────────────────────────────────────────────────
        // "1 (1920x1080) mainmenu.mp4" — bad name with spaces/parens
        MoveAsset("Assets/Videos/1 (1920x1080) mainmenu.mp4",
                  "Assets/Videos/MainMenu_Background.mp4");

        // ── PREFABS ───────────────────────────────────────────────────────────
        // FirstPerson/ prefabs → Prefabs/Player/
        MoveAsset("Assets/FirstPerson/CameraHolder.prefab",
                  "Assets/Prefabs/Player/CameraHolder.prefab");
        MoveAsset("Assets/FirstPerson/Player.prefab",
                  "Assets/Prefabs/Player/Player.prefab");

        // ── SCRIPTS ───────────────────────────────────────────────────────────
        // "Rayan Scripts" (personal name as folder = bad convention)
        MoveAsset("Assets/Rayan Scripts/Editor/Terrainraisebase.cs",
                  "Assets/Editor/Terrainraisebase.cs");
        MoveAsset("Assets/Rayan Scripts/EnvironmentScatterTool.cs",
                  "Assets/Scripts/Utilities/EnvironmentScatterTool.cs");
        MoveAsset("Assets/Rayan Scripts/SetLODCrossFade.cs",
                  "Assets/Scripts/Utilities/SetLODCrossFade.cs");

        // FirstPerson/Scripts/ sub-systems → Scripts/Player/ (maintaining structure)
        MoveFolder("Assets/FirstPerson/Scripts/Audio",
                   "Assets/Scripts/Player/Audio");
        MoveFolder("Assets/FirstPerson/Scripts/Camera",
                   "Assets/Scripts/Player/Camera");
        MoveFolder("Assets/FirstPerson/Scripts/Input",
                   "Assets/Scripts/Player/Input");
        MoveFolder("Assets/FirstPerson/Scripts/Interfaces",
                   "Assets/Scripts/Player/Interfaces");
        // FirstPerson/Scripts/Player/ merges into Scripts/Player/ (dest already exists)
        MoveAsset("Assets/FirstPerson/Scripts/Player/PlayerController.cs",
                  "Assets/Scripts/Player/PlayerController.cs");
        MoveAsset("Assets/FirstPerson/Scripts/Player/PlayerMovement.cs",
                  "Assets/Scripts/Player/PlayerMovement.cs");

        // "Level-Gen" (hyphen in folder name) → "LevelGen" — move each subfolder
        MoveFolder("Assets/Scripts/Level-Gen/Core",
                   "Assets/Scripts/LevelGen/Core");
        MoveFolder("Assets/Scripts/Level-Gen/Data",
                   "Assets/Scripts/LevelGen/Data");
        MoveFolder("Assets/Scripts/Level-Gen/Editor",
                   "Assets/Scripts/LevelGen/Editor");
        MoveFolder("Assets/Scripts/Level-Gen/Props",
                   "Assets/Scripts/LevelGen/Props");
        MoveFolder("Assets/Scripts/Level-Gen/Rules",
                   "Assets/Scripts/LevelGen/Rules");
    }

    // ── Step 3 — Remove now-empty source folders ──────────────────────────────
    private static void Step3_DeleteEmptySourceFolders()
    {
        // Order matters: delete children before parents
        TryDeleteEmpty("Assets/Scripts/Level-Gen");
        TryDeleteEmpty("Assets/FirstPerson/Scripts/Player");
        TryDeleteEmpty("Assets/FirstPerson/Scripts");
        TryDeleteEmpty("Assets/FirstPerson");
        TryDeleteEmpty("Assets/Rayan Scripts/Editor");
        TryDeleteEmpty("Assets/Rayan Scripts");
        TryDeleteEmpty("Assets/Objects/PulsingCable_");
        TryDeleteEmpty("Assets/Objects");
        TryDeleteEmpty("Assets/CalibriFont");
        TryDeleteEmpty("Assets/Images");
        TryDeleteEmpty("Assets/Profiles");
        TryDeleteEmpty("Assets/Audio/Dirt Footsteps");
    }

    // ── Core helpers ──────────────────────────────────────────────────────────

    // Guarantees a folder exists, creating parent chain if needed.
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = ParentOf(path);
        string name   = Path.GetFileName(path);
        EnsureFolder(parent); // recurse
        AssetDatabase.CreateFolder(parent, name);
    }

    // Move a single file asset; skips with warning if source not found.
    private static void MoveAsset(string from, string to)
    {
        if (!FileExists(from))
        {
            Debug.LogWarning($"[Organizer] Skipped (not found): {from}");
            return;
        }
        string err = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(err))
            Debug.LogError($"[Organizer] FAILED: {from} → {to}\n{err}");
        else
            Debug.Log($"[Organizer] ✓  {Rel(from)}  →  {Rel(to)}");
    }

    // Move a folder.
    // If the destination doesn't exist yet  → straightforward whole-folder rename.
    // If the destination already exists     → merge contents file-by-file.
    private static void MoveFolder(string from, string to)
    {
        if (!AssetDatabase.IsValidFolder(from))
        {
            Debug.LogWarning($"[Organizer] Skipped folder (not found): {from}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(to))
        {
            // Simple rename / move to new location
            EnsureFolder(ParentOf(to));
            string err = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(err))
                Debug.LogError($"[Organizer] FAILED folder: {from} → {to}\n{err}");
            else
                Debug.Log($"[Organizer] ✓  {Rel(from)}/  →  {Rel(to)}/");
        }
        else
        {
            // Destination already exists — merge
            MergeFolder(from, to);
        }
    }

    // Recursively moves all files (and sub-folders) from src into dst.
    private static void MergeFolder(string src, string dst)
    {
        string absSrc = Abs(src);
        if (!Directory.Exists(absSrc)) return;

        // Move files at this level
        foreach (string file in Directory.GetFiles(absSrc, "*", SearchOption.TopDirectoryOnly))
        {
            if (file.EndsWith(".meta")) continue;
            string assetFrom = AssetRel(file);
            string assetTo   = dst + "/" + Path.GetFileName(file);
            MoveAsset(assetFrom, assetTo);
        }

        // Recurse into sub-folders
        foreach (string dir in Directory.GetDirectories(absSrc, "*", SearchOption.TopDirectoryOnly))
        {
            string subName = Path.GetFileName(dir);
            string subSrc  = src + "/" + subName;
            string subDst  = dst + "/" + subName;
            EnsureFolder(subDst);
            MergeFolder(subSrc, subDst);
        }
    }

    // Moves all files with a given extension directly inside a folder (non-recursive).
    private static void MoveFilesByExtension(string fromFolder, string toFolder, string ext)
    {
        string abs = Abs(fromFolder);
        if (!Directory.Exists(abs)) return;

        foreach (string file in Directory.GetFiles(abs, "*" + ext, SearchOption.TopDirectoryOnly))
        {
            string assetFrom = AssetRel(file);
            string assetTo   = toFolder + "/" + Path.GetFileName(file);
            MoveAsset(assetFrom, assetTo);
        }
    }

    // Delete a folder only if it contains no non-meta files anywhere inside.
    private static void TryDeleteEmpty(string path)
    {
        string abs = Abs(path);
        if (!Directory.Exists(abs)) return;

        bool hasContent = Directory.EnumerateFiles(abs, "*", SearchOption.AllDirectories)
                                   .Any(f => !f.EndsWith(".meta"));
        if (hasContent)
        {
            Debug.LogWarning($"[Organizer] Folder still has content, keeping: {path}");
            return;
        }

        AssetDatabase.DeleteAsset(path);
        Debug.Log($"[Organizer] Removed empty folder: {path}");
    }

    // ── Path utilities ────────────────────────────────────────────────────────

    // Convert an absolute OS path back to an "Assets/..." Unity asset path.
    private static string AssetRel(string absPath)
    {
        string dataPath = Path.GetFullPath(Application.dataPath);
        string full     = Path.GetFullPath(absPath);
        return "Assets" + full.Substring(dataPath.Length).Replace('\\', '/');
    }

    // Convert "Assets/..." to an absolute OS path.
    private static string Abs(string assetPath)
        => Path.GetFullPath(Application.dataPath + assetPath.Substring("Assets".Length));

    private static string ParentOf(string assetPath)
        => assetPath.Substring(0, assetPath.LastIndexOf('/'));

    private static string Rel(string assetPath)
        => assetPath.Replace("Assets/", "");

    private static bool FileExists(string assetPath)
        => File.Exists(Abs(assetPath));
}
