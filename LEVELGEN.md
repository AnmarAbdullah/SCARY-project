# LEVELGEN — DEPRECATED

> ## ⚠️ Procedural Level Generation is CANCELLED
>
> As of **June 2026**, SCARY-project no longer uses procedural map generation. The game is now a **linear, hand-built, story-driven 7-level campaign**. This document described the old seed-deterministic generator (`Assets/Scripts/Level-Gen/`, namespace `ScaryGame.LevelGen`) and is retained only for historical reference.
>
> **Do not build new features on the procedural system.** It is being phased out of the codebase.

## What replaced it

Levels are now **authored by hand**, one scene/prefab set per level, loaded by index (Level 1 → 7). There are no seeds, no runtime tile placement, no road auto-tiling, and no prop scattering driving the playable map.

- **Per-level objective designs:** see [CurrentLevels.md](CurrentLevels.md)
- **Project overview & structure:** see [CLAUDE.md](CLAUDE.md)
- **Ghost AI** (unchanged behavior, but spawns at designer-placed points and uses NavMesh baked per hand-built level): see [AI-System.md](AI-System.md)

## Building a level (the new way)

1. Create a new scene (or extend a prefab set) for the level under `Assets/`.
2. Lay out the environment, props, player spawn(s), and the ghost spawn point(s) by hand.
3. **Bake the NavMesh in-editor** for that level so the ghost's `NavMeshAgent` can path.
4. Place the level's objective interactables (signal towers, levers, generator, etc.), Speaker Lady trigger volumes, audio-recording pickups, and the progression gate/door.
5. Wire the gate so it opens when the level objective is satisfied, then advances to the next level.

## Legacy system (historical, not in use)

The old generator lived in `Assets/Scripts/Level-Gen/` and built a 20×20 tile grid from a seed: MonsterBase at center, BaseStation at the bottom, power cores placed by rules, roads carved between them, terrain filling the rest. Its README still exists in that folder with a deprecation banner. None of it is part of the shipping game design anymore.
