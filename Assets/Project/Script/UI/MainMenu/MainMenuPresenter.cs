using System.Collections;
using PowerMath.Bootstrap;
using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.UI.MainMenu.SocialProfile;
using PowerMath.UI.Settings;
using UnityEngine;

namespace PowerMath.UI.MainMenu
{
    [RequireComponent(typeof(MainMenuView))]
    [RequireComponent(typeof(SceneFlowController))]
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [SerializeField] private GameApiSettings apiSettings;

        private MainMenuView _view;
        private SceneFlowController _sceneFlow;
        private PlayerSessionStore _sessionStore;
        private IAuthenticationService _authenticationService;
        private IMainMenuInteractionGate _interactionGate;
        private PlayerSnapshot _pendingSnapshot;
        private bool _logoutActive;

        public GameApiSettings ApiSettings => apiSettings;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            _view = GetComponent<MainMenuView>();
            _sceneFlow = GetComponent<SceneFlowController>();

            if (GetComponent<MainMenuPanelHostProvider>() == null)
            {
                gameObject.AddComponent<MainMenuPanelHostProvider>();
            }
            if (GetComponent<CombatLobbyCompositionRoot>() == null)
            {
                // Existing scenes predate ADR-004; runtime attachment keeps the
                // compatibility bridge isolated until the scene asset is resaved.
                gameObject.AddComponent<CombatLobbyCompositionRoot>();
            }
            if (GetComponent<SocialProfileCompositionRoot>() == null)
            {
                gameObject.AddComponent<SocialProfileCompositionRoot>();
            }
            if (GetComponent<SettingsCompositionRoot>() == null)
            {
                gameObject.AddComponent<SettingsCompositionRoot>();
            }
            if (GetComponent<ProfileActivityTracker>() == null)
            {
                gameObject.AddComponent<ProfileActivityTracker>();
            }
            if (GetComponent<MainMenuInteractionGateProvider>() == null)
            {
                gameObject.AddComponent<MainMenuInteractionGateProvider>();
            }
            if (GetComponent<MainMenuTransitionController>() == null)
            {
                gameObject.AddComponent<MainMenuTransitionController>();
            }
        }

        private void OnEnable()
        {
            _view.LogoutRequested += OnLogoutRequested;
            _sessionStore = PlayerSessionStore.Instance;
            _interactionGate = GetComponent<MainMenuInteractionGateProvider>()?.Gate;
            if (_interactionGate != null)
            {
                _interactionGate.Changed += OnInteractionGateChanged;
            }

            if (_sessionStore == null || !_sessionStore.IsReady)
            {
                _view.RenderUnavailable();
                GetComponent<MainMenuTransitionController>()?
                    .NotifyRecoveryReady();
                PowerMath.Diagnostics.AppLog.Error(
                    "UI",
                    "MainMenuScene requires a successful BootstrapScene player session."
                );
                return;
            }

            _sessionStore.Changed += OnPlayerSessionChanged;
            _view.Render(MainMenuViewModel.From(_sessionStore.Snapshot));
            NotifyMainMenuReady();

            if (apiSettings == null)
            {
                _view.RenderLogoutFailure("Sign out is not configured.");
                return;
            }

#if UNITY_EDITOR
            _authenticationService = apiSettings.UseEditorSampleStudent
                ? (IAuthenticationService)new EditorMockAuthenticationService()
                : new DirectFirestoreAuthenticationService(apiSettings);
#else
            _authenticationService = new DirectFirestoreAuthenticationService(apiSettings);
#endif
        }

        private void OnDisable()
        {
            _view.LogoutRequested -= OnLogoutRequested;
            if (_interactionGate != null)
            {
                _interactionGate.Changed -= OnInteractionGateChanged;
            }
            if (_sessionStore != null)
            {
                _sessionStore.Changed -= OnPlayerSessionChanged;
            }
        }

        private void OnPlayerSessionChanged(PlayerSnapshot snapshot)
        {
            if (snapshot == null)
            {
                _pendingSnapshot = null;
                _view.RenderUnavailable();
                GetComponent<MainMenuTransitionController>()?
                    .NotifyRecoveryReady();
                return;
            }

            if (_interactionGate != null && !_interactionGate.IsAllowed(InteractionScope.All))
            {
                _pendingSnapshot = snapshot;
                return;
            }

            _pendingSnapshot = null;
            _view.Render(MainMenuViewModel.From(snapshot));
            NotifyMainMenuReady();
        }

        private void OnInteractionGateChanged(InteractionGateSnapshot snapshot)
        {
            if (_pendingSnapshot != null && _interactionGate != null && _interactionGate.IsAllowed(InteractionScope.All))
            {
                PlayerSnapshot pending = _pendingSnapshot;
                _pendingSnapshot = null;
                _view.Render(MainMenuViewModel.From(pending));
                NotifyMainMenuReady();
            }
        }

        private void NotifyMainMenuReady()
        {
            // Main-menu visibility belongs to the hydrated session shell.
            // Feature initialization (including Combat Lobby) may finish later
            // or enter its own unavailable state without blocking the menu.
            GetComponent<MainMenuTransitionController>()?.NotifySessionReady();
        }

        private void OnLogoutRequested()
        {
            if (_logoutActive || _authenticationService == null)
            {
                return;
            }

            _logoutActive = true;
            _view.RenderLogoutBusy();
            StartCoroutine(Logout());
        }

        private IEnumerator Logout()
        {
            yield return _authenticationService.Logout(
                OnLogoutSucceeded,
                OnLogoutFailed
            );
        }

        private void OnLogoutSucceeded()
        {
            _sessionStore?.Clear();
            if (!_sceneFlow.TryLoadScene(
                apiSettings.AuthenticationSceneName,
                OnSceneLoadFailed))
            {
                _logoutActive = false;
            }
        }

        private void OnLogoutFailed(FirestoreRestClient.Failure failure)
        {
            _logoutActive = false;
            _view.RenderLogoutFailure(failure.PlayerMessage);
        }

        private void OnSceneLoadFailed(string playerMessage)
        {
            _logoutActive = false;
            _view.RenderLogoutFailure(playerMessage);
        }
    }
}
