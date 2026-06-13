using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CabinElectricitySwitch : Interactable
{
    //Light Variables and computer
 
    public override void OnInteractPress()
    {
        SwitchOnCabin();
    }
    
    [Command(requiresAuthority = false)]
    private void SwitchOnCabin()
    {
        if (isDone) return;
        
        CompleteInteraction();
        OpenCabinLights();
    }

    [ClientRpc]
    private void OpenCabinLights()
    {
        // Switch on lights and computer.
    }
}
