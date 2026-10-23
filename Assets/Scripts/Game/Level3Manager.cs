using Mirror;
using UnityEngine;

namespace ScaryGame.Game
{
    /// <summary>
    /// Level 3 orchestrator.
    ///
    /// Responsibilities:
    ///   - Listens for each lab puzzle completing and chains the note reveals.
    ///   - Lab 1 complete → reveals Lab 2's sabotage protocol note.
    ///   - Lab 2 complete → reveals Lab 3's symbol note.
    ///   - Lab 3 opens the Level 4 door directly (last code entry triggers it).
    ///   - Tracks overall level completion state.
    ///
    /// Scene setup:
    ///   Assign all three puzzle managers in the Inspector. The Level 4 door
    ///   animator is wired directly inside Lab3CodePuzzle (it opens on the
    ///   3rd correct code). Level3Manager exposes AllComplete for save/stats.
    /// </summary>
    public class Level3Manager : NetworkBehaviour
    {
        public static Level3Manager Instance { get; private set; }

        [Header("Puzzle Managers")]
        [SerializeField] private Lab1SimonSaysPuzzle lab1;
        [SerializeField] private Lab2ValvePuzzle lab2;
        [SerializeField] private Lab3CodePuzzle lab3;

        [SyncVar] private bool _lab1Done;
        [SyncVar] private bool _lab2Done;
        [SyncVar] private bool _lab3Done;

        public bool AllComplete => _lab1Done && _lab2Done && _lab3Done;

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

            if (lab1 != null) lab1.OnPuzzleCompleted += OnLab1Done;
            if (lab2 != null) lab2.OnPuzzleCompleted += OnLab2Done;
            if (lab3 != null) lab3.OnPuzzleCompleted += OnLab3Done;
        }

        public override void OnStopServer()
        {
            if (lab1 != null) lab1.OnPuzzleCompleted -= OnLab1Done;
            if (lab2 != null) lab2.OnPuzzleCompleted -= OnLab2Done;
            if (lab3 != null) lab3.OnPuzzleCompleted -= OnLab3Done;
            base.OnStopServer();
        }

        // -----------------------------------------------------------------------
        // Puzzle completion chain
        // -----------------------------------------------------------------------

        [Server]
        private void OnLab1Done()
        {
            _lab1Done = true;
            Debug.Log("[Level3Manager] Lab 1 complete — revealing sabotage protocol note.");
            lab2?.RevealSabotageNote();
        }

        [Server]
        private void OnLab2Done()
        {
            _lab2Done = true;
            Debug.Log("[Level3Manager] Lab 2 complete — revealing symbol note for Lab 3.");
            lab3?.RevealSymbolNote();
        }

        [Server]
        private void OnLab3Done()
        {
            _lab3Done = true;
            Debug.Log("[Level3Manager] Lab 3 complete — Level 4 door opened. Level 3 finished!");
            // Level 4 door is opened inside Lab3CodePuzzle on the final code entry.
            // Hook into save/stats here if needed.
        }
    }
}
