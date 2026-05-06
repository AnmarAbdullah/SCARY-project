using System;
using UnityEngine;

namespace TimeFracture.Audio
{
    /// <summary>
    /// One profile holds footstep clips for every surface type.
    /// Create via: Right Click → Create → TimeFracture → Footstep Profile
    /// </summary>
    [CreateAssetMenu(menuName = "TimeFracture/Footstep Profile", fileName = "FP_Profile")]
    public class FootstepProfile : ScriptableObject
    {
        [Serializable]
        public class SurfaceEntry
        {
            public SurfaceType surface = SurfaceType.Default;

            [Header("Clips")]
            public AudioClip[] walkClips;
            public AudioClip[] sprintClips;
            public AudioClip[] crouchClips;

            [Header("Volume")]
            [Range(0f, 1f)] public float walkVolume   = 0.5f;
            [Range(0f, 1f)] public float sprintVolume = 0.7f;
            [Range(0f, 1f)] public float crouchVolume = 0.25f;

            [Header("Pitch Variance")]
            [Range(0f, 0.3f)] public float pitchVariance = 0.08f;
        }

        public SurfaceEntry[] surfaces;

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Returns the entry for a surface type. Falls back to Default if not found.</summary>
        public SurfaceEntry GetEntry(SurfaceType type)
        {
            if (surfaces == null || surfaces.Length == 0) return null;

            SurfaceEntry fallback = null;

            foreach (var entry in surfaces)
            {
                if (entry.surface == type)    return entry;
                if (entry.surface == SurfaceType.Default) fallback = entry;
            }

            return fallback ?? surfaces[0];
        }

        public AudioClip GetClip(SurfaceType surface, FootstepState state, ref int lastIndex)
        {
            SurfaceEntry entry = GetEntry(surface);
            if (entry == null) return null;

            AudioClip[] pool = state == FootstepState.Sprint ? entry.sprintClips
                             : state == FootstepState.Crouch ? entry.crouchClips
                             : entry.walkClips;

            if (pool == null || pool.Length == 0) return null;
            if (pool.Length == 1) return pool[0];

            int index;
            do { index = UnityEngine.Random.Range(0, pool.Length); }
            while (index == lastIndex);

            lastIndex = index;
            return pool[index];
        }

        public float GetVolume(SurfaceType surface, FootstepState state)
        {
            SurfaceEntry entry = GetEntry(surface);
            if (entry == null) return 0.5f;
            return state == FootstepState.Sprint ? entry.sprintVolume
                 : state == FootstepState.Crouch ? entry.crouchVolume
                 : entry.walkVolume;
        }

        public float GetPitchVariance(SurfaceType surface)
        {
            return GetEntry(surface)?.pitchVariance ?? 0.08f;
        }
    }
}
