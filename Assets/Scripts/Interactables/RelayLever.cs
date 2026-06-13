using Mirror;
using UnityEngine;
using ScaryGame.Game;

/// <summary>
/// Attach to each of the 16 levers across the 4 relay cabins.
/// Set relayType to match the cabin it lives in, and leverType to
/// match the lever's physical label. Level2Manager decides correctness.
/// </summary>
public class RelayLever : Interactable
{
    [SerializeField] private RelayType relayType;
    [SerializeField] private LeverType leverType;

    public override void OnInteractPress()
    {
        CmdPullLever();
        Debug.Log($"[RelayLever] Pulled lever {relayType}/{leverType}");
    }

    [Command(requiresAuthority = false)]
    private void CmdPullLever()
    {
        if (Level2Manager.Instance == null) return;
        Level2Manager.Instance.ServerLeverPulled(relayType, leverType);
    }

    // Editor-only test: bypasses Mirror Command so no network dispatch is needed.
    [ContextMenu("Force Interact (Debug)")]
    private void DebugForceInteract()
    {
        if (Level2Manager.Instance == null) { Debug.LogWarning("[RelayLever] No Level2Manager in scene."); return; }
        Level2Manager.Instance.ServerLeverPulled(relayType, leverType);
    }
}
