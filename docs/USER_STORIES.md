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
| M.TELE-T7 | 2026-10-16 | chore | open | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | user request 2026-10-16: approved repo sweep |
| M.TELE-T8 | 2026-10-16 | chore | open | Run the full unit test suite and fix every failure (each under its own fix ticket) until it passes; put the passing run in --ref. No test project: close this wont-do saying so | user request 2026-10-16: approved repo sweep |
| M.TELE-T9 | 2026-10-16 | chore | open | Clean up code and docs: remove dead code, stale comments and outdated or duplicated docs, and simplify where behaviour doesn't change (/simplify); tests stay green | user request 2026-10-16: approved repo sweep |
| M.TELE-T10 | 2026-10-16 | docs | open | Bring README.md up to date with the code as it is now (what it is, setup, usage, commands, current state), following codex-standard/README-STYLE.md; README.htm regenerated if the repo has one | user request 2026-10-16: approved repo sweep |
| M.TELE-T11 | 2026-10-16 | docs | closed | Update USER_STORIES.md | — |
| M.TELE-T12 | 2026-10-19 | chore | wont-do | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | — |
| M.TELE-T13 | 2026-10-20 | docs | open | Publish MindAttic.Telemetry docs to GitHub Pages via the shared Toolkit DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/MindAttic.Telemetry/ returns 200. Depends on MAT-T8. | — |
| M.TELE-T14 | 2026-10-22 | docs | closed | Update USER_STORIES.md | — |
