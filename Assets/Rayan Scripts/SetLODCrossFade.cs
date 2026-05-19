#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SetLODCrossFade : EditorWindow
{
    [MenuItem("Tools/Set All LODs to CrossFade")]
    static void SetAllLODsCrossFade()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            LODGroup[] lodGroups = prefab.GetComponentsInChildren<LODGroup>(true);
            if (lodGroups.Length == 0) continue;

            bool changed = false;
            foreach (LODGroup group in lodGroups)
            {
                LOD[] lods = group.GetLODs();
                for (int i = 0; i < lods.Length; i++)
                    lods[i].fadeTransitionWidth = 0.5f;

                group.SetLODs(lods);
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                EditorUtility.SetDirty(group);
                changed = true;
                count++;
            }

            if (changed)
                PrefabUtility.SavePrefabAsset(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Set {count} LOD Groups to CrossFade with animation across all prefabs.");
    }
}
#endif