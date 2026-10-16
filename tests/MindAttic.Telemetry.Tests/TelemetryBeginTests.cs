using MindAttic.Telemetry;
using NUnit.Framework;

namespace MindAttic.Telemetry.Tests;

internal sealed class RecordingTelemetryLog : ITelemetryLog
{
    public List<(TelemetryLogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public void Log(TelemetryLogLevel level, string message, Exception? exception = null) =>
        Entries.Add((level, message, exception));
}

public class TelemetryBeginTests
{
    private string _root = "";

    [SetUp]
    public void SetUp() => _root = Directory.CreateTempSubdirectory("mindattic-telemetry-begin-tests-").FullName;

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

    private static string? FirstUnmountedDriveRoot()
    {
        var mounted = new HashSet<char>(DriveInfo.GetDrives().Select(d => char.ToUpperInvariant(d.Name[0])));
        for (var c = 'Z'; c >= 'A'; c--)
        {
            if (!mounted.Contains(c)) return $"{c}:\\Telemetry";
        }
        return null;
    }

    [Test]
    public void Begin_with_null_options_throws_ArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Telemetry.Begin(null!, "TestGame"));
        Assert.That(ex!.ParamName, Is.EqualTo("options"));
    }

    [Test]
    public void Begin_with_blank_gameId_throws_ArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Telemetry.Begin(Options(), "   "));
        Assert.That(ex!.ParamName, Is.EqualTo("gameId"));
    }

    [Test]
    public void Begin_with_null_Json_throws_TelemetryException()
    {
        var options = Options();
        options.Json = null;

        Assert.Throws<TelemetryException>(() => Telemetry.Begin(options, "TestGame"));
    }

    [Test]
    public async Task Begin_sets_Current_to_the_new_session_and_resets_it_when_disabled()
    {
        var session = Telemetry.Begin(Options(), "TestGame");
        Assert.That(Telemetry.Current, Is.SameAs(session));
        await session.FinishAsync();

        var disabledOptions = Options();
        disabledOptions.Enabled = false;
        var noop = Telemetry.Begin(disabledOptions, "TestGame");

        Assert.That(noop, Is.SameAs(NullTelemetrySession.Instance));
        Assert.That(Telemetry.Current, Is.SameAs(NullTelemetrySession.Instance));
    }

    [Test]
    public async Task An_unavailable_OutputRoot_drive_falls_back_to_FallbackRoot_with_a_warning_logged()
    {
        var unmounted = FirstUnmountedDriveRoot();
        if (unmounted is null) Assert.Ignore("No unmounted drive letter available to simulate an unavailable root.");

        var log = new RecordingTelemetryLog();
        var options = Options();
        options.OutputRoot = unmounted!;
        options.Log = log;

        var session = Telemetry.Begin(options, "TestGame");

        Assert.That(session.SessionFile, Does.StartWith(_root));
        Assert.That(log.Entries, Has.Some.Matches<(TelemetryLogLevel Level, string Message, Exception? Exception)>(
            e => e.Level == TelemetryLogLevel.Warning && e.Message.Contains(unmounted!)));

        await session.FinishAsync();
    }

    [Test]
    public async Task Session_output_lands_under_AppName_and_date_sessionId()
    {
        var session = Telemetry.Begin(Options(), "TestGame");

        var expectedAppDir = Path.Combine(_root, "TestGame") + Path.DirectorySeparatorChar;
        Assert.That(session.SessionFile, Does.StartWith(expectedAppDir));

        var relative = session.SessionFile.Substring(expectedAppDir.Length);
        var dirName = relative.Split(Path.DirectorySeparatorChar)[0];
        Assert.That(dirName, Does.Match(@"^\d{4}-\d{2}-\d{2}_.+"));
        Assert.That(dirName, Does.EndWith(session.SessionId));
        Assert.That(Path.GetFileName(session.SessionFile), Is.EqualTo($"{session.SessionId}.ndjson.tmp"));

        await session.FinishAsync();
    }
}
