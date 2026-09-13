using PowerMath.PlayerData;
using PowerMath.Localization;
using PowerMath.UI.Authentication;
using PowerMath.UI.MainMenu;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Settings
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsCompositionRoot : MonoBehaviour
    {
        private SettingsPanelController _controller;

        private void Start()
        {
            UIDocument document = GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null) return;

            MainMenuPresenter mainMenu = GetComponent<MainMenuPresenter>();
            AuthenticationPresenter authentication =
                GetComponent<AuthenticationPresenter>();
            IMainMenuPanelHost panelHost =
                GetComponent<MainMenuPanelHostProvider>()?.Host;
            PlayerSnapshot player = mainMenu == null
                ? null
                : PlayerSessionStore.Instance?.Snapshot;
            _controller = new SettingsPanelController(
                document.rootVisualElement,
                this,
                panelHost,
                mainMenu != null
                    ? mainMenu.ApiSettings
                    : authentication?.ApiSettings,
                player);
            if (!_controller.IsValid)
            {
                PowerMath.Diagnostics.AppLog.Error(
                    "Settings",
                    "Shared Settings panel UI elements are missing.");
                return;
            }

            _controller.Bind();
            PowerMath.Audio.UiSfxAudioBinder.Bind(document.rootVisualElement);
            LocalizedDocument localized = GetComponent<LocalizedDocument>();
            if (localized == null) localized = gameObject.AddComponent<LocalizedDocument>();
            localized.Bind(document.rootVisualElement);
        }

        private void OnDisable()
        {
            _controller?.Dispose();
            _controller = null;
        }
    }
}
