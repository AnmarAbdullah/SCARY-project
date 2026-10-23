using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using ScaryGame.Noise;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScaryGame.Game
{
    /// <summary>
    /// Lab 1 — Simon Says puzzle.
    ///
    /// Flow:
    ///   1. Players step on pressure plates (one per shape). When occupied count
    ///      equals connected player count, a 3-2-1 countdown begins.
    ///   2. A random sequence of shapes is shown on the big screen canvas,
    ///      one every 0.5 s, then cleared.
    ///   3. Players have an input window to press buttons matching the sequence
    ///      in order. Any wrong press or timeout triggers a ghost-attracting noise
    ///      and resets — players must step off a plate and back on to retry.
    ///   4. Each round's sequence grows by 2 symbols. Completing all rounds
    ///      fires OnPuzzleCompleted, which Level3Manager listens to.
    ///
    /// Player-count aware: only occupied plates' shapes are used in the sequence.
    /// If 2 players are present and stand on 2 plates, only those 2 shapes appear.
    /// </summary>
    public class Lab1SimonSaysPuzzle : NetworkBehaviour
    {
        public static Lab1SimonSaysPuzzle Instance { get; private set; }

        // -----------------------------------------------------------------------
        // Inspector
        // -----------------------------------------------------------------------

        [Header("Rounds")]
        [Tooltip("Total rounds to complete. Sequence grows by 2 each round.")]
        [SerializeField, Min(1)] private int totalRounds = 3;

        [Tooltip("Number of symbols in the sequence for round 0.")]
        [SerializeField, Min(1)] private int baseSequenceLength = 4;

        [Header("Pressure Plates (assign all 4 in order: Triangle, Circle, Diamond, Square)")]
        [SerializeField] private PressurePlate[] plates = new PressurePlate[4];

        [Header("Big Screen Canvas")]
        [Tooltip("Parent RectTransform into which shape images are spawned horizontally.")]
        [SerializeField] private RectTransform symbolContainer;

        [Tooltip("Non-networked Image prefab instantiated per symbol in the sequence.")]
        [SerializeField] private GameObject symbolImagePrefab;

        [Tooltip("Shape sprites indexed by ShapeType: [0]=Triangle [1]=Circle [2]=Diamond [3]=Square.")]
        [SerializeField] private Sprite[] shapeSprites = new Sprite[4];

        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text statusText;

        [Header("Timing")]
        [SerializeField] private float symbolDisplayInterval = 0.5f;
        [SerializeField] private float inputWindowSeconds = 5f;

        [Header("Ghost Noise on Mistake")]
        [SerializeField] private float mistakeNoiseIntensity = 0.8f;
        [SerializeField] private float mistakeNoiseRadius = 35f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip correctStepSfx;
        [SerializeField] private AudioClip wrongPressSfx;
        [SerializeField] private AudioClip roundCompleteSfx;
        [SerializeField] private AudioClip puzzleCompleteSfx;

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

        /// <summary>Server-only. Fires once when all rounds are finished.</summary>
        public event Action OnPuzzleCompleted;

        // -----------------------------------------------------------------------
        // Internal state (server)
        // -----------------------------------------------------------------------

        private enum PuzzleState { Idle, Countdown, ShowingSequence, WaitingForInput, RoundComplete, Failed, Complete }

        [SyncVar] private PuzzleState _state = PuzzleState.Idle;
        [SyncVar] private int _currentRound;

        private List<ShapeType> _sequence = new List<ShapeType>();
        private List<ShapeType> _activeShapes = new List<ShapeType>();
        private int _currentStep;
        private Coroutine _puzzleCoroutine;

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
            foreach (PressurePlate plate in plates)
                if (plate != null)
                    plate.OnOccupancyChanged += OnPlateChanged;
        }

        public override void OnStopServer()
        {
            foreach (PressurePlate plate in plates)
                if (plate != null)
                    plate.OnOccupancyChanged -= OnPlateChanged;

            if (_puzzleCoroutine != null) StopCoroutine(_puzzleCoroutine);
            base.OnStopServer();
        }

        // -----------------------------------------------------------------------
        // Plate monitoring
        // -----------------------------------------------------------------------

        [Server]
        private void OnPlateChanged(PressurePlate plate, bool occupied)
        {
            // A player stepping off while failed/roundComplete resets to Idle
            if (!occupied && (_state == PuzzleState.Failed || _state == PuzzleState.RoundComplete))
            {
                _state = PuzzleState.Idle;
                RpcSetStatus("");
            }

            if (_state != PuzzleState.Idle) return;

            int playerCount = Mathf.Clamp(NetworkManager.singleton.numPlayers, 1, 4);
            if (CountOccupied() >= playerCount)
            {
                BuildActiveShapes();
                _puzzleCoroutine = StartCoroutine(RunRound());
            }
        }

        [Server]
        private int CountOccupied()
        {
            int n = 0;
            foreach (PressurePlate p in plates) if (p != null && p.IsOccupied) n++;
            return n;
        }

        [Server]
        private void BuildActiveShapes()
        {
            _activeShapes.Clear();
            foreach (PressurePlate p in plates)
                if (p != null && p.IsOccupied)
                    _activeShapes.Add(p.shapeType);
        }

        // -----------------------------------------------------------------------
        // Round coroutine (server)
        // -----------------------------------------------------------------------

        [Server]
        private IEnumerator RunRound()
        {
            // --- Countdown ---
            _state = PuzzleState.Countdown;
            for (int i = 3; i >= 1; i--)
            {
                RpcShowCountdown(i);
                yield return new WaitForSeconds(1f);
            }
            RpcShowCountdown(0);

            // --- Generate & display sequence ---
            _state = PuzzleState.ShowingSequence;
            int seqLen = baseSequenceLength + (_currentRound * 2);
            GenerateSequence(seqLen);
            yield return StartCoroutine(ShowSequence());

            // --- Input window ---
            _state = PuzzleState.WaitingForInput;
            _currentStep = 0;
            RpcSetStatus($"GO! Press buttons in order — {inputWindowSeconds:F0}s");

            float elapsed = 0f;
            while (_state == PuzzleState.WaitingForInput && elapsed < inputWindowSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // If still waiting when time ran out → timeout
            if (_state == PuzzleState.WaitingForInput)
                TriggerMistake("Timed out");
        }

        [Server]
        private void GenerateSequence(int length)
        {
            _sequence.Clear();
            for (int i = 0; i < length; i++)
                _sequence.Add(_activeShapes[UnityEngine.Random.Range(0, _activeShapes.Count)]);
        }

        [Server]
        private IEnumerator ShowSequence()
        {
            int[] indices = new int[_sequence.Count];
            for (int i = 0; i < _sequence.Count; i++)
                indices[i] = (int)_sequence[i];

            // Send full sequence in one RPC — clients animate locally
            RpcPlaySequence(indices, symbolDisplayInterval);

            // Server just waits for the animation to finish before opening input window
            yield return new WaitForSeconds(indices.Length * symbolDisplayInterval + 1f);
        }

        // -----------------------------------------------------------------------
        // Button input — called by ShapeButton via Command
        // -----------------------------------------------------------------------

        [Server]
        public void ServerButtonPressed(ShapeType shape)
        {
            if (_state != PuzzleState.WaitingForInput) return;

            ShapeType expected = _sequence[_currentStep];

            if (shape != expected)
            {
                TriggerMistake($"Wrong: got {shape}, expected {expected}");
                return;
            }

            _currentStep++;
            RpcOnStepCorrect(_currentStep - 1, (int)shape);

            if (_currentStep < _sequence.Count) return;

            // --- Sequence complete ---
            _currentRound++;

            if (_currentRound >= totalRounds)
            {
                _state = PuzzleState.Complete;
                if (_puzzleCoroutine != null) { StopCoroutine(_puzzleCoroutine); _puzzleCoroutine = null; }
                RpcPuzzleComplete();
                OnPuzzleCompleted?.Invoke();
            }
            else
            {
                _state = PuzzleState.RoundComplete;
                if (_puzzleCoroutine != null) { StopCoroutine(_puzzleCoroutine); _puzzleCoroutine = null; }
                RpcRoundComplete(_currentRound);
            }
        }

        [Server]
        private void TriggerMistake(string reason)
        {
            Debug.Log($"[Lab1] Mistake: {reason}");
            _state = PuzzleState.Failed;

            if (_puzzleCoroutine != null) { StopCoroutine(_puzzleCoroutine); _puzzleCoroutine = null; }

            RpcClearSymbols();
            RpcPuzzleFailed();

            NoiseEventBus.Emit(new NoiseEvent(
                transform.position, NoiseType.Custom,
                mistakeNoiseIntensity, mistakeNoiseRadius, gameObject));
        }

        // -----------------------------------------------------------------------
        // Client RPCs
        // -----------------------------------------------------------------------

        [ClientRpc]
        private void RpcShowCountdown(int value)
        {
            if (countdownText == null) return;
            countdownText.text = value > 0 ? value.ToString() : "";
        }

        [ClientRpc]
        private void RpcPlaySequence(int[] indices, float interval)
        {
            StartCoroutine(ClientPlaySequence(indices, interval));
        }

        private IEnumerator ClientPlaySequence(int[] indices, float interval)
        {
            ClearSymbolContainer();
            yield return null; // let any pending destroys flush

            foreach (int shapeIndex in indices)
            {
                if (symbolContainer == null || symbolImagePrefab == null) yield break;
                if (shapeSprites == null || shapeIndex < 0 || shapeIndex >= shapeSprites.Length) yield break;

                GameObject go = Instantiate(symbolImagePrefab, symbolContainer);
                Image img = go.GetComponent<Image>();
                if (img != null) img.sprite = shapeSprites[shapeIndex];

                yield return new WaitForSeconds(interval);
            }

            yield return new WaitForSeconds(1f);
            ClearSymbolContainer();
        }

        [ClientRpc]
        private void RpcClearSymbols()
        {
            ClearSymbolContainer();
        }

        private void ClearSymbolContainer()
        {
            if (symbolContainer == null) return;
            foreach (Transform child in symbolContainer)
                Destroy(child.gameObject);
        }

        [ClientRpc]
        private void RpcOnStepCorrect(int step, int shapeIndex)
        {
            if (statusText != null)
                statusText.text = $"Step {step + 1} correct!";
            PlaySfx(correctStepSfx);
        }

        [ClientRpc]
        private void RpcRoundComplete(int round)
        {
            if (statusText != null)
                statusText.text = $"Round {round}/{totalRounds} done! Step off a plate and back on.";
            PlaySfx(roundCompleteSfx);
        }

        [ClientRpc]
        private void RpcPuzzleFailed()
        {
            if (statusText != null)
                statusText.text = "WRONG! Step off a plate to reset.";
            PlaySfx(wrongPressSfx);
        }

        [ClientRpc]
        private void RpcPuzzleComplete()
        {
            if (countdownText != null) countdownText.text = "";
            if (statusText != null) statusText.text = "LAB 1 COMPLETE! Collect the protocol note.";
            PlaySfx(puzzleCompleteSfx);
        }

        [ClientRpc]
        private void RpcSetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        // Client-side: called from the RPCs above so every player hears it.
        private void PlaySfx(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
