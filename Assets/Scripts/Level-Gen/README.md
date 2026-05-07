# Level Generation System

Procedural level generator for the SCARY co-op horror game. Builds a tile-based map every match: forest, roads, structures, spawns, the Monster Base in the dead center, Power Cores, Objectives — all governed by designer-authored rules. Designed to be **designer-friendly** (everything's a ScriptableObject), **easy to extend** (add a new tile type or rule without touching the engine), and **multiplayer-safe** (one shared seed → identical maps on every client).

---

## TL;DR — How To Use

1. In a scene, create an empty GameObject called `LevelGenerator`.
2. Add the `LevelGenerator` component (this folder, `Core/LevelGenerator.cs`).
3. Create a `LevelConfig` asset: right-click in Project → `Create / SCARY / LevelGen / Level Config`. Fill it in (grid size, tile list, center tile, etc.).
4. Drag the config onto the `LevelGenerator` component's `Config` field.
5. Press Play. The map builds.
6. For multiplayer: also add a `NetworkIdentity` and the `NetworkLevelSync` component to the same GameObject.

---

## The Mental Model (Donkey Edition)

Imagine the map is a chessboard. Each square is a "tile slot." We stamp different stamps onto squares — a forest stamp here, a road stamp there, the monster's house dead in the middle. Stamps are defined by **cards** (ScriptableObjects), and **rules** (also cards) decide where each stamp can land.

- **Tile = a stamp.** A `TileDefinition` asset.
- **Rule = "I can only land if X is true".** A `PlacementRule` asset.
- **Recipe = list of stamps + map size.** A `LevelConfig` asset.
- **Engine = the thing that runs the recipe.** The `LevelGenerator` component.
- **Multiplayer = everyone runs the same recipe with the same random seed.** The `NetworkLevelSync` component.

That's it. Designers only ever touch the cards.

---

## Generation Phases (in order)

When `LevelGenerator.GenerateFromSeed(seed)` runs, it does this:

1. **Init** — creates a `LevelGrid` of the configured size with a seeded RNG.
2. **Center tile** — places `LevelConfig.centerTile` (typically `Tile_MonsterBase`) at the grid center.
3. **Required tiles** — for every `TileDefinition` with `minCount > 0`, it picks random cells until rules pass. Order: PowerCore → Spawn → Objective → Structure → Custom.
4. **Roads** — if `LevelConfig.roadTileSet` is assigned and `carveRoads = true`, paths are carved between every endpoint pair enabled by the connection toggles (Spawn→Core, Core→Core, Core→Objective, etc.). Each path is routed via random perpendicular waypoints (controlled by `pathWindiness`) so it curves instead of going straight. Roads route AROUND any cell whose category is in `roadAvoidCategories` (defaults to MonsterBase). After carving, a second pass walks every road cell, looks at its 4 neighbors, and picks the right shape (straight / curve / T-junction / cross / end-cap) plus rotation from the `RoadTileSet`. See the **Roads** section below for the full system.
5. **Terrain fill** — every empty cell gets a `Terrain`-category tile picked by weight. If no terrain tile fits, `fallbackTerrain` is used.
6. **Instantiate** — every cell with a tile spawns its prefab at `(cell.x - halfW) * tileSize, 0, (cell.y - halfH) * tileSize`. If the prefab has a `PropScatterer`, it scatters props using a per-cell deterministic seed.
7. **Notify** — fires `LevelGenerator.OnLevelGenerated(grid, seed)`. Gameplay code (objective tracker, monster spawner) hooks into this.

If a required tile fails to place, the generator retries the whole pass with a new seed up to `LevelConfig.maxRetries` times before erroring.

---

## Folder Map

```
Assets/Scripts/Level-Gen/
├── README.md                          # this file
├── Core/
│   ├── LevelGenerator.cs              # MonoBehaviour. Runs the phases. Public: GenerateFromSeed(seed), OnLevelGenerated event.
│   ├── LevelGrid.cs                   # 2D cell array. Helpers: Get/Set/IsEmpty, ChebyshevDistance, CellsByCategory.
│   ├── TileInstance.cs                # Runtime record: which TileDefinition occupies which cell, and the spawned GameObject.
│   ├── GenerationContext.cs           # Bundle passed to rules: grid + config + RNG + seed.
│   └── NetworkLevelSync.cs            # NetworkBehaviour. Server rolls seed → SyncVar → all clients regenerate identically.
├── Data/
│   ├── TileCategory.cs                # enum: None, Terrain, Road, Structure, Spawn, Objective, MonsterBase, PowerCore, Custom.
│   ├── TileDefinition.cs              # ScriptableObject. Prefab + category + counts + weight + rules.
│   ├── RoadTileSet.cs                 # ScriptableObject. Bundle of 6 road shape variants + Resolve(mask) → (def, yaw).
│   └── LevelConfig.cs                 # ScriptableObject. Grid size, tile size, seed mode, center tile, tile list, road tile set, road network toggles, windiness, fallback.
├── Rules/
│   ├── PlacementRule.cs               # Abstract base. Override CanPlace(cell, tile, ctx).
│   ├── DistanceFromCategoryRule.cs    # min/max tiles from any tile of a category (e.g., "min 8 from MonsterBase").
│   ├── DistanceFromTileRule.cs        # Same, but relative to a specific TileDefinition asset.
│   ├── EdgeBufferRule.cs              # Must be N tiles from any map edge.
│   ├── ExclusiveZoneRule.cs           # Cannot be within R cells of any tile of these categories.
│   └── WithinRangeRule.cs             # Must be within N cells of at least one tile of a category.
├── Props/
│   ├── PropEntry.cs                   # Serializable struct: prefab + count range + spacing + rotation/scale.
│   └── PropScatterer.cs               # Component on tile prefabs. Scatter(seed) instantiates child props.
└── Editor/
    └── LevelGeneratorGizmos.cs        # Custom inspector + Scene-view gizmo dots colored by category.
```

---

## Adding a New Tile Type (the Healing Chamber walkthrough)

This is the test that proves the system is designer-friendly. **Zero code changes.**

1. Modeller hands over `Tile_HealingChamber.prefab` (10×10 m, pivot bottom-center).
2. In Unity: right-click in Project → `Create / SCARY / LevelGen / Tile Definition`. Name it `Tile_HealingChamber`.
3. In its Inspector:
   - `Prefab`: drag in the chamber prefab.
   - `Category`: `Custom`.
   - `Custom Tag`: `HealingChamber` (optional, for gameplay code lookup).
   - `Min Count`: 1, `Max Count`: 2, `Weight`: 1.
   - `Rules`: click `+` and add:
     - `Rule_DistFromMonster_12` (DistanceFromCategoryRule, category=MonsterBase, minDistance=12).
     - `Rule_DistFromCore_5` (DistanceFromCategoryRule, category=PowerCore, minDistance=5).
     - `Rule_EdgeBuffer_2` (EdgeBufferRule, buffer=2).
4. Open the `LevelConfig` asset and drag `Tile_HealingChamber` into its `Tiles` list.
5. Press Play. Done.

If you need rules that don't exist yet, see "Adding a New Rule" below.

---

## Adding a New Rule

Rules are tiny ScriptableObjects. Example — a rule that forbids placement on diagonal-adjacent corners of any Spawn cell:

```csharp
using UnityEngine;
namespace ScaryGame.LevelGen
{
    [CreateAssetMenu(menuName = "SCARY/LevelGen/Rules/No Diagonal Adjacent Spawn")]
    public class NoDiagonalAdjacentSpawnRule : PlacementRule
    {
        public override bool CanPlace(Vector2Int cell, TileDefinition tile, GenerationContext ctx)
        {
            foreach (var s in ctx.grid.CellsByCategory(TileCategory.Spawn))
            {
                int dx = Mathf.Abs(s.x - cell.x);
                int dy = Mathf.Abs(s.y - cell.y);
                if (dx == 1 && dy == 1) return false;
            }
            return true;
        }
    }
}
```

That's the whole rule. Drop it in `Rules/`, create an `.asset` from the menu, drag it onto any tile.

---

## The Prop System (designer-friendly clutter)

`PropScatterer` lives on a tile prefab. It exposes a list of `PropEntry`. Each entry is a prop prefab plus its scatter rules:

| Field | Purpose |
|---|---|
| `prefab` | The prop (rock, dead branch, debris, glowing wire). |
| `countMin / countMax` | How many to scatter on each instance of this tile. |
| `minSpacing` | World-unit minimum gap between props of the same entry. |
| `randomYRotation` | Yaw randomization on/off. |
| `snapTo90` | If randomYRotation is on, snap to 0/90/180/270. |
| `scaleMin / scaleMax` | Random uniform scale range. |
| `edgePadding` | Buffer from tile edges so props don't clip into neighbors. |

Adding a prop: drag it into the list, set numbers, save. Removing: remove from list. **No scripting.**

Determinism: each tile gets a per-cell seed derived from `(worldSeed, cellX, cellY)`. Same world seed → every client scatters props identically. We never sync prop positions over the network.

---

## Networking (Mirror)

### Architecture
- **The level itself is local.** Trees, ground, roads, structures — all spawned on each client from the same seed. We do NOT send 10,000 tree positions over the wire.
- **Gameplay actors are networked.** Pickup objectives, the monster, doors, anything players interact with. Spawn these on the server using `NetworkServer.Spawn` — typically by listening to `LevelGenerator.OnLevelGenerated` and walking the grid to find marker cells (e.g., Objective category).

### Setup
1. GameObject with `NetworkIdentity` + `LevelGenerator` + `NetworkLevelSync`.
2. `NetworkLevelSync` sets `LevelGenerator.generateOnStart = false` in Awake.
3. Server `OnStartServer`: roll seed → write to `[SyncVar] uint syncedSeed` → call `GenerateFromSeed`.
4. Clients receive the SyncVar via the hook → call `GenerateFromSeed` with the same value → identical map.
5. The generator must be present in the scene **before** clients connect (or place the GameObject as a NetworkManager `spawnPrefab` and spawn it server-side at scene load). For now, easiest is "in the scene".

### Spawning networked objects on top
Pseudocode for a server-side objective spawner:

```csharp
public class ObjectiveSpawner : NetworkBehaviour
{
    public LevelGenerator generator;
    public GameObject objectivePrefab;

    void Start() { generator.OnLevelGenerated += HandleGenerated; }
    void OnDestroy() { if (generator) generator.OnLevelGenerated -= HandleGenerated; }

    void HandleGenerated(LevelGrid grid, uint seed)
    {
        if (!isServer) return;
        float ts = generator.config.tileSize;
        float halfW = (grid.width - 1) * 0.5f;
        float halfH = (grid.height - 1) * 0.5f;
        foreach (var cell in grid.CellsByCategory(TileCategory.Objective))
        {
            var pos = new Vector3((cell.x - halfW) * ts, 1f, (cell.y - halfH) * ts);
            var go = Instantiate(objectivePrefab, pos, Quaternion.identity);
            NetworkServer.Spawn(go);
        }
    }
}
```

---

## LevelConfig Cheat Sheet

| Field | What it does |
|---|---|
| `gridSize` | Map dimensions in tiles. **Use odd numbers** (e.g., 21×21) for the center cell to land at world origin. Even sizes work but offset by half a tile. |
| `tileSize` | World units per tile. All tile prefabs should fit this footprint. |
| `useFixedSeed` / `fixedSeed` | When testing, lock the seed to reproduce the same map. |
| `centerTile` | Always placed at `grid.Center`. Use the Monster Base. |
| `tiles` | The pool. Required tiles (minCount > 0) are placed first. Terrain-category tiles fill empties weighted by `weight`. |
| `fallbackTerrain` | Last-resort terrain when nothing else fits an empty cell. Should have no rules. |
| `roadTileSet` | The `RoadTileSet` asset bundling road shape variants. See **Roads** section. |
| `carveRoads` | Master toggle for the road phase. |
| `roadBranchiness` | 0–1 slider. **0** = pure tree (every redundant path is skipped — clean but sparse). **1** = full mesh (every candidate pair carved — dense). **0.2–0.4** = mostly tree with some loop branches for organic feel. Default 0.25. |
| `connectSpawnsToCores` / `connectSpawnsToObjectives` / `connectCoresToObjectives` / `connectCoresToCores` / `connectObjectivesToObjectives` | Which endpoint categories get linked. Defaults: spawns→cores ON, cores→objectives ON, all others OFF. (Spawns reach objectives THROUGH cores.) |
| `extraRandomConnections` | N additional random paths between any endpoints. Default 0. Adds redundant cycles for variety. |
| `pathWindiness` | 0–1. 0 = direct Manhattan paths. 1 = paths take dramatic curving detours via random waypoints. Default 0.25. |
| `roadAvoidCategories` | Categories that paths route AROUND. Default: `[MonsterBase]`. |
| `maxPlacementAttempts` | Per required-tile, how many random picks before considering this attempt failed. |
| `maxRetries` | How many full regenerations (with a different seed offset) before erroring out. |

---

## Roads

The road system has two parts: **carving** (where roads go) and **resolving** (which prefab + rotation to spawn at each road cell).

### Carving — building the network
1. The generator collects all Spawn, PowerCore, and Objective cells.
2. It builds a deduped list of endpoint pairs based on the connection toggles in LevelConfig.
3. It adds `extraRandomConnections` random extra pairs.
4. Pairs are sorted (deterministic order across runs) and processed one at a time.
5. **Branchiness check**: the generator runs a BFS through existing road cells and gameplay endpoints. If the pair's endpoints can already reach each other via the network so far, the `roadBranchiness` slider decides whether to skip (clean tree) or carve anyway (loop branch). At 0 every redundant pair is skipped; at 1 every pair is carved; in between, a random fraction is carved.
6. For each remaining pair, it routes a path: 0 to 2 random "waypoints" perpendicular to the straight A→B line (offset distance is `pathWindiness × gridRadius`). Each segment is walked greedily, choosing N/E/S/W steps that move toward the next waypoint. If a cell is in `roadAvoidCategories` or already occupied by a non-road structure, the walker tries the alternative axis; if both blocked, it stops that segment.
7. Walked cells get a placeholder road tile (the `straight` shape) — the resolver swaps it for the right shape next.

### Resolving — picking shapes and rotations
After carving, a post-pass walks every road cell. For each cell, it builds a 4-bit neighbor mask:
- bit `BIT_N` (1) — cell to the north is a road or endpoint (Spawn/PowerCore/Objective)
- bit `BIT_E` (2) — cell to the east
- bit `BIT_S` (4) — cell to the south
- bit `BIT_W` (8) — cell to the west

Then `RoadTileSet.Resolve(mask)` returns `(TileDefinition, yawDegrees)` and the cell is updated. 16 mask combinations collapse into 6 shapes via rotation.

### The 5 road prefabs (modeller spec)

All 10×10 m planes, pivot bottom-center, Y=0. The system *requires* all 4 main prefabs (the end-cap and solo are optional). If you only have one prefab during testing, **assign it to every slot** — shapes won't differ but rotations will, which is enough to verify the system works.

| Slot | Authored orientation |
|---|---|
| `straight` | Road runs **N–S** (along +Z). |
| `curve` | 90° L bend connecting **N and E** (enters from +Z, exits to +X). |
| `tJunction` | Bar runs **E–W**, stem points **south (-Z)**. (Missing connection: north.) |
| `cross` | 4-way intersection. Symmetric, no rotation needed. |
| `endCap` (optional) | Dead-end stub, opening points **+Z (north)**. Used when a road has only one neighbor. |
| `solo` (optional) | Lone road cell with zero neighbors. Rare. Falls back to `straight` if null. |

If you author in a different orientation than above, edit the rotation constants inside `RoadTileSet.Resolve(...)` to match. But the convention above is the path of least surprise.

### Designer recipe — wiring it up

1. Make 4–5 graybox road prefabs (just colored planes is fine for now).
2. Right-click → `Create / SCARY / LevelGen / Tile Definition` for each. Set Category = Road. Drag prefab in. (Names like `Tile_Road_Straight`, `Tile_Road_Curve`, etc.)
3. Right-click → `Create / SCARY / LevelGen / Road Tile Set`. Drag each TileDefinition into its slot.
4. Open your LevelConfig and drag the RoadTileSet into the `Road Tile Set` field.
5. Tweak the network toggles + `pathWindiness` to taste.

### Tuning knobs (the only two that really matter)

- **`roadBranchiness`** controls density. 0 = pure spine, 1 = mesh. Try 0.2 first, raise to 0.4 if it feels too sparse.
- **`pathWindiness`** controls how curvy each individual path is. 0 = Manhattan straight, 1 = wandering. Try 0.25 first.

### Recipes

- **Clean tree spine**: `roadBranchiness = 0`, `pathWindiness = 0.2`, only `connectSpawnsToCores` + `connectCoresToObjectives` enabled.
- **Branchy organic network (recommended)**: `roadBranchiness = 0.25`, `pathWindiness = 0.3`, same toggles. Some loop branches, mostly tree.
- **Park feel**: `roadBranchiness = 0.4`, `pathWindiness = 0.5`. Lots of curves and small loops.
- **Dense city grid**: `roadBranchiness = 0.9`, `pathWindiness = 0.1`, all `connect*` toggles on.
- **Roads avoid the monster's house**: keep `MonsterBase` in `roadAvoidCategories`. Add other categories if you want them sacred.

---

## Modeller Brief (paste this to the modeller)

- **Tile size**: 10 m × 10 m. All tile prefabs must fit this footprint.
- **Pivot**: bottom-center, Y = 0. Ground level is exactly Y = 0.
- **Tile-able edges**: forest and ground tiles must look continuous when placed next to copies of themselves. No trees/rocks crossing the seam.
- **Structures (Monster Base, Power Core)**: occupy 1 tile. Must fit inside 10×10×whateverHeight.
- **Naming**: `Tile_<Type>_<Variant>` — e.g., `Tile_Forest_A`, `Tile_Forest_B`. Variants let us add visual variety later by creating multiple TileDefinitions sharing the same category.
- **Props**: separate prefabs (one rock = one prefab), small (≤2 m), pivot at base, Y = 0. Hand them over in a `Props/` folder. We assemble them into PropScatterer lists in Unity.
- **Graybox first**: solid-color materials are fine for testing the system. Visual polish comes later.

---

## Verification Checklist

After first import (or after changes):

1. Compile clean (no console errors).
2. Open a test scene, drop a `LevelGenerator` + `LevelConfig` with cube placeholders for each tile type.
3. Press Play (single-player, no NetworkManager). Map builds.
4. Open Scene view — gizmo dots should show: red center (MonsterBase), blue (PowerCore), green (Spawn), yellow (Objective), brown lines (Road), green wash (Terrain).
5. Confirm: Monster Base dead-center; Power Core respects its min-distance rule; 4 Spawns far from Monster Base; 3 Objectives placed; roads connect Spawn → Core/Objectives.
6. Add a `Tile_HealingChamber` with a "min 12 from MonsterBase" rule. Regenerate. Confirm it never appears within 12 cells of center.
7. Multiplayer test: launch via Mirror's NetworkManager (host + 1 client). Both clients must see identical maps.
8. Change the seed (or set `useFixedSeed = false`). Regenerate. Confirm the layout changes.

---

## Common Errors

- **"Could not place required tile X"**: Rules are too strict for the grid size. Loosen a rule, increase `gridSize`, or raise `maxRetries`.
- **"No LevelConfig assigned"**: Drag a config asset onto the `LevelGenerator` component.
- **Clients see different maps**: They have different seeds. Make sure `NetworkLevelSync` is on the same GameObject as `LevelGenerator` and that clients are NOT calling `GenerateFromSeed` themselves anywhere.
- **Center is offset half a tile**: You're using an even `gridSize`. Use odd dimensions for true centering, or accept the offset.

---

## Out of Scope (intentional, for now)

- Multi-tile structures (3×3 buildings). Single-tile only — we'll add a `footprint` field on TileDefinition when needed.
- Vertical terrain / hills. Flat grid; height variation is a future `TerrainModifier` rule.
- Biome blending in one map.
- Save/load of generated levels — the seed *is* the save.

These are all clean to add later because the data/rules architecture is already separated from the engine.

---

## Public API Summary

| Type | Member | Purpose |
|---|---|---|
| `LevelGenerator` | `LevelConfig config` | The recipe. |
| `LevelGenerator` | `GenerateFromSeed(uint seed)` | Build the map. |
| `LevelGenerator` | `event OnLevelGenerated(LevelGrid, uint)` | Fired when build finishes. Hook gameplay spawners here. |
| `LevelGenerator` | `LevelGrid Grid` | Read-only access to the placed grid. |
| `LevelGenerator` | `uint LastSeed` | The seed of the most recent successful generation. |
| `LevelGenerator` | `ClearExisting()` | Destroy all spawned tiles. |
| `LevelGrid` | `Get(Vector2Int) / Set(...)` | Cell access. |
| `LevelGrid` | `CellsByCategory(TileCategory)` | Iterate placed cells of a category. |
| `LevelGrid` | `CellsByDefinition(TileDefinition)` | Iterate placed cells of a specific tile asset. |
| `LevelGrid` | `ChebyshevDistance(a, b)` | Tile-distance helper used by rules. |
| `PlacementRule` | `CanPlace(cell, tile, ctx)` | Override this in subclasses. |
| `NetworkLevelSync` | (component) | Drop on the same GameObject + NetworkIdentity to sync the seed. |
