using System.Collections;
using PowerMath.Bootstrap;
using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.UI.MainMenu.SocialProfile;
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
        private bool _logoutActive;

        public GameApiSettings ApiSettings => apiSettings;

        private void Awake()
        {
            _view = GetComponent<MainMenuView>();
            _sceneFlow = GetComponent<SceneFlowController>();

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
            if (GetComponent<ProfileActivityTracker>() == null)
            {
                gameObject.AddComponent<ProfileActivityTracker>();
            }
        }

        private void OnEnable()
        {
            _view.LogoutRequested += OnLogoutRequested;
            _sessionStore = PlayerSessionStore.Instance;

            if (_sessionStore == null || !_sessionStore.IsReady)
            {
                _view.RenderUnavailable();
                Debug.LogError(
                    "MainMenuScene requires a successful BootstrapScene player session."
                );
                return;
            }

            _sessionStore.Changed += OnPlayerSessionChanged;
            _view.Render(MainMenuViewModel.From(_sessionStore.Snapshot));

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
            if (_sessionStore != null)
            {
                _sessionStore.Changed -= OnPlayerSessionChanged;
            }
        }

        private void OnPlayerSessionChanged(PlayerSnapshot snapshot)
        {
            if (snapshot == null)
            {
                _view.RenderUnavailable();
                return;
            }

            _view.Render(MainMenuViewModel.From(snapshot));
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
