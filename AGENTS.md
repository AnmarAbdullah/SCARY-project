# SCARY-project — Agent Guide

This project's full context lives in **[CLAUDE.md](CLAUDE.md)** — read it first. This file is kept intentionally short so the two never drift out of sync.

## TL;DR for agents

- **Story-driven 4-player co-op horror game**, Unity HDRP + Mirror + Dissonance, shipping to Steam.
- **Linear 7-level campaign** (Day/Night 1–7). Each level is a **hand-built scene** with its own objective/puzzle.
- A rogue robot **"ghost"** (built by a scientist as a therapist AI, then went rogue) hunts players. Her AI behavior is defined in **[AI-System.md](AI-System.md)** and is **unchanged** from its original plan.
- A **Speaker Lady** voice guides objectives; **audio recording** pickups play one-time story logs.

> **PROCEDURAL GENERATION IS CANCELLED.** There is no random map generation, no seeds, no "collect 3 power cores and extract." Ignore/avoid the deprecated `Assets/Scripts/Level-Gen/` system and any doc text describing it. Levels are authored by hand and loaded by index.

## Where to look

| Topic | Doc |
|---|---|
| Project overview, structure, what's done/left | [CLAUDE.md](CLAUDE.md) |
| Per-level objective designs (Lvl 1–2 done, 3–7 pending) | [CurrentLevels.md](CurrentLevels.md) |
| Ghost AI (behavior unchanged) | [AI-System.md](AI-System.md) |
| Networking & Mirror patterns | [NETWORKING.md](NETWORKING.md) |
| Steam Cloud (settings, save slots, stats) | [STEAM_CLOUD.md](STEAM_CLOUD.md) |
| Roadmap / remaining work | [ROADMAP.md](ROADMAP.md) |

## Working rules

1. Keep it simple — 2-person team, ship fast.
2. Server-authoritative gameplay; inherit `Mirror.NetworkBehaviour`, use `[Command]`/`[ClientRpc]`/`[SyncVar]`.
3. Use existing namespaces (`ScaryGame.*`, `TimeFracture.*`).
4. **Never extend the deprecated `Level-Gen` procedural system.** Build levels as hand-authored scenes.
