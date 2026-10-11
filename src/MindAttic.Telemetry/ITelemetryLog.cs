using System.Diagnostics;

namespace MindAttic.Telemetry;

/// <summary>Log severity.</summary>
public enum TelemetryLogLevel
{
    /// <summary>Verbose detail (file opens/closes, orphan scans).</summary>
    Debug,
    /// <summary>Normal activity.</summary>
    Info,
    /// <summary>Something was skipped or degraded.</summary>
    Warning,
    /// <summary>A write or read failed.</summary>
    Error,
}

/// <summary>Logging sink; hosts bridge it to ILogger, Unity's Debug.Log, etc. (mirrors MindAttic.Capture's <c>ICaptureLog</c>).</summary>
public interface ITelemetryLog
{
    /// <summary>Writes one entry.</summary>
    void Log(TelemetryLogLevel level, string message, Exception? exception = null);
}

/// <summary>Writes to <see cref="Trace"/>; the default when nothing else is configured.</summary>
public sealed class TraceTelemetryLog : ITelemetryLog
{
    /// <summary>Entries below this level are dropped.</summary>
    public TelemetryLogLevel MinimumLevel { get; set; } = TelemetryLogLevel.Info;

    /// <inheritdoc/>
    public void Log(TelemetryLogLevel level, string message, Exception? exception = null)
    {
        if (level < MinimumLevel) return;
        Trace.WriteLine($"[MindAttic.Telemetry] {level}: {message}{(exception is null ? "" : Environment.NewLine + exception)}");
    }
}

internal static class LogExtensions
{
    public static void Debug(this ITelemetryLog log, string m) => log.Log(TelemetryLogLevel.Debug, m);
    public static void Info(this ITelemetryLog log, string m) => log.Log(TelemetryLogLevel.Info, m);
    public static void Warn(this ITelemetryLog log, string m, Exception? ex = null) => log.Log(TelemetryLogLevel.Warning, m, ex);
    public static void Error(this ITelemetryLog log, string m, Exception? ex = null) => log.Log(TelemetryLogLevel.Error, m, ex);
}
