#if UNITY_EDITOR
// =====================================================================
//  EnvironmentScatterTool.cs  (v4 - Free-form polygon zones)
//  Drop inside any  Assets/Editor/  folder.
//  Open via:  Tools > Environment Scatter Tool
// =====================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EnvironmentScatterTool : EditorWindow
{
    // ─────────────────────────────────────────────────────────────────
    //  Data classes
    // ─────────────────────────────────────────────────────────────────

    [System.Serializable]
    private class PrefabLayer
    {
        public string     label           = "Layer";
        public GameObject prefab          = null;
        public bool       enabled         = true;

        [Range(0f,1f)] public float density        = 0.5f;
        [Range(0f,1f)] public float noiseScale     = 0.3f;
        [Range(0f,1f)] public float noiseThreshold = 0.4f;

        public float minScale        = 0.8f;
        public float maxScale        = 1.2f;
        public bool  randomRotationY = true;
        public bool  alignToNormal   = false;

        public bool  paintTexture        = false;
        public int   textureLayerIndex   = 0;
        public float radiusMultiplier    = 1.2f;
        public float paintNoiseStrength  = 0.4f;
        public float paintNoiseFrequency = 0.3f;
    }

    [System.Serializable]
    private class PlacementZone
    {
        public string     label      = "Zone";
        public bool       enabled    = true;

        // The zone marker GO sets the Y height for handle display.
        // Points are stored in WORLD XZ space (Y is ignored, terrain raycast sets Y).
        public GameObject zoneMarker = null;

        // Free-form polygon corners (world XZ). Default = quad.
        public List<Vector3> points = new List<Vector3>();

        public List<GameObject> prefabs = new List<GameObject>();

        public float density    = 0.6f;
        public float minSpacing = 1.5f;

        // Scale falloff centroid=centerScale, edge=edgeScale
        public float centerScale = 1.5f;
        public float edgeScale   = 0.4f;

        public bool randomRotationY = true;
        public bool alignToNormal   = false;

        public bool  paintTexture        = false;
        public int   textureLayerIndex   = 0;
        public float radiusMultiplier    = 1.2f;
        public float paintNoiseStrength  = 0.4f;
        public float paintNoiseFrequency = 0.3f;

        [System.NonSerialized] public bool fold         = true;
        [System.NonSerialized] public bool pointsFold  = true;
        [System.NonSerialized] public bool prefabsFold = true;
        [System.NonSerialized] public bool settingsFold= true;
        [System.NonSerialized] public bool textureFold = false;
        [System.NonSerialized] public int  selectedPt  = -1;

        public void InitDefaultQuad(Vector3 centre, float size = 10f)
        {
            float h = centre.y;
            points = new List<Vector3>
            {
                new Vector3(centre.x - size, h, centre.z - size),
                new Vector3(centre.x + size, h, centre.z - size),
                new Vector3(centre.x + size, h, centre.z + size),
                new Vector3(centre.x - size, h, centre.z + size),
            };
        }

        // World-space centroid (XZ)
        public Vector3 Centroid()
        {
            if (points.Count == 0) return Vector3.zero;
            Vector3 s = Vector3.zero;
            foreach (var p in points) s += p;
            return s / points.Count;
        }
    }

    [System.Serializable]
    private class SlopeTextureRule
    {
        public string label             = "Slope Rule";
        public bool   enabled           = true;
        public int    textureLayerIndex = 1;
        public float  minAngle          = 45f;
        public float  maxAngle          = 90f;
        public float  blendWidth        = 5f;
        public float  strength          = 1f;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Window state
    // ─────────────────────────────────────────────────────────────────

    [SerializeField] private List<PrefabLayer>    _layers     = new List<PrefabLayer>();
    [SerializeField] private List<PlacementZone>  _zones      = new List<PlacementZone>();
    [SerializeField] private List<SlopeTextureRule> _slopeRules = new List<SlopeTextureRule>();
    private Vector2 _scroll;

    [SerializeField] private int _tab = 0;
    private readonly string[] _tabNames = { "Scatter", "Zones", "Terrain Textures" };

    [SerializeField] private GameObject _scatterParent = null;
    [SerializeField] private Vector3    _center        = Vector3.zero;
    [SerializeField] private float      _areaWidth     = 50f;
    [SerializeField] private float      _areaLength    = 50f;
    [SerializeField] private float      _minSpacing    = 2f;
    [SerializeField] private int        _seed          = 42;
    [SerializeField] private bool       _useSeed       = true;
    [SerializeField] private float      _raycastHeight = 50f;
    [SerializeField] private LayerMask  _groundMask    = ~0;

    [SerializeField] private Terrain _targetTerrain  = null;
    [SerializeField] private bool    _slopeRulesFold = true;

    private readonly Dictionary<int, List<GameObject>> _spawnedByLayer
        = new Dictionary<int, List<GameObject>>();
    private readonly Dictionary<int, List<GameObject>> _spawnedByZone
        = new Dictionary<int, List<GameObject>>();

    // ─────────────────────────────────────────────────────────────────
    //  Menu
    // ─────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Environment Scatter Tool")]
    public static void ShowWindow()
    {
        var win = GetWindow<EnvironmentScatterTool>("Env Scatter");
        win.minSize = new Vector2(390, 580);
    }

    // ─────────────────────────────────────────────────────────────────
    //  OnGUI
    // ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        DrawHeader();
        _tab = GUILayout.Toolbar(_tab, _tabNames, GUILayout.Height(26));
        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        switch (_tab)
        {
            case 0: DrawScatterTab();         break;
            case 1: DrawZonesTab();           break;
            case 2: DrawTerrainTexturesTab(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        var title = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 14, alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🌲  Environment Scatter Tool  v4", title);
        EditorGUILayout.Space(4);
        Separator();
    }

    // ─────────────────────────────────────────────────────────────────
    //  TAB 0 — Scatter
    // ─────────────────────────────────────────────────────────────────

    private void DrawScatterTab()
    {
        EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        _scatterParent = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Parent Object","Leave null to auto-create."),
            _scatterParent, typeof(GameObject), true);
        _center        = EditorGUILayout.Vector3Field("Center", _center);
        _areaWidth     = EditorGUILayout.FloatField("Area Width",     _areaWidth);
        _areaLength    = EditorGUILayout.FloatField("Area Length",    _areaLength);
        _minSpacing    = EditorGUILayout.FloatField("Min Spacing",    _minSpacing);
        _raycastHeight = EditorGUILayout.FloatField("Raycast Height", _raycastHeight);
        _groundMask    = LayerMaskField("Ground Mask", _groundMask);
        _useSeed       = EditorGUILayout.Toggle("Use Fixed Seed", _useSeed);
        if (_useSeed) _seed = EditorGUILayout.IntField("Seed", _seed);
        EditorGUI.indentLevel--;
        Separator();

        EditorGUILayout.LabelField($"Perlin Scatter Layers  ({_layers.Count})", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        for (int i = 0; i < _layers.Count; i++)
        {
            var L = _layers[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            L.enabled = EditorGUILayout.Toggle(L.enabled, GUILayout.Width(18));
            L.label   = EditorGUILayout.TextField(L.label);
            GUI.backgroundColor = new Color(1f,0.4f,0.4f);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                ClearLayer(i); _layers.RemoveAt(i);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            if (L.enabled)
            {
                L.prefab         = PrefabField("Prefab", L.prefab);
                L.density        = EditorGUILayout.Slider("Density",         L.density,        0f, 1f);
                L.noiseScale     = EditorGUILayout.Slider("Noise Scale",     L.noiseScale,     0.01f, 1f);
                L.noiseThreshold = EditorGUILayout.Slider("Noise Threshold", L.noiseThreshold, 0f,   1f);
                ScaleRange(ref L.minScale, ref L.maxScale);
                L.randomRotationY = EditorGUILayout.Toggle("Random Rot Y",    L.randomRotationY);
                L.alignToNormal   = EditorGUILayout.Toggle("Align to Normal", L.alignToNormal);
                EditorGUILayout.Space(3);
                L.paintTexture = EditorGUILayout.Toggle(
                    new GUIContent("Paint Terrain Texture"), L.paintTexture);
                if (L.paintTexture)
                {
                    EditorGUI.indentLevel++;
                    L.textureLayerIndex   = EditorGUILayout.IntField("Texture Layer Index", L.textureLayerIndex);
                    L.radiusMultiplier    = EditorGUILayout.FloatField("Radius Multiplier",    L.radiusMultiplier);
                    L.paintNoiseStrength  = EditorGUILayout.Slider("Edge Noise Strength",   L.paintNoiseStrength,  0f, 1f);
                    L.paintNoiseFrequency = EditorGUILayout.Slider("Edge Noise Frequency",  L.paintNoiseFrequency, 0.05f, 2f);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Re-scatter")) { ClearLayer(i); ScatterLayer(i, L); }
                if (GUILayout.Button("Clear"))       ClearLayer(i);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        EditorGUI.indentLevel--;
        if (GUILayout.Button("+ Add Scatter Layer"))
            _layers.Add(new PrefabLayer { label = "Layer " + _layers.Count });
        Separator();

        GUI.backgroundColor = new Color(0.4f,0.9f,0.5f);
        if (GUILayout.Button("Scatter All  (Layers + Zones)", GUILayout.Height(36))) ScatterAll();
        GUI.backgroundColor = new Color(0.9f,0.6f,0.3f);
        if (GUILayout.Button("Re-scatter All",                GUILayout.Height(28))) { ClearAll(); ScatterAll(); }
        GUI.backgroundColor = new Color(1f,0.4f,0.4f);
        if (GUILayout.Button("Clear All Spawned",             GUILayout.Height(28))) ClearAll();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Focus Scene on Global Area"))
            SceneView.lastActiveSceneView?.LookAt(_center, Quaternion.Euler(60,0,0),
                Mathf.Max(_areaWidth, _areaLength));
    }

    // ─────────────────────────────────────────────────────────────────
    //  TAB 1 — Zones
    // ─────────────────────────────────────────────────────────────────

    private void DrawZonesTab()
    {
        EditorGUILayout.HelpBox(
            "Each zone is a free-form polygon you draw in the Scene view.\n" +
            "• Drag the yellow dots to reshape.\n" +
            "• Use + / - buttons below to add or remove points.\n" +
            "• Objects scale larger at the centroid and smaller near the edges.",
            MessageType.Info);

        for (int i = 0; i < _zones.Count; i++)
        {
            var Z = _zones[i];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header
            EditorGUILayout.BeginHorizontal();
            Z.enabled = EditorGUILayout.Toggle(Z.enabled, GUILayout.Width(18));
            Z.fold    = EditorGUILayout.Foldout(Z.fold, Z.label, true);
            GUI.backgroundColor = new Color(1f,0.4f,0.4f);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                ClearZone(i); _zones.RemoveAt(i);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (Z.fold && Z.enabled)
            {
                Z.label      = EditorGUILayout.TextField("Label", Z.label);
                Z.zoneMarker = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Zone Marker","Empty GO — its position = zone centre."),
                    Z.zoneMarker, typeof(GameObject), true);
                if (Z.zoneMarker != null && Z.points.Count == 0)
                    Z.InitDefaultQuad(Z.zoneMarker.transform.position);

                // ── Shape Points (compact) ────────────────────────────
                Z.pointsFold = EditorGUILayout.BeginFoldoutHeaderGroup(
                    Z.pointsFold, $"Shape Points  ({Z.points.Count} dots)");
                if (Z.pointsFold)
                {
                    int chipsPerRow = 5, chipIdx = 0;
                    EditorGUILayout.BeginHorizontal();
                    for (int p = 0; p < Z.points.Count; p++)
                    {
                        if (chipIdx > 0 && chipIdx % chipsPerRow == 0)
                        { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
                        bool sel = Z.selectedPt == p;
                        GUI.backgroundColor = sel ? new Color(1f,0.9f,0.2f) : new Color(0.75f,0.75f,0.75f);
                        if (GUILayout.Button($"P{p}", GUILayout.Width(30), GUILayout.Height(20)))
                        { Z.selectedPt = sel ? -1 : p; SceneView.RepaintAll(); }
                        GUI.backgroundColor = new Color(1f,0.4f,0.4f);
                        if (Z.points.Count > 3 && GUILayout.Button("×", GUILayout.Width(18), GUILayout.Height(20)))
                        {
                            Undo.RecordObject(this, "Remove Zone Point");
                            Z.points.RemoveAt(p);
                            if (Z.selectedPt >= Z.points.Count) Z.selectedPt = -1;
                            GUI.backgroundColor = Color.white;
                            EditorGUILayout.EndHorizontal(); goto donePoints;
                        }
                        GUI.backgroundColor = Color.white;
                        chipIdx++;
                    }
                    EditorGUILayout.EndHorizontal();
                    donePoints:
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("+ Add Point"))
                    {
                        Undo.RecordObject(this, "Add Zone Point");
                        int last = Z.points.Count-1;
                        Z.points.Add((Z.points[last]+Z.points[0])*0.5f);
                        SceneView.RepaintAll();
                    }
                    if (Z.selectedPt >= 0 && Z.selectedPt < Z.points.Count &&
                        GUILayout.Button("Insert After P" + Z.selectedPt))
                    {
                        Undo.RecordObject(this, "Insert Zone Point");
                        int next = (Z.selectedPt+1)%Z.points.Count;
                        Z.points.Insert(Z.selectedPt+1, (Z.points[Z.selectedPt]+Z.points[next])*0.5f);
                        Z.selectedPt++; SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                // ── Prefabs (drag-and-drop) ───────────────────────────
                Z.prefabsFold = EditorGUILayout.BeginFoldoutHeaderGroup(
                    Z.prefabsFold, $"Prefabs  ({Z.prefabs.FindAll(p=>p!=null).Count} assigned)");
                if (Z.prefabsFold)
                {
                    for (int p = 0; p < Z.prefabs.Count; p++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        Z.prefabs[p] = PrefabField($"  [{p}]", Z.prefabs[p]);
                        GUI.backgroundColor = new Color(1f,0.4f,0.4f);
                        if (GUILayout.Button("×", GUILayout.Width(22)))
                        { Z.prefabs.RemoveAt(p); GUI.backgroundColor=Color.white; EditorGUILayout.EndHorizontal(); break; }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                    // Drop area
                    Rect dropRect = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
                    GUI.Box(dropRect, "⬇  Drag prefabs here",
                        new GUIStyle(EditorStyles.helpBox) { alignment=TextAnchor.MiddleCenter, fontSize=11 });
                    var evt = Event.current;
                    if (dropRect.Contains(evt.mousePosition))
                    {
                        if (evt.type == EventType.DragUpdated)
                        { DragAndDrop.visualMode = DragAndDropVisualMode.Copy; evt.Use(); }
                        else if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (var obj in DragAndDrop.objectReferences)
                                if (obj is GameObject go) Z.prefabs.Add(go);
                            evt.Use();
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                // ── Scatter settings ──────────────────────────────────
                Z.settingsFold = EditorGUILayout.BeginFoldoutHeaderGroup(Z.settingsFold, "Scatter Settings");
                if (Z.settingsFold)
                {
                    Z.density    = EditorGUILayout.Slider("Density",     Z.density,    0f, 1f);
                    Z.minSpacing = EditorGUILayout.FloatField("Min Spacing", Z.minSpacing);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Scale  Centre", GUILayout.Width(90));
                    Z.centerScale = EditorGUILayout.FloatField(Z.centerScale, GUILayout.Width(50));
                    EditorGUILayout.LabelField("→  Edge", GUILayout.Width(50));
                    Z.edgeScale   = EditorGUILayout.FloatField(Z.edgeScale, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    Z.randomRotationY = EditorGUILayout.Toggle("Random Rot Y",    Z.randomRotationY);
                    Z.alignToNormal   = EditorGUILayout.Toggle("Align to Normal", Z.alignToNormal);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                // ── Texture paint ─────────────────────────────────────
                Z.textureFold = EditorGUILayout.BeginFoldoutHeaderGroup(Z.textureFold, "Terrain Texture Paint");
                if (Z.textureFold)
                {
                    Z.paintTexture = EditorGUILayout.Toggle("Enabled", Z.paintTexture);
                    if (Z.paintTexture)
                    {
                        Z.textureLayerIndex   = EditorGUILayout.IntField("Layer Index",          Z.textureLayerIndex);
                        Z.radiusMultiplier    = EditorGUILayout.FloatField("Radius Multiplier",   Z.radiusMultiplier);
                        Z.paintNoiseStrength  = EditorGUILayout.Slider("Edge Noise Strength",     Z.paintNoiseStrength,  0f, 1f);
                        Z.paintNoiseFrequency = EditorGUILayout.Slider("Edge Noise Frequency",    Z.paintNoiseFrequency, 0.05f, 2f);
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.4f,0.9f,0.5f);
                if (GUILayout.Button("Re-scatter Zone", GUILayout.Height(24))) { ClearZone(i); ScatterZone(i, Z); }
                GUI.backgroundColor = new Color(1f,0.4f,0.4f);
                if (GUILayout.Button("Clear", GUILayout.Width(55), GUILayout.Height(24))) ClearZone(i);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (GUILayout.Button("+ Add Placement Zone"))
        {
            var z = new PlacementZone { label = "Zone " + _zones.Count };
            _zones.Add(z);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  TAB 2 — Terrain Textures
    // ─────────────────────────────────────────────────────────────────

    private void DrawTerrainTexturesTab()
    {
        EditorGUILayout.HelpBox(
            "Slope Texture Painter: scans the terrain and paints a chosen texture " +
            "onto faces within a slope angle range.\n\n" +
            "0° = flat ground   |   90° = vertical cliff/wall",
            MessageType.Info);

        EditorGUILayout.Space(3);
        _targetTerrain = (Terrain)EditorGUILayout.ObjectField(
            new GUIContent("Target Terrain","Leave null to auto-detect."),
            _targetTerrain, typeof(Terrain), true);
        Separator();

        _slopeRulesFold = EditorGUILayout.BeginFoldoutHeaderGroup(
            _slopeRulesFold, $"Slope Rules  ({_slopeRules.Count})");
        if (_slopeRulesFold)
        {
            for (int i = 0; i < _slopeRules.Count; i++)
            {
                var R = _slopeRules[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                R.enabled = EditorGUILayout.Toggle(R.enabled, GUILayout.Width(18));
                R.label   = EditorGUILayout.TextField(R.label);
                GUI.backgroundColor = new Color(1f,0.4f,0.4f);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    _slopeRules.RemoveAt(i);
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                if (R.enabled)
                {
                    R.textureLayerIndex = EditorGUILayout.IntField("Texture Layer Index", R.textureLayerIndex);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Slope Range (deg)", GUILayout.Width(120));
                    R.minAngle = EditorGUILayout.FloatField(R.minAngle, GUILayout.Width(45));
                    EditorGUILayout.LabelField("-", GUILayout.Width(10));
                    R.maxAngle = EditorGUILayout.FloatField(R.maxAngle, GUILayout.Width(45));
                    EditorGUILayout.EndHorizontal();
                    R.blendWidth = EditorGUILayout.Slider("Blend Width (deg)", R.blendWidth, 0f, 30f);
                    R.strength   = EditorGUILayout.Slider("Strength",          R.strength,   0f, 1f);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            if (GUILayout.Button("+ Add Slope Rule"))
                _slopeRules.Add(new SlopeTextureRule { label = "Rule " + _slopeRules.Count });
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        Separator();

        GUI.backgroundColor = new Color(0.4f,0.75f,1f);
        if (GUILayout.Button("Apply Slope Textures to Terrain", GUILayout.Height(36)))
            ApplySlopeTextures();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.HelpBox(
            "Warning: modifies terrain alphamaps. Undo supported.",
            MessageType.Warning);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Scatter logic
    // ─────────────────────────────────────────────────────────────────

    private void ScatterAll()
    {
        if (_useSeed) Random.InitState(_seed);
        for (int i = 0; i < _layers.Count; i++) ScatterLayer(i, _layers[i]);
        for (int i = 0; i < _zones.Count;  i++) ScatterZone(i,  _zones[i]);
        Debug.Log("[EnvScatter] Done.");
    }

    private void ScatterLayer(int idx, PrefabLayer L)
    {
        if (!L.enabled || L.prefab == null) return;
        GameObject root      = GetOrCreateParent();
        GameObject layerRoot = new GameObject($"[Scatter] {L.label}");
        layerRoot.transform.SetParent(root.transform);
        RegisterSpawned(_spawnedByLayer, idx, layerRoot);

        float halfW = _areaWidth*0.5f, halfLn = _areaLength*0.5f;
        float step  = Mathf.Max(_minSpacing, 0.1f);
        float ox    = Random.Range(0f,9999f), oz = Random.Range(0f,9999f);
        Terrain terrain = GetTargetTerrain(_center);

        for (float x = -halfW; x < halfW; x += step)
        for (float z = -halfLn; z < halfLn; z += step)
        {
            float wx = _center.x + x + Random.Range(-step*0.5f, step*0.5f);
            float wz = _center.z + z + Random.Range(-step*0.5f, step*0.5f);
            float noise = Mathf.PerlinNoise((wx+ox)*L.noiseScale, (wz+oz)*L.noiseScale);
            if (noise < L.noiseThreshold) continue;
            if (Random.value > L.density) continue;
            if (!RaycastGround(wx, wz, out RaycastHit hit)) continue;
            float scale = Random.Range(L.minScale, L.maxScale);
            var go = SpawnObject(L.prefab, hit, layerRoot.transform, scale, L.randomRotationY, L.alignToNormal);
            if (L.paintTexture && terrain != null)
                PaintTerrainTexture(terrain, hit.point, L.textureLayerIndex,
                    GetObjectFootprintRadius(go)*L.radiusMultiplier,
                    L.paintNoiseStrength, L.paintNoiseFrequency);
        }
    }

    private void ScatterZone(int idx, PlacementZone Z)
    {
        if (!Z.enabled || Z.points.Count < 3) return;
        if (Z.prefabs.Count == 0 || Z.prefabs.TrueForAll(p => p == null)) return;

        GameObject root     = GetOrCreateParent();
        GameObject zoneRoot = new GameObject($"[Zone] {Z.label}");
        zoneRoot.transform.SetParent(root.transform);
        RegisterSpawned(_spawnedByZone, idx, zoneRoot);

        // Bounding box of polygon
        float minX = Z.points.Min(p => p.x), maxX = Z.points.Max(p => p.x);
        float minZ = Z.points.Min(p => p.z), maxZ = Z.points.Max(p => p.z);
        float step  = Mathf.Max(Z.minSpacing, 0.1f);

        // Centroid + max radius for falloff
        Vector3 centroid = Z.Centroid();
        float maxDist = Z.points.Max(p =>
            Mathf.Sqrt(Sqr(p.x-centroid.x)+Sqr(p.z-centroid.z)));
        if (maxDist < 0.001f) maxDist = 1f;

        Terrain terrain = GetTargetTerrain(centroid);

        for (float x = minX; x <= maxX; x += step)
        for (float z = minZ; z <= maxZ; z += step)
        {
            float wx = x + Random.Range(-step*0.5f, step*0.5f);
            float wz = z + Random.Range(-step*0.5f, step*0.5f);

            if (!PointInPolygon(wx, wz, Z.points)) continue;
            if (Random.value > Z.density)          continue;
            if (!RaycastGround(wx, wz, out RaycastHit hit)) continue;

            // Edge factor: 0=centroid, 1=polygon edge
            float dist = Mathf.Sqrt(Sqr(wx-centroid.x)+Sqr(wz-centroid.z));
            float edgeFactor = Mathf.Clamp01(dist / maxDist);
            float scale = Mathf.Lerp(Z.centerScale, Z.edgeScale, edgeFactor);

            GameObject prefab = PickPrefab(Z.prefabs);
            if (prefab == null) continue;

            var go = SpawnObject(prefab, hit, zoneRoot.transform, scale, Z.randomRotationY, Z.alignToNormal);
            if (Z.paintTexture && terrain != null)
                PaintTerrainTexture(terrain, hit.point, Z.textureLayerIndex,
                    GetObjectFootprintRadius(go)*Z.radiusMultiplier,
                    Z.paintNoiseStrength, Z.paintNoiseFrequency);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Polygon helpers
    // ─────────────────────────────────────────────────────────────────

    // Ray-casting point-in-polygon on XZ plane
    private static bool PointInPolygon(float px, float pz, List<Vector3> poly)
    {
        int n = poly.Count;
        bool inside = false;
        for (int i = 0, j = n-1; i < n; j = i++)
        {
            float xi = poly[i].x, zi = poly[i].z;
            float xj = poly[j].x, zj = poly[j].z;
            if (((zi > pz) != (zj > pz)) &&
                (px < (xj-xi)*(pz-zi)/(zj-zi)+xi))
                inside = !inside;
        }
        return inside;
    }

    private static float Sqr(float v) => v*v;

    // ─────────────────────────────────────────────────────────────────
    //  Slope texture painter
    // ─────────────────────────────────────────────────────────────────

    private void ApplySlopeTextures()
    {
        Terrain t = _targetTerrain != null ? _targetTerrain : Terrain.activeTerrain;
        if (t == null) { Debug.LogError("[EnvScatter] No terrain found."); return; }

        TerrainData td = t.terrainData;
        int mapW = td.alphamapWidth, mapH = td.alphamapHeight, nLyr = td.alphamapLayers;

        foreach (var R in _slopeRules)
            if (R.enabled && R.textureLayerIndex >= nLyr)
            { Debug.LogError($"[EnvScatter] Rule '{R.label}': layer index out of range."); return; }

        Undo.RegisterCompleteObjectUndo(td, "Slope Texture Paint");
        float[,,] maps = td.GetAlphamaps(0, 0, mapW, mapH);
        try
        {
            for (int zi = 0; zi < mapH; zi++)
            {
                if (zi%32==0 && EditorUtility.DisplayCancelableProgressBar(
                    "Painting Slope Textures", $"Row {zi}/{mapH}", (float)zi/mapH)) break;
                for (int xi = 0; xi < mapW; xi++)
                {
                    float u = (float)xi/(mapW-1), v = (float)zi/(mapH-1);
                    float slope = Vector3.Angle(Vector3.up, td.GetInterpolatedNormal(u, v));
                    foreach (var R in _slopeRules)
                    {
                        if (!R.enabled) continue;
                        float alpha = SlopeAlpha(slope, R.minAngle, R.maxAngle, R.blendWidth);
                        if (alpha <= 0f) continue;
                        float blend = alpha * R.strength;
                        for (int l = 0; l < nLyr; l++) maps[zi,xi,l] *= (1f-blend);
                        maps[zi,xi,R.textureLayerIndex] += blend;
                        float sum = 0f;
                        for (int l = 0; l < nLyr; l++) sum += maps[zi,xi,l];
                        if (sum > 0f) for (int l = 0; l < nLyr; l++) maps[zi,xi,l] /= sum;
                    }
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        td.SetAlphamaps(0, 0, maps);
        Debug.Log("[EnvScatter] Slope paint complete.");
    }

    private static float SlopeAlpha(float slope, float minA, float maxA, float blend)
    {
        if (slope < minA-blend || slope > maxA+blend) return 0f;
        return Mathf.Min(
            Mathf.InverseLerp(minA-blend, minA, slope),
            Mathf.InverseLerp(maxA+blend, maxA, slope));
    }

    // ─────────────────────────────────────────────────────────────────
    //  Per-object terrain texture paint (noisy edge)
    // ─────────────────────────────────────────────────────────────────

    private static void PaintTerrainTexture(Terrain terrain, Vector3 worldPos,
        int layerIndex, float worldRadius, float noiseStrength, float noiseFrequency)
    {
        TerrainData td = terrain.terrainData;
        if (layerIndex < 0 || layerIndex >= td.alphamapLayers) return;
        Vector3 local = worldPos - terrain.transform.position;
        int mapW = td.alphamapWidth, mapH = td.alphamapHeight;
        int cx = Mathf.RoundToInt(local.x/td.size.x*mapW);
        int cz = Mathf.RoundToInt(local.z/td.size.z*mapH);
        int pr = Mathf.Max(1, Mathf.RoundToInt(worldRadius/td.size.x*mapW));
        int pad = Mathf.CeilToInt(pr*noiseStrength*0.6f)+1;
        int x0 = Mathf.Clamp(cx-pr-pad,0,mapW-1), z0 = Mathf.Clamp(cz-pr-pad,0,mapH-1);
        int x1 = Mathf.Clamp(cx+pr+pad,0,mapW-1), z1 = Mathf.Clamp(cz+pr+pad,0,mapH-1);
        int w = x1-x0+1, h = z1-z0+1;
        float[,,] maps = td.GetAlphamaps(x0, z0, w, h);
        int nLyr = maps.GetLength(2);
        float noiseOffX = worldPos.x*3.7f, noiseOffZ = worldPos.z*3.7f;
        for (int zi = 0; zi < h; zi++)
        for (int xi = 0; xi < w; xi++)
        {
            float dx = (x0+xi)-cx, dz = (z0+zi)-cz;
            float dist = Mathf.Sqrt(dx*dx+dz*dz);
            float noiseVal = Mathf.PerlinNoise((xi+noiseOffX)*noiseFrequency,(zi+noiseOffZ)*noiseFrequency);
            float effectiveR = pr*(1f+noiseStrength*(noiseVal-0.5f));
            if (dist > effectiveR) continue;
            float blend = Mathf.Clamp01(1f-Mathf.Pow(dist/Mathf.Max(effectiveR,0.001f),2f));
            for (int l = 0; l < nLyr; l++) maps[zi,xi,l] *= (1f-blend);
            if (layerIndex < nLyr) maps[zi,xi,layerIndex] += blend;
            float sum = 0f;
            for (int l = 0; l < nLyr; l++) sum += maps[zi,xi,l];
            if (sum > 0f) for (int l = 0; l < nLyr; l++) maps[zi,xi,l] /= sum;
        }
        td.SetAlphamaps(x0, z0, maps);
    }

    // ─────────────────────────────────────────────────────────────────
    //  Scene view gizmos + interactive polygon handles
    // ─────────────────────────────────────────────────────────────────

    private void OnEnable()  => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        // ── Green global scatter area ─────────────────────────────────
        DrawRect(_center, _areaWidth, _areaLength,
            new Color(0.2f,1f,0.4f,0.06f), new Color(0.2f,1f,0.4f,0.9f));
        EditorGUI.BeginChangeCheck();
        Vector3 nc = Handles.PositionHandle(_center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Move Scatter Center");
            _center = nc; Repaint();
        }

        // ── Polygon zones ─────────────────────────────────────────────
        for (int zi = 0; zi < _zones.Count; zi++)
        {
            var Z = _zones[zi];
            if (!Z.enabled || Z.points.Count < 2) continue;

            float y = Z.zoneMarker != null
                ? Z.zoneMarker.transform.position.y
                : (Z.points.Count > 0 ? Z.points[0].y : 0f);

            // ── Draw filled polygon ───────────────────────────────────
            // Outline edges
            Handles.color = new Color(1f, 0.85f, 0.1f, 0.85f);
            for (int p = 0; p < Z.points.Count; p++)
            {
                int next = (p+1) % Z.points.Count;
                Vector3 a = new Vector3(Z.points[p].x,    y, Z.points[p].z);
                Vector3 b = new Vector3(Z.points[next].x, y, Z.points[next].z);
                Handles.DrawAAPolyLine(3f, a, b);
            }

            // Subtle fill via triangles from centroid
            Vector3 centroid = Z.Centroid();
            centroid.y = y;
            Handles.color = new Color(1f, 0.9f, 0.1f, 0.05f);
            for (int p = 0; p < Z.points.Count; p++)
            {
                int next = (p+1) % Z.points.Count;
                Vector3 a = new Vector3(Z.points[p].x,    y, Z.points[p].z);
                Vector3 b = new Vector3(Z.points[next].x, y, Z.points[next].z);
                Handles.DrawAAConvexPolygon(centroid, a, b);
            }

            // Zone label at centroid
            Handles.Label(centroid + Vector3.up*0.8f, $"  {Z.label}",
                new GUIStyle(EditorStyles.boldLabel)
                    { normal = { textColor = new Color(1f,0.9f,0.2f) } });

            // ── Draggable dot per point ───────────────────────────────
            for (int p = 0; p < Z.points.Count; p++)
            {
                Vector3 worldPt = new Vector3(Z.points[p].x, y, Z.points[p].z);
                float   dotSize = HandleUtility.GetHandleSize(worldPt) * 0.07f;
                bool    isSel   = Z.selectedPt == p;

                // Colour: selected = white, unselected = yellow
                Handles.color = isSel
                    ? Color.white
                    : new Color(1f, 0.85f, 0.15f, 1f);

                EditorGUI.BeginChangeCheck();
                Vector3 newPt = Handles.FreeMoveHandle(
                    worldPt, dotSize, Vector3.zero, Handles.DotHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move Zone Point");
                    Z.points[p] = new Vector3(newPt.x, Z.points[p].y, newPt.z);
                    Z.selectedPt = p;
                    Repaint();
                }

                // Point index label next to dot
                Handles.Label(worldPt + Vector3.up*0.3f + Vector3.right*dotSize*1.5f,
                    $"P{p}",
                    new GUIStyle(EditorStyles.miniLabel)
                        { normal = { textColor = isSel
                            ? Color.white
                            : new Color(1f,0.9f,0.4f) } });
            }

            // ── Edge midpoints: click to insert a new point ───────────
            Handles.color = new Color(1f,0.85f,0.1f,0.5f);
            for (int p = 0; p < Z.points.Count; p++)
            {
                int next  = (p+1) % Z.points.Count;
                Vector3 a = new Vector3(Z.points[p].x,    y, Z.points[p].z);
                Vector3 b = new Vector3(Z.points[next].x, y, Z.points[next].z);
                Vector3 mid = (a+b)*0.5f;
                float   sz  = HandleUtility.GetHandleSize(mid)*0.045f;

                if (Handles.Button(mid, Quaternion.identity, sz, sz*1.5f, Handles.DotHandleCap))
                {
                    Undo.RecordObject(this, "Insert Zone Point");
                    Z.points.Insert(p+1, new Vector3(mid.x, Z.points[p].y, mid.z));
                    Z.selectedPt = p+1;
                    Repaint();
                    break;
                }
            }

            Handles.color = Color.white;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    //  Shared helpers
    // ─────────────────────────────────────────────────────────────────

    private bool RaycastGround(float wx, float wz, out RaycastHit hit)
    {
        var origin = new Vector3(wx, _center.y+_raycastHeight, wz);
        return Physics.Raycast(origin, Vector3.down, out hit, _raycastHeight*2f, _groundMask);
    }

    private static GameObject SpawnObject(GameObject prefab, RaycastHit hit,
        Transform parent, float scale, bool randRotY, bool alignNormal)
    {
        Quaternion rot = alignNormal
            ? Quaternion.FromToRotation(Vector3.up, hit.normal) : Quaternion.identity;
        if (randRotY) rot *= Quaternion.Euler(0, Random.Range(0f,360f), 0);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.SetPositionAndRotation(hit.point, rot);
        go.transform.localScale = Vector3.one * scale;
        Undo.RegisterCreatedObjectUndo(go, "Scatter Object");
        return go;
    }

    private static float GetObjectFootprintRadius(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return Mathf.Max(b.extents.x, b.extents.z);
        }
        var cols = go.GetComponentsInChildren<Collider>();
        if (cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            foreach (var c in cols) b.Encapsulate(c.bounds);
            return Mathf.Max(b.extents.x, b.extents.z);
        }
        return go.transform.localScale.x*0.5f;
    }

    private static Terrain GetTargetTerrain(Vector3 pos)
    {
        foreach (var t in Terrain.activeTerrains)
        {
            var td = t.terrainData; var tp = t.transform.position;
            if (pos.x>=tp.x && pos.x<=tp.x+td.size.x &&
                pos.z>=tp.z && pos.z<=tp.z+td.size.z) return t;
        }
        return Terrain.activeTerrain;
    }

    private static GameObject PickPrefab(List<GameObject> list)
    {
        var valid = list.FindAll(p => p != null);
        return valid.Count == 0 ? null : valid[Random.Range(0, valid.Count)];
    }

    private void ClearAll()
    {
        for (int i=0;i<_layers.Count;i++) ClearLayer(i);
        for (int i=0;i<_zones.Count;i++)  ClearZone(i);
    }
    private void ClearLayer(int i) => ClearTracked(_spawnedByLayer, i);
    private void ClearZone(int i)  => ClearTracked(_spawnedByZone,  i);
    private static void ClearTracked(Dictionary<int,List<GameObject>> dict, int key)
    {
        if (!dict.TryGetValue(key, out var list)) return;
        foreach (var go in list) if (go) Undo.DestroyObjectImmediate(go);
        list.Clear();
    }
    private static void RegisterSpawned(Dictionary<int,List<GameObject>> dict, int key, GameObject go)
    {
        if (!dict.ContainsKey(key)) dict[key] = new List<GameObject>();
        dict[key].Add(go);
    }
    private GameObject GetOrCreateParent()
    {
        if (_scatterParent) return _scatterParent;
        var go = new GameObject("=== Scatter Root ===");
        Undo.RegisterCreatedObjectUndo(go, "Create Scatter Root");
        _scatterParent = go;
        return go;
    }

    // ─────────────────────────────────────────────────────────────────
    //  GUI utilities
    // ─────────────────────────────────────────────────────────────────

    private static void Separator()
    {
        Rect r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0.3f,0.3f,0.3f,0.5f));
        EditorGUILayout.Space(2);
    }
    private static void ScaleRange(ref float min, ref float max)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Scale Range", GUILayout.Width(85));
        min = EditorGUILayout.FloatField(min, GUILayout.Width(50));
        EditorGUILayout.LabelField("-", GUILayout.Width(12));
        max = EditorGUILayout.FloatField(max, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
    }
    private static GameObject PrefabField(string label, GameObject cur)
        => (GameObject)EditorGUILayout.ObjectField(label, cur, typeof(GameObject), false);
    private static LayerMask LayerMaskField(string label, LayerMask mask)
    {
        var names = new List<string>(); var nums = new List<int>();
        for (int i=0;i<32;i++) { string n=LayerMask.LayerToName(i); if(!string.IsNullOrEmpty(n)){names.Add(n);nums.Add(i);} }
        int flags=0;
        for (int i=0;i<nums.Count;i++) if((mask&(1<<nums[i]))!=0) flags|=(1<<i);
        flags = EditorGUILayout.MaskField(label, flags, names.ToArray());
        int result=0;
        for (int i=0;i<nums.Count;i++) if((flags&(1<<i))!=0) result|=(1<<nums[i]);
        return result;
    }
    private static void DrawRect(Vector3 c, float w, float l, Color fill, Color outline)
    {
        float hw=w*0.5f, hl=l*0.5f;
        Vector3[] corners = {
            new Vector3(c.x-hw,c.y,c.z-hl), new Vector3(c.x+hw,c.y,c.z-hl),
            new Vector3(c.x+hw,c.y,c.z+hl), new Vector3(c.x-hw,c.y,c.z+hl),
        };
        Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
    }
}
#endif