---
layer: bible
status: living
---
# MindAttic.Telemetry — Project Bible

> Single source of truth for what MindAttic.Telemetry IS, is NOT, and the rules that keep it coherent.
> README says how to build and run it; this says how to think about it. The finish line lives in
> `docs/RELEASE.md`.

## 1. The one sentence {#M.TELE-§1}
MindAttic.Telemetry records raw, replayable turn and event telemetry for MindAttic games as one
append-only NDJSON file per session, and reads it back as flat rows for analysis.

## 2. The product promise {#M.TELE-§2}
- **Crash-safe by construction.** Every line is flushed and independently valid JSON; a crash loses
  nothing already written.
- **The rename is the clean-finish signal.** A session is `.ndjson.tmp` until `FinishAsync` writes
  `SessionEnd` and renames it to `.ndjson`.
- **Orphans recover themselves.** The next `Telemetry.Begin` appends a `{"recovered":true}`
  `SessionEnd` to any abandoned `.tmp` file nobody holds open, and renames it.
- **Your shapes, verbatim.** `RecordTurn` takes whatever `before`/`command`/`events`/`after` DTOs the
  game already has; `RecordEvent` takes a name and any data.
- **Recording never breaks the game.** Unserializable payloads, access-denied files and throwing event
  sequences are logged and dropped, never thrown into the caller.
- **Same file from .NET and Unity.** Member names are camelCase and dictionary keys verbatim in both.

## 3. What it is NOT {#M.TELE-§3}
- **NOT a replay engine.** It records seed and commands; re-simulation is the game's own job.
- **NOT a service.** No agent, no background process, no encoding; just files and a file-lock probe.
- **NOT a universal event schema.** The core never assumes what a turn or event looks like.

## 4. Architecture canon {#M.TELE-§4}
```
 game ── Telemetry.Begin ──> TelemetrySession ── RecordTurn / RecordEvent ──> {session}.ndjson.tmp
                                              └─ FinishAsync ──> SessionEnd + rename ──> .ndjson
 next Begin ──> TelemetryMaintenance.RecoverOrphans
 analysis  ──> TelemetryReader.Read / Flatten ──> rows (pandas read_json lines=True)
```
Output: `<root>\<AppName>\<date>_<sessionId>\<sessionId>.ndjson`, root `R:\Telemetry` with a
`Documents\MindAttic\Telemetry` fallback when the drive is missing.

### 4.1 Projects
- `src/MindAttic.Telemetry` — core, `netstandard2.1` (for Unity) and `net10.0`; no third-party
  dependencies. Version 1.0.0.
- `unity/com.mindattic.telemetry` — Unity 6 package (`TelemetryUnity`), carrying the core as a plugin
  DLL refreshed by `tools/update-unity-plugin.ps1`.
- `tests/MindAttic.Telemetry.Tests` — NUnit.

### 4.2 Domain model (the nouns)
Session, record (`seq`, `kind`, `atUtc`, `name`, `payload`), kinds `SessionStart`, `Turn`, `Event`,
`SessionEnd`; `TelemetryOptions`, `TelemetryData`.

### 4.3 Key services (the verbs)
`Telemetry.Begin`, `TelemetrySession.RecordTurn/RecordEvent/FinishAsync`,
`TelemetryMaintenance.RecoverOrphans`, `TelemetryReader.Read/Flatten`, `ITelemetryJson`,
`ITelemetryLog`.

## 5. The Laws {#M.TELE-§5}
Inherits `MindAttic.HouseRules.md` by reference.
1. **Append-only.** {#M.TELE-LAW-1} A written line is never rewritten.
2. **Never throw into the game.** {#M.TELE-LAW-2} Record and lifecycle calls swallow and log storage
   and serialization failures.
3. **Core stays dependency-free.** {#M.TELE-LAW-3} Unity consumes the `netstandard2.1` build.
4. **Plugin follows source.** {#M.TELE-LAW-4} A core change is not finished until the Unity plugin
   DLL is refreshed from it.

## 6. Verified state {#M.TELE-§6}
- GridGame.Unity consumes the Unity package through a `file:` path and the plugin DLL.
- Hardening: 86076a4, bb9829c, 69c0ad6 (M.TELE-T28), 72c4c9e (M.TELE-T29). The plugin DLL was last
  refreshed in bb9829c, before T28 and T29.

## 7. Active frontier {#M.TELE-§7}
See `docs/RELEASE.md`: Unity plugin parity and one verified GridGame session.

## 8. Quality bar {#M.TELE-§8}
A change is done when the solution builds, NUnit covers it, the Unity plugin carries it and the README
describes any change to the file shape.

## 9. Glossary {#M.TELE-§9}
- **Session** — one run, one NDJSON file.
- **Orphan** — a `.ndjson.tmp` left by a run that never finished.
- **Flatten** — one row per event, standalone or inside a turn.
