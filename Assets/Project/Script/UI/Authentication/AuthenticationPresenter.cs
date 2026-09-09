using System.Collections;
using PowerMath.Bootstrap;
using PowerMath.Session;
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

        private void Awake()
        {
            _view = GetComponent<AuthenticationView>();
            _sceneFlow = GetComponent<SceneFlowController>();
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
            _view.RenderReady();
            PowerMath.Audio.MusicController.Instance.PlayLoginMusic();
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
            bool clearPassword = failure.Kind ==
                FirestoreRestClient.FailureKind.InvalidCredentials;
            _view.RenderFailure(failure.PlayerMessage, clearPassword);
        }

        private void OnSceneLoadFailed(string playerMessage)
        {
            _requestActive = false;
            _view.RenderFailure(playerMessage, false);
        }
    }
}
