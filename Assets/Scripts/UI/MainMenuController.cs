// Assets/Scripts/UI/MainMenuController.cs
// Auto-attached to MainMenuCanvas by the generator.
// Wire up each button's OnClick event in the Inspector to the methods below,
// or call them directly from code.

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Footer")]
    [Tooltip("Drag TXT_OnlineCount here")]
    [SerializeField] private TextMeshProUGUI onlineCountText;

    // ── Button callbacks ──────────────────────────────────────────────────────
    // Wire these to the four HIT_* Button OnClick events in the Inspector.

    public void OnFindGame()
    {
        // TODO: open matchmaking / lobby screen
        Debug.Log("[MainMenu] Find Game pressed");
    }

    public void OnLocalGame()
    {
        // TODO: start a local session or load the local lobby scene
        Debug.Log("[MainMenu] Local Game pressed");
    }

    public void OnSettings()
    {
        // TODO: open settings panel or load settings scene
        Debug.Log("[MainMenu] Settings pressed");
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Online player count ───────────────────────────────────────────────────
    // Call this from your network manager whenever the count changes.

    public void SetOnlinePlayerCount(int count)
    {
        if (onlineCountText != null)
            onlineCountText.text = $"{count:N0} online players";
    }
}
