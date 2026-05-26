// ============================================================
//  SCARY — Settings UI Generator
//  Place this file inside any /Editor folder in your project.
//  Menu: SCARY ▸ UI ▸ Generate Settings Screen
// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class SettingsUIGenerator
{
    // ── Palette ──────────────────────────────────────────────
    static readonly Color C_BG         = new Color(0.06f, 0.06f, 0.06f);
    static readonly Color C_DARK_BOX   = new Color(0.10f, 0.10f, 0.10f);
    static readonly Color C_DROPDOWN   = new Color(0.12f, 0.12f, 0.12f);
    static readonly Color C_BTN        = new Color(0.16f, 0.16f, 0.16f);
    static readonly Color C_SLIDER_BG  = new Color(0.22f, 0.22f, 0.22f);
    static readonly Color C_WHITE      = Color.white;
    static readonly Color C_GRAY       = new Color(0.60f, 0.60f, 0.60f);
    static readonly Color C_CLEAR      = new Color(0f, 0f, 0f, 0f);

    // ── Entry Point ──────────────────────────────────────────
    [MenuItem("SCARY/UI/Generate Settings Screen")]
    public static void Generate()
    {
        // Canvas
        var canvasGO = new GameObject("Settings_Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Generate Settings UI");

        var canvas            = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder   = 10;

        var scaler                    = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight     = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Root background
        var bg = MakeImg(canvasGO.transform, "Background", C_BG);
        Stretch(bg.rectTransform);

        // ── Title ────────────────────────────────────────────
        var title = MakeTMP(bg.transform, "Title_SETTINGS", "SETTINGS", 52, true);
        var tRT   = title.rectTransform;
        tRT.anchorMin        = new Vector2(0f, 1f);
        tRT.anchorMax        = new Vector2(0f, 1f);
        tRT.pivot            = new Vector2(0f, 1f);
        tRT.anchoredPosition = new Vector2(70f, -55f);
        tRT.sizeDelta        = new Vector2(500f, 80f);
        title.alignment      = TextAlignmentOptions.Left;
        title.color          = C_WHITE;

        // ── Left Panel  (Gameplay + Video) ───────────────────
        var leftPanel = MakeEmptyAnchored(bg.transform, "Panel_Left",
                            new Vector2(0.04f, 0.06f), new Vector2(0.48f, 0.80f));
        AddVLG(leftPanel, 28f);

        BuildGameplay(leftPanel);
        BuildVideo(leftPanel);

        // ── Right Panel  (Audio) ─────────────────────────────
        var rightPanel = MakeEmptyAnchored(bg.transform, "Panel_Right",
                             new Vector2(0.52f, 0.06f), new Vector2(0.96f, 0.80f));
        AddVLG(rightPanel, 28f);

        BuildAudio(rightPanel);

        // ── Apply / Cancel Buttons ───────────────────────────
        BuildButtons(bg.transform);

        Selection.activeGameObject = canvasGO;
        Debug.Log("[SCARY] Settings UI generated — object: " + canvasGO.name);
    }

    // ─────────────────────────────────────────────────────────
    //  SECTIONS
    // ─────────────────────────────────────────────────────────

    static void BuildGameplay(GameObject parent)
    {
        var sec = MakeSection(parent.transform, "Section_Gameplay");
        MakeSectionHeader(sec, "Gameplay Settings");
        MakeSliderRow(sec, "Row_MouseSensitivity", "- Mouse Sensitivity",   6f,   0f, 100f, "6/100");
        MakeSliderRow(sec, "Row_FieldOfView",       "- Field Of View",      60f,  30f,  90f, "60/90");
    }

    static void BuildVideo(GameObject parent)
    {
        var sec = MakeSection(parent.transform, "Section_Video");
        MakeSectionHeader(sec, "Video Settings");
        MakeDropdownRow(sec, "Row_VideoDropdowns", "Display Mode", "Resolution", "Graphics Quality");
        MakeSliderRow(sec, "Row_FrameRateLimit", "- Frame Rate Limit", 60f,  30f, 120f, "60/120");
        MakeSliderRow(sec, "Row_Brightness",     "- Brightness",       50f,   0f, 100f, "50/100");
        MakeSliderRow(sec, "Row_Gamma",          "- Gamma",            50f,   0f, 100f, "50/100");
        MakeToggleRow(sec, "Row_Vsync",          "- Vsync");
    }

    static void BuildAudio(GameObject parent)
    {
        var sec = MakeSection(parent.transform, "Section_Audio");
        MakeSectionHeader(sec, "Audio Settings");
        MakeSliderRow(sec, "Row_MasterVolume",  "- Master Volume",    100f, 0f, 100f, "100/100");
        MakeSliderRow(sec, "Row_MenuMusic",     "- Menu Music",       100f, 0f, 100f, "100/100");
        MakeSliderRow(sec, "Row_MicInput",      "- Microphone Input",  75f, 0f, 100f, "75/100");
        MakeSliderRow(sec, "Row_OthersMics",    "- Others Mics",       75f, 0f, 100f, "75/100");
        MakeDropdownRow(sec, "Row_AudioDropdowns", "Microphone Input Type", "Voice Chat Type");
        MakeDarkBox(sec,    "MicPreview_Box", 130f);
    }

    static void BuildButtons(Transform parent)
    {
        // Container — anchored bottom-right
        var container = new GameObject("Buttons_Container", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        var cRT              = container.GetComponent<RectTransform>();
        cRT.anchorMin        = new Vector2(1f, 0f);
        cRT.anchorMax        = new Vector2(1f, 0f);
        cRT.pivot            = new Vector2(1f, 0f);
        cRT.anchoredPosition = new Vector2(-50f, 35f);
        cRT.sizeDelta        = new Vector2(380f, 60f);

        var hlg                       = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                   = 20f;
        hlg.childAlignment            = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth     = false;
        hlg.childForceExpandHeight    = true;

        MakeButton(container.transform, "Button_Apply",  "Apply",  175f, 55f);
        MakeButton(container.transform, "Button_Cancel", "Cancel", 175f, 55f);
    }

    // ─────────────────────────────────────────────────────────
    //  ROW BUILDERS
    // ─────────────────────────────────────────────────────────

    static void MakeSliderRow(GameObject parent, string name, string label,
                               float value, float min, float max, string valueStr)
    {
        var row = MakeRow(parent.transform, name, 40f);

        // Label
        var lbl = MakeTMP(row.transform, "Label", label, 17f, false);
        lbl.color              = C_WHITE;
        lbl.alignment          = TextAlignmentOptions.Left;
        lbl.enableWordWrapping = false;
        SetLE(lbl.gameObject, minW: 215f, prefW: 215f);

        // Slider container
        var sGO = new GameObject("Slider", typeof(RectTransform));
        sGO.transform.SetParent(row.transform, false);
        var sLE = sGO.AddComponent<LayoutElement>();
        sLE.flexibleWidth = 1f;
        sLE.minHeight     = 20f;

        var slider = sGO.AddComponent<Slider>();

        //   Background track
        var trackImg = MakeImg(sGO.transform, "Background", C_SLIDER_BG);
        trackImg.rectTransform.anchorMin        = new Vector2(0f, 0.5f);
        trackImg.rectTransform.anchorMax        = new Vector2(1f, 0.5f);
        trackImg.rectTransform.sizeDelta        = new Vector2(0f, 4f);
        trackImg.rectTransform.anchoredPosition = Vector2.zero;

        //   Fill Area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sGO.transform, false);
        var faRT              = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin        = new Vector2(0f, 0.5f);
        faRT.anchorMax        = new Vector2(1f, 0.5f);
        faRT.sizeDelta        = new Vector2(-20f, 4f);
        faRT.anchoredPosition = new Vector2(-5f, 0f);

        var fillImg = MakeImg(fillArea.transform, "Fill", C_WHITE);
        fillImg.rectTransform.anchorMin = Vector2.zero;
        fillImg.rectTransform.anchorMax = new Vector2(0f, 1f);
        fillImg.rectTransform.sizeDelta = new Vector2(10f, 0f);

        //   Handle Slide Area
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sGO.transform, false);
        var haRT              = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin        = Vector2.zero;
        haRT.anchorMax        = Vector2.one;
        haRT.sizeDelta        = new Vector2(-20f, 0f);
        haRT.anchoredPosition = Vector2.zero;

        var handleImg = MakeImg(handleArea.transform, "Handle", C_WHITE);
        handleImg.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        handleImg.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        handleImg.rectTransform.sizeDelta = new Vector2(18f, 18f);

        // Wire slider
        slider.fillRect     = fillImg.rectTransform;
        slider.handleRect   = handleImg.rectTransform;
        slider.targetGraphic = handleImg;
        slider.direction    = Slider.Direction.LeftToRight;
        slider.minValue     = min;
        slider.maxValue     = max;
        slider.value        = value;

        // Value label
        var val = MakeTMP(row.transform, "Value_Text", valueStr, 15f, false);
        val.color              = C_GRAY;
        val.alignment          = TextAlignmentOptions.Right;
        val.enableWordWrapping = false;
        SetLE(val.gameObject, minW: 80f, prefW: 80f);
    }

    static void MakeDropdownRow(GameObject parent, string name, params string[] ddLabels)
    {
        var row = MakeRow(parent.transform, name, 45f);
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15f;

        foreach (var ddLabel in ddLabels)
        {
            // Dropdown root
            var ddGO = new GameObject("Dropdown_" + ddLabel.Replace(" ", "_"), typeof(RectTransform));
            ddGO.transform.SetParent(row.transform, false);
            SetLE(ddGO, minH: 40f, flexW: 1f);

            var bgImg = ddGO.AddComponent<Image>();
            bgImg.color = C_DROPDOWN;

            var dd = ddGO.AddComponent<TMP_Dropdown>();

            // Caption label
            var capLabel = MakeTMP(ddGO.transform, "Label", ddLabel, 15f, false);
            capLabel.color              = C_WHITE;
            capLabel.alignment          = TextAlignmentOptions.Left;
            capLabel.enableWordWrapping = false;
            capLabel.rectTransform.anchorMin = Vector2.zero;
            capLabel.rectTransform.anchorMax = Vector2.one;
            capLabel.rectTransform.offsetMin = new Vector2(12f, 0f);
            capLabel.rectTransform.offsetMax = new Vector2(-32f, 0f);

            // Arrow indicator
            var arrow = MakeTMP(ddGO.transform, "Arrow", "▼", 12f, false);
            arrow.color     = C_GRAY;
            arrow.alignment = TextAlignmentOptions.Center;
            var arRT              = arrow.rectTransform;
            arRT.anchorMin        = new Vector2(1f, 0.5f);
            arRT.anchorMax        = new Vector2(1f, 0.5f);
            arRT.pivot            = new Vector2(1f, 0.5f);
            arRT.anchoredPosition = new Vector2(-10f, 0f);
            arRT.sizeDelta        = new Vector2(20f, 20f);

            dd.captionText = capLabel;

            // Template (hidden — placeholder)
            var tmpl = new GameObject("Template", typeof(RectTransform));
            tmpl.transform.SetParent(ddGO.transform, false);
            var tmplImg  = tmpl.AddComponent<Image>();
            tmplImg.color = C_DROPDOWN;
            var tmplRT    = tmpl.GetComponent<RectTransform>();
            tmplRT.anchorMin        = new Vector2(0f, 0f);
            tmplRT.anchorMax        = new Vector2(1f, 0f);
            tmplRT.pivot            = new Vector2(0.5f, 1f);
            tmplRT.sizeDelta        = new Vector2(0f, 150f);
            tmplRT.anchoredPosition = Vector2.zero;

            //   Viewport
            var vp = new GameObject("Viewport", typeof(RectTransform));
            vp.transform.SetParent(tmpl.transform, false);
            Stretch(vp.GetComponent<RectTransform>());
            vp.AddComponent<RectMask2D>();

            //   Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vp.transform, false);
            var contentRT         = content.GetComponent<RectTransform>();
            contentRT.anchorMin   = new Vector2(0f, 1f);
            contentRT.anchorMax   = new Vector2(1f, 1f);
            contentRT.pivot       = new Vector2(0.5f, 1f);
            contentRT.sizeDelta   = Vector2.zero;
            var contentVLG                     = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.childForceExpandWidth   = true;
            contentVLG.childForceExpandHeight  = false;
            var contentCSF                     = content.AddComponent<ContentSizeFitter>();
            contentCSF.verticalFit             = ContentSizeFitter.FitMode.PreferredSize;

            //   Item prototype
            var item      = new GameObject("Item", typeof(RectTransform));
            item.transform.SetParent(content.transform, false);
            var itemBg    = item.AddComponent<Image>();
            itemBg.color  = C_DROPDOWN;
            var itemToggle = item.AddComponent<Toggle>();
            SetLE(item, minH: 36f);

            var itemBgChild      = MakeImg(item.transform, "Item Background", C_DROPDOWN);
            Stretch(itemBgChild.rectTransform);

            var itemChk          = MakeImg(item.transform, "Item Checkmark", C_CLEAR);
            var chkRT            = itemChk.rectTransform;
            chkRT.anchorMin      = new Vector2(0f, 0.5f);
            chkRT.anchorMax      = new Vector2(0f, 0.5f);
            chkRT.sizeDelta      = new Vector2(20f, 20f);
            chkRT.anchoredPosition = new Vector2(10f, 0f);

            var itemLbl = MakeTMP(item.transform, "Item Label", "Option", 14f, false);
            itemLbl.color     = C_WHITE;
            itemLbl.alignment = TextAlignmentOptions.Left;
            Stretch(itemLbl.rectTransform);
            itemLbl.rectTransform.offsetMin = new Vector2(25f, 0f);

            itemToggle.targetGraphic = itemBgChild;
            itemToggle.graphic       = itemChk;
            dd.itemText              = itemLbl;

            //   ScrollRect
            var sr            = tmpl.AddComponent<ScrollRect>();
            sr.content        = contentRT;
            sr.viewport       = vp.GetComponent<RectTransform>();
            sr.horizontal     = false;
            sr.movementType   = ScrollRect.MovementType.Clamped;

            dd.template = tmplRT;
            tmpl.SetActive(false);
        }
    }

    static void MakeToggleRow(GameObject parent, string name, string label)
    {
        var row = MakeRow(parent.transform, name, 40f);

        // Label
        var lbl = MakeTMP(row.transform, "Label", label, 17f, false);
        lbl.color              = C_WHITE;
        lbl.alignment          = TextAlignmentOptions.Left;
        lbl.enableWordWrapping = false;
        SetLE(lbl.gameObject, minW: 215f, prefW: 215f);

        // Single checkbox
        var tGO = new GameObject("Toggle_Vsync", typeof(RectTransform));
        tGO.transform.SetParent(row.transform, false);
        SetLE(tGO, minW: 30f, prefW: 30f, minH: 30f, prefH: 30f);

        var tBg = tGO.AddComponent<Image>();
        tBg.color = C_SLIDER_BG;

        var checkmark = MakeTMP(tGO.transform, "Checkmark", "✓", 20f, true);
        checkmark.color     = C_WHITE;
        checkmark.alignment = TextAlignmentOptions.Center;
        Stretch(checkmark.rectTransform);

        var toggle         = tGO.AddComponent<Toggle>();
        toggle.targetGraphic = tBg;
        toggle.graphic     = checkmark;
        toggle.isOn        = true;

        // Right spacer so toggle stays left
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(row.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    static void MakeDarkBox(GameObject parent, string name, float height)
    {
        var box = MakeImg(parent.transform, name, C_DARK_BOX);
        var le  = box.gameObject.AddComponent<LayoutElement>();
        le.minHeight       = height;
        le.preferredHeight = height;
        le.flexibleWidth   = 1f;
    }

    // ─────────────────────────────────────────────────────────
    //  SHARED BUILDERS
    // ─────────────────────────────────────────────────────────

    /// <summary>Creates a vertical-layout section with auto-height.</summary>
    static GameObject MakeSection(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        AddVLG(go, 10f);
        var csf            = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit    = ContentSizeFitter.FitMode.PreferredSize;
        var le             = go.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1f;
        return go;
    }

    static void MakeSectionHeader(GameObject parent, string text)
    {
        var hdr = MakeTMP(parent.transform,
                          "Header_" + text.Replace(" ", ""),
                          text, 23f, false);
        hdr.color              = C_WHITE;
        hdr.alignment          = TextAlignmentOptions.Left;
        hdr.enableWordWrapping = false;
        SetLE(hdr.gameObject, minH: 45f, prefH: 45f);
    }

    static GameObject MakeRow(Transform parent, string name, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var hlg                    = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 15f;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        SetLE(go, minH: height, prefH: height, flexW: 1f);
        return go;
    }

    static void MakeButton(Transform parent, string name, string label, float w, float h)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = C_BTN;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        SetLE(go, minW: w, prefW: w, minH: h, prefH: h);

        var lbl = MakeTMP(go.transform, "Label", label, 18f, false);
        lbl.color     = C_WHITE;
        lbl.alignment = TextAlignmentOptions.Center;
        Stretch(lbl.rectTransform);
    }

    // ─────────────────────────────────────────────────────────
    //  PRIMITIVES
    // ─────────────────────────────────────────────────────────

    static Image MakeImg(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
                                    float size, bool bold)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = size;
        tmp.color      = Color.white;
        tmp.fontStyle  = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    /// <summary>Creates an empty RectTransform anchored between two points (0–1).</summary>
    static GameObject MakeEmptyAnchored(Transform parent, string name,
                                         Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt        = go.GetComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
        return go;
    }

    // ─────────────────────────────────────────────────────────
    //  UTILITIES
    // ─────────────────────────────────────────────────────────

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void AddVLG(GameObject go, float spacing)
    {
        var vlg                    = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = spacing;
        vlg.childAlignment         = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
    }

    /// <summary>Shorthand LayoutElement setter — only assigns non-zero values.</summary>
    static void SetLE(GameObject go,
                      float minW  = 0f, float prefW = 0f, float flexW = 0f,
                      float minH  = 0f, float prefH  = 0f, float flexH = 0f)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        if (minW  > 0f) le.minWidth       = minW;
        if (prefW > 0f) le.preferredWidth  = prefW;
        if (flexW > 0f) le.flexibleWidth   = flexW;
        if (minH  > 0f) le.minHeight       = minH;
        if (prefH > 0f) le.preferredHeight = prefH;
        if (flexH > 0f) le.flexibleHeight  = flexH;
    }
}
