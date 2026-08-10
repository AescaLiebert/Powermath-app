using System.Collections;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;

namespace PowerMath.Bootstrap
{
    [RequireComponent(typeof(BootstrapView))]
    [RequireComponent(typeof(SceneFlowController))]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Direct Firestore REST and scene settings approved by ADR-003.")]
        [SerializeField] private GameApiSettings apiSettings;

        private BootstrapView _view;
        private SceneFlowController _sceneFlow;
        private PlayerSessionStore _sessionStore;
        private IPlayerBootstrapService _bootstrapService;
        private bool _isBootstrapping;
        private bool _sceneLoadRequested;

        private void Awake()
        {
            _view = GetComponent<BootstrapView>();
            _sceneFlow = GetComponent<SceneFlowController>();
            _sessionStore = PlayerSessionStore.Instance;

            if (_sessionStore == null)
            {
                _sessionStore = FindAnyObjectByType<PlayerSessionStore>();
            }
        }

        private void OnEnable()
        {
            _view.RetryRequested += OnRetryRequested;
        }

        private void Start()
        {
            if (apiSettings == null)
            {
                _view.Render(
                    BootstrapState.Recovering,
                    "Assign GameApiSettings on the Bootstrap object."
                );
                return;
            }

            if (_sessionStore == null)
            {
                _view.Render(
                    BootstrapState.Recovering,
                    "The player session store is unavailable."
                );
                return;
            }

#if UNITY_EDITOR
            _bootstrapService = apiSettings.UseEditorSampleStudent
                ? new EditorMockPlayerBootstrapService()
                : new DirectFirestorePlayerBootstrapService(apiSettings);
#else
            _bootstrapService = new DirectFirestorePlayerBootstrapService(apiSettings);
#endif
            BeginBootstrap();
        }

        private void OnDisable()
        {
            _view.RetryRequested -= OnRetryRequested;
        }

        private void BeginBootstrap()
        {
            if (_isBootstrapping || _sceneLoadRequested || _bootstrapService == null)
            {
                return;
            }

            _isBootstrapping = true;
            _view.Render(BootstrapState.CheckingSession);
            StartCoroutine(FetchPlayer());
        }

        private IEnumerator FetchPlayer()
        {
            yield return _bootstrapService.Fetch(
                OnBootstrapSucceeded,
                OnBootstrapFailed
            );
        }

        private void OnBootstrapSucceeded(BootstrapResponse response)
        {
            _view.Render(BootstrapState.LoadingPlayer);

            PlayerSessionStore.HydrationResult result =
                _sessionStore.TryHydrate(response);

            if (result == PlayerSessionStore.HydrationResult.IncompatibleSchema)
            {
                _isBootstrapping = false;
                _view.Render(
                    BootstrapState.IncompatibleClient,
                    "Refresh the page after the latest game is deployed."
                );
                return;
            }

            if (result != PlayerSessionStore.HydrationResult.Success)
            {
                _isBootstrapping = false;
                _sessionStore.Clear();
                _view.Render(
                    BootstrapState.Recovering,
                    "The server returned incomplete player data."
                );
                return;
            }

            _view.Render(BootstrapState.Ready);
            LoadScene(apiSettings.MainMenuSceneName);
        }

        private void OnBootstrapFailed(FirestoreRestClient.Failure failure)
        {
            _isBootstrapping = false;

            if (failure.Kind == FirestoreRestClient.FailureKind.AuthenticationRequired)
            {
                _sessionStore.Clear();
                _view.Render(BootstrapState.AuthenticationRequired);
                LoadScene(apiSettings.AuthenticationSceneName);
                return;
            }

            _view.Render(BootstrapState.Recovering, failure.PlayerMessage);
        }

        private void LoadScene(string sceneName)
        {
            if (_sceneLoadRequested)
            {
                return;
            }

            _sceneLoadRequested = true;
            _view.Render(BootstrapState.LoadingScene);

            if (!_sceneFlow.TryLoadScene(sceneName, OnSceneLoadFailed))
            {
                _sceneLoadRequested = false;
            }
        }

        private void OnSceneLoadFailed(string playerMessage)
        {
            _sceneLoadRequested = false;
            _isBootstrapping = false;
            _view.Render(BootstrapState.Recovering, playerMessage);
        }

        private void OnRetryRequested()
        {
            BeginBootstrap();
        }
    }
}
