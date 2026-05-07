#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ScaryGame.LevelGen.EditorTools
{
    [CustomEditor(typeof(LevelGenerator))]
    public class LevelGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var gen = (LevelGenerator)target;

            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Regenerate (Random Seed)"))
            {
                gen.GenerateFromSeed((uint)Random.Range(1, int.MaxValue));
            }
            if (gen.config != null && GUILayout.Button($"Regenerate (Fixed Seed: {gen.config.fixedSeed})"))
            {
                gen.GenerateFromSeed(gen.config.fixedSeed);
            }
            if (GUILayout.Button("Clear"))
            {
                gen.ClearExisting();
            }
            EditorGUI.EndDisabledGroup();

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play mode to regenerate.", MessageType.Info);

            if (Application.isPlaying && gen.LastSeed != 0)
                EditorGUILayout.HelpBox($"Last seed: {gen.LastSeed}", MessageType.None);
        }

        void OnSceneGUI()
        {
            var gen = (LevelGenerator)target;
            if (gen == null || gen.config == null) return;
            var grid = gen.Grid;

            float ts = gen.config.tileSize;
            int w = gen.config.gridSize.x;
            int h = gen.config.gridSize.y;
            float halfW = (w - 1) * 0.5f;
            float halfH = (h - 1) * 0.5f;
            Vector3 origin = gen.transform.position;

            // Outline of the map.
            Handles.color = Color.gray;
            Vector3 a = origin + new Vector3(-halfW * ts - ts * 0.5f, 0, -halfH * ts - ts * 0.5f);
            Vector3 b = origin + new Vector3( halfW * ts + ts * 0.5f, 0, -halfH * ts - ts * 0.5f);
            Vector3 c = origin + new Vector3( halfW * ts + ts * 0.5f, 0,  halfH * ts + ts * 0.5f);
            Vector3 d = origin + new Vector3(-halfW * ts - ts * 0.5f, 0,  halfH * ts + ts * 0.5f);
            Handles.DrawAAPolyLine(3f, a, b, c, d, a);

            if (grid == null) return;

            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    var t = grid.Get(new Vector2Int(x, y));
                    if (t == null || t.definition == null) continue;
                    Handles.color = ColorFor(t.definition.category);
                    Vector3 p = origin + new Vector3((x - halfW) * ts, 0.05f, (y - halfH) * ts);
                    Handles.DrawSolidDisc(p, Vector3.up, ts * 0.15f);
                }
            }
        }

        static Color ColorFor(TileCategory c)
        {
            switch (c)
            {
                case TileCategory.MonsterBase: return new Color(1f, 0f, 0f, 0.9f);
                case TileCategory.PowerCore:   return new Color(0f, 0.8f, 1f, 0.9f);
                case TileCategory.Spawn:       return new Color(0f, 1f, 0f, 0.9f);
                case TileCategory.Objective:   return new Color(1f, 1f, 0f, 0.9f);
                case TileCategory.Road:        return new Color(0.7f, 0.5f, 0.2f, 0.7f);
                case TileCategory.Structure:   return new Color(0.8f, 0.4f, 1f, 0.8f);
                case TileCategory.Terrain:     return new Color(0.3f, 0.6f, 0.3f, 0.4f);
                case TileCategory.Custom:      return new Color(1f, 0.6f, 0f, 0.9f);
                default:                       return new Color(1f, 1f, 1f, 0.4f);
            }
        }
    }
}
#endif
