using MindAttic.Telemetry;
using NUnit.Framework;

namespace MindAttic.Telemetry.Tests;

public class TelemetrySessionLifecycleTests
{
    private string _root = "";

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("mindattic-telemetry-lifecycle-tests-").FullName;

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
    public async Task FinishAsync_is_idempotent_and_a_second_call_writes_nothing()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        session.RecordEvent("a");
        await session.FinishAsync(new { result = "ok" });
        var countAfterFirst = session.RecordCount;
        var fileAfterFirst = await File.ReadAllTextAsync(session.SessionFile);

        await session.FinishAsync(new { result = "should-not-land" });

        Assert.That(session.RecordCount, Is.EqualTo(countAfterFirst));
        Assert.That(await File.ReadAllTextAsync(session.SessionFile), Is.EqualTo(fileAfterFirst));
    }

    [Test]
    public async Task RecordTurn_and_RecordEvent_after_finish_are_no_ops()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        await session.FinishAsync();
        var countAfterFinish = session.RecordCount;

        session.RecordEvent("late");
        session.RecordTurn(before: null, command: null, accepted: true, events: null, after: null);

        Assert.That(session.Enabled, Is.False);
        Assert.That(session.RecordCount, Is.EqualTo(countAfterFinish));
    }

    [Test]
    public async Task RecordEvent_with_null_or_empty_name_is_ignored()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        var countBefore = session.RecordCount;

        session.RecordEvent(name: null!);
        session.RecordEvent(name: "");

        Assert.That(session.RecordCount, Is.EqualTo(countBefore));
        await session.FinishAsync();
    }

    [Test]
    public async Task RecordCount_tracks_written_lines()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        Assert.That(session.RecordCount, Is.EqualTo(1), "session-start is written synchronously by Begin");

        session.RecordEvent("one");
        session.RecordTurn(before: null, command: null, accepted: true, events: null, after: null);
        Assert.That(session.RecordCount, Is.EqualTo(3));

        await session.FinishAsync();
        Assert.That(session.RecordCount, Is.EqualTo(4));

        var lineCount = (await File.ReadAllLinesAsync(session.SessionFile)).Length;
        Assert.That(lineCount, Is.EqualTo(4));
    }

    [Test]
    public async Task DisposeAsync_finishes_and_renames_the_tmp_file()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        var tmpPath = session.SessionFile;
        Assert.That(tmpPath, Does.EndWith(".ndjson.tmp"));

        await session.DisposeAsync();

        Assert.That(session.Enabled, Is.False);
        Assert.That(session.SessionFile, Does.Not.EndWith(".tmp"));
        Assert.That(File.Exists(session.SessionFile), Is.True);
        Assert.That(File.Exists(tmpPath), Is.False);
    }

    [Test]
    public async Task Null_events_on_RecordTurn_serialize_cleanly()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        session.RecordTurn(before: null, command: null, accepted: false, events: null, after: null);
        await session.FinishAsync();

        var records = TelemetryReader.Read(session.SessionFile).ToList();
        var turn = records.Single(r => r.Kind == TelemetryRecordKind.Turn);
        // the serializer omits null members (DefaultIgnoreCondition.WhenWritingNull) rather than writing "events":null
        Assert.That(turn.Payload.TryGetProperty("events", out _), Is.False);
    }

    [Test]
    public async Task Concurrent_RecordEvent_calls_produce_gapless_unique_seq_numbers()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        const int threadCount = 50;

        await Task.WhenAll(Enumerable.Range(0, threadCount)
            .Select(i => Task.Run(() => session.RecordEvent($"evt-{i}"))));

        await session.FinishAsync();

        var seqs = TelemetryReader.Read(session.SessionFile).Select(r => r.Seq).ToList();
        Assert.That(seqs, Has.Count.EqualTo(threadCount + 2)); // session-start + N events + session-end
        Assert.That(seqs.Distinct().Count(), Is.EqualTo(seqs.Count), "seq numbers must be unique");
        Assert.That(seqs.OrderBy(s => s), Is.EqualTo(Enumerable.Range(0, threadCount + 2).Select(i => (long)i)));
    }
}
