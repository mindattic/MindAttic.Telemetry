namespace MindAttic.Telemetry;

/// <summary>Everything that configures a telemetry session. Defaults suit MindAttic development machines.</summary>
public sealed class TelemetryOptions
{
    /// <summary>Global kill switch. When false, <see cref="Telemetry.Begin"/> returns a no-op session.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Where sessions go. A missing drive falls back to <see cref="FallbackRoot"/> with a warning.</summary>
    public string OutputRoot { get; set; } = @"R:\Telemetry";

    /// <summary>Used when <see cref="OutputRoot"/>'s drive is unavailable.</summary>
    public string FallbackRoot { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MindAttic", "Telemetry");

    /// <summary>App/game name: the per-app folder under the root (mirrors MindAttic.Capture's output layout).</summary>
    public string AppName { get; set; } = "App";

    /// <summary>Build/content version written into every session's start record. Never leave this null in
    /// production — without it, a balance-rule change between patches is invisible in the recorded data.</summary>
    public string? AppVersion { get; set; }

    /// <summary>JSON implementation (required on netstandard2.1 hosts — see <see cref="ITelemetryJson"/>).</summary>
    public ITelemetryJson? Json { get; set; } = TelemetryJsonDefaults.Create();

    /// <summary>Logging sink.</summary>
    public ITelemetryLog Log { get; set; } = new TraceTelemetryLog();

    /// <summary>Resolves <see cref="OutputRoot"/>, falling back to <see cref="FallbackRoot"/> if the root's drive
    /// doesn't exist (e.g. a dev-only network/RAM drive that isn't mounted on every machine).</summary>
    public string ResolveRoot()
    {
        try
        {
            var root = Path.GetPathRoot(OutputRoot);
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root)) return OutputRoot;
        }
        catch (ArgumentException)
        {
        }
        Log.Warn($"Telemetry output root \"{OutputRoot}\" is unavailable; using \"{FallbackRoot}\" instead.");
        return FallbackRoot;
    }
}
