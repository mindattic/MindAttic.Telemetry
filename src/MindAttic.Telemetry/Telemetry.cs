namespace MindAttic.Telemetry;

/// <summary>
/// Entry point for game/app code. <see cref="Begin"/> starts a session and makes it <see cref="Current"/>;
/// most call sites should just use <see cref="Current"/> after that (mirrors MindAttic.Capture's
/// <c>Capture.Current</c> pattern) rather than threading the session instance through everywhere.
/// </summary>
public static class Telemetry
{
    private static ITelemetrySession _current = NullTelemetrySession.Instance;

    /// <summary>The active session (a no-op instance until <see cref="Begin"/> is called, or after it finishes).</summary>
    public static ITelemetrySession Current
    {
        get => Volatile.Read(ref _current);
        private set => Volatile.Write(ref _current, value ?? NullTelemetrySession.Instance);
    }

    /// <summary>
    /// Starts a new session and sets it as <see cref="Current"/>. Also runs a quick, synchronous orphan sweep
    /// of the same output folder first (see <see cref="TelemetryMaintenance.RecoverOrphans"/>) — cheap, since
    /// it only ever finds something after a previous run crashed.
    /// </summary>
    /// <param name="options">Where/how to write.</param>
    /// <param name="gameId">Short game/app identifier (e.g. "GridGameGaiden") — the per-game subfolder name.</param>
    /// <param name="seed">RNG seed for this run, if there is one (needed to replay a deterministic sim).</param>
    /// <param name="contentVersion">Content/ruleset version, if the caller tracks one separately from <see cref="TelemetryOptions.AppVersion"/>.</param>
    /// <param name="tags">Free-form session-level key/values (stage id, player id, source, ...).</param>
    public static ITelemetrySession Begin(TelemetryOptions options, string gameId, long? seed = null, string? contentVersion = null, TelemetryData? tags = null)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(gameId)) throw new ArgumentException("A game id is required.", nameof(gameId));
        if (!options.Enabled)
        {
            Current = NullTelemetrySession.Instance;
            return Current;
        }
        var json = options.Json ?? throw new TelemetryException("TelemetryOptions.Json must be set on this platform (netstandard2.1).");
        var root = options.ResolveRoot();
        var appDir = Path.Combine(root, options.AppName);
        try { TelemetryMaintenance.RecoverOrphans(appDir, json, options.Log); }
        catch (IOException ex) { options.Log.Warn("Orphan telemetry sweep failed.", ex); }

        var sessionId = NewSessionId();
        var dir = Path.Combine(appDir, $"{DateTime.UtcNow:yyyy-MM-dd}_{sessionId}");
        var tempPath = Path.Combine(dir, $"{sessionId}.ndjson.tmp");
        var session = new TelemetrySession(tempPath, sessionId, gameId, json, options.Log, options.AppVersion, contentVersion, seed, tags);
        options.Log.Info($"Telemetry session \"{sessionId}\" recording to {dir}");
        Current = session;
        return session;
    }

    private static string NewSessionId() => $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 4)}";
}
