using Mirror;
using UnityEngine;

namespace ScaryGame.Game
{
    public enum ValveState { Off = 0, Low = 1, Medium = 2, High = 3 }

    /// <summary>
    /// A rotatable valve with 4 positions: Off (down), Low (left), Medium (up), High (right).
    /// Press F to cycle to the next state. Tag this GameObject "Interact".
    /// The pivot rotates around its local Z axis.
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    public class ValveInteractable : Interactable
    {
        [Tooltip("Which system this valve controls — informational only, read by Lab2ValvePuzzle.")]
        public Lab2ValvePuzzle.ValveType valveType;

        [Header("Rotation")]
        [Tooltip("The transform that physically rotates (the valve handle pivot).")]
        [SerializeField] private Transform valvePivot;

        [SerializeField] private float rotationSpeed = 8f;

        // Target Z angles matching each state (valve handle points: right=High, up=Medium, left=Low, down=Off)
        private static readonly float[] StateAngles = { 270f, 180f, 90f, 0f };
        // Off=270° (down), Low=180° (left), Medium=90° (up), High=0° (right)

        [SyncVar(hook = nameof(HookStateChanged))]
        private ValveState _currentState = ValveState.Off;

        public ValveState CurrentState => _currentState;

        private float _targetAngle;

        private void Start()
        {
            interactionType = InteractionType.Press;
            _targetAngle = StateAngles[(int)_currentState];
            if (valvePivot != null)
            {
                Vector3 e = valvePivot.localEulerAngles;
                valvePivot.localEulerAngles = new Vector3(e.x, e.y, _targetAngle);
            }
        }

        private void Update()
        {
            if (valvePivot == null) return;
            Vector3 current = valvePivot.localEulerAngles;
            float newZ = Mathf.LerpAngle(current.z, _targetAngle, Time.deltaTime * rotationSpeed);
            valvePivot.localEulerAngles = new Vector3(current.x, current.y, newZ);
        }

        public override void OnInteractPress()
        {
            CmdCycleState();
        }

        [Command(requiresAuthority = false)]
        private void CmdCycleState()
        {
            _currentState = (ValveState)(((int)_currentState + 1) % 4);
        }

        private void HookStateChanged(ValveState _, ValveState newState)
        {
            _targetAngle = StateAngles[(int)newState];
        }
    }
}
