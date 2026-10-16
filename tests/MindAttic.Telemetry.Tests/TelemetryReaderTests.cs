using System.Text.Json;
using MindAttic.Telemetry;
using NUnit.Framework;

namespace MindAttic.Telemetry.Tests;

public class TelemetryReaderTests
{
    private string _root = "";

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("mindattic-telemetry-reader-tests-").FullName;

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private string WriteSession(string content)
    {
        var path = Path.Combine(_root, $"{Guid.NewGuid()}.ndjson");
        File.WriteAllText(path, content);
        return path;
    }

    private static string Line(long seq, string kind, string? name = null, string payloadJson = "{}")
    {
        var nameJson = name is null ? "null" : $"\"{name}\"";
        return $"{{\"seq\":{seq},\"kind\":\"{kind}\",\"atUtc\":\"2026-01-01T00:00:00Z\",\"name\":{nameJson},\"payload\":{payloadJson}}}";
    }

    [Test]
    public void Read_skips_blank_lines_between_records()
    {
        var content = string.Join("\n",
            Line(0, "SessionStart"),
            "",
            "   ",
            Line(1, "Event", name: "ping"),
            "");
        var records = TelemetryReader.Read(WriteSession(content)).ToList();

        Assert.That(records, Has.Count.EqualTo(2));
        Assert.That(records[0].Seq, Is.EqualTo(0));
        Assert.That(records[1].Seq, Is.EqualTo(1));
    }

    [Test]
    public void Read_parses_kind_case_insensitively()
    {
        var content = string.Join("\n",
            Line(0, "sessionstart"),
            Line(1, "TURN"),
            Line(2, "event", name: "x"),
            Line(3, "SessionEnd"));
        var records = TelemetryReader.Read(WriteSession(content)).ToList();

        Assert.That(records.Select(r => r.Kind), Is.EqualTo(new[]
        {
            TelemetryRecordKind.SessionStart,
            TelemetryRecordKind.Turn,
            TelemetryRecordKind.Event,
            TelemetryRecordKind.SessionEnd,
        }));
    }

    [Test]
    public void Read_yields_null_name_when_name_is_missing_or_null()
    {
        var missingName = "{\"seq\":0,\"kind\":\"Event\",\"atUtc\":\"2026-01-01T00:00:00Z\",\"payload\":{}}";
        var nullName = Line(1, "Event", name: null);
        var records = TelemetryReader.Read(WriteSession(string.Join("\n", missingName, nullName))).ToList();

        Assert.That(records, Has.Count.EqualTo(2));
        Assert.That(records[0].Name, Is.Null);
        Assert.That(records[1].Name, Is.Null);
    }

    [Test]
    public void Read_keeps_the_recovered_sessionEnd_with_seq_negative_one()
    {
        var content = Line(-1, "SessionEnd", payloadJson: "{\"recovered\":true}");
        var records = TelemetryReader.Read(WriteSession(content)).ToList();

        Assert.That(records, Has.Count.EqualTo(1));
        Assert.That(records[0].Seq, Is.EqualTo(-1));
        Assert.That(records[0].Kind, Is.EqualTo(TelemetryRecordKind.SessionEnd));
        Assert.That(records[0].Payload.GetProperty("recovered").GetBoolean(), Is.True);
    }

    [Test]
    public void Flatten_carries_turnIndex_wave_accepted_from_the_turn_onto_each_event()
    {
        var turnPayload = "{\"index\":3,\"wave\":2,\"accepted\":true,\"events\":[{\"type\":\"Stepped\"},{\"type\":\"Damaged\"}]}";
        var content = Line(0, "Turn", payloadJson: turnPayload);
        var rows = TelemetryReader.Flatten(WriteSession(content)).ToList();

        Assert.That(rows, Has.Count.EqualTo(2));
        foreach (var row in rows)
        {
            Assert.That(row["_turnIndex"], Is.EqualTo(3L));
            Assert.That(row["_wave"], Is.EqualTo(2L));
            Assert.That(row["_accepted"], Is.EqualTo(true));
        }
    }

    [Test]
    public void Flatten_skips_a_turn_with_no_events_array()
    {
        var noEventsProperty = Line(0, "Turn", payloadJson: "{\"index\":0,\"accepted\":true}");
        var eventsNotAnArray = Line(1, "Turn", payloadJson: "{\"index\":1,\"accepted\":true,\"events\":{}}");
        var rows = TelemetryReader.Flatten(WriteSession(string.Join("\n", noEventsProperty, eventsNotAnArray))).ToList();

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void Flatten_skips_a_turn_whose_payload_is_not_an_object()
    {
        var content = Line(0, "Turn", payloadJson: "[1,2,3]");
        var rows = TelemetryReader.Flatten(WriteSession(content)).ToList();

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void Flatten_unwraps_integers_as_long_and_non_integers_as_double()
    {
        var content = Line(0, "Event", name: "hit", payloadJson: "{\"amount\":7,\"ratio\":1.5}");
        var rows = TelemetryReader.Flatten(WriteSession(content)).ToList();

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0]["amount"], Is.TypeOf<long>());
        Assert.That(rows[0]["amount"], Is.EqualTo(7L));
        Assert.That(rows[0]["ratio"], Is.TypeOf<double>());
        Assert.That(rows[0]["ratio"], Is.EqualTo(1.5d));
    }

    [Test]
    public void Flatten_unwraps_nested_objects_and_arrays_as_JsonElement()
    {
        var content = Line(0, "Event", name: "hit", payloadJson: "{\"meta\":{\"x\":1},\"tags\":[\"a\",\"b\"]}");
        var rows = TelemetryReader.Flatten(WriteSession(content)).ToList();

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0]["meta"], Is.TypeOf<JsonElement>());
        Assert.That(((JsonElement)rows[0]["meta"]!).ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(rows[0]["tags"], Is.TypeOf<JsonElement>());
        Assert.That(((JsonElement)rows[0]["tags"]!).ValueKind, Is.EqualTo(JsonValueKind.Array));
    }
}
