using Mirror;
using ScaryGame.Game;
using UnityEngine;

/// <summary>
/// Pick up and Use() near the matching lab door to open it.
/// Set labIndex to 1, 2, or 3 to match the LabDoor's labIndex.
/// </summary>
public class KeycardItem : Item
{
    [Tooltip("Which lab door this keycard opens (1, 2, or 3).")]
    [SerializeField] public int labIndex;

    private const float UseRange = 5f;

    public override void Use()
    {
        CmdTryOpenDoor();
    }

    // Items are never given client authority, so this must not require it.
    // The server checks the sender is the player actually holding this keycard.
    [Command(requiresAuthority = false)]
    private void CmdTryOpenDoor(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        if (transform.root != sender.identity.transform) return;

        Vector3 fromPosition = sender.identity.transform.position;
        LabDoor[] doors = FindObjectsOfType<LabDoor>();
        foreach (LabDoor door in doors)
        {
            if (door.labIndex != labIndex) continue;
            if (door.IsOpen) continue;
            if (Vector3.Distance(fromPosition, door.transform.position) <= UseRange)
            {
                door.ServerOpen();
                return;
            }
        }
    }
}
