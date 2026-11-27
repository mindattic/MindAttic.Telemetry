---
codex: 1
project: MindAttic.Telemetry
code: M.TELE
layer: stories
status: living
updated: 2026-10-12
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# MindAttic.Telemetry — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| M.TELE-T1 | 2026-10-12 | docs | closed | Start the permanent ticket log in USER_STORIES.md | — |
| M.TELE-T2 | 2026-10-14 | chore | closed | NUnit tests for TelemetryReader edge cases (TelemetryReader.cs): Read skips blank lines, parses kind case-insensitively, yields null Name when name is missing or null, keeps the seq=-1 recovered SessionEnd; Flatten carries _turnIndex/_wave/_accepted from the turn, skips turns with no events array or a non-object payload, unwraps ints as long, non-integers as double, nested objects/arrays as JsonElement - analysis scripts depend on this flattening being stable | cc04c1b |
| M.TELE-T3 | 2026-10-14 | chore | closed | NUnit tests for TelemetrySession lifecycle (TelemetrySession.cs): FinishAsync is idempotent and a second call writes nothing, RecordTurn/RecordEvent after finish are no-ops, RecordEvent with null/empty name is ignored, RecordCount tracks written lines, DisposeAsync finishes and renames the .tmp, null events serialize cleanly, concurrent RecordEvent calls from many threads produce gapless unique seq numbers - the never-throw, crash-safe contract games rely on | d9edb05 |
| M.TELE-T4 | 2026-10-14 | chore | closed | NUnit tests for Telemetry.Begin and TelemetryOptions.ResolveRoot (Telemetry.cs, TelemetryOptions.cs): null options throws ArgumentNullException, blank gameId throws ArgumentException, Json=null throws TelemetryException, Current is set to the new session and reset to NullTelemetrySession when disabled, an unavailable OutputRoot drive falls back to FallbackRoot with a warning logged, output lands under AppName/date_sessionId - wrong roots silently lose a run's data | a935812 |
| M.TELE-T5 | 2026-10-14 | chore | closed | NUnit tests for TelemetryMaintenance.RecoverOrphans and SystemTextTelemetryJson: missing appDir returns 0, nested orphan .ndjson.tmp files in several session folders are all recovered, an existing final .ndjson is overwritten, IOException on one file logs a warning and continues; JSON serializer writes camelCase, omits nulls, writes enums as strings and round-trips TelemetryData case-insensitively - the on-disk format other tools read | 4dc917f |
| M.TELE-T6 | 2026-10-16 | chore | closed | install.ps1 adds this repo's 15 .prose/commands stubs, AGENTS.md pointer for the shared global commands, so any LLM client finds them | mindattic-agent-standard/install.ps1 run 2026-10-16 |
| M.TELE-T7 | 2026-10-16 | chore | closed | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | 72c4c9e141070a7ac8daff8f1d31f204c699fef3 |
| M.TELE-T8 | 2026-10-16 | chore | wont-do | Run the full unit test suite and fix every failure (each under its own fix ticket) until it passes; put the passing run in --ref. No test project: close this wont-do saying so | portfolio reset: repo-sweep test loop; tests run only on explicit owner request |
| M.TELE-T9 | 2026-10-16 | chore | wont-do | Clean up code and docs: remove dead code, stale comments and outdated or duplicated docs, and simplify where behaviour doesn't change (/simplify); tests stay green | portfolio reset: generic cleanup, audit nice-to-have |
| M.TELE-T10 | 2026-10-16 | docs | closed | Bring README.md up to date with the code as it is now (what it is, setup, usage, commands, current state), following codex-standard/README-STYLE.md; README.htm regenerated if the repo has one | already done: README.md current (a99446b, bb9829c) |
| M.TELE-T11 | 2026-10-16 | docs | closed | Update USER_STORIES.md | — |
| M.TELE-T12 | 2026-10-19 | chore | wont-do | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | — |
| M.TELE-T13 | 2026-10-20 | docs | closed | Publish MindAttic.Telemetry docs to GitHub Pages via the shared Agency DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/MindAttic.Telemetry/ returns 200. Depends on MAT-T8. | — |
| M.TELE-T14 | 2026-10-22 | docs | closed | Update USER_STORIES.md | — |
| M.TELE-T15 | 2026-10-24 | docs | closed | Update USER_STORIES.md | — |
| M.TELE-T16 | 2026-11-02 | docs | closed | Adopt MindAttic.Agency docs-site pipeline, publish README to GitHub Pages (HOUSE-LAW-14) | — |
| M.TELE-T17 | 2026-11-02 | fix | closed | Bughunt sweep, two passes: find and fix real bugs across tracked source (user request 2026-11-02) | — |
| M.TELE-T18 | 2026-11-02 | fix | closed | Fix the open items the 2026-11-02 bughunt left for decision (Unity dictionary keys verbatim, Begin closes an open session, gameId doc, SessionEnd always written, Unity plugin DLL refresh) | — |
| M.TELE-T19 | 2026-11-10 | chore | closed | Crash recovery: commit the command-catalog sync (.agent/commands/*.md) and regenerated docs/USER_STORIES.md left uncommitted by the session crash | — |
| M.TELE-T20 | 2026-11-10 | chore | closed | Adopt the shared docs-site pipeline: turn on GitHub Pages | — |
| M.TELE-T21 | 2026-11-17 | docs | closed | Update USER_STORIES.md | — |
| M.TELE-T22 | 2026-11-18 | chore | closed | Add ryand_SNOWCRASH_2026-11-11_12_25_01_net10.0.trx; Update main-tests; Remove ryand_SNOWCRASH_2026-11-02_23_04_27_net10.0.trx | — |
| M.TELE-T23 | 2026-11-22 | chore | closed | 2026-11-22 morning | — |
| M.TELE-T24 | 2026-11-24 | fix | wont-do | Hunt the whole repo for real bugs with no time limit and fix every confirmed one | portfolio reset: open-ended bughunt with no concrete defect |
| M.TELE-T25 | 2026-11-24 | chore | closed | MindAttic.Telemetry comments and README compare against MindAttic.Media.Capture | children done |
| M.TELE-T26 | 2026-11-24 | chore | closed | MindAttic.Telemetry: comments in src/MindAttic.Telemetry (ITelemetryJson, ITelemetryLog, Telemetry, TelemetryOptions, TelemetrySession, TelemetryTypes), unity Runtime (TelemetryUnity.cs, UnitySupport.cs) and README.md (~120) say MindAttic.Media.Capture | a99446b |
| M.TELE-T27 | 2026-11-25 | fix | closed | Fails on main: the build — C:\Program Files\dotnet\sdk\11.0.100-rc.3.26522.148\Microsoft.Common.CurrentVersion.targets(5131,5): error MSB3030: Could not copy the file "R:\Temp\nuget\packages\microsoft.testing.extensions.telemetry\1.5.3\lib\net9.0\fr\Microsoft.Testing.Extensions.Telemetry.resources.dll" because it was not f... (landed on main since the last green run: tickets M.CAPT-T181, M.TELE-T26 in 5320dd7 Unity plugin MindAttic.Telemetry.xml regenerated from source so its doc comments name MindAttic.Media.Capture (M.CAPT-T181); a99446b Name MindAttic.Media.Capture in Telemetry comments and README (M.TELE-T26)) | 5320dd7 |
| M.TELE-T28 | 2026-11-26 | fix | closed | Telemetry.Begin throws UnauthorizedAccessException when the orphan sweep hits an inaccessible subfolder; only IOException is caught | 69c0ad6dfcea169c42e786c3997132e83d0ff7d3 |
| M.TELE-T29 | 2026-11-26 | fix | closed | RecordTurn throws into the game when the events enumerable throws while being enumerated | 72c4c9e141070a7ac8daff8f1d31f204c699fef3 |
| M.TELE-T30 | 2026-11-27 | feature | open | The Unity package runs the same core as the .NET build, proven in its one consumer (portfolio reset, M.EXPT-T48) | — |
| M.TELE-T31 | 2026-11-27 | feature | open | The Unity plugin DLL is rebuilt from current src | — |
| M.TELE-T32 | 2026-11-27 | chore | open | Run tools/update-unity-plugin.ps1 so unity/com.mindattic.telemetry/Runtime/Plugins/MindAttic.Telemetry.dll (and .xml) carry the M.TELE-T28 orphan-sweep and M.TELE-T29 RecordTurn fixes; the DLL was last refreshed in bb9829c, before them | — |
| M.TELE-T33 | 2026-11-27 | feature | open | GridGame records a session with the refreshed plugin | — |
| M.TELE-T34 | 2026-11-27 | chore | open | After the plugin refresh, run one GridGame.Unity editor battle and confirm a .ndjson session lands under R:\Telemetry\GridGameGaiden with SessionStart, Turn and SessionEnd lines that TelemetryReader.Flatten reads; record the path in --ref | — |
