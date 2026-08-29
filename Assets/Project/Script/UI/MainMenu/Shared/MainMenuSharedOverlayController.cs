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
        private VisualElement _bar;
        private Button _back;
        private Label _powerCoins;
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
            if (_back != null) _back.SetEnabled(enabled);
        }

        public void Publish(
            string message,
            MainMenuNoticeKind kind = MainMenuNoticeKind.Information,
            int durationMilliseconds = 2400)
        {
            if (!_initialized || string.IsNullOrWhiteSpace(message)) return;
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
            _bar = Require<VisualElement>(root, "main-menu-utility-bar");
            _back = Require<Button>(root, "main-menu-utility-back");
            _powerCoins = Require<Label>(root, "main-menu-utility-power-coins");
            _notice = Require<VisualElement>(root, "main-menu-notification");
            _noticeText = Require<Label>(root, "main-menu-notification-text");
            _back.clicked += Back;
            _panelHost.PanelOpened += OnPanelOpened;
            _panelHost.PanelClosed += OnPanelClosed;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;
            _initialized = true;
            _bar.style.display = DisplayStyle.None;
            _notice.style.display = DisplayStyle.None;
            OnPlayerChanged(PlayerSessionStore.Instance?.Snapshot);
        }

        private void OnDestroy()
        {
            if (!_initialized) return;
            _back.clicked -= Back;
            _panelHost.PanelOpened -= OnPanelOpened;
            _panelHost.PanelClosed -= OnPanelClosed;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
        }

        private void Back()
        {
            if (!_back.enabledSelf) return;
            Button localClose = ResolveLocalClose(_panelHost.OpenPanel);
            if (localClose != null && !localClose.enabledSelf)
            {
                Publish("Please wait for the current action to finish.",
                    MainMenuNoticeKind.Information);
                return;
            }
            _panelHost.TryCloseCurrent();
        }

        private Button ResolveLocalClose(MainMenuPanelId panelId)
        {
            string elementName;
            switch (panelId)
            {
                case MainMenuPanelId.WorldMap: elementName = "combat-map-close"; break;
                case MainMenuPanelId.PlayerHub: elementName = "player-hub-close"; break;
                case MainMenuPanelId.ProfileAnalytics: elementName = "profile-analytics-close"; break;
                case MainMenuPanelId.PetGacha: elementName = "pet-gacha-close"; break;
                case MainMenuPanelId.Leaderboard: elementName = "leaderboard-close"; break;
                default: return null;
            }
            return _root?.Q<Button>(elementName);
        }

        private void OnPanelOpened(MainMenuPanelId panelId)
        {
            bool visible = IsSharedPanel(panelId);
            _bar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            SetBackEnabled(true);
        }

        private void OnPanelClosed(MainMenuPanelId _)
        {
            _bar.style.display = DisplayStyle.None;
            SetBackEnabled(true);
        }

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            long powerCoins = player?.wallet?.powerCoins ?? 0;
            _powerCoins.text = $"⚡ {powerCoins:N0}";
        }

        private static bool IsSharedPanel(MainMenuPanelId panelId)
        {
            return panelId == MainMenuPanelId.WorldMap ||
                panelId == MainMenuPanelId.PlayerHub ||
                panelId == MainMenuPanelId.ProfileAnalytics ||
                panelId == MainMenuPanelId.PetGacha ||
                panelId == MainMenuPanelId.Leaderboard;
        }

        private static bool HasOverlayContract(VisualElement root)
        {
            return root.Q<VisualElement>("main-menu-utility-bar") != null &&
                root.Q<Button>("main-menu-utility-back") != null &&
                root.Q<Label>("main-menu-utility-power-coins") != null &&
                root.Q<VisualElement>("main-menu-notification") != null &&
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
