using System;
using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public enum MainMenuNoticeKind
    {
        Information,
        Success,
        Warning,
        Error
    }

    [DisallowMultipleComponent]
    public sealed class MainMenuSharedOverlayController : MonoBehaviour
    {
        private IMainMenuPanelHost _panelHost;
        private VisualElement _root;
        private VisualElement _notice;
        private Label _noticeText;
        private int _noticeRevision;
        private bool _initialized;

        public static MainMenuSharedOverlayController GetOrCreate(
            GameObject owner,
            VisualElement root,
            IMainMenuPanelHost panelHost)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (panelHost == null) throw new ArgumentNullException(nameof(panelHost));

            // The composition root is shared by scenes whose UI documents do not
            // include the Main Menu overlay. In those scenes the overlay is optional.
            if (!HasOverlayContract(root)) return null;

            MainMenuSharedOverlayController controller =
                owner.GetComponent<MainMenuSharedOverlayController>();
            if (controller == null)
                controller = owner.AddComponent<MainMenuSharedOverlayController>();
            controller.Initialize(root, panelHost);
            return controller;
        }

        public void SetBackEnabled(bool enabled)
        {
            // Utility bar has been deprecated and removed.
        }

        public void Publish(
            string message,
            MainMenuNoticeKind kind = MainMenuNoticeKind.Information,
            int durationMilliseconds = 2400)
        {
            if (!_initialized || string.IsNullOrWhiteSpace(message)) return;

            var severity = kind switch
            {
                MainMenuNoticeKind.Success => PowerMath.UI.Core.StatusSeverity.Success,
                MainMenuNoticeKind.Warning => PowerMath.UI.Core.StatusSeverity.Warning,
                MainMenuNoticeKind.Error => PowerMath.UI.Core.StatusSeverity.Error,
                _ => PowerMath.UI.Core.StatusSeverity.Info
            };
            PowerMath.UI.Core.StatusMessageService.Show(message, severity, durationMilliseconds);

            int revision = ++_noticeRevision;
            _noticeText.text = message.Trim();
            _notice.EnableInClassList("is-success", kind == MainMenuNoticeKind.Success);
            _notice.EnableInClassList("is-warning", kind == MainMenuNoticeKind.Warning);
            _notice.EnableInClassList("is-error", kind == MainMenuNoticeKind.Error);
            _notice.EnableInClassList("is-visible", true);
            _notice.style.display = DisplayStyle.Flex;
            _notice.schedule.Execute(() =>
            {
                if (revision != _noticeRevision) return;
                _notice.EnableInClassList("is-visible", false);
                _notice.schedule.Execute(() =>
                {
                    if (revision == _noticeRevision)
                        _notice.style.display = DisplayStyle.None;
                }).StartingIn(180);
            }).StartingIn(Math.Max(800, durationMilliseconds));
        }

        private void Initialize(VisualElement root, IMainMenuPanelHost panelHost)
        {
            if (_initialized) return;
            _panelHost = panelHost;
            _root = root;
            _notice = Require<VisualElement>(root, "main-menu-notification");
            _noticeText = Require<Label>(root, "main-menu-notification-text");
            _initialized = true;
            _notice.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            _initialized = false;
        }

        private static bool HasOverlayContract(VisualElement root)
        {
            return root.Q<VisualElement>("main-menu-notification") != null &&
                root.Q<Label>("main-menu-notification-text") != null;
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"Main Menu UI is missing '{name}'.");
        }
    }
}
