using System;
using Mirror;
using ScaryGame.Noise;
using TMPro;
using UnityEngine;

namespace ScaryGame.Game
{
    /// <summary>
    /// Lab 2 — Valve sabotage puzzle.
    ///
    /// On start the server generates a random target state (Low/Medium/High) for
    /// each of the 3 valves and maps them to a "Sabotage Protocol" sentence list.
    /// Players read the protocol note (revealed by Level3Manager after Lab 1),
    /// rotate the valves to match, then hit the Confirm Override button.
    /// Correct → success animation + Lab 3 note revealed.
    /// Any valve wrong → ghost noise, no reset (they can try again).
    /// </summary>
    public class Lab2ValvePuzzle : NetworkBehaviour
    {
        public static Lab2ValvePuzzle Instance { get; private set; }

        public enum ValveType { Fuel = 0, Oxygen = 1, MagneticContainment = 2 }

        // -----------------------------------------------------------------------
        // Inspector
        // -----------------------------------------------------------------------

        [Header("Valves (assign in order: Fuel, Oxygen, MagneticContainment)")]
        [SerializeField] private ValveInteractable fuelValve;
        [SerializeField] private ValveInteractable oxygenValve;
        [SerializeField] private ValveInteractable magneticValve;

        [Header("Sabotage Protocol Note (shown after Lab 1 complete)")]
        [Tooltip("World-space canvas root — disable it in the scene; Level3Manager enables it.")]
        [SerializeField] private GameObject sabotageNoteRoot;
        [SerializeField] private TMP_Text sabotageNoteText;

        [Header("Success")]
        [SerializeField] private Animator successAnimator;
        [Tooltip("Trigger name on the success animator (e.g. a panel sliding out to reveal Lab 3 note).")]
        [SerializeField] private string successTrigger = "Reveal";
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSfx;
        [SerializeField] private AudioClip wrongSfx;

        [Header("Ghost Noise on Wrong")]
        [SerializeField] private float wrongNoiseIntensity = 0.8f;
        [SerializeField] private float wrongNoiseRadius = 35f;

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

        public event Action OnPuzzleCompleted;

        // -----------------------------------------------------------------------
        // Server-side solution (generated once on start)
        // -----------------------------------------------------------------------

        private ValveState[] _solution = new ValveState[3]; // [Fuel, Oxygen, Magnetic]
        private string _sabotageText = "";

        [SyncVar] private bool _puzzleSolved;

        // -----------------------------------------------------------------------
        // Sentence lookup tables — creative, narrative-sounding clues
        // -----------------------------------------------------------------------

        private static readonly string[][] SabotageSentences =
        {
            // [0] Fuel: Off, Low, Medium, High
            new[] { "", "Starve the engine", "Keep it hidden / stable", "Flood the engine" },
            // [1] Oxygen: Off, Low, Medium, High
            new[] { "", "Suffocate combustion", "Maintain a controlled burn", "Create a fire hazard" },
            // [2] Magnetic: Off, Low, Medium, High
            new[] { "", "Collapse the field", "Balance the field harmonics", "Overload the coils" },
        };

        // -----------------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            GenerateSolution();
        }

        // -----------------------------------------------------------------------
        // Solution generation
        // -----------------------------------------------------------------------

        [Server]
        private void GenerateSolution()
        {
            // Pick a random non-Off state for each valve
            ValveState[] options = { ValveState.Low, ValveState.Medium, ValveState.High };

            for (int i = 0; i < 3; i++)
                _solution[i] = options[UnityEngine.Random.Range(0, options.Length)];

            _sabotageText =
                $"Sabotage Protocol:\n" +
                $"  - {SabotageSentences[0][(int)_solution[0]]}\n" +
                $"  - {SabotageSentences[1][(int)_solution[1]]}\n" +
                $"  - {SabotageSentences[2][(int)_solution[2]]}";

            Debug.Log($"[Lab2] Solution: Fuel={_solution[0]}, O2={_solution[1]}, Mag={_solution[2]}");
        }

        // -----------------------------------------------------------------------
        // Note reveal — called by Level3Manager after Lab 1 completes
        // -----------------------------------------------------------------------

        [Server][ContextMenu("Force Reveal Note")]
        public void RevealSabotageNote()
        {
            RpcRevealSabotageNote(_sabotageText);
        }

        [ClientRpc]
        private void RpcRevealSabotageNote(string text)
        {
            if (sabotageNoteRoot != null) sabotageNoteRoot.SetActive(true);
            if (sabotageNoteText != null) sabotageNoteText.text = text;
        }

        // -----------------------------------------------------------------------
        // Confirm override — called by Lab2ConfirmButton via Command
        // -----------------------------------------------------------------------

        [Server]
        public void ServerConfirmOverride()
        {
            if (_puzzleSolved) return;

            bool fuelOk = fuelValve != null && fuelValve.CurrentState == _solution[0];
            bool oxygenOk = oxygenValve != null && oxygenValve.CurrentState == _solution[1];
            bool magneticOk = magneticValve != null && magneticValve.CurrentState == _solution[2];

            if (fuelOk && oxygenOk && magneticOk)
            {
                _puzzleSolved = true;
                RpcSuccess();
                OnPuzzleCompleted?.Invoke();
            }
            else
            {
                string log = $"[Lab2] Wrong: Fuel={fuelValve?.CurrentState}(want {_solution[0]}), " +
                             $"O2={oxygenValve?.CurrentState}(want {_solution[1]}), " +
                             $"Mag={magneticValve?.CurrentState}(want {_solution[2]})";
                Debug.Log(log);

                NoiseEventBus.Emit(new NoiseEvent(
                    transform.position, NoiseType.Custom,
                    wrongNoiseIntensity, wrongNoiseRadius, gameObject));

                RpcWrong();
            }
        }

        // -----------------------------------------------------------------------
        // Client RPCs
        // -----------------------------------------------------------------------

        [ClientRpc]
        private void RpcSuccess()
        {
            if (audioSource != null && successSfx != null) audioSource.PlayOneShot(successSfx);
            if (successAnimator != null) successAnimator.SetTrigger(successTrigger);
        }

        [ClientRpc]
        private void RpcWrong()
        {
            if (audioSource != null && wrongSfx != null) audioSource.PlayOneShot(wrongSfx);
        }
    }
}
