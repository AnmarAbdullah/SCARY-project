using Mirror;
using UnityEngine;

namespace ScaryGame.Game
{
    /// <summary>
    /// One of the four Simon Says buttons. Tag this GameObject "Interact".
    /// Assign the shape that matches the pressure plate in front of it.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class ShapeButton : Interactable
    {
        [Tooltip("The shape this button represents. Must match its PressurePlate.")]
        public ShapeType shapeType;

        [SerializeField] private Animator buttonAnimator;
        [SerializeField] private string pressTrigger = "Press";

        private void Start()
        {
            interactionType = InteractionType.Press;
        }

        public override void OnInteractPress()
        {
            if (buttonAnimator != null) buttonAnimator.SetTrigger(pressTrigger);
            CmdPressButton();
        }

        [Command(requiresAuthority = false)]
        private void CmdPressButton()
        {
            Lab1SimonSaysPuzzle puzzle = Lab1SimonSaysPuzzle.Instance;
            if (puzzle != null)
                puzzle.ServerButtonPressed(shapeType);
        }
    }
}
