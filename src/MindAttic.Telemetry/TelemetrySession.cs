using System.Text;

namespace MindAttic.Telemetry;

/// <summary>
/// One recorded run (a battle, a level, a whole play session — whatever the host calls <see cref="Telemetry.Begin"/>
/// for). Calls are thread-safe and never throw for runtime problems (write failures log and drop the record
/// rather than crash the game, same contract as MindAttic.Capture's <c>ICapture</c>).
/// </summary>
public interface ITelemetrySession : IAsyncDisposable
{
    /// <summary>False when disabled (kill switch, or the output root is unwritable); calls become no-ops.</summary>
    bool Enabled { get; }
    /// <summary>Unique id for this run; also part of the file name.</summary>
    string SessionId { get; }
    /// <summary>The game/app id this session was started for.</summary>
    string GameId { get; }
    /// <summary>Full path to the session's NDJSON file (the <c>.tmp</c> path while open; see <see cref="FinishAsync"/>).</summary>
    string SessionFile { get; }
    /// <summary>How many records have been written so far (including session-start).</summary>
    long RecordCount { get; }

    /// <summary>
    /// Records one full rules-engine turn: the state before, the command submitted, whether it was accepted,
    /// every event it produced, and the state after. This is the generic shape every turn/event-sourced game
    /// rules engine already produces (GridGameGaiden's <c>BattleEngine.Apply</c> is the reference case) — pass
    /// your own serializable DTOs; the core never requires one universal event shape.
    /// </summary>
    void RecordTurn(object? before, object? command, bool accepted, IEnumerable<object>? events, object? after);

    /// <summary>
    /// Records one lightweight named moment that isn't a full turn — a new game, a shop purchase, a level-up,
    /// an expedition starting or ending. This is what fills the gap a turn-only recorder has: everything that
    /// happens outside the thing being turn-recorded.
    /// </summary>
    void RecordEvent(string name, object? data = null);

    /// <summary>Writes the closing record (the game's own outcome payload) and finalizes the file. Safe to call
    /// more than once; only the first call does anything.</summary>
    Task FinishAsync(object? outcome = null);
}

/// <summary>File-backed <see cref="ITelemetrySession"/>: one append-only NDJSON file, one line per record,
/// flushed after every write. Crash-safe by construction — every line already on disk is independently valid
/// JSON, and the file stays in a <c>.ndjson.tmp</c> staging name until <see cref="FinishAsync"/> renames it, so
/// an orphaned file is trivially identifiable on the next run (see <see cref="TelemetryMaintenance"/>).</summary>
public sealed class TelemetrySession : ITelemetrySession
{
    private readonly ITelemetryJson _json;
    private readonly ITelemetryLog _log;
    private readonly object _gate = new();
    private StreamWriter? _writer;
    private long _seq;
    private bool _finished;
    private bool _writeFailed;

    internal TelemetrySession(string tempPath, string sessionId, string gameId, ITelemetryJson json, ITelemetryLog log,
        string? appVersion, string? contentVersion, long? seed, TelemetryData? tags)
    {
        SessionFile = tempPath;
        SessionId = sessionId;
        GameId = gameId;
        _json = json;
        _log = log;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            _writer = new StreamWriter(new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
            WriteLine(TelemetryRecordKind.SessionStart, null, new SessionStartPayload(gameId, sessionId, appVersion, contentVersion, seed, tags));
        }
        catch (IOException ex)
        {
            _log.Error($"Could not start telemetry session at \"{tempPath}\".", ex);
            _writeFailed = true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _log.Error($"Could not start telemetry session at \"{tempPath}\".", ex);
            _writeFailed = true;
        }
    }

    /// <inheritdoc/>
    public bool Enabled => !_finished && !_writeFailed;
    /// <inheritdoc/>
    public string SessionId { get; }
    /// <inheritdoc/>
    public string GameId { get; }
    /// <inheritdoc/>
    public string SessionFile { get; private set; }
    /// <inheritdoc/>
    public long RecordCount => Interlocked.Read(ref _seq);

    /// <inheritdoc/>
    public void RecordTurn(object? before, object? command, bool accepted, IEnumerable<object>? events, object? after)
    {
        if (!Enabled) return;
        WriteLine(TelemetryRecordKind.Turn, null, new { before, command, accepted, events = events?.ToList(), after });
    }

    /// <inheritdoc/>
    public void RecordEvent(string name, object? data = null)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (!Enabled) return;
        WriteLine(TelemetryRecordKind.Event, name, data);
    }

    /// <inheritdoc/>
    public Task FinishAsync(object? outcome = null)
    {
        lock (_gate)
        {
            if (_finished) return Task.CompletedTask;
            _finished = true;
            if (_writer is null) return Task.CompletedTask;
            try
            {
                WriteLineLocked(TelemetryRecordKind.SessionEnd, null, outcome);
                _writer.Dispose();
                _writer = null;
                FinalizeFile();
            }
            catch (IOException ex)
            {
                _log.Error($"Telemetry session \"{SessionId}\" did not finish cleanly.", ex);
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => new(FinishAsync());

    /// <summary>Renames the <c>.tmp</c> staging file to its final name — the signal that this session ended cleanly.</summary>
    private void FinalizeFile()
    {
        if (!SessionFile.EndsWith(".tmp", StringComparison.Ordinal)) return;
        var final = SessionFile.Substring(0, SessionFile.Length - 4);
        try
        {
            if (File.Exists(final)) File.Delete(final);
            File.Move(SessionFile, final);
            SessionFile = final;
        }
        catch (IOException ex)
        {
            _log.Warn($"Could not finalize telemetry session file \"{SessionFile}\".", ex);
        }
    }

    private void WriteLine(TelemetryRecordKind kind, string? name, object? payload)
    {
        lock (_gate)
        {
            if (_finished || _writer is null) return;
            WriteLineLocked(kind, name, payload);
        }
    }

    private void WriteLineLocked(TelemetryRecordKind kind, string? name, object? payload)
    {
        try
        {
            var line = _json.Serialize(new
            {
                seq = _seq,
                kind = kind.ToString(),
                atUtc = DateTimeOffset.UtcNow,
                name,
                payload,
            });
            _writer!.WriteLine(line);
            _writer.Flush();
            _seq++;
        }
        catch (IOException ex)
        {
            _writeFailed = true;
            _log.Error($"Telemetry session \"{SessionId}\" stopped writing — the rest of this run won't be recorded.", ex);
        }
    }
}

/// <summary>A session that does nothing; used when telemetry is disabled or not initialized.</summary>
public sealed class NullTelemetrySession : ITelemetrySession
{
    /// <summary>Shared instance.</summary>
    public static readonly NullTelemetrySession Instance = new();

    private NullTelemetrySession() { }

    /// <inheritdoc/>
    public bool Enabled => false;
    /// <inheritdoc/>
    public string SessionId => "";
    /// <inheritdoc/>
    public string GameId => "";
    /// <inheritdoc/>
    public string SessionFile => "";
    /// <inheritdoc/>
    public long RecordCount => 0;
    /// <inheritdoc/>
    public void RecordTurn(object? before, object? command, bool accepted, IEnumerable<object>? events, object? after) { }
    /// <inheritdoc/>
    public void RecordEvent(string name, object? data = null) { }
    /// <inheritdoc/>
    public Task FinishAsync(object? outcome = null) => Task.CompletedTask;
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;
}
