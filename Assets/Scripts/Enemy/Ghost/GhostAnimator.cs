using UnityEngine;

namespace ScaryGame.Enemy
{
    /// <summary>
    /// Stub adapter. Sets parameters / triggers on an Animator. The rig will be
    /// wired by the artist later — until then this is safe to call with no clips.
    /// </summary>
    public class GhostAnimator : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        public const string ParamIsChasing = "IsChasing";
        public const string TriggerIdle = "Idle";
        public const string TriggerJumpscare = "Jumpscare";
        public const string TriggerLaserCharge = "LaserCharge";

        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void SetSpeed(float speed)
        {
            if (animator == null) return;
            animator.SetFloat(ParamSpeed, speed);
        }

        public void SetChasing(bool chasing)
        {
            if (animator == null) return;
            animator.SetBool(ParamIsChasing, chasing);
        }

        public void TriggerIdleAnim() => SafeTrigger(TriggerIdle);
        public void TriggerJumpscareAnim() => SafeTrigger(TriggerJumpscare);
        public void TriggerLaserChargeAnim() => SafeTrigger(TriggerLaserCharge);

        private void SafeTrigger(string t)
        {
            if (animator == null) return;
            animator.ResetTrigger(t);
            animator.SetTrigger(t);
        }
    }
}
