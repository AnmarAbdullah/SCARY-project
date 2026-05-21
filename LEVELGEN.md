# Procedural Level Generation System

All code lives in `Assets/Scripts/Level-Gen/` under namespace `ScaryGame.LevelGen`.

## Architecture Overview

The system is **seed-deterministic**: given the same seed, every client generates an identical map. The server rolls a seed and syncs it via `NetworkLevelSync` (Mirror SyncVar). Clients never exchange grid data -- they just run the same algorithm.

```
NetworkLevelSync (server rolls seed, SyncVar to clients)
    -> LevelGenerator.GenerateFromSeed(seed)
        -> Phase 1: Place center tile (MonsterBase) at grid center
        -> Phase 2: Place BaseStation at center-bottom
        -> Phase 3: Place required tiles (PowerCores, Structures) with rule validation
        -> Phase 4: Carve roads from BaseStation exits to PowerCores
        -> Phase 5: Fill remaining cells with weighted terrain tiles
        -> InstantiateTiles() (spawn prefabs, run PropScatterer on each)
```

## Core Classes

### LevelGenerator (`Core/LevelGenerator.cs`)
The orchestrator. Attached to a GameObject with `NetworkLevelSync` and `NetworkIdentity`.
- `config`: reference to a `LevelConfig` ScriptableObject
- `generateOnStart`: for offline testing without networking
- `GenerateFromSeed(uint seed)`: main entry point. Retries with seed+1, seed+2, etc. on failure (up to `maxRetries`)
- `OnLevelGenerated` event: fires after successful generation with the grid and seed
- `ClearExisting()`: destroys all spawned tiles

### LevelGrid (`Core/LevelGrid.cs`)
2D array of `TileInstance` cells. Pure data, no MonoBehaviour.
- `width`, `height`: grid dimensions from config
- `Center`: `Vector2Int(width/2, height/2)` -- where the MonsterBase goes
- `AllCells()`, `CellsByCategory()`, `CellsByDefinition()`: iteration helpers
- `ChebyshevDistance()`: used by placement rules

### NetworkLevelSync (`Core/NetworkLevelSync.cs`)
Mirror NetworkBehaviour that bridges networking and generation:
- `OnStartServer()`: rolls seed (random or fixed from config), writes to SyncVar, generates
- `OnStartClient()`: if not host, generates from synced seed
- `OnSeedChanged` hook: regenerates if seed updates mid-game (e.g., for restart)

### TileInstance (`Core/TileInstance.cs`)
Runtime data for one placed tile: definition reference, grid cell, spawned GameObject, optional rotation override (used by road auto-tiler).

### GenerationContext (`Core/GenerationContext.cs`)
Passed through all generation phases: holds the grid, config, seeded RNG (`System.Random`), and seed value.

## Data / Configuration

### LevelConfig (`Data/LevelConfig.cs`)
ScriptableObject. The "recipe" for a level. Key fields:
- `gridSize`: Vector2Int, default 20x20
- `tileSize`: world units per cell, default 10
- `useFixedSeed` / `fixedSeed`: for testing determinism
- `centerTile`: TileDefinition for the MonsterBase (always at grid center)
- `baseStationVariants`: list of BaseStationVariant (tile + exit directions + weight)
- `baseStationBottomOffset`: how many rows above the bottom edge (default 1)
- `tiles`: the tile pool (required tiles placed first, terrain fills the rest)
- `fallbackTerrain`: guaranteed fill for any cell that doesn't match anything
- `roadTileSet`: RoadTileSet SO for road shape auto-selection
- `carveRoads`: master toggle for road phase
- `roadBranchiness`: 0=tree, 1=mesh. Controls extra road loops
- `branchForkChance`: probability roads fork vs. chain through cores
- `pathWindiness`: 0=straight Manhattan paths, 1=dramatic curves via random waypoints
- `roadAvoidCategories`: categories roads won't overwrite (default: MonsterBase)
- `terrainPaintProfile`: shared terrain painting settings
- `maxPlacementAttempts`, `maxRetries`: safety limits

### TileDefinition (`Data/TileDefinition.cs`)
ScriptableObject per tile type. Fields:
- `prefab`: the GameObject to spawn
- `category`: TileCategory enum (Terrain, Road, Structure, MonsterBase, PowerCore, BaseStation, Custom)
- `minCount` / `maxCount`: required placement counts
- `weight`: probability weight for terrain fill phase
- `rules`: list of PlacementRule SOs that must all pass
- `randomYRotationStep90`: random 0/90/180/270 rotation on spawn

### TileCategory (`Data/TileCategory.cs`)
Enum: `None, Terrain, Road, Structure, MonsterBase, PowerCore, BaseStation, Custom`

### RoadTileSet (`Data/RoadTileSet.cs`)
ScriptableObject bundling 6 road shape slots: `straight`, `curve`, `tJunction`, `cross`, `endCap`, `solo`.
- `Resolve(int mask)` takes a 4-bit neighbor bitmask (N/E/S/W) and returns the correct tile + Y rotation
- Authoring convention: straight runs N-S, curve connects N-E, T-junction missing N, etc.

### BaseStationLayout (`Data/BaseStationLayout.cs`)
- `BaseStationExit` enum: Forward, Left, Right, and combinations
- `BaseStationVariant`: tile + exits + selection weight

## Placement Rules

Abstract base: `PlacementRule` (`Rules/PlacementRule.cs`) -- ScriptableObject with `bool CanPlace(cell, tileDef, context)`.

Concrete rules:
| Rule | What it does |
|---|---|
| `DistanceFromTileRule` | Min/max Chebyshev distance from a specific TileDefinition |
| `DistanceFromCategoryRule` | Min/max distance from any tile of a given TileCategory |
| `EdgeBufferRule` | Minimum margin from map edges |
| `ExclusiveZoneRule` | No tiles of specified categories within a radius |
| `WithinRangeRule` | Must be within N cells of a tile of a given category |

## Road Routing Algorithm

1. Find the BaseStation cell and its exit directions (Forward/Left/Right combos)
2. Find all PowerCore cells, sort by angle from BaseStation
3. Assign cores to exits (nearest-first, overflow to closest exit)
4. For each exit:
   - If `branchForkChance` fires: find a midpoint, route exit->fork, then fork->each core
   - Else: chain through cores sequentially
5. `PaintPath()` uses waypoints with windiness (random perpendicular offsets)
6. `WalkSegment()` does randomized Manhattan walking between waypoints, respecting `roadAvoidCategories`
7. `ResolveRoadShapes()` post-processes all road cells: checks 4 neighbors, picks correct shape + rotation from RoadTileSet

## Prop Scattering

`PropScatterer` (on tile prefabs) + `PropEntry` (serialized list):
- After tile instantiation, `LevelGenerator` calls `scatter.Scatter(seed)` with a deterministic per-cell seed
- Places props with random position, rotation, scale within tile footprint
- Respects `minSpacing`, `edgePadding`, placement attempt limits

## Terrain Painting

`TerrainPainter` (on tile prefabs): paints the Unity Terrain's splatmap under each tile.
- Uses `TerrainPaintProfile` SO: texture index, strength, noise influence, fade, corner rounding
- Handles rotated tiles via local-space coordinate transforms
- Runs in `Start()` -- each tile paints itself after spawning

## Map Layout (Always)

```
         +-----------------------------+
         |                             |
         |          TERRAIN            |
         |     (trees, clearings)      |
         |                             |
         |        POWER CORES          |
         |     (scattered, rule-       |
         |      constrained)           |
         |                             |
         |      MONSTER BASE           |  <-- Always grid center
         |      (center tile)          |
         |                             |
         |        ROADS                |
         |  (connecting BS to cores)   |
         |                             |
         |      BASE STATION           |  <-- Always center-bottom
         |      (player spawn)         |
         +-----------------------------+
              ^ SPAWN is here (south)
```

## Production Seed TODO

Currently `useFixedSeed = false` means random seed each game. For production:
- Option A: Curate a list of validated seeds that produce good maps, pick randomly from that list
- Option B: Add post-generation validation (check path lengths, core accessibility, monster distance) and reject bad seeds automatically
- The retry system already exists (`maxRetries`) -- just needs better validation criteria
