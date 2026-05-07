using Mirror;
using UnityEngine;

public class PlayerItems : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform items;
    [SerializeField] private Transform hand;
    [SerializeField] private GameObject pickupPrompt;
    [SerializeField] private float pickupRange = 3f;

    private Item _lookedAtItem;
    private Item _activeItem;
    private int _activeIndex = -1;

    private void Awake()
    {
        if (items == null)
            items = FindDeepChild(transform, "Items");

        if (hand == null)
            hand = FindDeepChild(transform, "Hand");

        if (cameraTransform == null)
        {
            Camera playerCamera = GetComponentInChildren<Camera>(true);
            if (playerCamera != null)
                cameraTransform = playerCamera.transform;
        }

        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        HandlePickupRay();
        HandleItemScroll();
        HandleUse();
    }

    private void LateUpdate()
    {
        MoveActiveItemToHand();
    }

    public void RefreshItems()
    {
        if (items == null)
            items = FindDeepChild(transform, "Items");

        int count = items != null ? items.childCount : 0;
        if (count == 0)
        {
            _activeIndex = -1;
            _activeItem = null;
            return;
        }

        if (_activeIndex < 0 || _activeIndex >= count)
            _activeIndex = count - 1;

        SetActiveItem(items.GetChild(_activeIndex).GetComponent<Item>());
    }

    private void HandlePickupRay()
    {
        _lookedAtItem = null;

        if (cameraTransform == null)
        {
            SetPickupPrompt(false);
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            NetworkItemPickup pickup = hit.collider.GetComponentInParent<NetworkItemPickup>();
            if (pickup != null && pickup.CompareTag("Pickable") && pickup.TryGetComponent(out Item item))
            {
                _lookedAtItem = item;
                SetPickupPrompt(true);

                if (Input.GetKeyDown(KeyCode.E))
                    pickup.CmdPickup(netIdentity);

                return;
            }
        }

        SetPickupPrompt(false);
    }

    private void HandleItemScroll()
    {
        if (items == null || items.childCount == 0)
            return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        int direction = scroll > 0f ? 1 : -1;
        int nextIndex = _activeIndex < 0 ? 0 : (_activeIndex + direction + items.childCount) % items.childCount;
        CmdSetActiveItem(items.GetChild(nextIndex).GetComponent<NetworkIdentity>());
    }

    private void HandleUse()
    {
        if (_activeItem != null && Input.GetMouseButtonDown(0))
            _activeItem.Use();
    }

    private void MoveActiveItemToHand()
    {
        if (_activeItem == null || hand == null)
            return;

        _activeItem.transform.SetPositionAndRotation(hand.position, hand.rotation);
    }

    [Command]
    private void CmdSetActiveItem(NetworkIdentity itemIdentity)
    {
        RpcSetActiveItem(itemIdentity);
    }

    [ClientRpc]
    private void RpcSetActiveItem(NetworkIdentity itemIdentity)
    {
        SetActiveItem(itemIdentity != null ? itemIdentity.GetComponent<Item>() : null);
    }

    private void SetActiveItem(Item item)
    {
        _activeItem = item;
        _activeIndex = -1;

        if (items == null)
            return;

        for (int i = 0; i < items.childCount; i++)
        {
            Transform child = items.GetChild(i);
            bool active = item != null && child == item.transform;
            child.gameObject.SetActive(active);

            if (active)
                _activeIndex = i;
        }

        MoveActiveItemToHand();
    }

    private void SetPickupPrompt(bool active)
    {
        if (pickupPrompt != null && pickupPrompt.activeSelf != active)
            pickupPrompt.SetActive(active);
    }

    public static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
