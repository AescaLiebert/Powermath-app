using System.Collections;
using PowerMath.Bootstrap;
using PowerMath.Session;
using PowerMath.UI.Settings;
using UnityEngine;

namespace PowerMath.UI.Authentication
{
    [RequireComponent(typeof(AuthenticationView))]
    [RequireComponent(typeof(SceneFlowController))]
    public sealed class AuthenticationPresenter : MonoBehaviour
    {
        [SerializeField] private GameApiSettings apiSettings;

        private AuthenticationView _view;
        private SceneFlowController _sceneFlow;
        private IAuthenticationService _authenticationService;
        private bool _requestActive;

        public GameApiSettings ApiSettings => apiSettings;

        private void Awake()
        {
            _view = GetComponent<AuthenticationView>();
            _sceneFlow = GetComponent<SceneFlowController>();
            if (GetComponent<SettingsCompositionRoot>() == null)
            {
                gameObject.AddComponent<SettingsCompositionRoot>();
            }
        }

        private void OnEnable()
        {
            _view.LoginRequested += OnLoginRequested;
        }

        private void Start()
        {
            if (apiSettings == null)
            {
                _view.RenderFailure(
                    "The game service is not configured.",
                    false
                );
                return;
            }

#if UNITY_EDITOR
            if (apiSettings.UseEditorSampleStudent)
            {
                _authenticationService = new EditorMockAuthenticationService();
            }
            else
            {
                _authenticationService = new DirectFirestoreAuthenticationService(apiSettings);
            }
#else
            _authenticationService = new DirectFirestoreAuthenticationService(apiSettings);
#endif
            PowerMath.Audio.MusicController.Instance.PlayLoginMusic();

            if (DirectFirestoreCredentialStore.TryGet(out var saved) && saved.Remembered)
            {
                // Remembered credentials found — skip the login form entirely.
                _view.RenderBusy();
                _requestActive = true;
                StartCoroutine(Login(new AuthenticationView.LoginIntent(
                    saved.Username,
                    saved.Password,
                    rememberDevice: true
                )));
            }
            else
            {
                // No remembered session — show the form.
                // Pre-fill the username if one was stored (but not remembered),
                // so the player only needs to type their password.
                _view.RenderReady(saved.Username);
            }
        }

        private void OnDisable()
        {
            _view.LoginRequested -= OnLoginRequested;
        }

        private void OnLoginRequested(AuthenticationView.LoginIntent intent)
        {
            if (_requestActive || _authenticationService == null)
            {
                return;
            }

            _requestActive = true;
            _view.RenderBusy();
            StartCoroutine(Login(intent));
        }

        private IEnumerator Login(AuthenticationView.LoginIntent intent)
        {
            yield return _authenticationService.Login(
                intent.Username,
                intent.Password,
                intent.RememberDevice,
                OnLoginSucceeded,
                OnLoginFailed
            );
        }

        private void OnLoginSucceeded()
        {
            PowerMath.Audio.SfxController.Instance.PlayAuthentication(PowerMath.Audio.AuthenticationSfxState.Success);
            _view.RenderSuccess();
            if (!_sceneFlow.TryLoadScene(
                apiSettings.BootstrapSceneName,
                OnSceneLoadFailed))
            {
                _requestActive = false;
            }
        }

        private void OnLoginFailed(FirestoreRestClient.Failure failure)
        {
            _requestActive = false;
            PowerMath.Audio.SfxController.Instance.PlayAuthentication(PowerMath.Audio.AuthenticationSfxState.Failure);
            bool clearPassword = failure.Kind ==
                FirestoreRestClient.FailureKind.InvalidCredentials;
            _view.RenderFailure(failure.PlayerMessage, clearPassword);
        }

        private void OnSceneLoadFailed(string playerMessage)
        {
            _requestActive = false;
            PowerMath.Audio.SfxController.Instance.PlayAuthentication(PowerMath.Audio.AuthenticationSfxState.Failure);
            _view.RenderFailure(playerMessage, false);
        }
    }
}
