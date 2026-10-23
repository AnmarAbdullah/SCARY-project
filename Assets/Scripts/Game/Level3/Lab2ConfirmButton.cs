using UnityEngine;

namespace ScaryGame.Game
{
    /// <summary>
    /// The "Confirm Override" button in Lab 2. Tag this GameObject "Interact".
    /// Calls Lab2ValvePuzzle.ServerConfirmOverride() when pressed.
    /// Becomes non-interactable once the puzzle is solved.
    /// </summary>
    [RequireComponent(typeof(Mirror.NetworkIdentity))]
    public class Lab2ConfirmButton : Interactable
    {
        [SerializeField] private Animator buttonAnimator;
        [SerializeField] private string pressTrigger = "Press";

        private void Start()
        {
            interactionType = InteractionType.Press;
        }

        public override void OnInteractPress()
        {
            if (buttonAnimator != null) buttonAnimator.SetTrigger(pressTrigger);
            CmdConfirm();
        }

        [Mirror.Command(requiresAuthority = false)]
        private void CmdConfirm()
        {
            Lab2ValvePuzzle puzzle = Lab2ValvePuzzle.Instance;
            if (puzzle != null)
                puzzle.ServerConfirmOverride();
        }
    }
}
