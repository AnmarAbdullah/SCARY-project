using UnityEngine;

namespace TimeFracture.Audio
{
    /// <summary>
    /// Attach to any ground object to identify its surface type.
    /// The footstep system reads this when the player walks on it.
    /// </summary>
    public class SurfaceIdentifier : MonoBehaviour
    {
        public SurfaceType surfaceType = SurfaceType.Default;
    }
}
