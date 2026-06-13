using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace ScaryGame.Game
{
    public enum RelayType { Anchor = 0, Echo = 1, Mute = 2, Balance = 3 }
    public enum LeverType { Ground, Amplify, Suppress, Stabilize }

    /// <summary>
    /// Server-authoritative manager for Level 2 — the Relay Sequence puzzle.
    ///
    /// Puzzle overview:
    ///   4 relays (small cabins) each contain 4 levers. The correct lever per
    ///   relay is always fixed:
    ///     Anchor → Ground | Echo → Amplify | Mute → Suppress | Balance → Stabilize
    ///
    ///   Each round, a random sequence of relays is generated and displayed on a
    ///   world-space canvas via 4 logo images. Players must pull their relay's
    ///   correct lever in that exact order. Wrong lever or wrong timing resets
    ///   to a fresh sequence.
    ///
    ///   Player-count aware: uses only as many relays as there are connected
    ///   players (1–4). If a player disconnects mid-round, a new sequence is
    ///   generated with the updated count.
    ///
    ///   Multi-round support: set roundsRequired > 1 for hardcore mode. After
    ///   each round, players return to the canvas to see the next sequence.
    /// </summary>
    public class Level2Manager : NetworkBehaviour
    {
        public static Level2Manager Instance { get; private set; }

        [Header("Rounds")]
        [Tooltip("Number of sequences that must be completed to finish the level. " +
                 "Set to 1 for normal mode, 2+ for hardcore.")]
        [SerializeField, Min(1)] private int roundsRequired = 1;

        [Header("Relay Sequence Canvas")]
        [Tooltip("The 4 Image slots in the world-space canvas, left to right. " +
                 "Slots beyond the active player count will be hidden automatically.")]
        [SerializeField] private Image[] sequenceSlots = new Image[4];

        [Tooltip("Relay logo sprites indexed by RelayType: [0]=Anchor, [1]=Echo, [2]=Mute, [3]=Balance. " +
                 "Assign the 4 PNGs here.")]
        [SerializeField] private Sprite[] relaySprites = new Sprite[4];

        [Header("Reward Cabinet")]
        [Tooltip("Animator on the cabinet that holds the level-exit key item.")]
        [SerializeField] private Animator cabinetAnimator;
        [Tooltip("Animator trigger name set on the cabinet's Open animation transition.")]
        [SerializeField] private string cabinetOpenTrigger = "Open";

        // Fixed mapping: relay → the one correct lever for that relay.
        private static readonly Dictionary<RelayType, LeverType> CorrectLever =
            new Dictionary<RelayType, LeverType>
            {
                { RelayType.Anchor,  LeverType.Ground    },
                { RelayType.Echo,    LeverType.Amplify   },
                { RelayType.Mute,    LeverType.Suppress  },
                { RelayType.Balance, LeverType.Stabilize },
            };

        // --- Server-only runtime state ---
        private List<RelayType> _currentSequence = new List<RelayType>();
        private int _currentStep;
        private int _currentRound;
        private int _trackedPlayerCount;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

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
            _trackedPlayerCount = Mathf.Clamp(NetworkManager.singleton.numPlayers, 1, 4);
            StartCoroutine(MonitorPlayerCount());
            StartCoroutine(InitialGenerate());
        }

        // Waits one frame so the host client finishes OnStartClient before the
        // first RpcUpdateCanvas fires — otherwise the RPC arrives before the
        // client-side NetworkBehaviour is ready to receive it.
        [Server]
        private IEnumerator InitialGenerate()
        {
            yield return new WaitForSeconds(1f);
            GenerateAndBroadcast();
        }

        public override void OnStopServer()
        {
            StopAllCoroutines();
            base.OnStopServer();
        }

        // -------------------------------------------------------------------------
        // Player-count monitoring (detects mid-game disconnects)
        // -------------------------------------------------------------------------

        [Server]
        private IEnumerator MonitorPlayerCount()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                int current = Mathf.Clamp(NetworkManager.singleton.numPlayers, 1, 4);
                if (current == _trackedPlayerCount) continue;

                Debug.Log($"[Level2Manager] Player count changed {_trackedPlayerCount} → {current}. Regenerating sequence.");
                _trackedPlayerCount = current;
                GenerateAndBroadcast();
            }
        }

        // -------------------------------------------------------------------------
        // Sequence generation & broadcast
        // -------------------------------------------------------------------------

        [Server]
        private void GenerateAndBroadcast()
        {
            print("Running Generate Broadcast");
            _currentStep = 0;
            _currentSequence = ShuffleAndPick(_trackedPlayerCount);

            int[] packed = new int[_currentSequence.Count];
            for (int i = 0; i < _currentSequence.Count; i++)
                packed[i] = (int)_currentSequence[i];

            print("Start update canvas" + packed.Length);
            RpcUpdateCanvas(packed);
        }

        // Fisher-Yates partial shuffle: returns 'count' unique RelayTypes in random order.
        private static List<RelayType> ShuffleAndPick(int count)
        {
            RelayType[] pool = { RelayType.Anchor, RelayType.Echo, RelayType.Mute, RelayType.Balance };
            
            for (int i = 0; i < pool.Length; i++)
            {
                int j = Random.Range(i, pool.Length);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new List<RelayType>(count);
            for (int i = 0; i < count; i++)
                result.Add(pool[i]);
            
            return result;
        }

        // -------------------------------------------------------------------------
        // Lever interaction — called by RelayLever via Command
        // -------------------------------------------------------------------------

        [Server]
        public void ServerLeverPulled(RelayType relay, LeverType lever)
        {
            if (_currentSequence.Count == 0) return;

            RelayType expectedRelay = _currentSequence[_currentStep];
            LeverType expectedLever = CorrectLever[expectedRelay];

            if (relay != expectedRelay || lever != expectedLever)
            {
                Debug.Log($"[Level2Manager] WRONG — pulled {relay}/{lever}, expected {expectedRelay}/{expectedLever}. Resetting.");
                RpcWrong();
                GenerateAndBroadcast();
                return;
            }

            _currentStep++;
            Debug.Log($"[Level2Manager] Step {_currentStep}/{_currentSequence.Count} correct — {relay}/{lever}");
            Debug.Log(_currentStep);
            
            if (_currentStep >= _currentSequence.Count)
                HandleRoundComplete();
        }

        // -------------------------------------------------------------------------
        // Round / level completion
        // -------------------------------------------------------------------------

        [Server]
        private void HandleRoundComplete()
        {
            _currentRound++;
            Debug.Log($"[Level2Manager] Round {_currentRound}/{roundsRequired} complete.");

            if (_currentRound >= roundsRequired)
            {
                Debug.Log("[Level2Manager] All rounds done — level objective complete!");
                RpcAllRoundsComplete();

                RpcOpenCabinet();

                return;
            }

            // More rounds — generate the next sequence so the canvas updates.
            GenerateAndBroadcast();
            RpcRoundComplete(_currentRound);
        }

        // -------------------------------------------------------------------------
        // Client RPCs
        // -------------------------------------------------------------------------

        [ClientRpc]
        private void RpcUpdateCanvas(int[] sequence)
        {
            if (sequenceSlots == null || relaySprites == null) return;

            for (int i = 0; i < sequenceSlots.Length; i++)
            {
                if (sequenceSlots[i] == null) continue;

                if (i < sequence.Length && sequence[i] >= 0 && sequence[i] < relaySprites.Length)
                {
                    sequenceSlots[i].sprite = relaySprites[sequence[i]];
                    sequenceSlots[i].enabled = true;
                }
                else
                {
                    sequenceSlots[i].sprite = null;
                    sequenceSlots[i].enabled = false;
                }
            }
        }

        [ClientRpc]
        private void RpcWrong()
        {
            Debug.Log("WRONG");
            // TODO: flash canvas red, play wrong-answer audio
        }

        [ClientRpc]
        private void RpcRoundComplete(int completedRound)
        {
            Debug.Log($"[Level2Manager] Round {completedRound} done! Return to the canvas for the next sequence.");
            // TODO: show on-screen prompt "Return to the relay board"
        }

        [ClientRpc]
        private void RpcAllRoundsComplete()
        {
            Debug.Log("[Level2Manager] Puzzle complete!");
            // TODO: victory feedback, Speaker Lady line
        }

        [ClientRpc]
        private void RpcOpenCabinet()
        {
            if (cabinetAnimator != null)
                cabinetAnimator.SetTrigger(cabinetOpenTrigger);
        }
    }
}
