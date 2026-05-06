using TimeFracture.Audio;

namespace TimeFracture.Interfaces
{
    public interface IAudioPlayer
    {
        void PlayFootstep(FootstepState state);
        void PlayJump();
        void PlayLand(float impactSpeed);
        void SetSprintBreathing(bool active);
    }
}
