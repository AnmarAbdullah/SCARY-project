using Mirror;
using UnityEngine;

public enum PlayerGender
{
    Male,
    Female
}

/// <summary>
/// Holds the player's chosen body (male/female skeleton). The choice is server-
/// synced so EVERY machine shows the same gender for each player — in the lobby
/// and in-game.
///
/// The local player picks via the lobby's M/F buttons (wired in LobbyController),
/// which call <see cref="CmdSetGender"/>.
///
/// No models yet: the change hook just logs. When the male/female skeleton meshes
/// exist, swap them in <see cref="ApplyGender"/> (assign the two mesh roots in the
/// inspector and toggle them) — one place to fill in.
/// </summary>
public class PlayerAppearance : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnGenderChanged))]
    public PlayerGender gender = PlayerGender.Male;

    [Header("Meshes (future — assign when models exist)")]
    [Tooltip("Root of the male skeleton mesh. Left empty for now (stub logs instead).")]
    [SerializeField] private GameObject maleMesh;
    [Tooltip("Root of the female skeleton mesh. Left empty for now (stub logs instead).")]
    [SerializeField] private GameObject femaleMesh;

    public override void OnStartClient()
    {
        // Make sure a late-joining client shows each existing player's current body.
        ApplyGender(gender);
    }

    /// <summary>Local player → server: set my chosen gender.</summary>
    [Command]
    public void CmdSetGender(PlayerGender newGender)
    {
        gender = newGender;   // SyncVar hook propagates to everyone (incl. server host)
    }

    private void OnGenderChanged(PlayerGender _, PlayerGender newGender)
    {
        ApplyGender(newGender);
    }

    private void ApplyGender(PlayerGender g)
    {
        // TODO: when skeleton meshes exist, toggle them here:
        //   if (maleMesh)   maleMesh.SetActive(g == PlayerGender.Male);
        //   if (femaleMesh) femaleMesh.SetActive(g == PlayerGender.Female);
        if (maleMesh == null && femaleMesh == null)
            Debug.Log($"[PlayerAppearance] '{name}' switched to {g} (no mesh assigned yet — stub).", this);
        else
        {
            if (maleMesh != null) maleMesh.SetActive(g == PlayerGender.Male);
            if (femaleMesh != null) femaleMesh.SetActive(g == PlayerGender.Female);
        }
    }
}
