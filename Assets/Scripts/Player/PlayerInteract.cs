using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class PlayerInteract : NetworkBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactRange = 3f;
    private Interactable currentInteractable;
    public GameObject setInteractablePrompt;
    private bool isHolding;
    private float timer;

    private bool HasLocalControl => isLocalPlayer || (!NetworkClient.active && !NetworkServer.active);

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    
    private void Update()
    {
        if (!HasLocalControl)
            return;

        HandleInteractRay();
    }

    public override void OnStopLocalPlayer()
    {
        SetCurrent(null);
        SetInteractablePrompt(false);
    }

    private void OnDisable()
    {
        SetCurrent(null);
        SetInteractablePrompt(false);
    }

    void HandleInteractRay()
    {
        if (cameraTransform == null)
        {
            SetCurrent(null);
            SetInteractablePrompt(false);
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
            if (interactable != null && interactable.CompareTag("Interact"))
            {
                if (interactable.isDone)
                {
                    SetCurrent(null);
                    SetInteractablePrompt(false);
                    return;
                }

                if (currentInteractable != interactable)
                    SetCurrent(interactable);
                
                SetInteractablePrompt(true);

                if (Input.GetKeyDown(KeyCode.F) && currentInteractable != null)
                {
                    
                    if (currentInteractable.isOccupied || currentInteractable.isDone) return;
                    
                    if(currentInteractable.interactionType == Interactable.InteractionType.Press)
                        currentInteractable.OnInteractPress();

                    else
                    {
                        isHolding = true;
                        currentInteractable.CmdStartInteract();
                    }

                }

                if (isHolding)
                {
                    timer += Time.deltaTime;
                    currentInteractable.OnHoldUpdate(timer);  // fires every frame while holding
                }

                if (Input.GetKeyUp(KeyCode.F) && isHolding)
                {
                    currentInteractable.OnHoldCancelled();
                    currentInteractable.CmdCancelInteract();
                    ResetHold();
                }
            }

            if (interactable == null || !interactable.CompareTag("Interact"))
            {
                SetCurrent(null);
                SetInteractablePrompt(false);
            }

            return;
        }

        SetCurrent(null);
        SetInteractablePrompt(false);

    }

    void SetInteractablePrompt(bool show)
    {
        if (setInteractablePrompt != null)
            setInteractablePrompt.SetActive(show);
    }

    void ResetHold()
    {
        isHolding = false;
        timer = 0;
        SetInteractablePrompt(false);
    }
    
    void SetCurrent(Interactable next)
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnCompleted -= ResetHold;
            if (isHolding) 
            {
                currentInteractable.OnHoldCancelled();
                ResetHold();
            }
        }

        currentInteractable = next;

        if (currentInteractable != null)
            currentInteractable.OnCompleted += ResetHold;
    }
}
