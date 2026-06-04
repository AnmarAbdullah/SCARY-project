using UnityEngine;
using ScaryGame.Noise;
using TimeFracture.Player;

namespace ScaryGame.Enemy
{
    // ------------------------------------------------------------------
    // 1. Idle at base
    // ------------------------------------------------------------------
    public class GhostIdleAtBase : IGhostState
    {
        private readonly GhostBrain _b;
        public GhostIdleAtBase(GhostBrain b) { _b = b; }

        public void Enter()
        {
            _b.mover.GoTo(_b.basePosition + _b.config.baseStationOffset, _b.config.roamSpeed * _b.SpeedMul);
            _b.animatorAdapter?.TriggerIdleAnim();
        }
        public void Run(float dt)
        {
            if (_b.mover.ReachedDestination()) _b.mover.Stop();
        }
        public void Exit() { }
    }

    // ------------------------------------------------------------------
    // 2. Investigate a noise
    // ------------------------------------------------------------------
    public class GhostInvestigateNoise : IGhostState
    {
        private readonly GhostBrain _b;
        private NoiseEvent _target;

        public GhostInvestigateNoise(GhostBrain b) { _b = b; }

        public void SetTarget(NoiseEvent e) { _target = e; }
        public bool ReachedTarget { get; private set; }

        public void Enter()
        {
            ReachedTarget = false;
            float speed = _b.config.GetSpeedForNoise(_target.type) * _b.SpeedMul;
            _b.mover.GoTo(_target.position, speed);
            _b.audioAdapter?.PlayDetect();
        }
        public void Run(float dt)
        {
            if (!ReachedTarget && _b.mover.ReachedDestination())
            {
                ReachedTarget = true;
                _b.roamAroundPoint.SetCenter(_target.position);
                _b.TransitionTo(_b.roamAroundPoint);
            }
        }
        public void Exit() { }
    }

    // ------------------------------------------------------------------
    // 3. Roam around a point
    // ------------------------------------------------------------------
    public class GhostRoamAroundPoint : IGhostState
    {
        private readonly GhostBrain _b;
        private Vector3 _center;
        private float _stateEnterTime;
        private float _nextWaypointTime;

        public GhostRoamAroundPoint(GhostBrain b) { _b = b; }

        public void SetCenter(Vector3 center) { _center = center; }

        public bool IsExpired => Time.time - _stateEnterTime >= _b.config.roamDuration;

        public void Enter()
        {
            _stateEnterTime = Time.time;
            _nextWaypointTime = 0f;
            _b.audioAdapter?.PlayRoam();
            PickNextWaypoint();
        }

        public void Run(float dt)
        {
            if (_b.mover.ReachedDestination() && Time.time >= _nextWaypointTime)
            {
                _nextWaypointTime = Time.time + _b.config.roamWaypointDwell;
                PickNextWaypoint();
            }
        }

        public void Exit() { }

        private void PickNextWaypoint()
        {
            if (_b.mover.TrySampleRandomPoint(_center, _b.config.roamMinRadius, _b.config.roamRadius, out var p))
                _b.mover.GoTo(p, _b.config.roamSpeed * _b.SpeedMul);
        }
    }

    // ------------------------------------------------------------------
    // 4. Chase a visible player
    // ------------------------------------------------------------------
    public class GhostChasePlayer : IGhostState
    {
        private readonly GhostBrain _b;
        private PlayerController _target;
        private float _retargetInterval = 0.1f;
        private float _nextRetarget;

        public GhostChasePlayer(GhostBrain b) { _b = b; }
        public void SetTarget(PlayerController p) { _target = p; }

        public void Enter()
        {
            _nextRetarget = 0f;
            _b.audioAdapter?.PlayChase();
        }
        public void Run(float dt)
        {
            if (_target == null) return;
            if (Time.time >= _nextRetarget)
            {
                _nextRetarget = Time.time + _retargetInterval;
                _b.mover.GoTo(_target.transform.position, _b.config.chasePlayerSpeed * _b.SpeedMul);
            }
        }
        public void Exit() { }
    }

    // ------------------------------------------------------------------
    // 5. Search last-known position
    // ------------------------------------------------------------------
    public class GhostSearchLastKnown : IGhostState
    {
        private readonly GhostBrain _b;
        private Vector3 _target;

        public GhostSearchLastKnown(GhostBrain b) { _b = b; }
        public void SetTarget(Vector3 pos) { _target = pos; }
        public bool ReachedTarget { get; private set; }

        public void Enter()
        {
            ReachedTarget = false;
            _b.mover.GoTo(_target, _b.config.searchLastKnownSpeed * _b.SpeedMul);
        }
        public void Run(float dt)
        {
            if (!ReachedTarget && _b.mover.ReachedDestination())
            {
                ReachedTarget = true;
                _b.roamAroundPoint.SetCenter(_target);
                _b.TransitionTo(_b.roamAroundPoint);
            }
        }
        public void Exit() { }
    }

    // ------------------------------------------------------------------
    // 6. Return to base
    // ------------------------------------------------------------------
    public class GhostReturnToBase : IGhostState
    {
        private readonly GhostBrain _b;
        public GhostReturnToBase(GhostBrain b) { _b = b; }

        public void Enter()
        {
            _b.mover.GoTo(_b.basePosition + _b.config.baseStationOffset, _b.config.returnToBaseSpeed * _b.SpeedMul);
        }
        public void Run(float dt)
        {
            if (_b.mover.ReachedDestination())
                _b.TransitionTo(_b.idleAtBase);
        }
        public void Exit() { }
    }

    // ------------------------------------------------------------------
    // 7. Jumpscare leap
    // ------------------------------------------------------------------
    public class GhostJumpscare : IGhostState
    {
        private readonly GhostBrain _b;
        private PlayerController _target;
        private Vector3 _startPos;
        private float _t0;
        private bool _struck;

        public GhostJumpscare(GhostBrain b) { _b = b; }
        public void SetTarget(PlayerController p) { _target = p; }

        public void Enter()
        {
            _t0 = Time.time;
            _struck = false;
            _startPos = _b.ghost.transform.position;
            _b.mover.SetEnabled(false);
            _b.animatorAdapter?.TriggerJumpscareAnim();
            _b.audioAdapter?.PlayJumpscareScream();
        }
        public void Run(float dt)
        {
            if (_target == null) { Finish(); return; }

            float elapsed = Time.time - _t0;
            float k = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, _b.config.jumpscareLeapDuration));
            Vector3 targetPos = _target.transform.position;
            _b.ghost.transform.position = Vector3.Lerp(_startPos, targetPos, k);

            float dist = Vector3.Distance(_b.ghost.transform.position, targetPos);
            if (!_struck && dist <= _b.config.jumpscareCollideDistance)
            {
                _struck = true;
                _target.ServerDown();
            }

            if (k >= 1f || _struck)
                Finish();
        }
        public void Exit()
        {
            _b.mover.SetEnabled(true);
        }

        private void Finish()
        {
            _b.TransitionTo(_b.idleAtBase); // Brain priority resolver will re-route immediately on next Tick
        }
    }

    // ------------------------------------------------------------------
    // 8. Laser ability
    // ------------------------------------------------------------------
    public class GhostLaserAbility : IGhostState
    {
        private readonly GhostBrain _b;
        private float _endTime;
        private bool _firstFrame;

        public GhostLaserAbility(GhostBrain b) { _b = b; }

        public void Enter()
        {
            _endTime = Time.time + _b.config.laserFreezeDuration;
            _firstFrame = true;
            _b.mover.Stop();
            _b.animatorAdapter?.TriggerLaserChargeAnim();
            _b.audioAdapter?.PlayLaserCharge();
            _b.ghost.ServerSetLaserActive(true);
        }
        public void Run(float dt)
        {
            // Skip first frame so players can react to the SyncVar.
            if (_firstFrame) { _firstFrame = false; return; }
            _b.DownAllMovingPlayers(_b.config.laserMaxAllowedSpeed);
            if (Time.time >= _endTime)
                _b.TransitionTo(_b.idleAtBase);
        }
        public void Exit()
        {
            _b.ghost.ServerSetLaserActive(false);
        }
    }
}
