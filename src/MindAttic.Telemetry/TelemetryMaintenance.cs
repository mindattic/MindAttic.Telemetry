namespace MindAttic.Telemetry;

/// <summary>
/// Recovers session files an earlier run left as <c>.ndjson.tmp</c> because it didn't shut down cleanly
/// (crash, force-kill, power loss). Every line already flushed to disk is still valid JSON — recovery just
/// appends a synthetic <c>SessionEnd</c> record (<c>{"recovered":true}</c>) and renames the file, the same
/// "every record is independently durable" property that makes the whole format crash-safe in the first place.
/// </summary>
public static class TelemetryMaintenance
{
    /// <summary>Finalizes every orphaned <c>.ndjson.tmp</c> file under <paramref name="appDir"/> that isn't
    /// currently open by a live session (in this or another process). Returns how many were recovered.</summary>
    public static int RecoverOrphans(string appDir, ITelemetryJson json, ITelemetryLog log)
    {
        if (!Directory.Exists(appDir)) return 0;
        var recovered = 0;
        foreach (var tmp in Directory.EnumerateFiles(appDir, "*.ndjson.tmp", SearchOption.AllDirectories).ToList())
        {
            if (IsStillOpen(tmp)) continue; // a live session (this process or another) still owns it
            try
            {
                using (var fs = new FileStream(tmp, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var w = new StreamWriter(fs))
                {
                    w.WriteLine(json.Serialize(new
                    {
                        seq = -1,
                        kind = TelemetryRecordKind.SessionEnd.ToString(),
                        atUtc = DateTimeOffset.UtcNow,
                        name = (string?)null,
                        payload = new { recovered = true },
                    }));
                }
                var final = tmp.Substring(0, tmp.Length - 4);
                if (File.Exists(final)) File.Delete(final);
                File.Move(tmp, final);
                recovered++;
                log.Warn($"Recovered an unfinished telemetry session: {Path.GetFileName(final)} (the previous run didn't shut down cleanly).");
            }
            catch (IOException ex)
            {
                log.Warn($"Could not recover \"{tmp}\".", ex);
            }
        }
        return recovered;
    }

    /// <summary>True when something else still has the file open for writing (a live session owns it).</summary>
    private static bool IsStillOpen(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }
}
