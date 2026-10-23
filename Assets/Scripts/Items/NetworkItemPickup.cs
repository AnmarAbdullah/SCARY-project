using Mirror;
using UnityEngine;

[RequireComponent(typeof(Item))]
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkItemPickup : NetworkBehaviour
{
    [SerializeField] private float serverPickupRange = 4f;

    [SyncVar(hook = nameof(OnPickedByChanged))]
    private NetworkIdentity _pickedBy;

    [Command(requiresAuthority = false)]
    public void CmdPickup(NetworkIdentity pickerIdentity, NetworkConnectionToClient sender = null)
    {
        if (_pickedBy != null || pickerIdentity == null || sender == null || sender.identity != pickerIdentity)
            return;

        if (!pickerIdentity.TryGetComponent<PlayerItems>(out _))
            return;

        if (Vector3.Distance(pickerIdentity.transform.position, transform.position) > serverPickupRange)
            return;

        _pickedBy = pickerIdentity;
        ApplyPickedUp(pickerIdentity);
        RpcOnPickedUp(pickerIdentity);
    }

    [Command(requiresAuthority = false)]
    public void CmdDrop(NetworkConnectionToClient sender = null)
    {
        if (_pickedBy == null) return;

        if (sender == null || sender.identity != _pickedBy) return;
        
        NetworkIdentity dropper = _pickedBy;
        _pickedBy = null;
        ApplyDrop(dropper);
        RpcOnDrop(dropper);
    }

    public override void OnStartClient()
    {
        if (_pickedBy != null)
            ApplyPickedUp(_pickedBy);
    }

    [ClientRpc]
    private void RpcOnPickedUp(NetworkIdentity pickerIdentity)
    {
        ApplyPickedUp(pickerIdentity);
    }
    
    [ClientRpc]
    private void RpcOnDrop(NetworkIdentity dropperIdentity)
    {
        ApplyDrop(dropperIdentity);
    }

    private void OnPickedByChanged(NetworkIdentity oldPicker, NetworkIdentity newPicker)
    {
        if (newPicker != null)
            ApplyPickedUp(newPicker);
    }

    private void ApplyPickedUp(NetworkIdentity pickerIdentity)
    {
        SetPickedUpState();

        if (pickerIdentity == null)
            return;

        Transform items = PlayerItems.FindDeepChild(pickerIdentity.transform, "Items");
        if (items == null)
            return;

        transform.SetParent(items, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (pickerIdentity.TryGetComponent(out PlayerItems playerItems))
            playerItems.RefreshItems();
    }

    private void ApplyDrop(NetworkIdentity dropperIdentity)
    {
        SetDroppedState();
        transform.SetParent(null, true);
        
        if (dropperIdentity.TryGetComponent(out PlayerItems playerItems))
            playerItems.RefreshItems();
    }

    private void SetPickedUpState()
    {
        foreach (Collider itemCollider in GetComponentsInChildren<Collider>())
            itemCollider.enabled = false;
        
        if(TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.isKinematic = true;
        
        gameObject.tag = "Untagged";
    }

    private void SetDroppedState()
    {
        foreach (Collider itemCollider in GetComponentsInChildren<Collider>())
            itemCollider.enabled = true;
        
        if(TryGetComponent<Rigidbody>(out Rigidbody rb))
            rb.isKinematic = false;

        gameObject.tag = "Pickable";
    }
}
