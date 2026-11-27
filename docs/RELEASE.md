---
layer: release
status: living
---
# MindAttic.Telemetry — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
Raw, replayable turn and event telemetry for MindAttic games: one append-only NDJSON file per session, a .NET and
Unity recorder, and a reader that flattens sessions for analysis.

## 2. The milestone: MindAttic.Telemetry 1.0 {#milestone}
- **Milestone:** MS-65, "MindAttic.Telemetry 1.0: Unity at parity"
- **Published means:** the Unity package `com.mindattic.telemetry` consumed by GridGame.Unity through its `file:`
  path (a finished internal library).
- **The natural stopping point:** the core is hardened (T17, T18, T28, T29), but the Unity plugin DLL was last
  rebuilt before T28 and T29, so the one real consumer runs stale code. Refreshing the plugin and proving one
  GridGame session reads back completes the library.

## 3. Done when (acceptance checks)
1. build — `MindAttic.Telemetry.slnx` builds clean on main.
2. signoff — a GridGame.Unity session written with the refreshed plugin reads back with `TelemetryReader.Flatten`.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | M.TELE-T30 Unity plugin parity | Unity runs the current core, proven in GridGame | M.TELE-T31 Plugin refresh (990: M.TELE-T32), M.TELE-T33 Consumer verification (980: M.TELE-T34) |

One epic only: the core is done; parity in the consumer is the whole remaining gap.

## 5. Out of scope (not before this milestone)
- Re-simulation or replay tooling.
- A background agent, service or encoder.
- Dashboards and analysis tooling beyond `TelemetryReader`.
- Generic cleanups, bughunts and repo-sweep test runs.

## 6. Current verified state
- Core hardening: 86076a4, bb9829c, 69c0ad6 (T28), 72c4c9e (T29). Plugin DLL last refreshed in bb9829c.
- README current (a99446b).

## 7. Decision record
- 2026-11-27 — charter drafted from README, git log and tickets; Legion vote: **Approve**, 9/9 voters, plurality
  quorum reached, consensus 1.0, no dissent.
- Triage: 4 open → done 1 (T10), duplicate 0, wont-do 3 (T8, T9, T24), kept 0, new tasks 2 (T32, T34).
