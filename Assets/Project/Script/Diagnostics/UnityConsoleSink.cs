using System;
using UnityEngine;

namespace PowerMath.Diagnostics
{
    public sealed class UnityConsoleSink : ILogSink
    {
        public void Emit(in LogMessage message)
        {
            string formatted = FormatMessage(message);

            switch (message.Level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    if (message.Context != null)
                        Debug.Log(formatted, message.Context);
                    else
                        Debug.Log(formatted);
                    break;

                case LogLevel.Warning:
                    if (message.Context != null)
                        Debug.LogWarning(formatted, message.Context);
                    else
                        Debug.LogWarning(formatted);
                    break;

                case LogLevel.Error:
                case LogLevel.Fatal:
                    if (message.Exception != null)
                    {
                        if (message.Context != null)
                            Debug.LogException(message.Exception, message.Context);
                        else
                            Debug.LogException(message.Exception);
                    }
                    else
                    {
                        if (message.Context != null)
                            Debug.LogError(formatted, message.Context);
                        else
                            Debug.LogError(formatted);
                    }
                    break;
            }
        }

        private static string FormatMessage(in LogMessage message)
        {
#if UNITY_EDITOR
            string colorTag = message.Level switch
            {
                LogLevel.Verbose => "#A0A0A0",
                LogLevel.Debug => "#B0BEC5",
                LogLevel.Info => "#4FC3F7",
                LogLevel.Warning => "#FFB74D",
                LogLevel.Error => "#E57373",
                LogLevel.Fatal => "#EF5350",
                _ => "#FFFFFF"
            };

            return $"<color=#B39DDB>[{message.Timestamp:HH:mm:ss.fff}]</color>" +
                   $"<color=#90CAF9>[F:{message.FrameCount}]</color>" +
                   $"<color={colorTag}>[{message.Level.ToString().ToUpperInvariant()}]</color>" +
                   $"<color=#80CBC4>[{message.Category}]</color> {message.Text}";
#else
            return message.ToString();
#endif
        }
    }
}
