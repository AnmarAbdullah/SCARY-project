using UnityEngine;
using UnityEditor;

public class TerrainRaiseBase : EditorWindow
{
    Terrain terrain;
    float raiseAmount = 0.05f; // 0 to 1 range (Unity heightmap is normalized)

    [MenuItem("Tools/Terrain Raise Base")]
    public static void ShowWindow()
    {
        GetWindow<TerrainRaiseBase>("Terrain Raise Base");
    }

    void OnGUI()
    {
        GUILayout.Label("Raise Terrain From Below", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        raiseAmount = EditorGUILayout.Slider("Raise Amount (0-1)", raiseAmount, 0.001f, 0.5f);

        EditorGUILayout.HelpBox(
            "Shifts the entire heightmap UP by the raise amount.\n" +
            "Your sculpting stays the same — you just get more room below.",
            MessageType.Info);

        if (GUILayout.Button("Raise Terrain"))
        {
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("No Terrain", "Please assign a Terrain first.", "OK");
                return;
            }
            RaiseTerrain();
        }
    }

    void RaiseTerrain()
    {
        TerrainData data = terrain.terrainData;
        int w = data.heightmapResolution;
        int h = data.heightmapResolution;

        // Get all current heights
        float[,] heights = data.GetHeights(0, 0, w, h);

        // Shift every point up by raiseAmount
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                heights[x, y] = Mathf.Clamp01(heights[x, y] + raiseAmount);
            }
        }

        // Apply back
        Undo.RegisterCompleteObjectUndo(data, "Raise Terrain Base");
        data.SetHeights(0, 0, heights);

        Debug.Log($"Terrain raised by {raiseAmount} units (normalized). Undo is supported!");
    }
}