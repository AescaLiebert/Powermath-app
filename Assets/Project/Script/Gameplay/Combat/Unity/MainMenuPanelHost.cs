using System;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public enum MainMenuPanelId
    {
        None,
        WorldMap,
        PlayerHub,
        Rebirth,
        PetGacha,
        Leaderboard,
        ProfileAnalytics,
        Settings
    }

    public interface IMainMenuPanelHost
    {
        MainMenuPanelId OpenPanel { get; }
        event Action<MainMenuPanelId> PanelOpened;
        event Action<MainMenuPanelId> PanelClosed;

        bool TryOpen(
            MainMenuPanelId panelId,
            VisualElement panelRoot,
            Focusable opener);

        bool TryClose(MainMenuPanelId panelId, Focusable fallbackFocus);

        bool TryCloseCurrent();

        void ForceCloseAll();
    }

    public sealed class MainMenuPanelHost : IMainMenuPanelHost
    {
        private readonly IMainMenuInteractionGate _interactionGate;
        private readonly IUiMotionDriver _motionDriver;
        private readonly UiMotionProfileDefinition _motionProfile;
        private VisualElement _openPanelRoot;
        private IUiPanelLifecycle _openPanelLifecycle;
        private Focusable _opener;
        private bool _closing;

        public MainMenuPanelHost(
            IMainMenuInteractionGate interactionGate = null,
            IUiMotionDriver motionDriver = null,
            UiMotionProfileDefinition motionProfile = null)
        {
            _interactionGate = interactionGate;
            _motionDriver = motionDriver;
            _motionProfile = motionProfile;
        }

        public MainMenuPanelId OpenPanel { get; private set; }
        public event Action<MainMenuPanelId> PanelOpened;
        public event Action<MainMenuPanelId> PanelClosed;

        public bool TryOpen(
            MainMenuPanelId panelId,
            VisualElement panelRoot,
            Focusable opener)
        {
            if (panelId == MainMenuPanelId.None || panelRoot == null)
            {
                return false;
            }

            bool terminalPanel = panelId == MainMenuPanelId.Rebirth &&
                _interactionGate != null &&
                _interactionGate.IsAllowed(InteractionScope.TerminalAction);
            if (_interactionGate != null && !terminalPanel &&
                !_interactionGate.IsAllowed(InteractionScope.Navigation))
            {
                return false;
            }

            if (_closing)
            {
                if (OpenPanel != panelId || _openPanelRoot != panelRoot ||
                    _openPanelLifecycle == null)
                {
                    return false;
                }

                _closing = false;
                EnterAndFocusWhenIdle(
                    panelRoot,
                    _openPanelLifecycle);
                return true;
            }

            if (OpenPanel != MainMenuPanelId.None)
            {
                return OpenPanel == panelId && _openPanelRoot == panelRoot;
            }

            OpenPanel = panelId;
            _openPanelRoot = panelRoot;
            _opener = opener;
            if (_motionDriver != null && _motionProfile != null)
            {
                _openPanelLifecycle = new UiPanelLifecycle(
                    panelRoot,
                    _motionDriver,
                    _motionProfile);
                EnterAndFocusWhenIdle(
                    panelRoot,
                    _openPanelLifecycle);
            }
            else
            {
                SetVisible(panelRoot, true);
                panelRoot.Focus();
            }
            PanelOpened?.Invoke(panelId);
            return true;
        }

        public bool TryCloseCurrent()
        {
            return OpenPanel != MainMenuPanelId.None &&
                TryClose(OpenPanel, _opener);
        }

        public bool TryClose(
            MainMenuPanelId panelId,
            Focusable fallbackFocus)
        {
            if (OpenPanel != panelId || _openPanelRoot == null || _closing)
            {
                return false;
            }

            Focusable focusTarget = fallbackFocus ?? _opener;
            if (_openPanelLifecycle == null)
            {
                SetVisible(_openPanelRoot, false);
                CompleteClose(panelId, focusTarget, null);
                return true;
            }

            _closing = true;
            IUiPanelLifecycle lifecycle = _openPanelLifecycle;
            lifecycle.Exit(() => CompleteClose(
                panelId,
                focusTarget,
                lifecycle));
            return true;
        }

        public void ForceCloseAll()
        {
            if (_openPanelRoot == null)
            {
                ClearOpenPanel();
                return;
            }

            Focusable focusTarget = _opener;
            if (_openPanelLifecycle != null)
            {
                _openPanelLifecycle.CancelAndApply(
                    UiMotionEndState.ApplyHidden);
                _openPanelLifecycle.Dispose();
            }
            else
            {
                SetVisible(_openPanelRoot, false);
            }
            ClearOpenPanel();
            focusTarget?.Focus();
        }

        private void CompleteClose(
            MainMenuPanelId panelId,
            Focusable focusTarget,
            IUiPanelLifecycle lifecycle)
        {
            if (lifecycle != null &&
                !ReferenceEquals(lifecycle, _openPanelLifecycle))
            {
                return;
            }

            lifecycle?.Dispose();
            ClearOpenPanel();
            focusTarget?.Focus();
            PanelClosed?.Invoke(panelId);
        }

        private void EnterAndFocusWhenIdle(
            VisualElement panelRoot,
            IUiPanelLifecycle lifecycle)
        {
            lifecycle.Enter(() =>
            {
                if (!_closing &&
                    ReferenceEquals(_openPanelRoot, panelRoot) &&
                    ReferenceEquals(_openPanelLifecycle, lifecycle))
                {
                    panelRoot.Focus();
                }
            });
        }

        private static void SetVisible(VisualElement panel, bool visible)
        {
            panel.EnableInClassList("is-hidden", !visible);
            panel.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void ClearOpenPanel()
        {
            OpenPanel = MainMenuPanelId.None;
            _openPanelRoot = null;
            _openPanelLifecycle = null;
            _opener = null;
            _closing = false;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MainMenuPanelHostProvider : MonoBehaviour
    {
        public IMainMenuPanelHost Host { get; private set; }

        private void Awake()
        {
            MainMenuInteractionGateProvider gateProvider =
                GetComponent<MainMenuInteractionGateProvider>();
            if (gateProvider == null)
                gateProvider = gameObject.AddComponent<MainMenuInteractionGateProvider>();
            UiMotionDriverProvider motionProvider =
                GetComponent<UiMotionDriverProvider>();
            if (motionProvider == null)
                motionProvider = gameObject.AddComponent<UiMotionDriverProvider>();
            Host = new MainMenuPanelHost(
                gateProvider.Gate,
                motionProvider.Driver,
                motionProvider.Profile);
        }

        private void OnDisable()
        {
            Host?.ForceCloseAll();
        }
    }
}
