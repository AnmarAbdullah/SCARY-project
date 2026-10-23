using System.Collections;
using System.Collections.Generic;
using Mirror;
using ScaryGame.Items;
using UnityEngine;

public class CorePlug : Interactable
{
    public override void OnInteractPress()
    {
        CmdInputPlug();
    }

    [Command(requiresAuthority = false)]
    void CmdInputPlug(NetworkConnectionToClient sender = null)
    {
        if (isDone || sender == null || !ValidateItem(sender.identity))
            return;

        CompleteInteraction();
    }

    // Player must be carrying a keycard somewhere in their inventory.
    [Server]
    bool ValidateItem(NetworkIdentity player)
    {
        if (player == null) return false;

        Transform items = PlayerItems.FindDeepChild(player.transform, "Items");
        return items != null && items.GetComponentInChildren<KeycardItem>(true) != null;
    }
}
