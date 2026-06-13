using System;
using Mirror;
using UnityEngine;

public abstract class Interactable : NetworkBehaviour
{
    public enum InteractionType
    {
        Press,
        HoldFree
    };

    [SyncVar] public bool isInteractable = true;
    [SyncVar] public bool isOccupied;
    [SyncVar] public bool isDone;
    
    public event Action OnCompleted;
    
    public InteractionType interactionType;

    public virtual void OnInteractPress(){}
    public virtual void OnHoldUpdate(float holdTimer){}
    public virtual void OnHoldCancelled(){}
    public virtual void OnHoldCompleted(){}

    [Command(requiresAuthority = false)]
    public void CmdStartInteract()
    {
        if (isOccupied || isDone) return;  // server rejects if locked
        isOccupied = true;
    }

    [Command(requiresAuthority = false)]
    public void CmdCancelInteract()
    {
        isOccupied = false;
    }

    [Server]
    public void SetIsInteractable(bool value)
    {
        isInteractable = value;
    }
    
    protected void CompleteInteraction()
    {
        if (isDone)
            return;

        isDone = true;
        OnHoldCompleted();
        OnCompleted?.Invoke();
    }
}
