#if NET
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace MindAttic.Telemetry;

/// <summary>
/// JSON used for every record line and the session manifest. On .NET it defaults to System.Text.Json;
/// netstandard2.1 hosts (Unity) must supply one (the Unity package uses Newtonsoft, same split as
/// MindAttic.Capture's <c>ICaptureJson</c>). Serialize camelCase; deserialize case-insensitively.
/// </summary>
public interface ITelemetryJson
{
    /// <summary>Serializes <paramref name="value"/> as single-line JSON (no indentation — one record per line).</summary>
    string Serialize<T>(T value);
    /// <summary>Parses <paramref name="json"/>.</summary>
    T? Deserialize<T>(string json);
}

#if NET
/// <summary>System.Text.Json implementation.</summary>
public sealed class SystemTextTelemetryJson : ITelemetryJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <inheritdoc/>
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <inheritdoc/>
    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
}
#endif

/// <summary>Default <see cref="ITelemetryJson"/> for the current platform (null on netstandard2.1 — the host must supply one).</summary>
public static class TelemetryJsonDefaults
{
    /// <summary>Creates the platform default, or null where none exists (netstandard2.1).</summary>
    public static ITelemetryJson? Create() =>
#if NET
        new SystemTextTelemetryJson();
#else
        null;
#endif
}
