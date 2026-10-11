using System.Collections.Generic;
using UnityEngine;

namespace MindAttic.Telemetry.Unity
{
    /// <summary>
    /// MindAttic.Telemetry for Unity. Unlike MindAttic.Capture (one session per app launch), a telemetry
    /// session is scoped to whatever unit of play the game calls <see cref="Begin"/> for — a battle, a level,
    /// a whole run — so there's no auto-boot lifecycle here: call <see cref="Begin"/> when your own "this thing
    /// started" moment happens, and <see cref="Finish"/> when it ends. Production builds ignore all of it
    /// (same gate as MindAttic.Capture: editor, development builds, or the <c>MINDATTIC_TELEMETRY</c> define).
    /// Output: R:\Telemetry\{gameId}\{date}_{session}\ (falls back to Documents\MindAttic\Telemetry).
    /// </summary>
    public static class TelemetryUnity
    {
        /// <summary>True in the editor, in development builds, and in builds with the MINDATTIC_TELEMETRY scripting define.</summary>
        public static bool IsTelemetryBuild =>
#if MINDATTIC_TELEMETRY
            true;
#else
            Application.isEditor || Debug.isDebugBuild;
#endif

        /// <summary>The active session (a no-op instance until <see cref="Begin"/> is called, or in production builds).</summary>
        public static ITelemetrySession Current => Telemetry.Current;

        /// <summary>
        /// Starts a session and makes it <see cref="Current"/>. A no-op in production builds.
        /// </summary>
        /// <param name="gameId">Short id for this game (also the output subfolder name).</param>
        /// <param name="seed">RNG seed for this run, if there is one.</param>
        /// <param name="contentVersion">Content/ruleset version, if tracked separately from <c>Application.version</c>.</param>
        /// <param name="tags">Free-form session-level key/values.</param>
        public static ITelemetrySession Begin(string gameId, long? seed = null, string contentVersion = null, TelemetryData tags = null)
        {
            if (!IsTelemetryBuild) return NullTelemetrySession.Instance;
            var options = new TelemetryOptions
            {
                AppName = gameId,
                AppVersion = Application.version,
                Json = new NewtonsoftTelemetryJson(),
                Log = new UnityTelemetryLog(),
            };
            return Telemetry.Begin(options, gameId, seed, contentVersion, tags);
        }

        /// <summary>Records one full rules-engine turn on <see cref="Current"/>. See <see cref="ITelemetrySession.RecordTurn"/>.</summary>
        public static void RecordTurn(object before, object command, bool accepted, IEnumerable<object> events, object after) =>
            Current.RecordTurn(before, command, accepted, events, after);

        /// <summary>Records one lightweight named moment on <see cref="Current"/>. See <see cref="ITelemetrySession.RecordEvent"/>.</summary>
        public static void RecordEvent(string name, TelemetryData data = null) => Current.RecordEvent(name, data);

        /// <summary>Finishes <see cref="Current"/> (fire-and-forget; writes are synchronous so this returns almost immediately).</summary>
        public static void Finish(object outcome = null) => _ = Current.FinishAsync(outcome);
    }
}
