using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    public class LevelGenerator : MonoBehaviour
    {
        [Tooltip("The level recipe.")]
        public LevelConfig config;

        [Tooltip("If true, generation runs in Start() with a random/fixed seed (no networking).")]
        public bool generateOnStart = true;

        [Tooltip("Parent transform for spawned tiles. Auto-created if null.")]
        public Transform tileParent;

        public event Action<LevelGrid, uint> OnLevelGenerated;

        private LevelGrid grid;
        public LevelGrid Grid => grid;

        public uint LastSeed { get; private set; }

        void Start()
        {
            if (!generateOnStart) return;
            uint seed = config != null && config.useFixedSeed
                ? config.fixedSeed
                : (uint)UnityEngine.Random.Range(1, int.MaxValue);
            GenerateFromSeed(seed);
        }

        public void GenerateFromSeed(uint seed)
        {
            if (config == null)
            {
                Debug.LogError("[LevelGenerator] No LevelConfig assigned.", this);
                return;
            }

            ClearExisting();

            for (int attempt = 0; attempt < Mathf.Max(1, config.maxRetries); attempt++)
            {
                uint trySeed = seed + (uint)attempt;
                if (TryGenerateOnce(trySeed))
                {
                    LastSeed = trySeed;
                    InstantiateTiles();
                    OnLevelGenerated?.Invoke(grid, trySeed);
                    return;
                }
                Debug.LogWarning($"[LevelGenerator] Generation attempt {attempt + 1} failed validation. Retrying with new seed.");
            }

            Debug.LogError($"[LevelGenerator] Failed to generate level after {config.maxRetries} attempts. Check rules and tile counts.");
        }

        // ------------------------------------------------------------------
        // Phases
        // ------------------------------------------------------------------

        bool TryGenerateOnce(uint seed)
        {
            var ctx = new GenerationContext
            {
                grid = new LevelGrid(config.gridSize.x, config.gridSize.y),
                config = config,
                seed = seed,
                rng = new System.Random(unchecked((int)seed))
            };
            grid = ctx.grid;

            // Phase 1: center tile (Enemy Base).
            if (config.centerTile != null)
            {
                grid.Set(grid.Center, new TileInstance(config.centerTile, grid.Center));
            }

            // Phase 2: base station — always center-bottom.
            BaseStationExit chosenExits = BaseStationExit.Forward;
            if (config.baseStationVariants != null && config.baseStationVariants.Count > 0)
            {
                var variant = PickBaseStationVariant(config.baseStationVariants, ctx);
                if (variant != null && variant.tile != null)
                {
                    int bsX = grid.width / 2;
                    int bsY = config.baseStationBottomOffset;
                    var bsCell = new Vector2Int(bsX, bsY);
                    grid.Set(bsCell, new TileInstance(variant.tile, bsCell));
                    chosenExits = variant.exits;
                }
            }

            // Phase 3: required tiles (minCount > 0).
            var required = new List<TileDefinition>();
            foreach (var t in config.tiles)
            {
                if (t == null || t == config.centerTile) continue;
                if (t.minCount > 0) required.Add(t);
            }
            required.Sort((a, b) => CategoryOrder(a.category).CompareTo(CategoryOrder(b.category)));

            foreach (var def in required)
            {
                int placed = 0;
                while (placed < def.minCount)
                {
                    var candidates = new List<Vector2Int>();
                    foreach (var cell in ctx.grid.AllCells())
                    {
                        if (!ctx.grid.IsEmpty(cell)) continue;
                        if (!RulesPass(cell, def, ctx)) continue;
                        candidates.Add(cell);
                    }
                    if (candidates.Count == 0)
                    {
                        Debug.LogWarning($"[LevelGenerator] Could not place required tile '{def.name}' ({placed}/{def.minCount}). No valid cells remain.");
                        return false;
                    }
                    // Shuffle candidates so placement is truly random across all valid spots.
                    for (int i = candidates.Count - 1; i > 0; i--)
                    {
                        int j = ctx.rng.Next(i + 1);
                        var tmp = candidates[i];
                        candidates[i] = candidates[j];
                        candidates[j] = tmp;
                    }
                    var pick = candidates[0];
                    ctx.grid.Set(pick, new TileInstance(def, pick));
                    placed++;
                    Debug.Log($"[LevelGenerator] Placed '{def.name}' #{placed} at {pick} (had {candidates.Count} valid cells)");
                }
            }

            // Phase 4: roads from base station to power cores.
            if (config.carveRoads && config.roadTileSet != null && config.roadTileSet.straight != null)
            {
                CarveBaseStationRoutes(ctx, chosenExits);
                ResolveRoadShapes(ctx);
            }

            // Phase 5: terrain fill on remaining empty cells.
            FillTerrain(ctx);

            return true;
        }

        static int CategoryOrder(TileCategory c)
        {
            switch (c)
            {
                case TileCategory.BaseStation: return 0;
                case TileCategory.PowerCore: return 1;
                case TileCategory.Structure: return 2;
                case TileCategory.Custom: return 3;
                default: return 9;
            }
        }

        bool RulesPass(Vector2Int cell, TileDefinition def, GenerationContext ctx)
        {
            if (def.maxCount > 0 && ctx.grid.CountByDefinition(def) >= def.maxCount) return false;
            if (def.rules == null) return true;
            foreach (var r in def.rules)
            {
                if (r == null) continue;
                if (!r.CanPlace(cell, def, ctx)) return false;
            }
            return true;
        }

        // ------------------------------------------------------------------
        // Road routing: base station exits → power cores
        // ------------------------------------------------------------------

        void CarveBaseStationRoutes(GenerationContext ctx, BaseStationExit exits)
        {
            var bsCells = new List<Vector2Int>();
            foreach (var c in ctx.grid.CellsByCategory(TileCategory.BaseStation)) bsCells.Add(c);
            if (bsCells.Count == 0)
            {
                Debug.LogWarning("[LevelGenerator] No BaseStation tile found on grid. Make sure the TileDefinition used in baseStationVariants has category = BaseStation.");
                return;
            }
            var bs = bsCells[0];

            var cores = new List<Vector2Int>();
            foreach (var c in ctx.grid.CellsByCategory(TileCategory.PowerCore)) cores.Add(c);
            if (cores.Count == 0)
            {
                Debug.LogWarning("[LevelGenerator] No PowerCore tiles found on grid. Roads need cores to connect to.");
                return;
            }
            Debug.Log($"[LevelGenerator] Routing roads: BaseStation at {bs}, exits={exits}, {cores.Count} cores found.");

            bool hasForward = exits == BaseStationExit.Forward
                           || exits == BaseStationExit.ForwardLeft
                           || exits == BaseStationExit.ForwardRight
                           || exits == BaseStationExit.ForwardLeftRight;
            bool hasLeft    = exits == BaseStationExit.Left
                           || exits == BaseStationExit.ForwardLeft
                           || exits == BaseStationExit.LeftRight
                           || exits == BaseStationExit.ForwardLeftRight;
            bool hasRight   = exits == BaseStationExit.Right
                           || exits == BaseStationExit.ForwardRight
                           || exits == BaseStationExit.LeftRight
                           || exits == BaseStationExit.ForwardLeftRight;

            var exitPoints = new List<Vector2Int>();
            if (hasForward && ctx.grid.InBounds(bs + new Vector2Int(0, 1)))
                exitPoints.Add(bs + new Vector2Int(0, 1));
            if (hasLeft && ctx.grid.InBounds(bs + new Vector2Int(-1, 0)))
                exitPoints.Add(bs + new Vector2Int(-1, 0));
            if (hasRight && ctx.grid.InBounds(bs + new Vector2Int(1, 0)))
                exitPoints.Add(bs + new Vector2Int(1, 0));

            if (exitPoints.Count == 0) return;

            cores.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.x - bs.x, a.y - bs.y);
                float angleB = Mathf.Atan2(b.x - bs.x, b.y - bs.y);
                return angleA.CompareTo(angleB);
            });

            var exitAssignments = new List<List<Vector2Int>>();
            for (int i = 0; i < exitPoints.Count; i++)
                exitAssignments.Add(new List<Vector2Int>());

            if (exitPoints.Count >= cores.Count)
            {
                for (int i = 0; i < cores.Count; i++)
                    exitAssignments[i].Add(cores[i]);
            }
            else
            {
                var assigned = new bool[cores.Count];

                for (int e = 0; e < exitPoints.Count; e++)
                {
                    int bestCore = -1;
                    float bestDist = float.MaxValue;
                    for (int c = 0; c < cores.Count; c++)
                    {
                        if (assigned[c]) continue;
                        float dist = Vector2Int.Distance(exitPoints[e], cores[c]);
                        if (dist < bestDist) { bestDist = dist; bestCore = c; }
                    }
                    if (bestCore >= 0)
                    {
                        exitAssignments[e].Add(cores[bestCore]);
                        assigned[bestCore] = true;
                    }
                }

                for (int c = 0; c < cores.Count; c++)
                {
                    if (assigned[c]) continue;
                    int bestExit = 0;
                    float bestDist = float.MaxValue;
                    for (int e = 0; e < exitPoints.Count; e++)
                    {
                        float dist = Vector2Int.Distance(exitPoints[e], cores[c]);
                        if (dist < bestDist) { bestDist = dist; bestExit = e; }
                    }
                    exitAssignments[bestExit].Add(cores[c]);
                }
            }

            float forkChance = Mathf.Clamp01(ctx.config.branchForkChance);
            for (int e = 0; e < exitPoints.Count; e++)
            {
                var exitPt = exitPoints[e];
                var assignedCores = exitAssignments[e];
                if (assignedCores.Count == 0) continue;

                if (ctx.grid.IsEmpty(exitPt))
                    ctx.grid.Set(exitPt, new TileInstance(ctx.config.roadTileSet.straight, exitPt));

                if (assignedCores.Count == 1)
                {
                    PaintPath(exitPt, assignedCores[0], ctx);
                }
                else
                {
                    bool doFork = ctx.rng.NextDouble() < forkChance;
                    if (doFork)
                    {
                        Vector2 mid = Vector2.zero;
                        foreach (var core in assignedCores) mid += (Vector2)core;
                        mid /= assignedCores.Count;
                        Vector2 forkPos = Vector2.Lerp(exitPt, mid, 0.4f + (float)ctx.rng.NextDouble() * 0.2f);
                        var forkCell = new Vector2Int(
                            Mathf.Clamp(Mathf.RoundToInt(forkPos.x), 1, ctx.grid.width - 2),
                            Mathf.Clamp(Mathf.RoundToInt(forkPos.y), 1, ctx.grid.height - 2)
                        );

                        PaintPath(exitPt, forkCell, ctx);
                        foreach (var core in assignedCores)
                            PaintPath(forkCell, core, ctx);
                    }
                    else
                    {
                        var prev = exitPt;
                        foreach (var core in assignedCores)
                        {
                            PaintPath(prev, core, ctx);
                            prev = core;
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Path painting
        // ------------------------------------------------------------------

        void PaintPath(Vector2Int from, Vector2Int to, GenerationContext ctx)
        {
            var waypoints = new List<Vector2Int> { from };

            float windiness = Mathf.Clamp01(ctx.config.pathWindiness);
            if (windiness > 0.01f)
            {
                int waypointCount = (windiness > 0.6f) ? 2 : 1;
                Vector2 dir = (Vector2)(to - from);
                float dirLen = dir.magnitude;
                if (dirLen > 0.001f) dir /= dirLen;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                float gridRadius = (ctx.grid.width + ctx.grid.height) * 0.25f;
                float maxOffset = windiness * gridRadius;

                for (int i = 1; i <= waypointCount; i++)
                {
                    float t = (float)i / (waypointCount + 1);
                    Vector2 mid = Vector2.Lerp(from, to, t);
                    float offset = (float)((ctx.rng.NextDouble() * 2.0 - 1.0) * maxOffset);
                    mid += perp * offset;
                    var wp = new Vector2Int(
                        Mathf.Clamp(Mathf.RoundToInt(mid.x), 1, ctx.grid.width - 2),
                        Mathf.Clamp(Mathf.RoundToInt(mid.y), 1, ctx.grid.height - 2)
                    );
                    waypoints.Add(wp);
                }
            }
            waypoints.Add(to);

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                WalkSegment(waypoints[i], waypoints[i + 1], ctx);
            }
        }

        void WalkSegment(Vector2Int from, Vector2Int to, GenerationContext ctx)
        {
            var cur = from;
            int safety = (ctx.grid.width + ctx.grid.height) * 4;
            while (cur != to && safety-- > 0)
            {
                int dx = Math.Sign(to.x - cur.x);
                int dy = Math.Sign(to.y - cur.y);

                bool preferX = (dx != 0 && (dy == 0 || ctx.rng.Next(2) == 0));
                Vector2Int firstChoice = preferX
                    ? new Vector2Int(cur.x + dx, cur.y)
                    : new Vector2Int(cur.x, cur.y + dy);
                Vector2Int secondChoice = preferX
                    ? new Vector2Int(cur.x, cur.y + dy)
                    : new Vector2Int(cur.x + dx, cur.y);

                Vector2Int next;
                if (CanRoadEnter(firstChoice, to, ctx)) next = firstChoice;
                else if (CanRoadEnter(secondChoice, to, ctx)) next = secondChoice;
                else break;

                if (ctx.grid.IsEmpty(next))
                {
                    ctx.grid.Set(next, new TileInstance(ctx.config.roadTileSet.straight, next));
                }
                cur = next;
            }
        }

        bool CanRoadEnter(Vector2Int cell, Vector2Int destination, GenerationContext ctx)
        {
            if (!ctx.grid.InBounds(cell)) return false;
            if (cell == destination) return true;
            var existing = ctx.grid.Get(cell);
            if (existing == null || existing.definition == null) return true;
            var cat = existing.definition.category;
            if (cat == TileCategory.Road) return true;
            if (ctx.config.roadAvoidCategories != null)
            {
                foreach (var avoidCat in ctx.config.roadAvoidCategories)
                    if (cat == avoidCat) return false;
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Road shape resolution
        // ------------------------------------------------------------------

        void ResolveRoadShapes(GenerationContext ctx)
        {
            var roadCells = new List<Vector2Int>();
            foreach (var c in ctx.grid.CellsByCategory(TileCategory.Road)) roadCells.Add(c);

            foreach (var cell in roadCells)
            {
                int mask = 0;
                if (IsRoadConnection(cell + new Vector2Int(0,  1), ctx)) mask |= RoadTileSet.BIT_N;
                if (IsRoadConnection(cell + new Vector2Int(1,  0), ctx)) mask |= RoadTileSet.BIT_E;
                if (IsRoadConnection(cell + new Vector2Int(0, -1), ctx)) mask |= RoadTileSet.BIT_S;
                if (IsRoadConnection(cell + new Vector2Int(-1, 0), ctx)) mask |= RoadTileSet.BIT_W;

                var resolved = ctx.config.roadTileSet.Resolve(mask);
                var tile = ctx.grid.Get(cell);
                tile.definition = resolved.def;
                tile.useRotationOverride = true;
                tile.rotationOverride = Quaternion.Euler(0f, resolved.yaw, 0f);
            }
        }

        static bool IsRoadConnection(Vector2Int cell, GenerationContext ctx)
        {
            if (!ctx.grid.InBounds(cell)) return false;
            var t = ctx.grid.Get(cell);
            if (t == null || t.definition == null) return false;
            var cat = t.definition.category;
            return cat == TileCategory.Road
                || cat == TileCategory.PowerCore
                || cat == TileCategory.BaseStation;
        }

        // ------------------------------------------------------------------
        // Terrain fill
        // ------------------------------------------------------------------

        void FillTerrain(GenerationContext ctx)
        {
            var pool = new List<TileDefinition>();
            float totalWeight = 0f;
            foreach (var t in ctx.config.tiles)
            {
                if (t == null) continue;
                if (t.category != TileCategory.Terrain) continue;
                if (t.weight <= 0f) continue;
                pool.Add(t);
                totalWeight += t.weight;
            }

            var fallback = ctx.config.fallbackTerrain;
            if (fallback == null && pool.Count > 0) fallback = pool[0];
            if (fallback == null)
            {
                foreach (var t in ctx.config.tiles)
                {
                    if (t != null && t.prefab != null && t.category != TileCategory.MonsterBase)
                    {
                        fallback = t;
                        break;
                    }
                }
            }

            foreach (var cell in ctx.grid.AllCells())
            {
                if (!ctx.grid.IsEmpty(cell)) continue;
                var def = PickWeighted(pool, totalWeight, ctx);
                if (def == null || !RulesPass(cell, def, ctx)) def = fallback;
                if (def == null) continue;
                ctx.grid.Set(cell, new TileInstance(def, cell));
            }

            int stillEmpty = 0;
            foreach (var cell in ctx.grid.AllCells())
            {
                if (!ctx.grid.IsEmpty(cell)) continue;
                if (fallback != null) ctx.grid.Set(cell, new TileInstance(fallback, cell));
                else stillEmpty++;
            }
            if (stillEmpty > 0)
            {
                Debug.LogWarning($"[LevelGenerator] {stillEmpty} cells left empty — no usable terrain or fallback.");
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        static BaseStationVariant PickBaseStationVariant(List<BaseStationVariant> variants, GenerationContext ctx)
        {
            float totalWeight = 0f;
            foreach (var v in variants)
            {
                if (v != null && v.tile != null) totalWeight += v.weight;
            }
            if (totalWeight <= 0f) return variants.Count > 0 ? variants[0] : null;
            float pick = (float)(ctx.rng.NextDouble() * totalWeight);
            float acc = 0f;
            foreach (var v in variants)
            {
                if (v == null || v.tile == null) continue;
                acc += v.weight;
                if (pick <= acc) return v;
            }
            return variants[variants.Count - 1];
        }

        static TileDefinition PickWeighted(List<TileDefinition> pool, float total, GenerationContext ctx)
        {
            if (pool.Count == 0 || total <= 0f) return null;
            float pick = (float)(ctx.rng.NextDouble() * total);
            float acc = 0f;
            foreach (var t in pool)
            {
                acc += t.weight;
                if (pick <= acc) return t;
            }
            return pool[pool.Count - 1];
        }

        // ------------------------------------------------------------------
        // Instantiation
        // ------------------------------------------------------------------

        void InstantiateTiles()
        {
            if (tileParent == null)
            {
                var go = new GameObject("LevelTiles");
                go.transform.SetParent(transform, false);
                tileParent = go.transform;
            }

            float ts = config.tileSize;
            float halfW = (grid.width - 1) * 0.5f;
            float halfH = (grid.height - 1) * 0.5f;

            foreach (var cell in grid.AllCells())
            {
                var tile = grid.Get(cell);
                if (tile == null || tile.definition == null || tile.definition.prefab == null) continue;

                var pos = new Vector3((cell.x - halfW) * ts, config.tileYOffset, (cell.y - halfH) * ts);
                Quaternion rot = Quaternion.identity;
                if (tile.useRotationOverride)
                {
                    rot = tile.rotationOverride;
                }
                else if (tile.definition.randomYRotationStep90)
                {
                    int q = new System.Random(unchecked((int)(LastSeed ^ (uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663)))).Next(0, 4);
                    rot = Quaternion.Euler(0f, q * 90f, 0f);
                }

                var go = Instantiate(tile.definition.prefab, pos, rot, tileParent);
                go.name = $"{tile.definition.name}_{cell.x}_{cell.y}";
                tile.spawned = go;

                var scatter = go.GetComponent<PropScatterer>();
                if (scatter != null)
                {
                    scatter.tileSize = ts;
                    uint propSeed = unchecked(LastSeed ^ (uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663));
                    scatter.Scatter(propSeed);
                }
            }
        }

        public void ClearExisting()
        {
            if (tileParent != null)
            {
                if (Application.isPlaying) Destroy(tileParent.gameObject);
                else DestroyImmediate(tileParent.gameObject);
                tileParent = null;
            }
            grid = null;
        }
    }
}
