// Assets/Editor/MainMenuUIGenerator.cs
// Run via: Tools > SCARY Project > Generate Main Menu UI

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public static class MainMenuUIGenerator
{
    // ── Design constants ──────────────────────────────────────────────────────
    private const float REF_W = 1920f;
    private const float REF_H = 1080f;

    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly Color C_RED        = new Color(0.851f, 0.235f, 0.110f, 1.000f); // #D93C1C
    private static readonly Color C_WHITE      = Color.white;
    private static readonly Color C_DARK       = new Color(0.040f, 0.040f, 0.040f, 1.000f); // near-black bg
    private static readonly Color C_BTN_FILL   = new Color(0.040f, 0.040f, 0.040f, 0.900f); // button interior
    private static readonly Color C_BTN_BORDER = new Color(0.820f, 0.820f, 0.820f, 1.000f); // button border

    // ── Entry point ───────────────────────────────────────────────────────────
    [MenuItem("Tools/SCARY Project/Generate Main Menu UI", false, 10)]
    public static void Generate()
    {
        // Replace existing canvas if user agrees
        GameObject existing = GameObject.Find("MainMenuCanvas");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Main Menu Generator",
                "A MainMenuCanvas already exists in the scene.\nReplace it?",
                "Replace", "Cancel");
            if (!replace) return;
            Object.DestroyImmediate(existing);
        }

        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = NewUI("MainMenuCanvas", null);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(REF_W, REF_H);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── EventSystem (create only if absent) ───────────────────────────────
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // ── BG_Background ─────────────────────────────────────────────────────
        // TODO: assign your background image sprite here in the Inspector
        GameObject bgGO = NewUI("BG_Background", canvasGO);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = C_DARK;
        Stretch(bgGO.GetComponent<RectTransform>());

        // ── PANEL_Logo ────────────────────────────────────────────────────────
        // Right side, upper third — mirrors image layout
        GameObject logoPanelGO = NewUI("PANEL_Logo", canvasGO);
        Place(logoPanelGO, anchoredPos: new Vector2(465f, 190f), size: new Vector2(570f, 255f));

        //   TXT_RedlineLogo — uses rich text for two-colour title
        //   TODO: assign your logo/display font to this TMP component
        GameObject titleGO = NewUI("TXT_RedlineLogo", logoPanelGO);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin       = new Vector2(0f, 0.40f);
        titleRT.anchorMax       = new Vector2(1f, 1.00f);
        titleRT.offsetMin       = titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text           = "<color=#D93C1C>RED</color><color=#FFFFFF>LINE</color>";
        titleTMP.fontSize       = 112f;
        titleTMP.fontStyle      = FontStyles.Bold;
        titleTMP.alignment      = TextAlignmentOptions.Center;
        titleTMP.characterSpacing = -3f;

        //   PANEL_OnlineBox — white-border rectangle beneath the title
        GameObject onlineBoxGO = NewUI("PANEL_OnlineBox", logoPanelGO);
        RectTransform onlineBoxRT = onlineBoxGO.GetComponent<RectTransform>();
        onlineBoxRT.anchorMin   = new Vector2(0.27f, 0f);
        onlineBoxRT.anchorMax   = new Vector2(0.73f, 0.36f);
        onlineBoxRT.offsetMin   = onlineBoxRT.offsetMax = Vector2.zero;
        Image onlineBoxBorder = onlineBoxGO.AddComponent<Image>();
        onlineBoxBorder.color   = C_WHITE; // outer white border

        //     BG_OnlineFill — dark interior inset by 2 px
        GameObject onlineFillGO = NewUI("BG_OnlineFill", onlineBoxGO);
        RectTransform onlineFillRT = onlineFillGO.GetComponent<RectTransform>();
        Stretch(onlineFillRT);
        onlineFillRT.offsetMin  = new Vector2(2f,  2f);
        onlineFillRT.offsetMax  = new Vector2(-2f, -2f);
        Image onlineFill = onlineFillGO.AddComponent<Image>();
        onlineFill.color        = C_DARK;

        //     TXT_Online
        //     TODO: assign your logo/display font
        GameObject onlineTextGO = NewUI("TXT_Online", onlineBoxGO);
        Stretch(onlineTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI onlineTMP = onlineTextGO.AddComponent<TextMeshProUGUI>();
        onlineTMP.text           = "ONLINE";
        onlineTMP.fontSize       = 26f;
        onlineTMP.fontStyle      = FontStyles.Bold;
        onlineTMP.alignment      = TextAlignmentOptions.Center;
        onlineTMP.color          = C_WHITE;

        // ── PANEL_Buttons ─────────────────────────────────────────────────────
        // Right side, centre — stacked vertically via VerticalLayoutGroup
        GameObject btnPanelGO = NewUI("PANEL_Buttons", canvasGO);
        Place(btnPanelGO, anchoredPos: new Vector2(465f, -158f), size: new Vector2(280f, 316f));

        VerticalLayoutGroup vlg     = btnPanelGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                 = 16f;
        vlg.childAlignment          = TextAnchor.UpperCenter;
        vlg.childControlWidth       = true;
        vlg.childControlHeight      = false;
        vlg.childForceExpandWidth   = true;
        vlg.childForceExpandHeight  = false;
        vlg.padding                 = new RectOffset(0, 0, 0, 0);

        MakeButton(btnPanelGO, "FindGame",  "FIND GAME");
        MakeButton(btnPanelGO, "LocalGame", "LOCAL GAME");
        MakeButton(btnPanelGO, "Settings",  "SETTINGS");
        MakeButton(btnPanelGO, "Quit",      "QUIT");

        // ── PANEL_Footer ──────────────────────────────────────────────────────
        // Pinned to the bottom of the screen
        GameObject footerGO = NewUI("PANEL_Footer", canvasGO);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin          = new Vector2(0f, 0f);
        footerRT.anchorMax          = new Vector2(1f, 0f);
        footerRT.pivot              = new Vector2(0.5f, 0f);
        footerRT.anchoredPosition   = new Vector2(0f, 28f);
        footerRT.sizeDelta          = new Vector2(0f, 30f);

        HorizontalLayoutGroup hlg   = footerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment          = TextAnchor.MiddleCenter;
        hlg.spacing                 = 6f;
        hlg.childControlWidth       = false;
        hlg.childControlHeight      = false;
        hlg.childForceExpandWidth   = false;
        hlg.childForceExpandHeight  = false;

        //   TXT_FooterLabel — static white text
        GameObject lblGO = NewUI("TXT_FooterLabel", footerGO);
        lblGO.GetComponent<RectTransform>().sizeDelta = new Vector2(285f, 28f);
        TextMeshProUGUI lblTMP  = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text             = "Current online players:";
        lblTMP.fontSize         = 18f;
        lblTMP.color            = C_WHITE;
        lblTMP.alignment        = TextAlignmentOptions.MidlineRight;

        //   TXT_OnlineCount — live count, red-orange
        //   TODO: drive this value at runtime from MainMenuController
        GameObject cntGO = NewUI("TXT_OnlineCount", footerGO);
        cntGO.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 28f);
        TextMeshProUGUI cntTMP  = cntGO.AddComponent<TextMeshProUGUI>();
        cntTMP.text             = "0 online players";
        cntTMP.fontSize         = 18f;
        cntTMP.color            = C_RED;
        cntTMP.alignment        = TextAlignmentOptions.MidlineLeft;

        // ── Attach runtime controller ─────────────────────────────────────────
        canvasGO.AddComponent<MainMenuController>();

        // ── Finalize ──────────────────────────────────────────────────────────
        Undo.RegisterCreatedObjectUndo(canvasGO, "Generate Main Menu UI");
        Selection.activeGameObject = canvasGO;

        Debug.Log("[MainMenuUIGenerator] Done. " +
                  "Assign sprites/fonts where marked with TODO in the Inspector.");
    }

    // ── Button factory ────────────────────────────────────────────────────────
    // Structure:  BTN_<Id>  (border Image, VerticalLayoutGroup child)
    //               BG_<Id>Fill  (dark interior Image)
    //               HIT_<Id>     (transparent Button hit area)
    //                 TXT_<Id>   (TMP label)
    private static void MakeButton(GameObject parent, string id, string label)
    {
        // Outer border panel — white/gray fill acts as the 2-px border
        // TODO: swap the border Image sprite with your own button frame sprite
        GameObject borderGO = NewUI("BTN_" + id, parent);
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.sizeDelta      = new Vector2(280f, 63f);
        Image borderImg         = borderGO.AddComponent<Image>();
        borderImg.color         = C_BTN_BORDER;

        // Dark interior, inset 2 px
        GameObject fillGO = NewUI("BG_" + id + "Fill", borderGO);
        Stretch(fillGO.GetComponent<RectTransform>());
        fillGO.GetComponent<RectTransform>().offsetMin = new Vector2( 2f,  2f);
        fillGO.GetComponent<RectTransform>().offsetMax = new Vector2(-2f, -2f);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = C_BTN_FILL;

        // Transparent full-size hit zone that carries the Button component
        GameObject hitGO = NewUI("HIT_" + id, borderGO);
        Stretch(hitGO.GetComponent<RectTransform>());
        Image hitImg        = hitGO.AddComponent<Image>();
        hitImg.color        = Color.clear;
        Button btn          = hitGO.AddComponent<Button>();
        btn.targetGraphic   = hitImg;

        ColorBlock cb           = btn.colors;
        cb.normalColor          = new Color(1f, 1f, 1f, 0.00f);
        cb.highlightedColor     = new Color(1f, 1f, 1f, 0.12f);
        cb.pressedColor         = new Color(1f, 1f, 1f, 0.28f);
        cb.selectedColor        = new Color(1f, 1f, 1f, 0.08f);
        cb.fadeDuration         = 0.1f;
        btn.colors              = cb;

        // Label
        // TODO: assign your UI font to TXT_<Id>
        GameObject textGO = NewUI("TXT_" + id, hitGO);
        Stretch(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp     = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text                = label;
        tmp.fontSize            = 22f;
        tmp.fontStyle           = FontStyles.Bold;
        tmp.alignment           = TextAlignmentOptions.Center;
        tmp.color               = C_WHITE;
        tmp.characterSpacing    = 3f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject NewUI(string name, GameObject parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.transform.SetParent(parent.transform, false);
        return go;
    }

    // Anchors to canvas centre; sets anchoredPosition and sizeDelta
    private static void Place(GameObject go, Vector2 anchoredPos, Vector2 size)
    {
        RectTransform rt    = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    // Full-parent stretch
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
