using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScaryGame.LevelGen
{
    /// <summary>
    /// Plain MonoBehaviour. Builds a map from a LevelConfig + seed.
    /// For singleplayer/test: generates automatically in Start with a random seed.
    /// For networking: pair with NetworkLevelSync, which calls GenerateFromSeed(seed) on every client.
    /// </summary>
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

            // Phase 1: center tile (Monster Base).
            if (config.centerTile != null)
            {
                grid.Set(grid.Center, new TileInstance(config.centerTile, grid.Center));
            }

            // Phase 2: required tiles (minCount > 0). Order: more constrained categories first.
            var required = new List<TileDefinition>();
            foreach (var t in config.tiles)
            {
                if (t == null || t == config.centerTile) continue;
                if (t.minCount > 0) required.Add(t);
            }
            // Stable order: PowerCore, Spawn, Objective, Custom, then everything else.
            required.Sort((a, b) => CategoryOrder(a.category).CompareTo(CategoryOrder(b.category)));

            foreach (var def in required)
            {
                int placed = 0;
                int attempts = 0;
                while (placed < def.minCount && attempts < config.maxPlacementAttempts * def.minCount)
                {
                    attempts++;
                    var cell = new Vector2Int(ctx.rng.Next(0, grid.width), ctx.rng.Next(0, grid.height));
                    if (!grid.IsEmpty(cell)) continue;
                    if (!RulesPass(cell, def, ctx)) continue;
                    grid.Set(cell, new TileInstance(def, cell));
                    placed++;
                }
                if (placed < def.minCount)
                {
                    Debug.LogWarning($"[LevelGenerator] Could not place required tile '{def.name}' ({placed}/{def.minCount}).");
                    return false;
                }
            }

            // Phase 3: roads.
            if (config.carveRoads && config.roadTileSet != null && config.roadTileSet.straight != null)
            {
                CarveRoads(ctx);
                ResolveRoadShapes(ctx);
            }

            // Phase 4: terrain fill on remaining empty cells.
            FillTerrain(ctx);

            return true;
        }

        static int CategoryOrder(TileCategory c)
        {
            switch (c)
            {
                case TileCategory.PowerCore: return 0;
                case TileCategory.Spawn: return 1;
                case TileCategory.Objective: return 2;
                case TileCategory.Structure: return 3;
                case TileCategory.Custom: return 4;
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

        // Build the list of endpoint pairs to connect based on the LevelConfig toggles,
        // then carve a winding path between each pair.
        void CarveRoads(GenerationContext ctx)
        {
            var spawns = new List<Vector2Int>();
            var cores = new List<Vector2Int>();
            var objectives = new List<Vector2Int>();
            foreach (var c in ctx.grid.CellsByCategory(TileCategory.Spawn)) spawns.Add(c);
            foreach (var c in ctx.grid.CellsByCategory(TileCategory.PowerCore)) cores.Add(c);
            foreach (var c in ctx.grid.CellsByCategory(TileCategory.Objective)) objectives.Add(c);

            var pairs = new HashSet<(Vector2Int, Vector2Int)>();

            if (ctx.config.connectSpawnsToCores)
                foreach (var s in spawns) foreach (var c in cores) AddPair(pairs, s, c);
            if (ctx.config.connectSpawnsToObjectives)
                foreach (var s in spawns) foreach (var o in objectives) AddPair(pairs, s, o);
            if (ctx.config.connectCoresToObjectives)
                foreach (var c in cores) foreach (var o in objectives) AddPair(pairs, c, o);
            if (ctx.config.connectCoresToCores)
                for (int i = 0; i < cores.Count; i++)
                    for (int j = i + 1; j < cores.Count; j++)
                        AddPair(pairs, cores[i], cores[j]);
            if (ctx.config.connectObjectivesToObjectives)
                for (int i = 0; i < objectives.Count; i++)
                    for (int j = i + 1; j < objectives.Count; j++)
                        AddPair(pairs, objectives[i], objectives[j]);

            var allEndpoints = new List<Vector2Int>();
            allEndpoints.AddRange(spawns);
            allEndpoints.AddRange(cores);
            allEndpoints.AddRange(objectives);
            for (int i = 0; i < ctx.config.extraRandomConnections && allEndpoints.Count >= 2; i++)
            {
                int a = ctx.rng.Next(allEndpoints.Count);
                int b = ctx.rng.Next(allEndpoints.Count);
                if (a != b) AddPair(pairs, allEndpoints[a], allEndpoints[b]);
            }

            // Sort for deterministic order so the same seed produces the same network across runs.
            var orderedPairs = new List<(Vector2Int, Vector2Int)>(pairs);
            orderedPairs.Sort((p, q) =>
            {
                int cmp = p.Item1.x.CompareTo(q.Item1.x); if (cmp != 0) return cmp;
                cmp = p.Item1.y.CompareTo(q.Item1.y); if (cmp != 0) return cmp;
                cmp = p.Item2.x.CompareTo(q.Item2.x); if (cmp != 0) return cmp;
                return p.Item2.y.CompareTo(q.Item2.y);
            });

            // For each candidate pair: if it's already reachable through existing roads, the
            // 'roadBranchiness' setting decides whether to carve it anyway (creating a loop branch)
            // or skip it (keeping the network tree-like).
            float branchiness = Mathf.Clamp01(ctx.config.roadBranchiness);
            foreach (var pair in orderedPairs)
            {
                if (AlreadyConnected(pair.Item1, pair.Item2, ctx))
                {
                    // Already connected — only carve if random roll says yes.
                    if (ctx.rng.NextDouble() > branchiness) continue;
                }
                PaintPath(pair.Item1, pair.Item2, ctx);
            }
        }

        // BFS through road cells (and gameplay endpoints) to test if A reaches B.
        bool AlreadyConnected(Vector2Int a, Vector2Int b, GenerationContext ctx)
        {
            if (a == b) return true;
            var visited = new HashSet<Vector2Int> { a };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(a);
            var dirs = new Vector2Int[]
            {
                new Vector2Int(0,  1), new Vector2Int(1, 0),
                new Vector2Int(0, -1), new Vector2Int(-1, 0)
            };
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var d in dirs)
                {
                    var next = cur + d;
                    if (visited.Contains(next)) continue;
                    if (next == b) return true;
                    if (!IsRoadConnection(next, ctx)) continue;
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        static void AddPair(HashSet<(Vector2Int, Vector2Int)> set, Vector2Int a, Vector2Int b)
        {
            if (a == b) return;
            // Canonical order so (a,b) and (b,a) are the same pair.
            if (a.x < b.x || (a.x == b.x && a.y < b.y)) set.Add((a, b));
            else set.Add((b, a));
        }

        // Routes from -> to via 1-2 random perpendicular waypoints. Higher windiness = more dramatic detours.
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

        // Greedy path with axis preference + obstacle avoidance.
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
                    // Placeholder shape; ResolveRoadShapes swaps it for the right one later.
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
            // Other placed tiles (PowerCore, Spawn, Objective, Custom) — don't overwrite, stop here.
            return false;
        }

        // After paths are carved, walk every road cell and pick the right shape + rotation
        // based on which of its 4 cardinal neighbors are roads or road-connected structures.
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

        // A cell counts as a road "neighbor" for shape-picking if it's another road or any
        // gameplay endpoint the road plugs into (Spawn, PowerCore, Objective).
        static bool IsRoadConnection(Vector2Int cell, GenerationContext ctx)
        {
            if (!ctx.grid.InBounds(cell)) return false;
            var t = ctx.grid.Get(cell);
            if (t == null || t.definition == null) return false;
            var cat = t.definition.category;
            return cat == TileCategory.Road
                || cat == TileCategory.Spawn
                || cat == TileCategory.PowerCore
                || cat == TileCategory.Objective;
        }

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

            // Resolve a guaranteed fallback so no cell is ever left empty.
            // Order: explicit fallbackTerrain > first Terrain tile in pool > first tile in config with a prefab.
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

            // Final guarantee pass — anything still empty gets the fallback (or a clear warning).
            int stillEmpty = 0;
            foreach (var cell in ctx.grid.AllCells())
            {
                if (!ctx.grid.IsEmpty(cell)) continue;
                if (fallback != null) ctx.grid.Set(cell, new TileInstance(fallback, cell));
                else stillEmpty++;
            }
            if (stillEmpty > 0)
            {
                Debug.LogWarning($"[LevelGenerator] {stillEmpty} cells left empty — no usable terrain or fallback. Add a Terrain-category tile to LevelConfig.tiles or set fallbackTerrain.");
            }
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

                var pos = new Vector3((cell.x - halfW) * ts, 0f, (cell.y - halfH) * ts);
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
