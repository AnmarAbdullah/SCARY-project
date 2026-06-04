using System.Collections.Generic;
using TimeFracture.Player;

namespace ScaryGame.Players
{
    /// <summary>
    /// Server-side directory of player controllers. Avoids per-frame
    /// FindObjectsOfType from systems like Ghost perception.
    /// </summary>
    public static class PlayerRegistry
    {
        private static readonly List<PlayerController> _all = new List<PlayerController>();

        public static IReadOnlyList<PlayerController> All => _all;

        public static void Register(PlayerController p)
        {
            if (p == null || _all.Contains(p)) return;
            _all.Add(p);
        }

        public static void Unregister(PlayerController p)
        {
            if (p == null) return;
            _all.Remove(p);
        }

        public static void GetAlive(List<PlayerController> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                var p = _all[i];
                if (p != null && !p.IsDowned) buffer.Add(p);
            }
        }
    }
}
