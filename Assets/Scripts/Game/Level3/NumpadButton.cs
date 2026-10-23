using UnityEngine;

namespace ScaryGame.Game
{
    /// <summary>
    /// One key on the Lab 3 numpad. Tag this GameObject "Interact".
    /// Set digit to 0-9 for number keys, or use isSubmit for the Enter key.
    /// Each button is freely pressable (never marked Done) so it can be
    /// pressed multiple times.
    /// </summary>
    [RequireComponent(typeof(Mirror.NetworkIdentity))]
    public class NumpadButton : Interactable
    {
        [Tooltip("The digit this button represents (0-9). Ignored if isSubmit is true.")]
        [Range(0, 9)]
        [SerializeField] public int digit;

        [Tooltip("If true this is the Submit/Enter key, not a digit.")]
        [SerializeField] public bool isSubmit;

        [SerializeField] private Animator buttonAnimator;
        [SerializeField] private string pressTrigger = "Press";

        private void Start()
        {
            interactionType = InteractionType.Press;
        }

        public override void OnInteractPress()
        {
            if (buttonAnimator != null) buttonAnimator.SetTrigger(pressTrigger);
            CmdPressKey();
        }

        [Mirror.Command(requiresAuthority = false)]
        private void CmdPressKey()
        {
            Lab3CodePuzzle puzzle = Lab3CodePuzzle.Instance;
            if (puzzle == null) return;

            if (isSubmit)
                puzzle.ServerSubmitCode();
            else
                puzzle.ServerInputDigit(digit);
        }
    }
}
