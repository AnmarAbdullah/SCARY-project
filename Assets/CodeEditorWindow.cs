// ╔══════════════════════════════════════════════════════════════╗
// ║        UNITY IN-ENGINE CODE EDITOR  —  Custom Renderer      ║
// ║  Drop inside any  Editor/  folder in your project           ║
// ╚══════════════════════════════════════════════════════════════╝
//  No Unity TextArea — each line is drawn token-by-token so every
//  keyword, string, comment, etc. gets its own colour.
//
//  SHORTCUTS
//    Arrows / Home / End / PgUp / PgDn  — navigation
//    Shift + any navigation             — extend selection
//    Ctrl+A                             — select all
//    Ctrl+C / Ctrl+X / Ctrl+V          — copy / cut / paste
//    Tab                               — indent 4 spaces
//    Shift+Tab                         — unindent 4 spaces
//    Enter                             — newline with auto-indent
//    Backspace / Delete                — delete
//    Ctrl+D                            — duplicate line
//    Ctrl+/                            — toggle // comment
//    Ctrl+S                            — save
//    Ctrl+Shift+S                      — save & compile
//    Ctrl+F                            — search
//    Ctrl+ / Ctrl-                     — zoom in / out
//    Ctrl+0                            — reset zoom

using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class CodeEditorWindow : EditorWindow
{
    // ── File ──────────────────────────────────────────────────────
    private string filePath = "";
    private string fileName = "";
    private string fileExt  = "";
    private bool   isDirty  = false;

    // ── Text Model ────────────────────────────────────────────────
    private List<string> lines = new List<string> { "" };
    private bool needsRebuild  = false;
    private bool cursorMoved   = false;
    private bool isTextDragging = false; // true only when drag started in text area // only auto-scroll when cursor actually moved

    // ── Cursor ────────────────────────────────────────────────────
    private int  curLine = 0;
    private int  curCol  = 0;

    // ── Selection ─────────────────────────────────────────────────
    private int  ancLine = 0;   // anchor = fixed end of selection
    private int  ancCol  = 0;
    private bool hasSel  = false;

    // ── View ──────────────────────────────────────────────────────
    private Vector2 scroll;
    private bool    editorFocused = false;
    private float   fontSize  = 13f;
    private float   charW     = 8f;    // measured at style build time
    private float   lineH     = 18f;   // measured at style build time
    private bool    showSearch = false;
    private string  searchQuery = "";
    private int     matchCount  = 0;

    // ── Cursor blink ──────────────────────────────────────────────
    private double lastBlink    = 0;
    private bool   showCursor   = true;
    private const double BLINK  = 0.53;

    // ── Syntax cache ──────────────────────────────────────────────
    struct Token { public string text; public Color color;
                   public Token(string t, Color c){ text=t; color=c; } }
    private List<List<Token>> cache = new List<List<Token>>();

    // ── Styles ────────────────────────────────────────────────────
    private GUIStyle             sBase;    // monospace, GUIStyle.none
    private Dictionary<Color,GUIStyle> sColored = new Dictionary<Color,GUIStyle>();
    private GUIStyle             sLineNum;
    private GUIStyle             sPlaceholder;
    private GUIStyle             sSearch;
    private bool   stylesBuilt = false;
    private float  builtAt     = -1f;

    // ── Theme ─────────────────────────────────────────────────────
    static readonly Color BG       = new Color(0.118f,0.118f,0.133f);
    static readonly Color HDR      = new Color(0.16f, 0.16f, 0.19f);
    static readonly Color GUT      = new Color(0.14f, 0.14f, 0.16f);
    static readonly Color BRD      = new Color(0.25f, 0.25f, 0.30f);
    static readonly Color C_TEXT   = new Color(0.85f, 0.85f, 0.85f);
    static readonly Color C_LNUM   = new Color(0.40f, 0.40f, 0.46f);
    static readonly Color C_DIRTY  = new Color(1.00f, 0.72f, 0.20f);
    static readonly Color C_SAVE   = new Color(0.18f, 0.45f, 0.78f);
    static readonly Color C_BUILD  = new Color(0.18f, 0.62f, 0.38f);
    static readonly Color C_OK     = new Color(0.40f, 0.78f, 0.40f);
    static readonly Color C_CURL   = new Color(1f,    1f,    1f,    0.035f);
    static readonly Color C_SEL    = new Color(0.26f, 0.46f, 0.70f, 0.45f);
    static readonly Color C_CUR    = new Color(0.85f, 0.85f, 0.85f, 1f);

    // ── Syntax Colors ─────────────────────────────────────────────
    static readonly Color S_KW  = new Color(0.337f,0.612f,0.839f);  // blue   keywords
    static readonly Color S_TY  = new Color(0.306f,0.788f,0.690f);  // teal   types
    static readonly Color S_ST  = new Color(0.808f,0.569f,0.471f);  // orange strings
    static readonly Color S_CM  = new Color(0.416f,0.600f,0.333f);  // green  comments
    static readonly Color S_NM  = new Color(0.710f,0.808f,0.659f);  // lime   numbers
    static readonly Color S_PP  = new Color(0.773f,0.525f,0.753f);  // purple preprocessor
    static readonly Color S_AT  = new Color(0.863f,0.863f,0.667f);  // yellow attributes

    static readonly HashSet<string> KEYWORDS = new HashSet<string>
    {
        "abstract","as","base","bool","break","byte","case","catch","char",
        "checked","class","const","continue","decimal","default","delegate",
        "do","double","else","enum","event","explicit","extern","false",
        "finally","fixed","float","for","foreach","goto","if","implicit",
        "in","int","interface","internal","is","lock","long","namespace",
        "new","null","object","operator","out","override","params","private",
        "protected","public","readonly","ref","return","sbyte","sealed",
        "short","sizeof","stackalloc","static","string","struct","switch",
        "this","throw","true","try","typeof","uint","ulong","unchecked",
        "unsafe","ushort","using","virtual","void","volatile","while",
        "var","async","await","yield","get","set","value","add","remove",
        "partial","where","nameof","record","init","with","global","when",
        "and","or","not"
    };
    static readonly HashSet<string> SHADER_KW = new HashSet<string>
    {
        "Shader","Properties","SubShader","Pass","Tags","LOD","Blend","ZWrite",
        "ZTest","Cull","CGPROGRAM","ENDCG","HLSLPROGRAM","ENDHLSL",
        "float","float2","float3","float4","half","half2","half3","half4",
        "fixed","fixed2","fixed3","fixed4","int","uint","bool","void","return",
        "if","else","for","while","do","struct","in","out","inout","true","false",
        "sampler2D","sampler3D","samplerCUBE","Texture2D","SamplerState",
        "uniform","varying","attribute","mul","normalize","dot","cross",
        "reflect","pow","sqrt","abs","max","min","clamp","lerp","saturate",
        "frac","floor","ceil","sin","cos","tan","length","tex2D","texCUBE",
        "SV_POSITION","SV_Target","TEXCOORD0","TEXCOORD1","COLOR","NORMAL",
        "UnityObjectToClipPos","TRANSFORM_TEX","#pragma","#include","vertex","fragment"
    };

    // ══════════════════════════════════════════════════════════════
    //  Window
    // ══════════════════════════════════════════════════════════════

    [MenuItem("Window/Code Editor %F12")]
    public static void ShowWindow()
    {
        var w = GetWindow<CodeEditorWindow>();
        w.titleContent = new GUIContent(" Code Editor",
            EditorGUIUtility.IconContent("cs Script Icon").image);
        w.minSize = new Vector2(600, 400);
    }

    void OnEnable()
    {
        Selection.selectionChanged += OnSelect;
        EditorApplication.update   += Tick;
        OnSelect();
    }
    void OnDisable()
    {
        Selection.selectionChanged -= OnSelect;
        EditorApplication.update   -= Tick;
    }

    void Tick()
    {
        if (EditorApplication.timeSinceStartup - lastBlink >= BLINK)
        {
            lastBlink   = EditorApplication.timeSinceStartup;
            showCursor  = !showCursor;
            if (editorFocused) Repaint();
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  File loading
    // ══════════════════════════════════════════════════════════════

    void OnSelect()
    {
        var obj = Selection.activeObject;
        if (obj == null) return;
        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return;
        string ext = Path.GetExtension(path).ToLower();
        if (ext != ".cs" && ext != ".shader" && ext != ".hlsl"
                         && ext != ".glsl"   && ext != ".cginc") return;

        if (isDirty && !string.IsNullOrEmpty(filePath))
            if (!EditorUtility.DisplayDialog("Unsaved Changes",
                    $"'{fileName}' has unsaved changes. Switch anyway?","Switch","Stay"))
                return;

        filePath = path;
        fileName = Path.GetFileName(path);
        fileExt  = ext;
        SetText(File.ReadAllText(path));
        isDirty = false; curLine=0; curCol=0; hasSel=false; scroll=Vector2.zero;
        Repaint();
    }

    void SetText(string raw)
    {
        raw   = raw.Replace("\r\n","\n").Replace("\r","\n");
        lines = new List<string>(raw.Split('\n'));
        needsRebuild = true;
    }

    string GetText() => string.Join("\n", lines);

    // ══════════════════════════════════════════════════════════════
    //  Styles
    // ══════════════════════════════════════════════════════════════

    void BuildStyles()
    {
        if (stylesBuilt && Mathf.Approximately(builtAt, fontSize)) return;

        sColored.Clear();
        int fs = Mathf.RoundToInt(fontSize);

        Font mono = null;
        foreach (var n in new[]{"Consolas","Courier New","Monaco","Lucida Console","Cascadia Code"})
        { mono = Font.CreateDynamicFontFromOSFont(n,fs); if (mono!=null) break; }

        sBase = new GUIStyle(GUIStyle.none)
        {
            font=mono, fontSize=fs, wordWrap=false, richText=false,
            padding=new RectOffset(0,0,0,0), margin=new RectOffset(0,0,0,0),
            clipping=TextClipping.Clip
        };
        sBase.normal.textColor = C_TEXT;

        var sz = sBase.CalcSize(new GUIContent("W"));
        charW  = sz.x;
        lineH  = sz.y + 2f;

        sLineNum = new GUIStyle(GUIStyle.none)
        {
            font=mono, fontSize=fs, wordWrap=false, richText=false,
            alignment=TextAnchor.UpperRight,
            padding=new RectOffset(4,8,1,0), clipping=TextClipping.Clip
        };
        sLineNum.normal.textColor = C_LNUM;

        sPlaceholder = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        { fontSize=15, fontStyle=FontStyle.Italic, alignment=TextAnchor.MiddleCenter };
        sPlaceholder.normal.textColor = new Color(0.4f,0.4f,0.5f);

        sSearch = new GUIStyle(EditorStyles.toolbarSearchField){ fontSize=12 };

        stylesBuilt  = true;
        builtAt      = fontSize;
        needsRebuild = true;
    }

    GUIStyle ColorStyle(Color c)
    {
        if (!sColored.TryGetValue(c, out var st))
        { st = new GUIStyle(sBase); st.normal.textColor=c; sColored[c]=st; }
        return st;
    }

    // ══════════════════════════════════════════════════════════════
    //  Syntax Tokeniser
    // ══════════════════════════════════════════════════════════════

    void RebuildCache()
    {
        cache.Clear();
        bool shader = fileExt != ".cs";
        bool inBlock = false;
        foreach (var line in lines)
            cache.Add(TokeniseLine(line, shader, ref inBlock));
        needsRebuild = false;
    }

    void InvalidateLine(int from)
    {
        // Re-tokenise from 'from' to end (block-comment propagation)
        if (cache.Count == 0) { needsRebuild=true; return; }
        bool shader = fileExt != ".cs";
        bool inBlock = from==0 ? false : GetBlockStateAfter(from-1);
        for (int i=from; i<lines.Count; i++)
        {
            List<Token> tok = TokeniseLine(lines[i], shader, ref inBlock);
            if (i < cache.Count) cache[i]=tok; else cache.Add(tok);
        }
        while (cache.Count > lines.Count) cache.RemoveAt(cache.Count-1);
    }

    bool GetBlockStateAfter(int lineIdx)
    {
        // Scan the cached tokens for line lineIdx to find its end-block-comment state
        if (lineIdx < 0 || lineIdx >= cache.Count) return false;
        bool state = false;
        foreach (var t in cache[lineIdx])
            if (t.color == S_CM) { /* comment tokens might span across — simplified */ }
        // For correctness, do a quick re-scan of the raw line
        bool dummy = false;
        bool shader = fileExt != ".cs";
        string raw = lines[lineIdx];
        // simple pass: does the line end inside a block comment?
        TokeniseLine(raw, shader, ref dummy); // note: we need the starting state
        // This simplified version just triggers a full rebuild for block-comment changes
        return state;
    }

    List<Token> TokeniseLine(string line, bool shader, ref bool inBlock)
    {
        var result = new List<Token>();
        var kws    = shader ? SHADER_KW : KEYWORDS;
        int pos    = 0;
        int len    = line.Length;

        // If we're inside a block comment from a previous line
        if (inBlock)
        {
            int close = line.IndexOf("*/");
            if (close < 0) { result.Add(new Token(line, S_CM)); return result; }
            result.Add(new Token(line.Substring(0, close+2), S_CM));
            inBlock = false;
            pos     = close+2;
        }

        while (pos < len)
        {
            char c = line[pos];

            // Block comment open
            if (c=='/' && pos+1<len && line[pos+1]=='*')
            {
                int close = line.IndexOf("*/", pos+2);
                if (close >= 0)
                {
                    result.Add(new Token(line.Substring(pos, close-pos+2), S_CM));
                    pos = close+2; continue;
                }
                result.Add(new Token(line.Substring(pos), S_CM));
                inBlock = true; return result;
            }

            // Line comment
            if (c=='/' && pos+1<len && line[pos+1]=='/')
            {
                result.Add(new Token(line.Substring(pos), S_CM));
                return result;
            }

            // C# attribute  [Something]
            if (!shader && c=='[')
            {
                int close = line.IndexOf(']', pos+1);
                if (close >= 0)
                {
                    result.Add(new Token(line.Substring(pos, close-pos+1), S_AT));
                    pos = close+1; continue;
                }
            }

            // Preprocessor  #...
            if (c=='#')
            {
                int e = pos+1;
                while (e<len && (char.IsLetterOrDigit(line[e]) || line[e]=='_')) e++;
                result.Add(new Token(line.Substring(pos, e-pos), S_PP));
                pos = e; continue;
            }

            // String  "..."
            if (c=='"' || c=='\'')
            {
                char q = c; int e = pos+1;
                while (e<len) { if (line[e]=='\\'){e+=2;continue;} if (line[e]==q){e++;break;} e++; }
                e = Mathf.Min(e, len);
                result.Add(new Token(line.Substring(pos, e-pos), S_ST));
                pos = e; continue;
            }

            // Verbatim string  @"..."
            if (c=='@' && pos+1<len && line[pos+1]=='"')
            {
                int e=pos+2;
                while (e<len && line[e]!='"') e++;
                e = Mathf.Min(e+1, len);
                result.Add(new Token(line.Substring(pos, e-pos), S_ST));
                pos = e; continue;
            }

            // Number
            if (char.IsDigit(c))
            {
                int e=pos;
                while (e<len && (char.IsLetterOrDigit(line[e]) || line[e]=='.')) e++;
                result.Add(new Token(line.Substring(pos, e-pos), S_NM));
                pos = e; continue;
            }

            // Identifier / keyword / type
            if (char.IsLetter(c) || c=='_')
            {
                int e=pos;
                while (e<len && (char.IsLetterOrDigit(line[e]) || line[e]=='_')) e++;
                string word = line.Substring(pos, e-pos);

                Color col;
                if (kws.Contains(word))       col = S_KW;
                else if (char.IsUpper(word[0]) && word.Length>1) col = S_TY;
                else                          col = C_TEXT;

                result.Add(new Token(word, col));
                pos = e; continue;
            }

            // Everything else  (operators, punctuation, whitespace, <, >, etc.)
            // Collect run of "plain" chars for efficiency
            int end = pos;
            while (end<len)
            {
                char ch = line[end];
                if (ch=='/' || ch=='"' || ch=='\'' || ch=='#' || ch=='@' ||
                    char.IsDigit(ch) || char.IsLetter(ch) || ch=='_' ||
                    (!shader && ch=='[')) break;
                end++;
            }
            if (end==pos) end=pos+1;
            result.Add(new Token(line.Substring(pos, end-pos), C_TEXT));
            pos = end;
        }
        return result;
    }

    // ══════════════════════════════════════════════════════════════
    //  OnGUI
    // ══════════════════════════════════════════════════════════════

    void OnGUI()
    {
        BuildStyles();
        if (needsRebuild) RebuildCache();

        EditorGUI.DrawRect(new Rect(0,0,position.width,position.height), BG);
        HandleWindowKeys();
        DrawHeader();

        if (string.IsNullOrEmpty(filePath)) { DrawPlaceholder(); return; }
        if (showSearch) DrawSearchBar();

        float top = showSearch ? 80f : 44f;
        DrawEditor(top);
        DrawFooter();
    }

    // ══════════════════════════════════════════════════════════════
    //  Header / Search / Placeholder / Footer
    // ══════════════════════════════════════════════════════════════

    void DrawHeader()
    {
        const float H = 44f;
        EditorGUI.DrawRect(new Rect(0,0,position.width,H), HDR);
        EditorGUI.DrawRect(new Rect(0,H-1,position.width,1), BRD);

        GUILayout.BeginArea(new Rect(0,0,position.width,H));
        GUILayout.BeginHorizontal(GUILayout.Height(H));
        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(fileExt))
        {
            var ico = EditorGUIUtility.IconContent(
                fileExt==".cs" ? "cs Script Icon" : "Shader Icon").image;
            if (ico!=null) GUILayout.Label(ico, GUILayout.Width(18), GUILayout.Height(H));
            GUILayout.Space(6);
        }

        var ns = new GUIStyle(EditorStyles.boldLabel){fontSize=13,alignment=TextAnchor.MiddleLeft};
        ns.normal.textColor = isDirty ? C_DIRTY : C_TEXT;
        string title = string.IsNullOrEmpty(filePath) ? "" : (isDirty ? "● "+fileName : fileName);
        GUILayout.Label(title, ns, GUILayout.Height(H));

        if (!string.IsNullOrEmpty(fileExt))
        {
            var bs = new GUIStyle(EditorStyles.miniLabel)
                {fontSize=10,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(6,6,2,2)};
            bs.normal.textColor = new Color(0.55f,0.82f,1f);
            GUILayout.Space(6);
            GUILayout.Label(fileExt.ToUpperInvariant(), bs, GUILayout.Height(H));
        }

        GUILayout.FlexibleSpace();
        var tb = new GUIStyle(EditorStyles.miniButton){fontSize=11,padding=new RectOffset(8,8,3,3)};
        if (GUILayout.Button(showSearch?"✕ Search":"⌕ Search",tb,GUILayout.Height(26),GUILayout.Width(82)))
            showSearch = !showSearch;
        GUILayout.Space(4);
        var zs = new GUIStyle(EditorStyles.miniLabel){fontSize=10,alignment=TextAnchor.MiddleCenter};
        zs.normal.textColor = C_LNUM;
        GUILayout.Label($"{(int)fontSize}px",zs,GUILayout.Width(34),GUILayout.Height(26));
        GUILayout.Space(8);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void DrawSearchBar()
    {
        EditorGUI.DrawRect(new Rect(0,44,position.width,36),new Color(0.16f,0.16f,0.19f));
        EditorGUI.DrawRect(new Rect(0,79,position.width,1),BRD);
        GUILayout.Space(44);
        GUILayout.BeginHorizontal(GUILayout.Height(36));
        GUILayout.Space(12);
        GUILayout.Label("Find:",GUILayout.Width(36),GUILayout.Height(36));
        string q=GUILayout.TextField(searchQuery,sSearch,GUILayout.Height(22),GUILayout.MaxWidth(260));
        if (q!=searchQuery){searchQuery=q;matchCount=CountMatches(GetText(),q);}
        if (!string.IsNullOrEmpty(searchQuery))
        {
            var ms=new GUIStyle(EditorStyles.miniLabel);
            ms.normal.textColor=matchCount>0?C_OK:new Color(0.9f,0.4f,0.4f);
            GUILayout.Label($"{matchCount} match{(matchCount==1?"":"es")}",ms,GUILayout.Height(36));
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕",EditorStyles.miniButton,GUILayout.Width(22),GUILayout.Height(22)))
        {showSearch=false;searchQuery="";}
        GUILayout.Space(12);
        GUILayout.EndHorizontal();
    }

    void DrawPlaceholder()
    {
        GUILayout.FlexibleSpace();
        GUILayout.Label("No script open", sPlaceholder);
        var sub=new GUIStyle(EditorStyles.centeredGreyMiniLabel){fontSize=11};
        sub.normal.textColor=new Color(0.32f,0.32f,0.4f);
        GUILayout.Label("Click any .cs or .shader file in the Project window",sub);
        GUILayout.FlexibleSpace();
    }

    void DrawFooter()
    {
        float fy=position.height-42f, W=position.width;
        EditorGUI.DrawRect(new Rect(0,fy,W,1),BRD);
        EditorGUI.DrawRect(new Rect(0,fy+1,W,41),HDR);

        GUILayout.BeginArea(new Rect(0,fy+1,W,41));
        GUILayout.BeginHorizontal(GUILayout.Height(41));
        GUILayout.Space(12);

        var st=new GUIStyle(EditorStyles.miniLabel){fontSize=11};
        st.normal.textColor=isDirty?C_DIRTY:C_OK;
        GUILayout.Label(isDirty?"● Unsaved":"✓ Saved",st,GUILayout.Height(41));

        var ms=new GUIStyle(EditorStyles.miniLabel){fontSize=10};
        ms.normal.textColor=C_LNUM;
        GUILayout.Label($"   Lines {lines.Count}  ·  Chars {GetText().Length}",ms,GUILayout.Height(41));
        if (!string.IsNullOrEmpty(fileExt))
        {
            string lang=fileExt==".cs"?"C#":fileExt==".shader"?"ShaderLab":"HLSL/GLSL";
            GUILayout.Label($"   {lang}",ms,GUILayout.Height(41));
        }

        GUILayout.FlexibleSpace();

        var hs=new GUIStyle(EditorStyles.miniLabel){fontSize=9};
        hs.normal.textColor=new Color(0.35f,0.35f,0.42f);
        GUILayout.Label("Tab Indent   Ctrl+D Duplicate   Ctrl+/ Comment   Ctrl+S Save   Ctrl⇧S Compile",
            hs,GUILayout.Height(41));
        GUILayout.Space(12);

        GUI.enabled=isDirty;
        Color prev=GUI.backgroundColor;
        GUI.backgroundColor=isDirty?C_SAVE:new Color(0.28f,0.28f,0.32f);
        var btn=new GUIStyle(GUI.skin.button){fontSize=12,fontStyle=FontStyle.Bold,padding=new RectOffset(14,14,0,0)};
        btn.normal.textColor=Color.white;
        if (GUILayout.Button("💾 Save",btn,GUILayout.Height(30),GUILayout.Width(100))) Save();
        GUILayout.Space(6);
        GUI.enabled=true;
        GUI.backgroundColor=C_BUILD;
        if (GUILayout.Button("⚙ Save & Compile",btn,GUILayout.Height(30),GUILayout.Width(150))) SaveAndCompile();
        GUI.backgroundColor=prev;
        GUILayout.Space(12);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ══════════════════════════════════════════════════════════════
    //  Main Editor Area
    // ══════════════════════════════════════════════════════════════

    void DrawEditor(float top)
    {
        const float GW = 52f, FH = 42f;
        float aH = position.height - top - FH;
        float aW = position.width;

        EditorGUI.DrawRect(new Rect(0,top,GW,aH),GUT);
        EditorGUI.DrawRect(new Rect(GW,top,1,aH),BRD);
        EditorGUI.DrawRect(new Rect(GW,top,aW-GW,aH),BG);

        // Content dimensions
        float maxW = 0;
        foreach (var l in lines) maxW = Mathf.Max(maxW, l.Length * charW);
        float totH = Mathf.Max(lines.Count * lineH + 40f, aH+1f);
        float totW = Mathf.Max(maxW + 120f, aW-GW+1f);

        Rect viewRect = new Rect(GW, top, aW-GW, aH);
        Rect contRect = new Rect(0, 0, totW, totH);

        // Input before scroll view so mouse coords are correct
        HandleEditorInput(viewRect);

        Vector2 newScroll = GUI.BeginScrollView(viewRect, scroll, contRect, true, true);

            EditorGUI.DrawRect(contRect, BG);

            int first = Mathf.Max(0, Mathf.FloorToInt(newScroll.y / lineH)-1);
            int last  = Mathf.Min(lines.Count-1, Mathf.CeilToInt((newScroll.y+aH)/lineH)+1);

            float PAD = 6f; // left padding inside gutter

            for (int i=first; i<=last; i++)
            {
                float y = i * lineH;
                float lw = lines[i].Length * charW;

                // Current-line highlight
                if (i==curLine)
                    EditorGUI.DrawRect(new Rect(0,y,totW,lineH), C_CURL);

                // Selection highlight
                if (hasSel)
                {
                    GetSelectionOrder(out int sL,out int sC,out int eL,out int eC);
                    if (i>=sL && i<=eL)
                    {
                        float x1 = PAD + (i==sL ? sC*charW : 0);
                        float x2 = PAD + (i==eL ? eC*charW : Mathf.Max(lw+charW, totW));
                        if (x2<x1) x2=x1;
                        EditorGUI.DrawRect(new Rect(x1,y,x2-x1,lineH), C_SEL);
                    }
                }

                // Syntax-coloured tokens
                if (i < cache.Count && cache[i] != null)
                {
                    float x = PAD;
                    foreach (var tok in cache[i])
                    {
                        float tw = tok.text.Length * charW;
                        GUI.Label(new Rect(x, y, tw+charW, lineH), tok.text, ColorStyle(tok.color));
                        x += tw;
                    }
                }
            }

            // Cursor
            if (editorFocused && showCursor)
            {
                float cx = PAD + curCol * charW;
                float cy = curLine * lineH;
                EditorGUI.DrawRect(new Rect(cx, cy, 2f, lineH), C_CUR);
            }

        GUI.EndScrollView();

        // Gutter (synced vertical scroll)
        GUI.BeginScrollView(new Rect(0,top,GW,aH), new Vector2(0,newScroll.y),
            new Rect(0,0,GW,totH), false,false,GUIStyle.none,GUIStyle.none);
        for (int i=first; i<=last; i++)
            GUI.Label(new Rect(0,i*lineH,GW,lineH),(i+1).ToString(),sLineNum);
        GUI.EndScrollView();

        scroll = newScroll;
        if (cursorMoved) { AutoScroll(aH, aW-GW); cursorMoved = false; }
    }

    void AutoScroll(float viewH, float viewW)
    {
        float cy = curLine * lineH;
        float cx = PAD_L + curCol * charW;
        if (cy < scroll.y)               scroll.y = cy;
        if (cy+lineH > scroll.y+viewH)   scroll.y = cy+lineH-viewH;
        if (cx < scroll.x+PAD_L)         scroll.x = Mathf.Max(0, cx-PAD_L);
        if (cx+charW > scroll.x+viewW)   scroll.x = cx+charW-viewW;
    }
    const float PAD_L = 6f;

    // ══════════════════════════════════════════════════════════════
    //  Input Handling
    // ══════════════════════════════════════════════════════════════

    void HandleWindowKeys()
    {
        var e=Event.current;
        if (e.type!=EventType.KeyDown) return;
        bool ctrl=e.control||e.command;
        if (e.keyCode==KeyCode.Escape&&showSearch){showSearch=false;e.Use();return;}
        if (!ctrl) return;
        switch (e.keyCode)
        {
            case KeyCode.S:      if(e.shift)SaveAndCompile();else Save(); e.Use();break;
            case KeyCode.F:      showSearch=!showSearch; e.Use();break;
            case KeyCode.Equals: fontSize=Mathf.Min(fontSize+1,24);stylesBuilt=false;e.Use();break;
            case KeyCode.Minus:  fontSize=Mathf.Max(fontSize-1,9); stylesBuilt=false;e.Use();break;
            case KeyCode.Alpha0: fontSize=13;stylesBuilt=false;e.Use();break;
        }
    }

    void HandleEditorInput(Rect viewRect)
    {
        var e = Event.current;

        // Click to focus + position cursor
        if (e.type==EventType.MouseDown && viewRect.Contains(e.mousePosition))
        {
            // Exclude the scrollbar strip (~16px on right + bottom edges)
            float sbW = GUI.skin.verticalScrollbar.fixedWidth + 2f;
            float sbH = GUI.skin.horizontalScrollbar.fixedHeight + 2f;
            bool inTextArea = e.mousePosition.x < viewRect.xMax - sbW &&
                              e.mousePosition.y < viewRect.yMax - sbH;

            if (inTextArea)
            {
                editorFocused = true;
                GUI.FocusControl("");
                Vector2 local = e.mousePosition - new Vector2(viewRect.x, viewRect.y) + scroll;
                local.x -= PAD_L;
                curLine = Mathf.Clamp(Mathf.FloorToInt(local.y/lineH), 0, lines.Count-1);
                curCol  = Mathf.Clamp(Mathf.RoundToInt(local.x/charW), 0, lines[curLine].Length);
                ancLine = curLine; ancCol = curCol;
                hasSel  = false;
                cursorMoved    = true;
                isTextDragging = true;
                ResetBlink();
                Repaint();
            }
            else
            {
                isTextDragging = false;
            }
        }

        if (e.type==EventType.MouseUp)
            isTextDragging = false;

        // Drag to select — only when drag started inside text area
        if (e.type==EventType.MouseDrag && isTextDragging)
        {
            Vector2 local = e.mousePosition - new Vector2(viewRect.x, viewRect.y) + scroll;
            local.x -= PAD_L;
            int newLine = Mathf.Clamp(Mathf.FloorToInt(local.y/lineH), 0, lines.Count-1);
            int newCol  = Mathf.Clamp(Mathf.RoundToInt(local.x/charW), 0, lines[newLine].Length);
            if (newLine != ancLine || newCol != ancCol)
            {
                curLine = newLine;
                curCol  = newCol;
                hasSel  = true;
            }
            Repaint();
            // No e.Use() — lets the scrollbar handle its own drag events
        }
        // Click outside unfocuses
        if (e.type==EventType.MouseDown && !viewRect.Contains(e.mousePosition))
            editorFocused = false;

        // Scroll wheel
        if (e.type==EventType.ScrollWheel && viewRect.Contains(e.mousePosition))
        {
            scroll.y += e.delta.y * lineH * 3f;
            scroll.y = Mathf.Max(0, scroll.y);
            e.Use(); Repaint();
        }

        if (!editorFocused) return;

        if (e.type==EventType.KeyDown)
        {
            ProcessKey(e);
        }
    }

    void ProcessKey(Event e)
    {
        bool ctrl  = e.control || e.command;
        bool shift = e.shift;
        ResetBlink();
        cursorMoved = true; // any key press = assume cursor may have moved

        // ── Set selection anchor before first shift-navigation ────
        bool isNav = IsNavKey(e.keyCode);
        if (shift && isNav && !hasSel)
        {
            ancLine = curLine; ancCol = curCol; hasSel = true;
        }
        if (!shift && isNav && !ctrl)
        {
            // Pressing arrow WITHOUT shift collapses selection
            // (handled per-key below)
        }

        switch (e.keyCode)
        {
            // ── Arrows ───────────────────────────────────────────
            case KeyCode.LeftArrow:
                if (hasSel && !shift) { GetSelectionOrder(out int sL,out int sC,out _,out _); curLine=sL;curCol=sC;hasSel=false; }
                else { MoveH(-1,ctrl); if(!shift) hasSel=false; }
                e.Use(); break;

            case KeyCode.RightArrow:
                if (hasSel && !shift) { GetSelectionOrder(out _,out _,out int eL,out int eC); curLine=eL;curCol=eC;hasSel=false; }
                else { MoveH(1,ctrl); if(!shift) hasSel=false; }
                e.Use(); break;

            case KeyCode.UpArrow:
                if (!shift) hasSel=false;
                if (curLine>0){curLine--;curCol=Mathf.Min(curCol,lines[curLine].Length);}
                e.Use(); break;

            case KeyCode.DownArrow:
                if (!shift) hasSel=false;
                if (curLine<lines.Count-1){curLine++;curCol=Mathf.Min(curCol,lines[curLine].Length);}
                e.Use(); break;

            case KeyCode.Home:
                if (!shift) hasSel=false;
                { int fw=FirstNonWS(curLine); curCol=(curCol==fw)?0:fw; }
                e.Use(); break;

            case KeyCode.End:
                if (!shift) hasSel=false;
                curCol=lines[curLine].Length;
                e.Use(); break;

            case KeyCode.PageUp:
                if (!shift) hasSel=false;
                curLine=Mathf.Max(0, curLine - Mathf.RoundToInt(200f/lineH));
                curCol=Mathf.Min(curCol,lines[curLine].Length);
                e.Use(); break;

            case KeyCode.PageDown:
                if (!shift) hasSel=false;
                curLine=Mathf.Min(lines.Count-1, curLine + Mathf.RoundToInt(200f/lineH));
                curCol=Mathf.Min(curCol,lines[curLine].Length);
                e.Use(); break;

            // ── Ctrl+Home/End ────────────────────────────────────
            case KeyCode.Alpha0: break; // handled in window keys

            // ── Tab ──────────────────────────────────────────────
            case KeyCode.Tab:
                if (shift) UnindentLine();
                else       TypeText("    ");
                Dirty(); e.Use(); break;

            // ── Enter ────────────────────────────────────────────
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (!ctrl)
                {
                    DeleteSel();
                    string ind = LineIndent(curLine);
                    if (lines[curLine].TrimEnd().EndsWith("{")) ind+="    ";
                    TypeText("\n"+ind);
                    Dirty(); e.Use();
                }
                break;

            // ── Backspace ────────────────────────────────────────
            case KeyCode.Backspace:
                if (hasSel) DeleteSel();
                else if (ctrl) DelWordLeft();
                else DelLeft();
                Dirty(); e.Use(); break;

            // ── Delete ───────────────────────────────────────────
            case KeyCode.Delete:
                if (hasSel) DeleteSel(); else DelRight();
                Dirty(); e.Use(); break;

            default:
                if (ctrl)
                {
                    switch (e.keyCode)
                    {
                        case KeyCode.A:  SelectAll(); e.Use(); break;
                        case KeyCode.C:  Copy();      e.Use(); break;
                        case KeyCode.X:  Copy(); DeleteSel(); Dirty(); e.Use(); break;
                        case KeyCode.V:  DeleteSel(); TypeText(GUIUtility.systemCopyBuffer.Replace("\r\n","\n").Replace("\r","\n")); Dirty(); e.Use(); break;
                        case KeyCode.D:  DupLine();   Dirty(); e.Use(); break;
                        case KeyCode.Slash: ToggleComment(); Dirty(); e.Use(); break;
                        case KeyCode.Home: hasSel=false;curLine=0;curCol=0;e.Use();break;
                        case KeyCode.End:  hasSel=false;curLine=lines.Count-1;curCol=lines[curLine].Length;e.Use();break;
                    }
                    break;
                }
                // Regular printable character
                if (e.character!=0 && !char.IsControl(e.character))
                {
                    DeleteSel();
                    TypeText(e.character.ToString());
                    Dirty(); e.Use();
                }
                break;
        }
    }

    bool IsNavKey(KeyCode k) =>
        k==KeyCode.LeftArrow||k==KeyCode.RightArrow||
        k==KeyCode.UpArrow||k==KeyCode.DownArrow||
        k==KeyCode.Home||k==KeyCode.End||
        k==KeyCode.PageUp||k==KeyCode.PageDown;

    void ResetBlink() { showCursor=true; lastBlink=EditorApplication.timeSinceStartup; }

    // ══════════════════════════════════════════════════════════════
    //  Text Operations
    // ══════════════════════════════════════════════════════════════

    void TypeText(string text)
    {
        // Split on newlines and splice into lines list
        string[] parts = text.Split('\n');
        string before  = lines[curLine].Substring(0, curCol);
        string after   = lines[curLine].Substring(curCol);

        if (parts.Length == 1)
        {
            lines[curLine] = before + parts[0] + after;
            curCol += parts[0].Length;
            InvalidateLine(curLine);
        }
        else
        {
            lines[curLine] = before + parts[0];
            for (int i=1; i<parts.Length-1; i++)
                lines.Insert(curLine+i, parts[i]);
            int last = curLine + parts.Length - 1;
            lines.Insert(last, parts[parts.Length-1] + after);
            curLine = last;
            curCol  = parts[parts.Length-1].Length;
            InvalidateLine(curLine - parts.Length + 1);
        }
    }

    void DelLeft()
    {
        if (curCol > 0)
        {
            lines[curLine] = lines[curLine].Remove(curCol-1,1);
            curCol--;
            InvalidateLine(curLine);
        }
        else if (curLine > 0)
        {
            int prev = lines[curLine-1].Length;
            lines[curLine-1] += lines[curLine];
            lines.RemoveAt(curLine);
            curLine--; curCol = prev;
            InvalidateLine(curLine);
        }
    }

    void DelRight()
    {
        if (curCol < lines[curLine].Length)
        {
            lines[curLine] = lines[curLine].Remove(curCol,1);
            InvalidateLine(curLine);
        }
        else if (curLine < lines.Count-1)
        {
            lines[curLine] += lines[curLine+1];
            lines.RemoveAt(curLine+1);
            InvalidateLine(curLine);
        }
    }

    void DelWordLeft()
    {
        if (curCol == 0) { DelLeft(); return; }
        string line = lines[curLine];
        int i = curCol-1;
        while (i>0 && line[i]==' ') i--;
        while (i>0 && (char.IsLetterOrDigit(line[i-1])||line[i-1]=='_')) i--;
        lines[curLine] = line.Remove(i, curCol-i);
        curCol = i;
        InvalidateLine(curLine);
    }

    void DeleteSel()
    {
        if (!hasSel) return;
        GetSelectionOrder(out int sL,out int sC,out int eL,out int eC);
        if (sL==eL)
        {
            lines[sL] = lines[sL].Remove(sC, eC-sC);
        }
        else
        {
            string merged = lines[sL].Substring(0,sC) + lines[eL].Substring(eC);
            lines[sL] = merged;
            lines.RemoveRange(sL+1, eL-sL);
        }
        curLine=sL; curCol=sC; hasSel=false;
        InvalidateLine(sL);
    }

    void MoveH(int dir, bool word)
    {
        if (dir < 0)
        {
            if (curCol > 0)
            {
                if (word) { int i=curCol-1; while(i>0&&lines[curLine][i-1]==' ')i--; while(i>0&&(char.IsLetterOrDigit(lines[curLine][i-1])||lines[curLine][i-1]=='_'))i--; curCol=i; }
                else curCol--;
            }
            else if (curLine > 0) { curLine--; curCol=lines[curLine].Length; }
        }
        else
        {
            string ln=lines[curLine];
            if (curCol < ln.Length)
            {
                if (word) { int i=curCol; while(i<ln.Length&&(char.IsLetterOrDigit(ln[i])||ln[i]=='_'))i++; while(i<ln.Length&&ln[i]==' ')i++; curCol=i; }
                else curCol++;
            }
            else if (curLine < lines.Count-1) { curLine++; curCol=0; }
        }
    }

    void UnindentLine()
    {
        string ln = lines[curLine];
        int rem = 0;
        while (rem<4 && rem<ln.Length && ln[rem]==' ') rem++;
        if (rem==0) return;
        lines[curLine] = ln.Substring(rem);
        curCol = Mathf.Max(0, curCol-rem);
        InvalidateLine(curLine);
    }

    void DupLine()
    {
        string ln = lines[curLine];
        lines.Insert(curLine+1, ln);
        curLine++;
        InvalidateLine(curLine-1);
    }

    void ToggleComment()
    {
        string ln = lines[curLine];
        int fw = FirstNonWS(curLine);
        if (fw+1<ln.Length && ln[fw]=='/' && ln[fw+1]=='/')
        {
            int rem = (fw+2<ln.Length && ln[fw+2]==' ') ? 3 : 2;
            lines[curLine] = ln.Remove(fw,rem);
            curCol = Mathf.Max(0, curCol-rem);
        }
        else
        {
            lines[curLine] = ln.Insert(fw,"// ");
            curCol += 3;
        }
        InvalidateLine(curLine);
    }

    void SelectAll()
    {
        ancLine=0; ancCol=0; hasSel=true;
        curLine=lines.Count-1; curCol=lines[curLine].Length;
    }

    void Copy()
    {
        if (!hasSel) { GUIUtility.systemCopyBuffer=lines[curLine]; return; }
        GetSelectionOrder(out int sL,out int sC,out int eL,out int eC);
        var sb=new StringBuilder();
        if (sL==eL) { sb.Append(lines[sL].Substring(sC,eC-sC)); }
        else
        {
            sb.AppendLine(lines[sL].Substring(sC));
            for(int i=sL+1;i<eL;i++) sb.AppendLine(lines[i]);
            sb.Append(lines[eL].Substring(0,eC));
        }
        GUIUtility.systemCopyBuffer = sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════

    void GetSelectionOrder(out int sL,out int sC,out int eL,out int eC)
    {
        sL=ancLine;sC=ancCol;eL=curLine;eC=curCol;
        if (sL>eL||(sL==eL&&sC>eC)){int t=sL;sL=eL;eL=t;t=sC;sC=eC;eC=t;}
    }

    int FirstNonWS(int lineIdx)
    {
        string ln=lines[lineIdx]; int i=0;
        while(i<ln.Length&&ln[i]==' ')i++; return i;
    }

    string LineIndent(int lineIdx)
    {
        string ln=lines[lineIdx]; int i=0;
        while(i<ln.Length&&ln[i]==' ')i++;
        return new string(' ',i);
    }

    void Dirty() { isDirty=true; Repaint(); }

    static int CountMatches(string text,string q)
    {
        if (string.IsNullOrEmpty(text)||string.IsNullOrEmpty(q)) return 0;
        int n=0,i=0; string lo=text.ToLower(),qlo=q.ToLower();
        while((i=lo.IndexOf(qlo,i))>=0){n++;i+=qlo.Length;} return n;
    }

    // ══════════════════════════════════════════════════════════════
    //  Save / Compile
    // ══════════════════════════════════════════════════════════════

    void Save()
    {
        if (string.IsNullOrEmpty(filePath)) return;
        File.WriteAllText(filePath, GetText());
        isDirty=false;
        AssetDatabase.Refresh();
        Debug.Log($"[Code Editor] Saved: {filePath}");
        Repaint();
    }

    void SaveAndCompile()
    {
        if (string.IsNullOrEmpty(filePath)) return;
        Save();
        AssetDatabase.ImportAsset(filePath,ImportAssetOptions.ForceUpdate);
        CompilationPipeline.RequestScriptCompilation();
        Debug.Log($"[Code Editor] Saved & compiling: {filePath}");
    }
}