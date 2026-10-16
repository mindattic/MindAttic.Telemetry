using System.Text.Json;
using MindAttic.Telemetry;
using NUnit.Framework;

namespace MindAttic.Telemetry.Tests;

public class TelemetryMaintenanceTests
{
    private string _root = "";
    private ITelemetryJson _json = new SystemTextTelemetryJson();

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("mindattic-telemetry-maintenance-tests-").FullName;

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private const string OrphanLine = "{\"seq\":0,\"kind\":\"SessionStart\",\"atUtc\":\"2026-01-01T00:00:00Z\",\"name\":null,\"payload\":{\"gameId\":\"TestGame\"}}\n";

    private string WriteOrphan(string appDir, string sessionId)
    {
        var dir = Path.Combine(appDir, $"2026-01-01_{sessionId}");
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, $"{sessionId}.ndjson.tmp");
        File.WriteAllText(tmp, OrphanLine);
        return tmp;
    }

    [Test]
    public void RecoverOrphans_with_a_missing_appDir_returns_zero()
    {
        var appDir = Path.Combine(_root, "DoesNotExist");
        var recovered = TelemetryMaintenance.RecoverOrphans(appDir, _json, new RecordingTelemetryLog());

        Assert.That(recovered, Is.EqualTo(0));
    }

    [Test]
    public void RecoverOrphans_recovers_orphans_nested_in_several_session_folders()
    {
        var appDir = Path.Combine(_root, "TestGame");
        var tmpA = WriteOrphan(appDir, "session-a");
        var tmpB = WriteOrphan(appDir, "session-b");

        var recovered = TelemetryMaintenance.RecoverOrphans(appDir, _json, new RecordingTelemetryLog());

        Assert.That(recovered, Is.EqualTo(2));
        foreach (var tmp in new[] { tmpA, tmpB })
        {
            var final = tmp.Substring(0, tmp.Length - 4);
            Assert.That(File.Exists(final), Is.True);
            Assert.That(File.Exists(tmp), Is.False);
            var records = TelemetryReader.Read(final).ToList();
            Assert.That(records[^1].Kind, Is.EqualTo(TelemetryRecordKind.SessionEnd));
            Assert.That(records[^1].Payload.GetProperty("recovered").GetBoolean(), Is.True);
        }
    }

    [Test]
    public void RecoverOrphans_overwrites_an_existing_final_file()
    {
        var appDir = Path.Combine(_root, "TestGame");
        var tmp = WriteOrphan(appDir, "session-a");
        var final = tmp.Substring(0, tmp.Length - 4);
        File.WriteAllText(final, "{\"stale\":true}\n");

        var recovered = TelemetryMaintenance.RecoverOrphans(appDir, _json, new RecordingTelemetryLog());

        Assert.That(recovered, Is.EqualTo(1));
        var records = TelemetryReader.Read(final).ToList();
        Assert.That(records, Has.Count.EqualTo(2), "the stale final file's content must be replaced, not appended to");
        Assert.That(records[0].Kind, Is.EqualTo(TelemetryRecordKind.SessionStart));
        Assert.That(records[1].Kind, Is.EqualTo(TelemetryRecordKind.SessionEnd));
    }

    [Test]
    public void RecoverOrphans_logs_a_warning_and_continues_when_one_file_throws_IOException()
    {
        var appDir = Path.Combine(_root, "TestGame");
        var tmpOk = WriteOrphan(appDir, "session-ok");
        var tmpBad = WriteOrphan(appDir, "session-bad");
        var finalBad = tmpBad.Substring(0, tmpBad.Length - 4);
        File.WriteAllText(finalBad, "{\"stale\":true}\n");

        var log = new RecordingTelemetryLog();
        using (new FileStream(finalBad, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var recovered = TelemetryMaintenance.RecoverOrphans(appDir, _json, log);

            Assert.That(recovered, Is.EqualTo(1), "only the file whose final path isn't locked should be recovered");
            Assert.That(File.Exists(tmpBad), Is.True, "the locked file is left in place (not renamed) after the failed attempt");
            Assert.That(log.Entries, Has.Some.Matches<(TelemetryLogLevel Level, string Message, Exception? Exception)>(
                e => e.Level == TelemetryLogLevel.Warning && e.Message.Contains("Could not recover")));
        }

        Assert.That(File.Exists(tmpOk.Substring(0, tmpOk.Length - 4)), Is.True, "the other orphan still recovers normally");
    }
}

public class SystemTextTelemetryJsonTests
{
    private readonly SystemTextTelemetryJson _json = new();

    [Test]
    public void Serialize_writes_property_names_as_camelCase()
    {
        var json = _json.Serialize(new { GameId = "g", SessionId = "s" });

        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("gameId", out _), Is.True);
        Assert.That(doc.RootElement.TryGetProperty("sessionId", out _), Is.True);
    }

    [Test]
    public void Serialize_omits_null_members()
    {
        var json = _json.Serialize(new { A = (string?)null, B = "kept" });

        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("a", out _), Is.False);
        Assert.That(doc.RootElement.GetProperty("b").GetString(), Is.EqualTo("kept"));
    }

    [Test]
    public void Serialize_writes_enums_as_strings()
    {
        var json = _json.Serialize(TelemetryRecordKind.Turn);

        Assert.That(json, Is.EqualTo("\"Turn\""));
    }

    [Test]
    public void TelemetryData_round_trips_case_insensitively()
    {
        var data = new TelemetryData { ["Item"] = "potion" };

        var json = _json.Serialize(data);
        var roundTripped = _json.Deserialize<TelemetryData>(json)!;

        Assert.That(roundTripped.TryGetValue("ITEM", out var value), Is.True);
        Assert.That(((JsonElement)value!).GetString(), Is.EqualTo("potion"));
    }
}
