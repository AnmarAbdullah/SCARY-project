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
        if (isDone) return;
        
        CompleteInteraction();
        RpcOpenGate();
    }

    [ClientRpc]
    private void RpcOpenGate()
    {
        gateAnim.SetTrigger("Open");
    }

    [ClientRpc]
    public void MakeButtonInteractable()
    {
        isInteractable = true;
        //And remove electricity Particle Effect.
    }
 
}
