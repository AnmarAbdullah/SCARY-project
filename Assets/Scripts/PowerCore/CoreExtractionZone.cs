using System.Collections.Generic;
using Mirror;
using ScaryGame.Game;
using ScaryGame.Items;
using UnityEngine;

public class CoreExtractionZone : NetworkBehaviour
{
    private readonly HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [Command(requiresAuthority = false)]
    public void CmdBeginDeposit(NetworkIdentity player, NetworkConnectionToClient sender = null)
    {
        if (player == null || CoreGameState.Instance == null)
            return;

        if (sender != null && sender.identity != player)
            return;

        if (!playersInZone.Contains(player.gameObject))
            return;

        CoreItemMarker core = player.GetComponentInChildren<CoreItemMarker>();
        if (core == null)
            return;

        NetworkIdentity coreIdentity = core.GetComponentInParent<NetworkIdentity>();
        if (coreIdentity == null || coreIdentity == player)
            return;

        CoreGameState.Instance.NotifyCoreExtracted(coreIdentity);
        NetworkServer.Destroy(coreIdentity.gameObject);
    }

    [Command(requiresAuthority = false)]
    public void CmdCancelDeposit(NetworkIdentity player, NetworkConnectionToClient sender = null)
    {
    }
    
    
    private void OnTriggerEnter(Collider other)
    {
        if(!isServer) return;

        if (TryGetPlayer(other, out NetworkIdentity player))
            playersInZone.Add(player.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if(!isServer) return;

        if (TryGetPlayer(other, out NetworkIdentity player))
            playersInZone.Remove(player.gameObject);
    }

    private static bool TryGetPlayer(Collider other, out NetworkIdentity player)
    {
        player = other.GetComponentInParent<NetworkIdentity>();
        return player != null && player.CompareTag("Player");
    }
}
