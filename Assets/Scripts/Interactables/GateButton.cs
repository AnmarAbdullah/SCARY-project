using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GateButton : Interactable
{
    [SerializeField] private Animator gateAnim;
 
    public override void OnInteractPress()
    {
        CmdOpenGate();
    }

    [Command(requiresAuthority = false)]
    private void CmdOpenGate()
    {
        if (isDone || !isInteractable) return;

        CompleteInteraction();
        RpcOpenGate();
    }

    [ClientRpc]
    private void RpcOpenGate()
    {
        gateAnim.SetTrigger("Open");
    }

    // isInteractable is a SyncVar and must only be set on the server.
    [Server]
    public void MakeButtonInteractable()
    {
        isInteractable = true;
        //And remove electricity Particle Effect.
    }
 
}
