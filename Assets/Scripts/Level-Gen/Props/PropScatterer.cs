using System.Collections.Generic;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    /// <summary>
    /// Add to a tile prefab. After the tile is instantiated by LevelGenerator,
    /// the generator calls Scatter(seed) so every client produces identical visuals.
    /// </summary>
    public class PropScatterer : MonoBehaviour
    {
        [Tooltip("Tile footprint in world units. Should match LevelConfig.tileSize.")]
        public float tileSize = 10f;

        [Tooltip("Props to scatter. Each entry is a list of placements with its own count and spacing.")]
        public List<PropEntry> props = new List<PropEntry>();

        [Tooltip("How many tries per prop placement before giving up that one.")]
        public int placementAttemptsPerProp = 20;

        public void Scatter(uint seed)
        {
            var rng = new System.Random(unchecked((int)seed));
            var placed = new List<Vector3>();

            foreach (var entry in props)
            {
                if (entry == null || entry.prefab == null) continue;
                int count = rng.Next(entry.countMin, entry.countMax + 1);
                int placedThis = 0;
                int safety = count * placementAttemptsPerProp;

                while (placedThis < count && safety-- > 0)
                {
                    float halfSize = tileSize * 0.5f;
                    float pad = entry.edgePadding;
                    float lx = (float)(rng.NextDouble() * (tileSize - pad * 2)) - (halfSize - pad);
                    float lz = (float)(rng.NextDouble() * (tileSize - pad * 2)) - (halfSize - pad);
                    var localPos = new Vector3(lx, 0f, lz);
                    var worldPos = transform.TransformPoint(localPos);

                    if (entry.minSpacing > 0f)
                    {
                        bool tooClose = false;
                        for (int i = 0; i < placed.Count; i++)
                        {
                            if (Vector3.Distance(placed[i], worldPos) < entry.minSpacing)
                            {
                                tooClose = true;
                                break;
                            }
                        }
                        if (tooClose) continue;
                    }

                    Quaternion rot = Quaternion.identity;
                    if (entry.randomYRotation)
                    {
                        float yaw = entry.snapTo90
                            ? rng.Next(0, 4) * 90f
                            : (float)(rng.NextDouble() * 360.0);
                        rot = Quaternion.Euler(0f, yaw, 0f);
                    }

                    float scale = Mathf.Lerp(entry.scaleMin, entry.scaleMax, (float)rng.NextDouble());
                    var go = Instantiate(entry.prefab, worldPos, rot, transform);
                    go.transform.localScale = entry.prefab.transform.localScale * scale;

                    placed.Add(worldPos);
                    placedThis++;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(tileSize, 0.1f, tileSize));
        }
    }
}
