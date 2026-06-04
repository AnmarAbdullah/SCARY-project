using UnityEngine;

namespace ScaryGame.Enemy
{
    /// <summary>
    /// Stub adapter for Ghost SFX. Clips can be assigned later — playback
    /// is a no-op when an AudioClip is null.
    /// </summary>
    public class GhostAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource source;

        [Header("Clips (optional — assign later)")]
        public AudioClip chaseLoop;
        public AudioClip detect;
        public AudioClip roam;
        public AudioClip jumpscareScream;
        public AudioClip laserCharge;

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
        }

        public void PlayChase()              => PlayOneShot(chaseLoop);
        public void PlayDetect()             => PlayOneShot(detect);
        public void PlayRoam()               => PlayOneShot(roam);
        public void PlayJumpscareScream()    => PlayOneShot(jumpscareScream);
        public void PlayLaserCharge()        => PlayOneShot(laserCharge);

        private void PlayOneShot(AudioClip clip)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip);
        }
    }
}
