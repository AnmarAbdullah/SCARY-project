using System;
using Mirror;
using ScaryGame.Noise;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScaryGame.Game
{
    /// <summary>
    /// Lab 3 — Symbol code entry puzzle.
    ///
    /// On start the server generates 3 random 3-digit codes (000–999).
    /// A symbol note (revealed after Lab 2) shows these codes as symbol images —
    /// each digit 0-9 has a fixed sprite that never changes between runs.
    ///
    /// Players decode the symbols, go to the numpad panel, and type each code
    /// in order then press Submit. Correct codes trigger sound effects; the 3rd
    /// correct code opens the Level 4 door.
    ///
    /// Wrong code → ghost noise, input cleared.
    /// </summary>
    public class Lab3CodePuzzle : NetworkBehaviour
    {
        public static Lab3CodePuzzle Instance { get; private set; }

        // -----------------------------------------------------------------------
        // Inspector
        // -----------------------------------------------------------------------

        [Header("Symbol Note (revealed after Lab 2)")]
        [Tooltip("Root of the world-space canvas note. Disabled in scene; revealed by Level3Manager.")]
        [SerializeField] private GameObject symbolNoteRoot;

        [Tooltip("9 Image slots for the 3×3 symbol grid (row-major: row0col0, row0col1, row0col2, row1col0…).")]
        [SerializeField] private Image[] symbolNoteSlots = new Image[9];

        [Tooltip("Digit symbol sprites, indexed 0-9. Assign permanent symbols here.")]
        [SerializeField] private Sprite[] digitSprites = new Sprite[10];

        [Header("Numpad Display")]
        [SerializeField] private TMP_Text codeDisplayText;
        [Tooltip("Shows which code slot is currently expected (e.g. 'CODE 1 / 3').")]
        [SerializeField] private TMP_Text codeIndexText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Level 4 Door")]
        [SerializeField] private Animator level4DoorAnimator;
        [SerializeField] private string doorOpenTrigger = "Open";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip code1CorrectSfx;
        [SerializeField] private AudioClip code2CorrectSfx;
        [SerializeField] private AudioClip code3CorrectSfx;
        [SerializeField] private AudioClip wrongCodeSfx;

        [Header("Ghost Noise on Wrong")]
        [SerializeField] private float wrongNoiseIntensity = 0.8f;
        [SerializeField] private float wrongNoiseRadius = 35f;

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

        public event Action OnPuzzleCompleted;

        // -----------------------------------------------------------------------
        // Server state
        // -----------------------------------------------------------------------

        private int[] _codes = new int[3];   // the 3 generated codes (0-999)
        private int _currentCodeIndex;        // 0, 1, or 2
        private string _inputBuffer = "";     // digits typed so far (max 3)

        [SyncVar] private bool _puzzleComplete;

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
            GenerateCodes();
        }

        // -----------------------------------------------------------------------
        // Code generation
        // -----------------------------------------------------------------------

        [Server]
        private void GenerateCodes()
        {
            for (int i = 0; i < 3; i++)
                _codes[i] = UnityEngine.Random.Range(0, 1000);

            Debug.Log($"[Lab3] Codes: {_codes[0]:D3}  {_codes[1]:D3}  {_codes[2]:D3}");
        }

        // -----------------------------------------------------------------------
        // Note reveal — called by Level3Manager after Lab 2 completes
        // -----------------------------------------------------------------------

        [Server][ContextMenu("Force Reveal Note")]
        public void RevealSymbolNote()
        {
            // Pack all 9 digit indices: row 0 = code[0] digits, row 1 = code[1], row 2 = code[2]
            int[] digits = new int[9];
            for (int code = 0; code < 3; code++)
            {
                int val = _codes[code];
                digits[code * 3 + 0] = val / 100;
                digits[code * 3 + 1] = (val / 10) % 10;
                digits[code * 3 + 2] = val % 10;
            }
            RpcRevealSymbolNote(digits);
        }

        [ClientRpc]
        private void RpcRevealSymbolNote(int[] digits)
        {
            if (symbolNoteRoot != null) symbolNoteRoot.SetActive(true);

            for (int i = 0; i < symbolNoteSlots.Length && i < digits.Length; i++)
            {
                if (symbolNoteSlots[i] == null) continue;
                int d = digits[i];
                if (d >= 0 && d < digitSprites.Length && digitSprites[d] != null)
                {
                    symbolNoteSlots[i].sprite = digitSprites[d];
                    symbolNoteSlots[i].enabled = true;
                }
            }

            UpdateDisplay();
        }

        // -----------------------------------------------------------------------
        // Numpad input — called by NumpadButton via Command
        // -----------------------------------------------------------------------

        [Server]
        public void ServerInputDigit(int digit)
        {
            if (_puzzleComplete) return;
            if (_inputBuffer.Length >= 3) return;

            _inputBuffer += digit.ToString();
            RpcUpdateDisplay(_inputBuffer, _currentCodeIndex);
        }

        [Server]
        public void ServerSubmitCode()
        {
            if (_puzzleComplete) return;
            if (_inputBuffer.Length < 3)
            {
                RpcFeedback("Enter 3 digits first.");
                return;
            }

            int entered = int.Parse(_inputBuffer);
            _inputBuffer = "";

            if (entered != _codes[_currentCodeIndex])
            {
                Debug.Log($"[Lab3] Wrong code: entered {entered}, expected {_codes[_currentCodeIndex]}");
                NoiseEventBus.Emit(new NoiseEvent(
                    transform.position, NoiseType.Custom,
                    wrongNoiseIntensity, wrongNoiseRadius, gameObject));
                RpcWrongCode();
                return;
            }

            // Correct
            int completedIndex = _currentCodeIndex;
            _currentCodeIndex++;

            if (_currentCodeIndex >= 3)
            {
                _puzzleComplete = true;
                RpcCodeCorrect(completedIndex, true);
                OnPuzzleCompleted?.Invoke();
            }
            else
            {
                RpcCodeCorrect(completedIndex, false);
            }
        }

        // -----------------------------------------------------------------------
        // Client RPCs
        // -----------------------------------------------------------------------

        [ClientRpc]
        private void RpcUpdateDisplay(string buffer, int codeIndex)
        {
            string display = buffer.PadRight(3, '_');
            if (codeDisplayText != null) codeDisplayText.text = display;
            if (codeIndexText != null) codeIndexText.text = $"CODE {codeIndex + 1} / 3";
            if (feedbackText != null) feedbackText.text = "";
        }

        [ClientRpc]
        private void RpcWrongCode()
        {
            if (codeDisplayText != null) codeDisplayText.text = "_ _ _";
            if (feedbackText != null) feedbackText.text = "INCORRECT";
            if (audioSource != null && wrongCodeSfx != null) audioSource.PlayOneShot(wrongCodeSfx);
        }

        [ClientRpc]
        private void RpcCodeCorrect(int codeIndex, bool isFinal)
        {
            if (feedbackText != null)
                feedbackText.text = isFinal ? "ACCESS GRANTED" : $"Code {codeIndex + 1} accepted.";

            if (codeDisplayText != null) codeDisplayText.text = "_ _ _";

            AudioClip clip = codeIndex == 0 ? code1CorrectSfx :
                             codeIndex == 1 ? code2CorrectSfx : code3CorrectSfx;
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);

            if (isFinal && level4DoorAnimator != null)
                level4DoorAnimator.SetTrigger(doorOpenTrigger);

            if (!isFinal && codeIndexText != null)
                codeIndexText.text = $"CODE {codeIndex + 2} / 3";
        }

        [ClientRpc]
        private void RpcFeedback(string msg)
        {
            if (feedbackText != null) feedbackText.text = msg;
        }

        // Runs on client only (initial state)
        private void UpdateDisplay()
        {
            if (codeDisplayText != null) codeDisplayText.text = "_ _ _";
            if (codeIndexText != null) codeIndexText.text = "CODE 1 / 3";
            if (feedbackText != null) feedbackText.text = "";
        }
    }
}
