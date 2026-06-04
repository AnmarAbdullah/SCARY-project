using System.Collections.Generic;
using UnityEngine;
using ScaryGame.Game;
using ScaryGame.Noise;
using ScaryGame.Players;
using TimeFracture.Player;

namespace ScaryGame.Enemy
{
    /// <summary>
    /// Server-only. Owns all state instances; each Tick picks the highest-
    /// priority valid state, transitions if it changed, and Runs the current one.
    /// </summary>
    public class GhostBrain
    {
        public readonly Ghost ghost;
        public readonly GhostConfig config;
        public readonly GhostDifficultyProfile difficulty;
        public readonly GhostMover mover;
        public readonly GhostPerception perception;
        public readonly GhostAnimator animatorAdapter;
        public readonly GhostAudio audioAdapter;
        public readonly Vector3 basePosition;

        public IGhostState CurrentState { get; private set; }

        public readonly GhostIdleAtBase idleAtBase;
        public readonly GhostInvestigateNoise investigateNoise;
        public readonly GhostRoamAroundPoint roamAroundPoint;
        public readonly GhostChasePlayer chasePlayer;
        public readonly GhostSearchLastKnown searchLastKnown;
        public readonly GhostReturnToBase returnToBase;
        public readonly GhostJumpscare jumpscare;
        public readonly GhostLaserAbility laserAbility;

        private float _nextLaserEligible;

        public GhostBrain(
            Ghost ghost,
            GhostConfig config,
            GhostDifficultyProfile difficulty,
            GhostMover mover,
            GhostPerception perception,
            GhostAnimator animatorAdapter,
            GhostAudio audioAdapter,
            Vector3 basePosition)
        {
            this.ghost = ghost;
            this.config = config;
            this.difficulty = difficulty;
            this.mover = mover;
            this.perception = perception;
            this.animatorAdapter = animatorAdapter;
            this.audioAdapter = audioAdapter;
            this.basePosition = basePosition;

            idleAtBase       = new GhostIdleAtBase(this);
            investigateNoise = new GhostInvestigateNoise(this);
            roamAroundPoint  = new GhostRoamAroundPoint(this);
            chasePlayer      = new GhostChasePlayer(this);
            searchLastKnown  = new GhostSearchLastKnown(this);
            returnToBase     = new GhostReturnToBase(this);
            jumpscare        = new GhostJumpscare(this);
            laserAbility     = new GhostLaserAbility(this);

            ScheduleNextLaserCheck();
        }

        public float SpeedMul => difficulty != null ? difficulty.speedMultiplier : 1f;

        public void Start()
        {
            TransitionTo(idleAtBase);
        }

        public void TransitionTo(IGhostState next)
        {
            if (next == null || next == CurrentState) return;
            CurrentState?.Exit();
            CurrentState = next;
            CurrentState.Enter();
        }

        public void Tick(float dt)
        {
            perception.UpdatePerception();

            // States that own their own life cycle aren't preempted by priority.
            if (CurrentState == jumpscare || CurrentState == laserAbility)
            {
                CurrentState.Run(dt);
                return;
            }

            IGhostState target = ResolveTargetState();
            if (target != null && target != CurrentState)
                TransitionTo(target);

            CurrentState?.Run(dt);

            if (animatorAdapter != null)
            {
                animatorAdapter.SetSpeed(mover.CurrentSpeed);
                animatorAdapter.SetChasing(CurrentState == chasePlayer);
            }
        }

        private IGhostState ResolveTargetState()
        {
            // 2. Jumpscare trigger met
            if (perception.VisiblePlayer != null)
            {
                float dist = Vector3.Distance(ghost.transform.position, perception.VisiblePlayer.transform.position);
                if (dist <= config.jumpscareTriggerDistance)
                {
                    jumpscare.SetTarget(perception.VisiblePlayer);
                    return jumpscare;
                }
            }

            // 3. Laser ability eligible
            if (Time.time >= _nextLaserEligible
                && CoreGameState.Instance != null
                && CoreGameState.Instance.CoresExtracted >= 1)
            {
                if (Random.value < config.laserActivationChance)
                {
                    ScheduleNextLaserCheck();
                    return laserAbility;
                }
                else
                {
                    ScheduleNextLaserCheck();
                }
            }

            // 4. Player visible in cone
            if (perception.VisiblePlayer != null)
            {
                chasePlayer.SetTarget(perception.VisiblePlayer);
                return chasePlayer;
            }

            // 5. Just lost sight
            if (perception.HasFreshLastKnown(out Vector3 lkp, out PlayerController _))
            {
                if (CurrentState != searchLastKnown)
                {
                    searchLastKnown.SetTarget(lkp);
                    return searchLastKnown;
                }
                return CurrentState;
            }

            // 6. Fresh relevant noise
            if (perception.TryGetMostRelevantNoise(out NoiseEvent n))
            {
                perception.ConsumeNoise(n);
                investigateNoise.SetTarget(n);
                return investigateNoise;
            }

            // 7. Currently roaming and not yet timed out
            if (CurrentState == roamAroundPoint && !roamAroundPoint.IsExpired)
                return roamAroundPoint;

            // 7b. Still on an in-flight non-priority state? Let it finish.
            if (CurrentState == investigateNoise && !investigateNoise.ReachedTarget)
                return investigateNoise;
            if (CurrentState == searchLastKnown && !searchLastKnown.ReachedTarget)
                return searchLastKnown;

            // 8. Core in play → stay outside / continue roam from current position
            if (CoreGameState.Instance != null && CoreGameState.Instance.AnyCoreInPlay)
            {
                roamAroundPoint.SetCenter(ghost.transform.position);
                return roamAroundPoint;
            }

            // 9. No core in play → return to base
            if (Vector3.Distance(ghost.transform.position, basePosition) > 1.5f)
                return returnToBase;

            return idleAtBase;
        }

        private void ScheduleNextLaserCheck()
        {
            float freqMul = difficulty != null ? Mathf.Max(0.01f, difficulty.laserFrequencyMultiplier) : 1f;
            float min = config.laserMinSecondsBetween / freqMul;
            float max = Mathf.Max(min, config.laserMaxSecondsBetween / freqMul);
            _nextLaserEligible = Time.time + Random.Range(min, max);
        }

        // Public helpers used by states ------------------------------------

        public void DownAllMovingPlayers(float maxAllowedSpeed)
        {
            var list = new List<PlayerController>();
            PlayerRegistry.GetAlive(list);
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (p == null) continue;
                float spd = p.Movement != null ? p.Movement.Velocity.magnitude : 0f;
                if (spd > maxAllowedSpeed) p.ServerDown();
            }
        }
    }
}
