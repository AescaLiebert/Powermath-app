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
        private PowerMath.PlayerLifecycle.PlayerPreparationPresenter _preparationPresenter;
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
                ? (IPlayerBootstrapService)new EditorMockPlayerBootstrapService()
                : new DirectFirestorePlayerBootstrapService(apiSettings);
#else
            _bootstrapService = new DirectFirestorePlayerBootstrapService(apiSettings);
#endif
            PowerMath.PlayerLifecycle.PlayerLifecycleRuntime.Configure(apiSettings);
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

#if UNITY_EDITOR
            if (apiSettings.UseEditorSampleStudent)
            {
                _view.Render(BootstrapState.CheckingSession);
                StartCoroutine(FetchPlayer());
                return;
            }
#endif

            if (apiSettings.EnableVersionCheck && string.IsNullOrWhiteSpace(apiSettings.VersionManifestUrl))
            {
                _isBootstrapping = false;
                _view.Render(BootstrapState.Recovering, PowerMath.Localization.LocalizationService.Get("errors.policy"));
                return;
            }
            if (apiSettings.EnableVersionCheck)
            {
                _view.Render(BootstrapState.CheckingVersion);
                StartCoroutine(CheckVersionThenFetchPlayer());
            }
            else
            {
                _view.Render(BootstrapState.CheckingSession);
                StartCoroutine(FetchPlayer());
            }
        }

        private IEnumerator CheckVersionThenFetchPlayer()
        {
            GameVersionManifest manifest = null;
            string fetchError = null;

            yield return GameVersionChecker.FetchManifest(
                apiSettings.VersionManifestUrl,
                apiSettings.RequestTimeoutSeconds,
                result => manifest = result,
                error => fetchError = error
            );

            if (manifest != null)
            {
                _sessionStore?.SetVersionManifest(manifest);
                var result = GameVersionChecker.EvaluateCompatibility(
                    manifest,
                    Application.version,
                    PlayerSessionStore.SupportedSchemaVersion,
                    out string statusMessage
                );

                if (result == VersionCompatibilityResult.NetworkError)
                {
                    _isBootstrapping = false;
                    _view.Render(BootstrapState.Recovering, PowerMath.Localization.LocalizationService.Get("errors.policy"));
                    yield break;
                }
                if (result == VersionCompatibilityResult.MaintenanceActive)
                {
                    _isBootstrapping = false;
                    _view.Render(BootstrapState.MaintenanceMode, statusMessage);
                    yield break;
                }

                if (result == VersionCompatibilityResult.HardUpdateRequired ||
                    result == VersionCompatibilityResult.IncompatibleSchema)
                {
                    _isBootstrapping = false;
                    _view.Render(
                        BootstrapState.IncompatibleClient,
                        statusMessage + " Refreshing the page to update..."
                    );
                    WebCacheBridge.PurgeCacheAndReload(manifest.clientVersion);
                    yield break;
                }

                if (result == VersionCompatibilityResult.UpdateRecommended)
                {
                    PowerMath.Diagnostics.AppLog.Info("Bootstrap", $"Soft update available: {statusMessage}");
                }
            }
            else if (!string.IsNullOrWhiteSpace(fetchError))
            {
                _isBootstrapping = false;
                _view.Render(BootstrapState.Recovering, PowerMath.Localization.LocalizationService.Get("errors.policy"));
                yield break;
            }

            _view.Render(BootstrapState.CheckingSession);
            yield return FetchPlayer();
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

            StartCoroutine(PreparePlayer());
        }

        private IEnumerator PreparePlayer()
        {
            if (!PlayerLifecyclePolicy.IsLocale(_sessionStore.Snapshot.preferences?.locale))
            {
                PlayerSnapshot saved = null;
                yield return PowerMath.PlayerLifecycle.PlayerLifecycleRuntime.Commands.Execute(
                    new PlayerLifecycleCommand
                    {
                        kind = PlayerLifecycleCommandKind.SetLocale,
                        operationId = System.Guid.NewGuid().ToString("N"),
                        playerId = _sessionStore.Snapshot.playerId,
                        expectedRevision = _sessionStore.Snapshot.revision,
                        value = PowerMath.Localization.LocalizationService.Locale
                    }, player => saved = player, _ => { });
                if (saved == null)
                {
                    _isBootstrapping = false;
                    _view.Render(BootstrapState.Recovering, PowerMath.Localization.LocalizationService.Get("errors.save"));
                    yield break;
                }
                _sessionStore.TryHydrate(new BootstrapResponse
                { player = saved, schemaVersion = saved.schemaVersion, remembered = _sessionStore.IsRemembered });
            }
            _view.Render(BootstrapState.Ready);
            if (!PowerMath.Session.PlayerLifecyclePolicy.IsComplete(_sessionStore.Snapshot))
            {
                _preparationPresenter = gameObject.AddComponent<PowerMath.PlayerLifecycle.PlayerPreparationPresenter>();
                _preparationPresenter.DestinationTransitionCompleted += OnPreparedTransitionCompleted;
                _preparationPresenter.Initialize(
                    GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement,
                    _sessionStore, PowerMath.PlayerLifecycle.PlayerLifecycleRuntime.Commands,
                    BeginPreparedPlayerTransition);
                yield break;
            }
            LoadScene(apiSettings.MainMenuSceneName);
        }

        private void BeginPreparedPlayerTransition()
        {
            if (_sceneLoadRequested)
            {
                _preparationPresenter?.NotifyDestinationSceneLoadFailed();
                return;
            }

            _sceneLoadRequested = true;
            _view.Render(BootstrapState.LoadingScene);
            bool started = _sceneFlow.TryLoadScene(
                apiSettings.MainMenuSceneName,
                OnSceneLoadFailed,
                () => _preparationPresenter?.NotifyDestinationSceneLoaded());
            if (!started)
            {
                _sceneLoadRequested = false;
                _preparationPresenter?.NotifyDestinationSceneLoadFailed();
                return;
            }

            DontDestroyOnLoad(gameObject);
            var document = GetComponent<UnityEngine.UIElements.UIDocument>();
            if (document != null)
                document.sortingOrder = 10000;
        }

        private void OnPreparedTransitionCompleted()
        {
            if (_preparationPresenter != null)
                _preparationPresenter.DestinationTransitionCompleted -= OnPreparedTransitionCompleted;
            Destroy(gameObject);
        }

        private void OnBootstrapFailed(FirestoreRestClient.Failure failure)
        {
            _isBootstrapping = false;

            if (failure.Kind == FirestoreRestClient.FailureKind.IncompatibleClient)
            {
                _view.Render(BootstrapState.IncompatibleClient, failure.PlayerMessage);
                return;
            }
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
            _preparationPresenter?.NotifyDestinationSceneLoadFailed();
        }

        private void OnRetryRequested()
        {
            BeginBootstrap();
        }
    }
}
