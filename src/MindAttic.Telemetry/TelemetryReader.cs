#if NET
using System.Text.Json;

namespace MindAttic.Telemetry;

/// <summary>One parsed line of a session file. <see cref="Payload"/> stays a <see cref="JsonElement"/> — the
/// reader has no knowledge of any particular game's turn/event shapes, by design (see <see cref="TelemetryReader"/>).</summary>
public readonly record struct TelemetryRecordView(long Seq, TelemetryRecordKind Kind, DateTimeOffset AtUtc, string? Name, JsonElement Payload);

/// <summary>
/// Reads session files back for offline analysis — the "replay programmatically outside the game" half of this
/// library. Desktop/analysis-only (the <c>net10.0</c> target): nothing in a running Unity game needs to parse
/// its own telemetry, so this never ships in the netstandard2.1 build Unity consumes.
/// </summary>
public static class TelemetryReader
{
    /// <summary>Streams every record in a session file, in order.</summary>
    public static IEnumerable<TelemetryRecordView> Read(string path)
    {
        using var reader = new StreamReader(path);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            var seq = root.GetProperty("seq").GetInt64();
            var kind = Enum.Parse<TelemetryRecordKind>(root.GetProperty("kind").GetString()!, ignoreCase: true);
            var atUtc = root.GetProperty("atUtc").GetDateTimeOffset();
            var name = root.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null ? n.GetString() : null;
            var payload = root.TryGetProperty("payload", out var p) ? p.Clone() : default;
            yield return new TelemetryRecordView(seq, kind, atUtc, name, payload);
        }
    }

    /// <summary>
    /// Flattens a session into one row per standalone event: every <see cref="TelemetryRecordKind.Event"/>
    /// record as-is, plus one row per entry in each <see cref="TelemetryRecordKind.Turn"/> record's
    /// <c>events</c> array (merged with that turn's own <c>index</c>/<c>wave</c>/<c>accepted</c> fields, when
    /// present, so a turn's events don't lose their context). This is the "don't make everyone re-derive their
    /// own flattening logic from nested JSON" piece — drop the result straight into
    /// <c>pandas.read_json(lines=True)</c> to ask questions like "what's the right heal-to-damage ratio".
    /// </summary>
    public static IEnumerable<IDictionary<string, object?>> Flatten(string path)
    {
        foreach (var rec in Read(path))
        {
            switch (rec.Kind)
            {
                case TelemetryRecordKind.Event:
                {
                    var row = ToDictionary(rec.Payload);
                    row["_seq"] = rec.Seq;
                    row["_atUtc"] = rec.AtUtc;
                    row["_kind"] = "event";
                    row["_name"] = rec.Name;
                    yield return row;
                    break;
                }
                case TelemetryRecordKind.Turn:
                {
                    if (rec.Payload.ValueKind != JsonValueKind.Object) break;
                    var index = rec.Payload.TryGetProperty("index", out var i) ? (object?)i.GetInt64() : null;
                    var wave = rec.Payload.TryGetProperty("wave", out var w) ? (object?)w.GetInt64() : null;
                    var accepted = rec.Payload.TryGetProperty("accepted", out var a) && a.GetBoolean();
                    if (!rec.Payload.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array) break;
                    foreach (var evt in events.EnumerateArray())
                    {
                        var row = ToDictionary(evt);
                        row["_seq"] = rec.Seq;
                        row["_atUtc"] = rec.AtUtc;
                        row["_kind"] = "turn-event";
                        row["_turnIndex"] = index;
                        row["_wave"] = wave;
                        row["_accepted"] = accepted;
                        yield return row;
                    }
                    break;
                }
            }
        }
    }

    private static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        if (element.ValueKind != JsonValueKind.Object) return dict;
        foreach (var prop in element.EnumerateObject()) dict[prop.Name] = Unwrap(prop.Value);
        return dict;
    }

    private static object? Unwrap(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => e.Clone(), // object/array: left as JsonElement for callers that want the nested structure
    };
}
#endif
