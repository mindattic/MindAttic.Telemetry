namespace MindAttic.Telemetry;

/// <summary>Key/value data attached to an event or a session's tags; lands in the record's JSON verbatim.</summary>
public sealed class TelemetryData : Dictionary<string, object?>
{
    /// <summary>Creates an empty, case-insensitive bag.</summary>
    public TelemetryData() : base(StringComparer.OrdinalIgnoreCase) { }

    /// <summary>Copies <paramref name="source"/>.</summary>
    public TelemetryData(IDictionary<string, object?> source) : base(source, StringComparer.OrdinalIgnoreCase) { }
}

/// <summary>What kind of line a record is, in sequence within a session file.</summary>
public enum TelemetryRecordKind
{
    /// <summary>The first line: session/game/build metadata.</summary>
    SessionStart,
    /// <summary>A full state/command/event turn (`RecordTurn`) — the unit most games' rules engines produce.</summary>
    Turn,
    /// <summary>A lightweight named moment (`RecordEvent`) — anything that isn't a full turn.</summary>
    Event,
    /// <summary>The last line on a clean finish: the game's own outcome payload.</summary>
    SessionEnd,
}

/// <summary>One line of a session file, with its envelope fields and the game-supplied payload.</summary>
/// <param name="Seq">0-based position in the session (gaps never occur; a session recovered after a crash
/// just ends early).</param>
/// <param name="Kind">What this line is.</param>
/// <param name="AtUtc">Wall-clock time the record was written — the single biggest gap found in
/// GridGameGaiden's original hand-rolled recorder (tick-only timestamps): lets any record be correlated
/// against out-of-band signals, e.g. a MindAttic.Capture video of the same session.</param>
/// <param name="Name">Set for <see cref="TelemetryRecordKind.Event"/> records (the event name); null otherwise.</param>
/// <param name="Payload">The game-supplied object: the turn, the event data, the session metadata, or the
/// outcome — whatever type the caller passed to <c>RecordTurn</c>/<c>RecordEvent</c>/<c>Begin</c>/<c>FinishAsync</c>.</param>
public sealed record TelemetryEnvelope(long Seq, TelemetryRecordKind Kind, DateTimeOffset AtUtc, string? Name, object? Payload);

/// <summary>The session-start payload (first line of every session file).</summary>
/// <param name="GameId">Short game/app identifier (e.g. "GridGameGaiden").</param>
/// <param name="SessionId">Unique per run; also part of the file name.</param>
/// <param name="AppVersion">Build/app version, if the host supplied one.</param>
/// <param name="ContentVersion">Content/ruleset version, if the caller supplied one (<see cref="Telemetry.Begin"/>).</param>
/// <param name="Seed">RNG seed for the run, if the caller supplied one — needed to replay a deterministic sim.</param>
/// <param name="Tags">Free-form session-level key/values (e.g. stage id, player id, source: "player"/"bot:greedy").</param>
public sealed record SessionStartPayload(string GameId, string SessionId, string? AppVersion, string? ContentVersion, long? Seed, TelemetryData? Tags);

/// <summary>Thrown by setup/IO code; runtime record calls never throw (they log and drop instead).</summary>
public sealed class TelemetryException(string message, Exception? inner = null) : Exception(message, inner);
