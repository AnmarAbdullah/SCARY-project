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

    private void SetPickedUpState()
    {
        foreach (Collider itemCollider in GetComponentsInChildren<Collider>())
            itemCollider.enabled = false;

        gameObject.tag = "Untagged";
    }
}
