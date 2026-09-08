using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Diagnostics
{
    public static class AppLog
    {
        private static readonly object _sync = new();
        private static readonly List<ILogSink> _sinks = new();
        private static readonly Queue<LogMessage> _history = new();
        private const int MaxHistoryCount = 128;

        public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;
        public static event Action<LogMessage> MessageLogged;

        static AppLog()
        {
            ResetToDefaults();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetToDefaults()
        {
            lock (_sync)
            {
                _sinks.Clear();
                _history.Clear();
                MessageLogged = null;
                MinimumLevel = LogLevel.Debug;
                _sinks.Add(new UnityConsoleSink());
            }
        }

        public static void AddSink(ILogSink sink)
        {
            if (sink == null) return;
            lock (_sync)
            {
                if (!_sinks.Contains(sink))
                {
                    _sinks.Add(sink);
                }
            }
        }

        public static void RemoveSink(ILogSink sink)
        {
            if (sink == null) return;
            lock (_sync)
            {
                _sinks.Remove(sink);
            }
        }

        public static void ClearSinks()
        {
            lock (_sync)
            {
                _sinks.Clear();
            }
        }

        public static IReadOnlyList<LogMessage> GetRecentLogs()
        {
            lock (_sync)
            {
                return _history.ToArray();
            }
        }

        public static void ClearHistory()
        {
            lock (_sync)
            {
                _history.Clear();
            }
        }

        public static void Verbose(string category, string message, UnityEngine.Object context = null) =>
            Log(LogLevel.Verbose, category, message, null, context);

        public static void Debug(string category, string message, UnityEngine.Object context = null) =>
            Log(LogLevel.Debug, category, message, null, context);

        public static void Info(string category, string message, UnityEngine.Object context = null) =>
            Log(LogLevel.Info, category, message, null, context);

        public static void Warning(string category, string message, UnityEngine.Object context = null) =>
            Log(LogLevel.Warning, category, message, null, context);

        public static void Error(string category, string message, UnityEngine.Object context = null) =>
            Log(LogLevel.Error, category, message, null, context);

        public static void Fatal(string category, string message, UnityEngine.Object context = null) =>
            Log(LogLevel.Fatal, category, message, null, context);

        public static void Exception(string category, Exception exception, string message = null, UnityEngine.Object context = null)
        {
            string text = string.IsNullOrEmpty(message) ? exception?.Message : $"{message}: {exception?.Message}";
            Log(LogLevel.Error, category, text, exception, context);
        }

        public static void InfoFormat(string category, string format, params object[] args)
        {
            if (LogLevel.Info < MinimumLevel) return;
            Log(LogLevel.Info, category, args != null && args.Length > 0 ? string.Format(format, args) : format);
        }

        public static void WarningFormat(string category, string format, params object[] args)
        {
            if (LogLevel.Warning < MinimumLevel) return;
            Log(LogLevel.Warning, category, args != null && args.Length > 0 ? string.Format(format, args) : format);
        }

        public static void ErrorFormat(string category, string format, params object[] args)
        {
            if (LogLevel.Error < MinimumLevel) return;
            Log(LogLevel.Error, category, args != null && args.Length > 0 ? string.Format(format, args) : format);
        }

        public static void Log(
            LogLevel level,
            string category,
            string text,
            Exception exception = null,
            UnityEngine.Object context = null)
        {
            if (level < MinimumLevel) return;

            var message = new LogMessage(level, category, text, exception, context);

            lock (_sync)
            {
                _history.Enqueue(message);
                while (_history.Count > MaxHistoryCount)
                {
                    _history.Dequeue();
                }

                for (int i = 0; i < _sinks.Count; i++)
                {
                    try
                    {
                        _sinks[i].Emit(message);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[AppLog] Sink {_sinks[i].GetType().Name} failed: {ex.Message}");
                    }
                }
            }

            try
            {
                MessageLogged?.Invoke(message);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[AppLog] MessageLogged subscriber threw exception: {ex.Message}");
            }
        }
    }
}
