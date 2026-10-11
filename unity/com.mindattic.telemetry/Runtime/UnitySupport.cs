using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace MindAttic.Telemetry.Unity
{
    /// <summary>Newtonsoft-backed <see cref="ITelemetryJson"/> — Unity's netstandard2.1 build has no
    /// System.Text.Json, same split as MindAttic.Capture's <c>NewtonsoftCaptureJson</c>.</summary>
    public sealed class NewtonsoftTelemetryJson : ITelemetryJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None, // one compact line per record
            Converters = { new StringEnumConverter() },
        };

        /// <inheritdoc/>
        public string Serialize<T>(T value) => JsonConvert.SerializeObject(value, Settings);

        /// <inheritdoc/>
        public T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
    }

    /// <summary>Routes telemetry logging to the Unity console (warnings/errors always; info in the editor and
    /// development builds).</summary>
    public sealed class UnityTelemetryLog : ITelemetryLog
    {
        /// <inheritdoc/>
        public void Log(TelemetryLogLevel level, string message, System.Exception exception = null)
        {
            var text = "[MindAttic.Telemetry] " + message + (exception == null ? "" : "\n" + exception);
            switch (level)
            {
                case TelemetryLogLevel.Error:
                    Debug.LogError(text);
                    break;
                case TelemetryLogLevel.Warning:
                    Debug.LogWarning(text);
                    break;
                default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log(text);
#endif
                    break;
            }
        }
    }
}
