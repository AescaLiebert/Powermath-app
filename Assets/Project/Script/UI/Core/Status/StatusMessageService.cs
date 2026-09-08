using System;
using System.Collections.Generic;
using PowerMath.Diagnostics;
using UnityEngine;

namespace PowerMath.UI.Core
{
    public static class StatusMessageService
    {
        private static readonly object _sync = new();
        private static readonly List<StatusToastOverlay> _overlays = new();

        public static event Action<string, StatusSeverity, int> MessagePublished;

        public static void RegisterOverlay(StatusToastOverlay overlay)
        {
            if (overlay == null) return;
            lock (_sync)
            {
                if (!_overlays.Contains(overlay))
                {
                    _overlays.Add(overlay);
                }
            }
        }

        public static void UnregisterOverlay(StatusToastOverlay overlay)
        {
            if (overlay == null) return;
            lock (_sync)
            {
                _overlays.Remove(overlay);
            }
        }

        public static void ShowSuccess(string message, int durationMilliseconds = 2600) =>
            Show(message, StatusSeverity.Success, durationMilliseconds);

        public static void ShowError(string message, int durationMilliseconds = 3200) =>
            Show(message, StatusSeverity.Error, durationMilliseconds);

        public static void ShowWarning(string message, int durationMilliseconds = 2800) =>
            Show(message, StatusSeverity.Warning, durationMilliseconds);

        public static void ShowInfo(string message, int durationMilliseconds = 2600) =>
            Show(message, StatusSeverity.Info, durationMilliseconds);

        public static void Show(string message, StatusSeverity severity = StatusSeverity.Info, int durationMilliseconds = 2600)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            string trimmed = message.Trim();

            // Log to global diagnostics
            switch (severity)
            {
                case StatusSeverity.Success:
                    AppLog.Info("StatusUI", $"[Success] {trimmed}");
                    break;
                case StatusSeverity.Warning:
                    AppLog.Warning("StatusUI", trimmed);
                    break;
                case StatusSeverity.Error:
                    AppLog.Error("StatusUI", trimmed);
                    break;
                default:
                    AppLog.Info("StatusUI", trimmed);
                    break;
            }

            // Dispatch to registered overlays
            lock (_sync)
            {
                for (int i = _overlays.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _overlays[i].Show(trimmed, severity, durationMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        AppLog.Error("StatusUI", $"Failed to deliver toast to overlay: {ex.Message}");
                    }
                }
            }

            try
            {
                MessagePublished?.Invoke(trimmed, severity, durationMilliseconds);
            }
            catch (Exception ex)
            {
                AppLog.Error("StatusUI", $"MessagePublished subscriber threw exception: {ex.Message}");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            lock (_sync)
            {
                _overlays.Clear();
                MessagePublished = null;
            }
        }
    }
}
