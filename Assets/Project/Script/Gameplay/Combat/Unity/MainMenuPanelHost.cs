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

        bool TryOpen(
            MainMenuPanelId panelId,
            VisualElement panelRoot,
            Focusable opener);

        bool TryClose(MainMenuPanelId panelId, Focusable fallbackFocus);

        void ForceCloseAll();
    }

    public sealed class MainMenuPanelHost : IMainMenuPanelHost
    {
        private VisualElement _openPanelRoot;
        private Focusable _opener;

        public MainMenuPanelId OpenPanel { get; private set; }

        public bool TryOpen(
            MainMenuPanelId panelId,
            VisualElement panelRoot,
            Focusable opener)
        {
            if (panelId == MainMenuPanelId.None || panelRoot == null)
            {
                return false;
            }

            if (OpenPanel != MainMenuPanelId.None)
            {
                return OpenPanel == panelId && _openPanelRoot == panelRoot;
            }

            OpenPanel = panelId;
            _openPanelRoot = panelRoot;
            _opener = opener;
            SetVisible(panelRoot, true);
            panelRoot.Focus();
            return true;
        }

        public bool TryClose(
            MainMenuPanelId panelId,
            Focusable fallbackFocus)
        {
            if (OpenPanel != panelId || _openPanelRoot == null)
            {
                return false;
            }

            Focusable focusTarget = fallbackFocus ?? _opener;
            SetVisible(_openPanelRoot, false);
            ClearOpenPanel();
            focusTarget?.Focus();
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
            SetVisible(_openPanelRoot, false);
            ClearOpenPanel();
            focusTarget?.Focus();
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
            _opener = null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class MainMenuPanelHostProvider : MonoBehaviour
    {
        public IMainMenuPanelHost Host { get; } = new MainMenuPanelHost();

        private void OnDisable()
        {
            Host.ForceCloseAll();
        }
    }
}
