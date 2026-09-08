using System;
using UnityEngine;

namespace PowerMath.Diagnostics
{
    public readonly struct LogMessage
    {
        public DateTime Timestamp { get; }
        public int FrameCount { get; }
        public LogLevel Level { get; }
        public string Category { get; }
        public string Text { get; }
        public Exception Exception { get; }
        public UnityEngine.Object Context { get; }

        public LogMessage(
            LogLevel level,
            string category,
            string text,
            Exception exception = null,
            UnityEngine.Object context = null)
        {
            Timestamp = DateTime.UtcNow;
            FrameCount = Application.isPlaying ? Time.frameCount : 0;
            Level = level;
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
            Text = text ?? string.Empty;
            Exception = exception;
            Context = context;
        }

        public override string ToString()
        {
            string baseMsg = $"[{Timestamp:HH:mm:ss.fff}][F:{FrameCount}][{Level.ToString().ToUpperInvariant()}][{Category}] {Text}";
            if (Exception != null)
            {
                baseMsg += $"\nException: {Exception.GetType().Name}: {Exception.Message}\n{Exception.StackTrace}";
            }
            return baseMsg;
        }
    }
}
