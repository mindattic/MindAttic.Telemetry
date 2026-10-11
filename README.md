# MindAttic.Telemetry

Raw, replayable turn/event telemetry for MindAttic games: every move, every state change, every "this
happened" moment, written as one append-only session per run — so later you can ask real questions of
real play (what's the right heal-to-damage ratio, how does monster damage trade against timeline
pushback, where do players actually lose) instead of guessing. Extracted and genericized from
GridGameGaiden's own battle recorder, which was already tuning real balance decisions from this data
before this repo existed.

```text
 your game                           MindAttic.Telemetry                        R:\Telemetry\
 ─────────                           ────────────────────                       ─────────────
 Telemetry.Begin(...)         ──>   one append-only NDJSON file per session ──>  {Game}\{date}_{session}\
 session.RecordTurn(...)      ──>   crash-safe by construction (every line        {session}.ndjson
 session.RecordEvent(...)     ──>   already on disk is independently valid)
 session.FinishAsync(outcome) ──>   host adapter (any .NET app, Unity)
 TelemetryReader.Read/Flatten ──>   offline analysis, ML, pandas
```

| Package | Target | What it does |
|---|---|---|
| `MindAttic.Telemetry` | netstandard2.1; net10.0 | Core: sessions, NDJSON writer, orphan recovery, reader/flattener. No third-party dependencies (Unity consumes the netstandard2.1 build). |
| `com.mindattic.telemetry` (Unity) | Unity 6, any platform | `TelemetryUnity.Begin/RecordTurn/RecordEvent/Finish`; editor and development builds only by default. |

## How it works

Everything is one **session**: a single append-only NDJSON (newline-delimited JSON) file, one record per
line. `Begin` writes a `SessionStart` line (game id, build/content version, seed, tags). Every
`RecordTurn` (a full before/command/accepted/events/after rules-engine turn — the shape any
event-sourced rules engine already produces; GridGameGaiden's `BattleEngine.Apply` is the reference
case) and every `RecordEvent` (a lightweight named moment — a new game, a shop purchase, a level-up,
anything that isn't a full turn) appends one more line and flushes immediately. `FinishAsync` writes a
`SessionEnd` line with the game's own outcome payload and renames the file from `.ndjson.tmp` to
`.ndjson` — the rename *is* the "this session ended cleanly" signal.

Because every flushed line is independently valid JSON, a crash mid-session loses nothing already
written: the file just stays named `.ndjson.tmp`. The next call to `Telemetry.Begin` (for anything,
not necessarily the same kind of session) sweeps the output folder, appends a synthetic
`{"recovered":true}` `SessionEnd` to any abandoned `.tmp` file nobody still has open, and renames it —
no process-alive tracking, no background service, just a file-lock probe.

The core never assumes a universal "event" shape. A tactics RPG's turn looks nothing like a
platformer's; pass whatever serializable DTOs your game already has as `before`/`command`/`events`/
`after`. `TelemetryReader.Flatten` is generic too — it doesn't know your event types, it just surfaces
every event (standalone or inside a turn) as one flat row, ready for `pandas.read_json(lines=True)`.

## Quick start

**Unity game:** add to `Packages/manifest.json`
`"com.mindattic.telemetry": "file:../../MindAttic.Telemetry/unity/com.mindattic.telemetry"`.

```csharp
using MindAttic.Telemetry;
using MindAttic.Telemetry.Unity;

// when a battle/level/run starts:
TelemetryUnity.Begin("GridGameGaiden", seed: setup.Seed, contentVersion: Application.version,
    tags: new TelemetryData { ["stage"] = stageId, ["source"] = "player" });

// after every command your rules engine applies:
TelemetryUnity.RecordTurn(before: beforeSnapshot, command: cmd, accepted: accepted, events: events, after: afterSnapshot);

// anywhere else something worth knowing happens:
TelemetryUnity.RecordEvent("shop.purchase", new TelemetryData { ["item"] = "x_potion", ["cost"] = 50 });

// when the run ends:
TelemetryUnity.Finish(new { victory = true, turns = 324 });
```

**Any .NET app (in-process):**

```csharp
var session = Telemetry.Begin(new TelemetryOptions { AppName = "MyApp", AppVersion = "1.4.0" }, "MyApp");
session.RecordEvent("level.start", new TelemetryData { ["level"] = 3 });
await session.FinishAsync(new { completed = true });
```

**Reading a session back (offline analysis, ML, a devlog chart — anything on the `net10.0` target):**

```csharp
foreach (var rec in TelemetryReader.Read(path)) { /* rec.Kind, rec.AtUtc, rec.Payload (JsonElement) */ }

// one flat row per event, turn-nested or standalone — straight into pandas:
var rows = TelemetryReader.Flatten(path).ToList();
```

## Session file shape

Every line is one JSON object: `{"seq": N, "kind": "...", "atUtc": "...", "name": "...", "payload": {...}}`.

- **`kind`**: `SessionStart` (first line) · `Turn` · `Event` · `SessionEnd` (last line, only on a clean
  finish or after recovery).
- **`SessionStart` payload**: `gameId`, `sessionId`, `appVersion`, `contentVersion`, `seed`, `tags`.
- **`Turn` payload**: `before`, `command`, `accepted`, `events` (array), `after` — whatever shapes your
  game passed in, verbatim.
- **`Event` payload**: whatever `data` your game passed in, verbatim; `name` carries the event name.
- **`SessionEnd` payload**: whatever `outcome` your game passed to `FinishAsync`, or `{"recovered":true}`
  if a previous run crashed and this session was recovered on the next launch.

## Output

```
R:\Telemetry\GridGameGaiden\2026-10-11_20261011-201734-a3f9\
  20261011-201734-a3f9.ndjson
```

Falls back to `Documents\MindAttic\Telemetry\...` if the configured output drive doesn't exist.

## Limits worth knowing

- **Not a replacement for a rules engine's own determinism.** Recording a seed and every command lets
  you *reconstruct* a run by re-simulating; this library doesn't re-simulate anything itself.
- **No encoding, no agent, no background service.** Unlike MindAttic.Capture (which hands video export
  off to a detached process), telemetry writes are synchronous file appends — there's nothing to detach,
  and nothing to install.
- **The reader/flattener are desktop-only** (the `net10.0` target). Nothing inside a running Unity game
  needs to parse its own telemetry back — that's an offline/analysis concern by design.
- **One file per session, not one file for a whole play session spanning many battles/levels.** A
  campaign-spanning session id that ties several per-battle sessions together is a reasonable fast
  follow, not in v1.
- **Flattening only knows about events, not state snapshots.** `before`/`after` snapshots stay nested
  JSON in `Flatten`'s output; only `events` arrays get exploded into rows. Pull snapshot fields yourself
  from `TelemetryReader.Read` when you need them.

## Development

```
dotnet build MindAttic.Telemetry.slnx
dotnet test  MindAttic.Telemetry.slnx
```

After changing `src/MindAttic.Telemetry`, run `tools/update-unity-plugin.ps1` to refresh the DLL vendored
into the Unity package.
