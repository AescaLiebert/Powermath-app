using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    public sealed class StatusToastOverlay : IDisposable
    {
        public const string ContainerName = "status-toast-container";
        public const string BannerName = "status-toast-banner";
        public const string BadgeName = "status-toast-badge";
        public const string TextName = "status-toast-text";

        private readonly VisualElement _container;
        private readonly VisualElement _banner;
        private readonly Label _badge;
        private readonly Label _text;
        private int _activeRevision;
        private bool _disposed;

        public VisualElement Container => _container;
        public VisualElement Banner => _banner;
        public Label Badge => _badge;
        public Label Text => _text;
        public bool IsVisible => _banner.ClassListContains("is-visible");

        public StatusToastOverlay(VisualElement root = null)
        {
            _container = new VisualElement
            {
                name = ContainerName,
                pickingMode = PickingMode.Ignore
            };
            _container.AddToClassList("status-toast-container");

            _banner = new VisualElement
            {
                name = BannerName,
                pickingMode = PickingMode.Ignore
            };
            _banner.AddToClassList("status-toast-banner");
            _banner.style.display = DisplayStyle.None;

            _badge = new Label
            {
                name = BadgeName,
                pickingMode = PickingMode.Ignore
            };
            _badge.AddToClassList("status-toast__badge");

            _text = new Label
            {
                name = TextName,
                pickingMode = PickingMode.Ignore
            };
            _text.AddToClassList("status-toast__text");

            _banner.Add(_badge);
            _banner.Add(_text);
            _container.Add(_banner);

            if (root != null)
            {
                root.Add(_container);
            }

            StatusMessageService.RegisterOverlay(this);
        }

        public static StatusToastOverlay Attach(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            var existingContainer = root.Q<VisualElement>(ContainerName);
            if (existingContainer?.userData is StatusToastOverlay existing)
            {
                return existing;
            }

            var overlay = new StatusToastOverlay(root);
            overlay.Container.userData = overlay;
            return overlay;
        }

        public void Show(string message, StatusSeverity severity = StatusSeverity.Info, int durationMilliseconds = 2600)
        {
            if (_disposed || string.IsNullOrWhiteSpace(message)) return;

            int revision = ++_activeRevision;
            _text.text = message.Trim();

            // Clear old severity classes
            _banner.RemoveFromClassList("status-toast--info");
            _banner.RemoveFromClassList("status-toast--success");
            _banner.RemoveFromClassList("status-toast--warning");
            _banner.RemoveFromClassList("status-toast--error");

            switch (severity)
            {
                case StatusSeverity.Success:
                    _badge.text = "✓";
                    _banner.AddToClassList("status-toast--success");
                    break;
                case StatusSeverity.Warning:
                    _badge.text = "⚠";
                    _banner.AddToClassList("status-toast--warning");
                    break;
                case StatusSeverity.Error:
                    _badge.text = "✕";
                    _banner.AddToClassList("status-toast--error");
                    break;
                default:
                    _badge.text = "ℹ";
                    _banner.AddToClassList("status-toast--info");
                    break;
            }

            // Enter routine
            _banner.style.display = DisplayStyle.Flex;
            _banner.schedule.Execute(() =>
            {
                if (revision == _activeRevision)
                {
                    _banner.EnableInClassList("is-visible", true);
                }
            });

            // Hold and Exit routine
            int holdTime = Math.Max(1000, durationMilliseconds);
            _banner.schedule.Execute(() =>
            {
                if (revision != _activeRevision) return;

                // Exit animation
                _banner.EnableInClassList("is-visible", false);

                // Hide after CSS transition concludes (240ms)
                _banner.schedule.Execute(() =>
                {
                    if (revision == _activeRevision)
                    {
                        _banner.style.display = DisplayStyle.None;
                    }
                }).StartingIn(240);
            }).StartingIn(holdTime);
        }

        public void HideImmediate()
        {
            _activeRevision++;
            _banner.EnableInClassList("is-visible", false);
            _banner.style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StatusMessageService.UnregisterOverlay(this);
            _container.RemoveFromHierarchy();
        }
    }
}
