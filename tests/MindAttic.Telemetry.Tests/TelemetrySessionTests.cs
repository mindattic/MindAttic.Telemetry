using MindAttic.Telemetry;
using NUnit.Framework;

namespace MindAttic.Telemetry.Tests;

public class TelemetrySessionTests
{
    private string _root = "";

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("mindattic-telemetry-tests-").FullName;

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private TelemetryOptions Options() => new()
    {
        OutputRoot = _root,
        FallbackRoot = _root,
        AppName = "TestGame",
        AppVersion = "1.2.3",
        Log = new TraceTelemetryLog(),
    };

    [Test]
    public async Task Session_writes_start_turn_event_and_end_in_order_and_reads_back()
    {
        var session = Telemetry.Begin(Options(), "TestGame", seed: 42, contentVersion: "content-7");
        Assert.That(session.Enabled, Is.True);
        Assert.That(session.SessionFile, Does.EndWith(".ndjson.tmp"));

        session.RecordTurn(
            before: new { hp = 10 },
            command: new { type = "move", path = new[] { 1, 2 } },
            accepted: true,
            events: new object[] { new { type = "Stepped", unitId = 1 }, new { type = "Damaged", unitId = 2, amount = 5 } },
            after: new { hp = 10 });
        session.RecordEvent("shop.purchase", new TelemetryData { ["item"] = "potion", ["cost"] = 50 });
        await session.FinishAsync(new { victory = true, turns = 1 });

        Assert.That(session.SessionFile, Does.Not.EndWith(".tmp"), "FinishAsync should rename the file to its final name.");
        Assert.That(File.Exists(session.SessionFile), Is.True);

        var records = TelemetryReader.Read(session.SessionFile).ToList();
        Assert.That(records, Has.Count.EqualTo(4));
        Assert.That(records[0].Kind, Is.EqualTo(TelemetryRecordKind.SessionStart));
        Assert.That(records[0].Payload.GetProperty("gameId").GetString(), Is.EqualTo("TestGame"));
        Assert.That(records[0].Payload.GetProperty("seed").GetInt64(), Is.EqualTo(42));
        Assert.That(records[1].Kind, Is.EqualTo(TelemetryRecordKind.Turn));
        Assert.That(records[1].Payload.GetProperty("events").GetArrayLength(), Is.EqualTo(2));
        Assert.That(records[2].Kind, Is.EqualTo(TelemetryRecordKind.Event));
        Assert.That(records[2].Name, Is.EqualTo("shop.purchase"));
        Assert.That(records[2].Payload.GetProperty("item").GetString(), Is.EqualTo("potion"));
        Assert.That(records[3].Kind, Is.EqualTo(TelemetryRecordKind.SessionEnd));
        Assert.That(records[3].Payload.GetProperty("victory").GetBoolean(), Is.True);

        // sequence numbers are gapless and in order
        Assert.That(records.Select(r => r.Seq), Is.EqualTo(new long[] { 0, 1, 2, 3 }));
    }

    [Test]
    public async Task Flatten_produces_one_row_per_turn_event_and_per_standalone_event()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        session.RecordTurn(before: null, command: new { type = "move" }, accepted: true,
            events: new object[] { new { type = "Stepped" }, new { type = "Damaged", amount = 7 } }, after: null);
        session.RecordEvent("level.up", new TelemetryData { ["class"] = "paladin" });
        await session.FinishAsync();

        var rows = TelemetryReader.Flatten(session.SessionFile).ToList();
        Assert.That(rows, Has.Count.EqualTo(3)); // 2 turn-events + 1 standalone event
        Assert.That(rows[0]["_kind"], Is.EqualTo("turn-event"));
        Assert.That(rows[0]["type"], Is.EqualTo("Stepped"));
        Assert.That(rows[1]["amount"], Is.EqualTo(7L));
        Assert.That(rows[2]["_kind"], Is.EqualTo("event"));
        Assert.That(rows[2]["_name"], Is.EqualTo("level.up"));
        Assert.That(rows[2]["class"], Is.EqualTo("paladin"));
    }

    [Test]
    public async Task RecoverOrphans_finalizes_a_file_abandoned_by_a_crash()
    {
        var options = Options();
        var appDir = Path.Combine(_root, options.AppName);
        var dir = Path.Combine(appDir, "2026-01-01_orphan-0001");
        Directory.CreateDirectory(dir);
        var tmp = Path.Combine(dir, "orphan-0001.ndjson.tmp");
        await File.WriteAllTextAsync(tmp, "{\"seq\":0,\"kind\":\"SessionStart\",\"atUtc\":\"2026-01-01T00:00:00Z\",\"name\":null,\"payload\":{\"gameId\":\"TestGame\"}}\n");

        var recovered = TelemetryMaintenance.RecoverOrphans(appDir, options.Json!, options.Log);

        Assert.That(recovered, Is.EqualTo(1));
        var final = Path.Combine(dir, "orphan-0001.ndjson");
        Assert.That(File.Exists(final), Is.True);
        Assert.That(File.Exists(tmp), Is.False);
        var records = TelemetryReader.Read(final).ToList();
        Assert.That(records, Has.Count.EqualTo(2));
        Assert.That(records[1].Kind, Is.EqualTo(TelemetryRecordKind.SessionEnd));
        Assert.That(records[1].Payload.GetProperty("recovered").GetBoolean(), Is.True);
    }

    [Test]
    public async Task A_live_sessions_tmp_file_is_never_touched_by_recovery()
    {
        var options = Options();
        var live = Telemetry.Begin(options, "TestGame");
        live.RecordEvent("still-going");

        var appDir = Path.Combine(_root, options.AppName);
        var recovered = TelemetryMaintenance.RecoverOrphans(appDir, options.Json!, options.Log);

        Assert.That(recovered, Is.EqualTo(0));
        Assert.That(File.Exists(live.SessionFile), Is.True);
        await live.FinishAsync();
    }

    [Test]
    public void Disabled_options_produce_a_no_op_session_that_writes_nothing()
    {
        var options = Options();
        options.Enabled = false;
        var session = Telemetry.Begin(options, "TestGame");

        Assert.That(session.Enabled, Is.False);
        session.RecordEvent("should-not-write");
        Assert.That(Directory.Exists(Path.Combine(_root, "TestGame")), Is.False);
    }
}
