using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerMath.Bootstrap;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Combat.Unity;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using LegacyImage = UnityEngine.UI.Image;

namespace PowerMath.UI.MainMenu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombatLobbyCompositionRoot : MonoBehaviour, ICombatCoroutineRunner
    {
        [Header("Content")]
        [Tooltip("Data-defined enemy. A runtime placeholder is used until the asset is assigned.")]
        [SerializeField] private EnemyDefinition enemyDefinition;

        [Tooltip("Seven-biome Stage Map. A complete development map is used until assigned.")]
        [SerializeField] private StageMapDefinition stageMapDefinition;

        [Tooltip("Development simulation and answer-window settings.")]
        [SerializeField] private CombatRuntimeSettingsDefinition runtimeSettings;

        [Tooltip("Optional explicit enemy texture. The existing scene monster image is used when empty.")]
        [SerializeField] private Texture2D enemyTexture;

        [Tooltip("Ordered visual/name milestones for the persistent Weapon Ascend system.")]
        [SerializeField] private WeaponAscensionCatalogDefinition weaponAscensionCatalog;

        [Tooltip("Versioned rarity rates and production pet presentation for Pet Gacha.")]
        [SerializeField] private PetGachaCatalogDefinition petGachaCatalog;

        [Tooltip("Optional reusable TMP FCT prefab. Runtime fallback is used until assigned.")]
        [SerializeField] private FloatingCombatTextView floatingCombatTextPrefab;

        [Tooltip("FCT text style, semantic colors, pooling, and Pop/Hold/Exit motion.")]
        [SerializeField] private FloatingCombatTextStyleDefinition floatingCombatTextStyle;

        [Tooltip("Actor timing/motion and critical-impact tuning. Runtime defaults are used until assigned.")]
        [SerializeField] private CombatJuiceProfileDefinition combatJuiceProfile;

        [Header("Combat Text Anchoring")]
        [Tooltip("Normalized anchor within enemy RectTransform for FCT spawn (0.5, 0.5 = center).")]
        [SerializeField] private Vector2 enemyFctNormalizedAnchor = new Vector2(0.5f, 0.5f);

        [Tooltip("Pixel offset added to the enemy FCT spawn position.")]
        [SerializeField] private Vector2 enemyFctOffset = Vector2.zero;

        private CombatLobbyPresenter _presenter;
        private CombatLobbyView _view;
        private IMainMenuPanelHost _panelHost;
        private IQuestionCatalogRepository _questionCatalogRepository;
        private FirestoreEventQuestionCatalogRepository _eventQuestionRepository;
        private RunEconomyPanelController _runEconomyController;
        private LegacyImage _sceneBackground;
        private LegacyImage _sceneBackgroundTransition;
        private LegacyImage _sceneEnemy;
        private LegacyImage _scenePlayer;
        private FloatingCombatTextService _floatingText;
        private ICombatAnchor _enemyDamageAnchor;
        private CombatWorldImpulsePlayer _impactImpulse;
        private ActorPresentationController _playerActor;
        private ActorPresentationController _enemyActor;
        private IMainMenuInteractionGate _interactionGate;
        private InteractionShieldView _interactionShield;
        private UiSceneContext _uiContext;
        private Sprite _runtimeEnemySprite;
        private RewardMagnetFeedbackPlayer _rewardMagnet;
        private int _questionCatalogLoadGeneration;
        private bool _questionCatalogLoadCompleted;

        private void Start()
        {
            if (stageMapDefinition == null)
                stageMapDefinition = Resources.Load<StageMapDefinition>("StageMapDefinition");
            if (enemyDefinition == null)
                enemyDefinition = Resources.Load<EnemyDefinition>("EnemyDefinition");
            if (runtimeSettings == null)
                runtimeSettings = Resources.Load<CombatRuntimeSettingsDefinition>(
                    "CombatRuntimeSettings");
            if (weaponAscensionCatalog == null)
                weaponAscensionCatalog = Resources.Load<WeaponAscensionCatalogDefinition>("WeaponAscensionCatalog");
            if (petGachaCatalog == null)
                petGachaCatalog = Resources.Load<PetGachaCatalogDefinition>("PetGachaCatalog");
            if (combatJuiceProfile == null)
                combatJuiceProfile = Resources.Load<CombatJuiceProfileDefinition>(
                    "CombatJuiceProfile");
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document?.rootVisualElement;
            if (root == null)
            {
                PowerMath.Diagnostics.AppLog.Error("Combat", "Combat Lobby requires the Main Menu UIDocument.");
                return;
            }
            root.pickingMode = PickingMode.Ignore;

            try
            {
                MainMenuPanelHostProvider provider =
                    GetComponent<MainMenuPanelHostProvider>();
                if (provider == null)
                    provider = gameObject.AddComponent<MainMenuPanelHostProvider>();
                _panelHost = provider.Host;
                UiMotionDriverProvider motionProvider =
                    GetComponent<UiMotionDriverProvider>();
                motionProvider.SetReducedMotion(
                    runtimeSettings != null && runtimeSettings.ReducedMotion);
                _uiContext = new UiSceneContext(
                    root,
                    motionProvider.Driver,
                    motionProvider.Profile);
                MainMenuSharedOverlayController.GetOrCreate(
                    gameObject,
                    root,
                    _panelHost);
                MainMenuInteractionGateProvider gateProvider =
                    GetComponent<MainMenuInteractionGateProvider>();
                _interactionGate = gateProvider?.Gate;
                _interactionShield = _interactionGate == null
                    ? null
                    : new InteractionShieldView(
                        root, _interactionGate);
                _view = new CombatLobbyView(
                    root,
                    _panelHost,
                    runtimeSettings != null && runtimeSettings.ReducedMotion,
                    GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap)
                );
            }
            catch (System.InvalidOperationException exception)
            {
                PowerMath.Diagnostics.AppLog.Error("Combat", exception.Message);
                return;
            }

            PlayerSessionStore sessionStore = PlayerSessionStore.Instance;
            if (sessionStore == null || !sessionStore.IsReady ||
                sessionStore.Snapshot == null)
            {
                SetUnavailable("Combat unavailable until player data is loaded.");
                return;
            }

            _view?.SetPowerCoins(sessionStore.Snapshot?.wallet?.powerCoins ?? 0);
            sessionStore.Changed += OnSessionChangedForMap;
            PowerMath.Localization.LocalizationService.Changed += OnLocaleChanged;

            GameApiSettings settings = GetComponent<MainMenuPresenter>()?.ApiSettings;
#if UNITY_EDITOR
            if (settings != null && settings.UseEditorSampleStudent)
                InitializeSimulation(sessionStore.Snapshot);
            else
                InitializeLive(sessionStore.Snapshot, settings);
#else
            InitializeLive(sessionStore.Snapshot, settings);
#endif
        }

        private void OnDisable()
        {
            PowerMath.Localization.LocalizationService.Changed -= OnLocaleChanged;
            if (PlayerSessionStore.Instance != null)
            {
                PlayerSessionStore.Instance.Changed -= OnSessionChangedForMap;
            }
            if (_presenter != null && _runEconomyController != null)
                _presenter.TerminalPresentationCompleted -=
                    _runEconomyController.NotifyTerminalPresentationCompleted;
            _presenter?.Dispose();
            _presenter = null;
            _questionCatalogRepository?.Cancel();
            _questionCatalogRepository = null;
            _questionCatalogLoadGeneration++;
            _questionCatalogLoadCompleted = true;
            _eventQuestionRepository?.Cancel();
            _eventQuestionRepository = null;
            _runEconomyController?.Dispose();
            _runEconomyController = null;
            _rewardMagnet?.Dispose();
            _rewardMagnet = null;
            _panelHost?.ForceCloseAll();
            _panelHost = null;
            _interactionShield?.Dispose();
            _interactionShield = null;
            _interactionGate = null;
            if (_playerActor != null)
            {
                _playerActor.Clicked -= OnActorTapped;
            }
            if (_enemyActor != null)
            {
                _enemyActor.Clicked -= OnActorTapped;
            }
            if (_runtimeEnemySprite != null)
            {
                Destroy(_runtimeEnemySprite);
                _runtimeEnemySprite = null;
            }
        }

        public void RunCombatRoutine(IEnumerator routine)
        {
            if (isActiveAndEnabled && routine != null)
            {
                StartCoroutine(routine);
            }
        }

        public void StopCombatRoutines()
        {
            StopAllCoroutines();
        }

        private void OnSessionChangedForMap(PlayerSnapshot snapshot)
        {
            _view?.SetPowerCoins(snapshot?.wallet?.powerCoins ?? 0);
            _view?.SetBiomeMapAvailable(GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap));
        }

        private void InitializeSimulation(PlayerSnapshot snapshot)
        {
            if (!TryLoadDevelopmentCatalog(out QuestionCatalog catalog))
            {
                SetUnavailable(
                    "Development question catalog is invalid. See the Unity Console."
                );
                return;
            }

            GameApiSettings settings = GetComponent<MainMenuPresenter>()?.ApiSettings;
            FirestoreAcademicProgressionStore progressionStore =
                settings != null ? TryCreateProgressionStore(settings, snapshot) : null;

            InitializeRuntime(
                snapshot,
                catalog,
                BuildDevelopmentEventQuestions(catalog),
                new SimulationQuestionPresentation(),
                progressionStore
            );
        }

        private void InitializeLive(PlayerSnapshot snapshot, GameApiSettings settings)
        {
            if (settings == null)
            {
                SetUnavailable("Firebase player configuration is missing.");
                return;
            }

            FirestoreAcademicProgressionStore progressionStore =
                TryCreateProgressionStore(settings, snapshot);

            if (!TryResolveStageMap(out StageMapData map, out string mapError))
            {
                SetUnavailable(mapError);
                return;
            }

            _questionCatalogRepository = new FirestoreQuestionCatalogRepository(this, settings);
            int loadGeneration = ++_questionCatalogLoadGeneration;
            _questionCatalogLoadCompleted = false;
            _questionCatalogRepository.Load(result =>
            {
                if (loadGeneration != _questionCatalogLoadGeneration ||
                    _questionCatalogLoadCompleted)
                    return;

                if (result == null || !result.IsSuccess)
                {
                    _questionCatalogLoadCompleted = true;
                    if (result != null)
                    {
                        foreach (string error in result.Errors)
                            PowerMath.Diagnostics.AppLog.Warning("Combat", $"Question catalog fallback: {error}");
                    }

                    InitializeLiveQuestionFallback(snapshot, progressionStore, map);
                    return;
                }

                IQuestionPresentation presentation = CreateLiveQuestionPresentation();
                _eventQuestionRepository = new FirestoreEventQuestionCatalogRepository(this, settings);
                _eventQuestionRepository.Load(map.EventQuestionDocumentIds, (eventCatalog, eventError) =>
                {
                    if (loadGeneration != _questionCatalogLoadGeneration ||
                        _questionCatalogLoadCompleted)
                        return;

                    _questionCatalogLoadCompleted = true;
                    if (eventCatalog == null)
                    {
                        PowerMath.Diagnostics.AppLog.Warning(
                            "Combat",
                            $"Event question catalog fallback: {eventError}");
                        eventCatalog = BuildDevelopmentEventQuestions(
                            result.Catalog,
                            map.EventQuestionDocumentIds);
                    }

                    InitializeRuntime(snapshot, result.Catalog, eventCatalog,
                        presentation, progressionStore, map);
                });
            });
            StartCoroutine(EnforceQuestionCatalogStartupDeadline(
                loadGeneration,
                snapshot,
                progressionStore,
                map,
                settings));
        }

        private IEnumerator EnforceQuestionCatalogStartupDeadline(
            int loadGeneration,
            PlayerSnapshot snapshot,
            FirestoreAcademicProgressionStore progressionStore,
            StageMapData map,
            GameApiSettings settings)
        {
            const float maximumStartupWaitSeconds = 2.5f;
            float configuredTimeout = settings == null
                ? maximumStartupWaitSeconds
                : Mathf.Max(1f, settings.RequestTimeoutSeconds);
            float deadline = Mathf.Min(maximumStartupWaitSeconds, configuredTimeout);
            float elapsed = 0f;
            while (elapsed < deadline)
            {
                if (loadGeneration != _questionCatalogLoadGeneration ||
                    _questionCatalogLoadCompleted)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (loadGeneration != _questionCatalogLoadGeneration ||
                _questionCatalogLoadCompleted)
                yield break;

            _questionCatalogLoadCompleted = true;
            _questionCatalogRepository?.Cancel();
            _eventQuestionRepository?.Cancel();
            PowerMath.Diagnostics.AppLog.Warning(
                "Combat",
                "Question catalog startup timed out. Activating development question catalog with live player persistence.");
            InitializeLiveQuestionFallback(snapshot, progressionStore, map);
        }

        private void InitializeLiveQuestionFallback(
            PlayerSnapshot snapshot,
            FirestoreAcademicProgressionStore progressionStore,
            StageMapData map)
        {
            if (!TryLoadDevelopmentCatalog(snapshot, out QuestionCatalog catalog))
            {
                SetUnavailable(
                    "Questions are offline and the development fallback is invalid.");
                return;
            }

            PowerMath.Diagnostics.AppLog.Warning(
                "Combat",
                "Shared Question Firebase is unavailable. Using development question catalog with live player persistence.");
            InitializeRuntime(
                snapshot,
                catalog,
                BuildDevelopmentEventQuestions(catalog, map.EventQuestionDocumentIds),
                CreateLiveQuestionPresentation(),
                progressionStore,
                map,
                string.Empty,
                isolateQuestionFallback: false
            );
        }

        private IQuestionPresentation CreateLiveQuestionPresentation()
        {
#if UNITY_EDITOR
            // Editor cannot host the browser DOM iframe. Catalog and persistence
            // remain live while only the content presentation is simulated.
            return new SimulationQuestionPresentation();
#else
            WebGlYouTubeQuestionPresentation presentation =
                GetComponent<WebGlYouTubeQuestionPresentation>();
            if (presentation == null)
                presentation = gameObject.AddComponent<WebGlYouTubeQuestionPresentation>();
            return presentation;
#endif
        }

        private void InitializeRuntime(
            PlayerSnapshot snapshot,
            QuestionCatalog catalog,
            EventQuestionCatalog eventQuestions,
            IQuestionPresentation questionPresentation,
            FirestoreAcademicProgressionStore progressionStore,
            StageMapData resolvedMap = null,
            string startupNotice = "",
            bool isolateQuestionFallback = false)
        {

            if (snapshot.progression == null ||
                !AcademicRank.TryParseExact(
                    snapshot.progression.activeRank,
                    out AcademicRank activeRank))
            {
                SetUnavailable("Player Rank data is unavailable.");
                return;
            }

            RankCurrencyBalances balances;
            try
            {
                balances = snapshot.wallet == null
                    ? new RankCurrencyBalances(0, 0, 0)
                    : new RankCurrencyBalances(
                        snapshot.wallet.silver,
                        snapshot.wallet.gold,
                        snapshot.wallet.diamond
                    );
            }
            catch (System.ArgumentOutOfRangeException exception)
            {
                PowerMath.Diagnostics.AppLog.Error("Combat", exception.Message);
                SetUnavailable("Player Rank Currency data is invalid.");
                return;
            }

            int startingStage = ResolveStartingStage(snapshot);
            if (resolvedMap == null && !TryResolveStageMap(out resolvedMap, out string mapError))
            {
                SetUnavailable(mapError);
                return;
            }
            var encounterResolver = new StageEncounterResolver(resolvedMap);

            int seed = runtimeSettings == null ? 1337 : runtimeSettings.RandomSeed;
            int maximumHearts = runtimeSettings == null
                ? 3
                : runtimeSettings.MaximumHearts;
            double criticalRate = runtimeSettings == null
                ? 0.2d
                : runtimeSettings.CriticalRate;
            double criticalDamage = runtimeSettings == null
                ? 50d
                : runtimeSettings.CriticalDamagePercent;
            int baseAttack = runtimeSettings == null ? 5 : runtimeSettings.BaseAttack;
            int baseWeaponAttack = runtimeSettings == null
                ? WeaponAscensionPolicy.DefaultBaseWeaponAttack
                : runtimeSettings.BaseWeaponAttack;
            PetGachaCatalog runtimePetCatalog = null;
            if (petGachaCatalog != null &&
                !petGachaCatalog.TryBuildCatalog(
                    out runtimePetCatalog,
                    out string petCatalogError))
            {
                PowerMath.Diagnostics.AppLog.Error("Combat", $"Pet catalog is invalid: {petCatalogError}");
                SetUnavailable("Saved pet progression is unavailable.");
                return;
            }
            PlayerCombatStats combatStats;
            try
            {
                combatStats = PlayerCombatStatsFactory.Create(
                    snapshot,
                    baseAttack,
                    baseWeaponAttack,
                    criticalRate,
                    criticalDamage,
                    runtimePetCatalog);
            }
            catch (System.Exception exception)
            {
                PowerMath.Diagnostics.AppLog.Error("Combat", $"Player combat stats are invalid: {exception.Message}");
                SetUnavailable("Saved weapon progression is invalid.");
                return;
            }

            var random = new SeededRandomSource(seed);
            string runId = isolateQuestionFallback
                ? "practice-" + System.Guid.NewGuid().ToString("N")
                : ResolveRunId(snapshot);
            AttemptPresentationReceipt pendingPresentation = null;
            if (!isolateQuestionFallback &&
                snapshot.activeRun?.pendingPresentation != null &&
                !TryMapPendingPresentation(
                    snapshot.activeRun.pendingPresentation,
                    out pendingPresentation,
                    out string presentationError))
            {
                SetUnavailable(presentationError);
                return;
            }
            ILocalEncounterEngine engine;
            if (!isolateQuestionFallback &&
                TryBuildRestoredCombat(snapshot, encounterResolver, runId,
                out CombatSnapshot restoredCombat))
            {
                if (restoredCombat.Phase == CombatPhase.Committed ||
                    restoredCombat.Phase == CombatPhase.Preparation ||
                    restoredCombat.Phase == CombatPhase.Answering ||
                    restoredCombat.Phase == CombatPhase.Resolving ||
                    (restoredCombat.Phase == CombatPhase.PresentingResult && pendingPresentation == null))
                {
                    PowerMath.Diagnostics.AppLog.Warning("Combat", "An unfinished saved attempt was interrupted. Restoring encounter in ready state.");
                    restoredCombat = new CombatSnapshot(
                        restoredCombat.Stage,
                        restoredCombat.EnemyId,
                        restoredCombat.EnemyName,
                        restoredCombat.EnemyCurrentHp > 0 ? restoredCombat.EnemyCurrentHp : restoredCombat.EnemyMaximumHp,
                        restoredCombat.EnemyMaximumHp,
                        restoredCombat.EnemyRemainingCooldown,
                        restoredCombat.EnemyMaximumCooldown,
                        restoredCombat.PlayerCurrentHearts > 0 ? restoredCombat.PlayerCurrentHearts : maximumHearts,
                        restoredCombat.PlayerMaximumHearts > 0 ? restoredCombat.PlayerMaximumHearts : maximumHearts,
                        restoredCombat.EncounterKind == StageEncounterKind.ChallengeEvent ? CombatPhase.EventReady : CombatPhase.EnemyReady,
                        false,
                        restoredCombat.BiomeId,
                        restoredCombat.BiomeTitle,
                        restoredCombat.EncounterKind,
                        restoredCombat.QuestionDocumentId,
                        restoredCombat.EventAttemptOrdinal);
                }
                engine = new LocalRunEncounterEngine(restoredCombat, runId,
                    encounterResolver, random, combatStats);
            }
            else
            {
                engine = new LocalRunEncounterEngine(new StageId(startingStage),
                    runId, encounterResolver, random, maximumHearts, combatStats);
            }

            if (pendingPresentation != null &&
                engine.Snapshot.Phase != CombatPhase.PresentingResult &&
                engine.Snapshot.Phase != CombatPhase.RunDefeat &&
                engine.Snapshot.Phase != CombatPhase.RunComplete)
            {
                PowerMath.Diagnostics.AppLog.Warning(
                    "Combat",
                    $"Pending presentation '{pendingPresentation.PresentationId}' does not match combat phase '{engine.Snapshot.Phase}' and was discarded.");
                pendingPresentation = null;
            }
            var academicEngine = new AcademicProgressionEngine(catalog);
            AcademicProgressionState academicState;
            if (isolateQuestionFallback || snapshot.academic == null)
            {
                academicState = academicEngine.CreateInitialState(activeRank, balances);
            }
            else
            {
                try
                {
                    academicState = academicEngine.Rehydrate(new AcademicPersistenceSnapshot(
                        activeRank,
                        snapshot.academic.auditResolvedCount,
                        snapshot.academic.auditScore,
                        balances,
                        ToInventorySnapshot(snapshot.academic.silver),
                        ToInventorySnapshot(snapshot.academic.gold),
                        ToInventorySnapshot(snapshot.academic.diamond)
                    ));
                }
                catch (System.Exception exception)
                {
                    PowerMath.Diagnostics.AppLog.Warning(
                        "Combat",
                        $"Academic progression inventory mismatch with active catalog: {exception.Message}. " +
                        "Re-seeding queue for active rank.");
                    academicState = academicEngine.CreateInitialState(activeRank, balances);
                }
            }
            var clock = new UnityMonotonicClock();
            var transactionEngine = new LocalAttemptTransactionEngine(
                engine,
                academicEngine,
                academicState,
                clock,
                runtimeSettings == null ? 1d : runtimeSettings.PreparationSeconds,
                runtimeSettings == null ? 10d : runtimeSettings.AnswerSeconds,
                eventQuestions,
                runId,
                pendingPresentation
            );
            var gateway = new LocalDevelopmentAttemptGateway(transactionEngine);
            var coordinator = new CombatAttemptCoordinator(gateway);
            IGameplayPersistence persistence;
            if (progressionStore == null || isolateQuestionFallback)
            {
                persistence = new ImmediateGameplayPersistence();
            }
            else
            {
                persistence = new FirestoreGameplayPersistence(
                    this,
                    progressionStore,
                    snapshot,
                    new FirestoreLeaderboardProjectionPublisher(
                        GetComponent<MainMenuPresenter>()?.ApiSettings));
            }

            AudioSource source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            BindSceneCanvas();
            CombatAudioPlayer audio = new CombatAudioPlayer(source);
            var academicAudio = new AcademicAudioPlayer(source);
            VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            _rewardMagnet = new RewardMagnetFeedbackPlayer(
                rootVisualElement,
                _uiContext?.MotionDriver,
                academicAudio,
                combatJuiceProfile);
            CombatFeedbackPlayer combatFeedback = new CombatFeedbackPlayer(
                _view,
                audio,
                runtimeSettings != null && runtimeSettings.ReducedMotion,
                _floatingText,
                _enemyDamageAnchor,
                _impactImpulse,
                _playerActor,
                _enemyActor,
                _rewardMagnet,
                this
            );
            var academicView = new AcademicProgressionView(
                rootVisualElement
            );
            var academicPresenter = new AcademicProgressionPresenter(academicView);
            var rankFeedback = new RankTransitionFeedbackPlayer(
                academicView,
                academicAudio
            );
            var feedback = new AttemptFeedbackSequence(
                combatFeedback,
                academicPresenter,
                academicAudio,
                rankFeedback
            );
            _view.ConfigureStageMap(resolvedMap, RenderBiomeOnCanvas,
                RenderEncounterOnCanvas, CrossfadeBiomeBackground,
                ResolveLocalizedEnemyName);

            _presenter = new CombatLobbyPresenter(
                _view,
                academicPresenter,
                coordinator,
                feedback,
                audio,
                questionPresentation,
                this,
                persistence,
                transactionEngine,
                _interactionGate
            );
            _presenter.Initialize();
            if (pendingPresentation != null)
                _presenter.RecoverPendingPresentation();
            else if (!string.IsNullOrWhiteSpace(startupNotice))
                _view.SetResult(startupNotice, true);
            GameApiSettings settings = GetComponent<MainMenuPresenter>()?.ApiSettings;
            try
            {
                _runEconomyController = new RunEconomyPanelController(
                    this,
                    GetComponent<UIDocument>().rootVisualElement,
                    settings,
                    snapshot,
                    catalog,
                    weaponAscensionCatalog,
                    petGachaCatalog,
                    baseAttack,
                    baseWeaponAttack,
                    criticalRate,
                    criticalDamage,
                    source,
                    runtimeSettings != null && runtimeSettings.ReducedMotion,
                    _uiContext.MotionDriver,
                    _panelHost,
                    _interactionGate,
                    _playerActor);
                _presenter.TerminalPresentationCompleted +=
                    _runEconomyController.NotifyTerminalPresentationCompleted;
            }
            catch (System.Exception exception)
            {
                PowerMath.Diagnostics.AppLog.Error("Combat", $"Run progression controls could not start: {exception.Message}");
            }

            GetComponent<MainMenuTransitionController>()?.NotifySessionReady();
        }

        private void SetUnavailable(string playerMessage)
        {
            _view.SetUnavailable(playerMessage);
            GetComponent<MainMenuTransitionController>()?
                .CancelAndApplyFinalState();
        }

        private bool TryLoadDevelopmentCatalog(out QuestionCatalog catalog)
        {
            return TryLoadDevelopmentCatalog(null, out catalog);
        }

        private bool TryLoadDevelopmentCatalog(
            PlayerSnapshot snapshot,
            out QuestionCatalog catalog)
        {
            catalog = null;
            QuestionCatalogLoadResult loadResult = null;
            _questionCatalogRepository = snapshot == null
                ? new InMemoryQuestionCatalogRepository()
                : new InMemoryQuestionCatalogRepository(
                    BuildFallbackQuestionDocuments(snapshot));
            _questionCatalogRepository.Load(result => loadResult = result);
            if (loadResult == null || !loadResult.IsSuccess)
            {
                if (loadResult != null)
                {
                    foreach (string error in loadResult.Errors)
                    {
                        PowerMath.Diagnostics.AppLog.Error("Combat", $"Question catalog: {error}");
                    }
                }

                return false;
            }

            catalog = loadResult.Catalog;
            return true;
        }

        private static IEnumerable<RankedQuestionDocument>
            BuildFallbackQuestionDocuments(PlayerSnapshot snapshot)
        {
            foreach (AcademicRank rank in new[]
            {
                AcademicRank.Silver,
                AcademicRank.Gold,
                AcademicRank.Diamond
            })
            {
                PlayerSnapshot.RankInventoryData inventory = rank.Tier switch
                {
                    AcademicRankTier.Gold => snapshot.academic?.gold,
                    AcademicRankTier.Diamond => snapshot.academic?.diamond,
                    _ => snapshot.academic?.silver
                };
                var ids = new SortedSet<long>();
                AddValidQuestionIds(ids, inventory?.pendingIds);
                AddValidQuestionIds(ids, inventory?.failedIds);
                AddValidQuestionIds(ids, inventory?.attemptedInAuditIds);
                AddValidQuestionIds(ids, inventory?.clearedInCycleIds);
                for (long candidate = 1; ids.Count < AuditWindow.RequiredResults; candidate++)
                    ids.Add(candidate);

                int ordinal = 0;
                foreach (long id in ids)
                {
                    yield return new RankedQuestionDocument(
                        rank,
                        new QuestionDocumentDto
                        {
                            id = id,
                            answer = 7 + ((int)rank.Tier * 5) + ordinal++,
                            video_link = "https://www.youtube.com/watch?v=M7lc1UVf-VE"
                        });
                }
            }
        }

        private static void AddValidQuestionIds(
            ISet<long> destination,
            IEnumerable<long> source)
        {
            foreach (long id in source ?? Enumerable.Empty<long>())
            {
                if (id >= 0) destination.Add(id);
            }
        }

        private static EventQuestionCatalog BuildDevelopmentEventQuestions(
            QuestionCatalog catalog,
            IEnumerable<string> documentIds = null)
        {
            IReadOnlyList<QuestionDefinition> questions =
                catalog.GetRankQuestions(AcademicRank.Diamond);
            var documents = new Dictionary<string, IReadOnlyList<QuestionDefinition>>(
                System.StringComparer.Ordinal)
            {
                { "challenge", questions }
            };
            foreach (string documentId in documentIds ?? Enumerable.Empty<string>())
            {
                string normalized = documentId?.Trim();
                if (!string.IsNullOrEmpty(normalized) && !documents.ContainsKey(normalized))
                    documents.Add(normalized, questions);
            }

            return new EventQuestionCatalog(
                documents);
        }

        private bool TryResolveStageMap(out StageMapData map, out string error)
        {
            if (stageMapDefinition != null)
                return stageMapDefinition.TryMap(out map, out error);
            map = DevelopmentStageMapFactory.Create();
            error = string.Empty;
            return true;
        }

        private static string ResolveRunId(PlayerSnapshot snapshot)
        {
            snapshot.activeRun = snapshot.activeRun ?? new PlayerSnapshot.ActiveRunData();
            if (string.IsNullOrWhiteSpace(snapshot.activeRun.runId))
                snapshot.activeRun.runId = System.Guid.NewGuid().ToString("N");
            return snapshot.activeRun.runId;
        }

        private void BindSceneCanvas()
        {
            _sceneBackground = GameObject.Find("bg")?.GetComponent<LegacyImage>();
            _sceneEnemy = GameObject.Find("monsterPrefab")?.GetComponent<LegacyImage>();
            _scenePlayer = GameObject.Find("playerPresentation")?.GetComponent<LegacyImage>();
            if (_sceneBackground != null)
            {
                _sceneBackground.raycastTarget = false;
                _sceneBackground.gameObject.SetActive(true);
            }
            if (_scenePlayer != null)
            {
                _scenePlayer.raycastTarget = true;
                _scenePlayer.gameObject.SetActive(true);
            }
            if (_sceneEnemy != null)
            {
                _sceneEnemy.raycastTarget = true;
                _sceneEnemy.gameObject.SetActive(true);
                _enemyDamageAnchor = new RectTransformCombatAnchor(
                    _sceneEnemy.rectTransform,
                    enemyFctNormalizedAnchor,
                    enemyFctOffset);
            }
            EnsureFloatingCombatText();
            EnsureActorPresenters();
        }

        private void EnsureActorPresenters()
        {
            bool reducedMotion = runtimeSettings != null && runtimeSettings.ReducedMotion;
            MainMenuTransitionController transition = GetComponent<MainMenuTransitionController>();
            Vector2? playerRest = transition != null ? transition.PlayerRestPosition : (Vector2?)null;
            Vector2? enemyRest = transition != null ? transition.EnemyRestPosition : (Vector2?)null;

            if (_sceneBackground != null)
            {
                _sceneBackground.raycastTarget = false;
                ActorPresentationController legacyActor = _sceneBackground.GetComponent<ActorPresentationController>();
                if (legacyActor != null)
                {
                    Destroy(legacyActor);
                }
            }
            if (_scenePlayer != null)
            {
                _playerActor = _scenePlayer.GetComponent<ActorPresentationController>();
                if (_playerActor == null)
                    _playerActor = _scenePlayer.gameObject.AddComponent<ActorPresentationController>();
                _playerActor.Clicked -= OnActorTapped;
                _playerActor.Clicked += OnActorTapped;
                _playerActor.ConfigureDamageTextAnchor(
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero);
                _playerActor.Initialize(
                    PowerMath.Gameplay.Combat.Presentation.PresentationActor.Player,
                    reducedMotion,
                    combatJuiceProfile,
                    playerRest);
                CharacterPresentationBinding.ApplyToPlayerActor(_playerActor);
            }
            if (_sceneEnemy != null)
            {
                _enemyActor = _sceneEnemy.GetComponent<ActorPresentationController>();
                if (_enemyActor == null)
                    _enemyActor = _sceneEnemy.gameObject.AddComponent<ActorPresentationController>();
                _enemyActor.Clicked -= OnActorTapped;
                _enemyActor.Clicked += OnActorTapped;
                _enemyActor.ConfigureDamageTextAnchor(
                    enemyFctNormalizedAnchor,
                    enemyFctOffset);
                _enemyActor.Initialize(
                    PowerMath.Gameplay.Combat.Presentation.PresentationActor.Enemy,
                    reducedMotion,
                    combatJuiceProfile,
                    enemyRest);
                _enemyDamageAnchor = _enemyActor.DamageTextAnchor;
            }
        }

        private void OnActorTapped()
        {
            _view?.RequestAttack();
        }

        private void EnsureFloatingCombatText()
        {
            Canvas canvas = _sceneEnemy != null
                ? _sceneEnemy.GetComponentInParent<Canvas>()
                : _scenePlayer != null
                    ? _scenePlayer.GetComponentInParent<Canvas>()
                    : null;
            if (canvas == null) return;

            RectTransform worldRoot = EnsureCombatWorldRoot(canvas);
            _impactImpulse = GetComponent<CombatWorldImpulsePlayer>();
            if (_impactImpulse == null)
                _impactImpulse = gameObject.AddComponent<CombatWorldImpulsePlayer>();
            _impactImpulse.Initialize(
                worldRoot,
                runtimeSettings != null && runtimeSettings.ReducedMotion,
                combatJuiceProfile);

            Transform existing = canvas.transform.Find("CombatFxRoot");
            RectTransform fxRoot;
            if (existing is RectTransform existingRect)
            {
                fxRoot = existingRect;
            }
            else
            {
                var root = new GameObject("CombatFxRoot", typeof(RectTransform));
                fxRoot = root.GetComponent<RectTransform>();
                fxRoot.SetParent(canvas.transform, false);
                fxRoot.anchorMin = Vector2.zero;
                fxRoot.anchorMax = Vector2.one;
                fxRoot.offsetMin = Vector2.zero;
                fxRoot.offsetMax = Vector2.zero;
                fxRoot.SetAsLastSibling();
            }

            _floatingText = GetComponent<FloatingCombatTextService>();
            if (_floatingText == null)
                _floatingText = gameObject.AddComponent<FloatingCombatTextService>();
            FloatingCombatTextStyleDefinition style = floatingCombatTextStyle != null
                ? floatingCombatTextStyle
                : Resources.Load<FloatingCombatTextStyleDefinition>(
                    "FloatingCombatTextStyle");
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            _floatingText.Initialize(
                fxRoot,
                uiCamera,
                floatingCombatTextPrefab,
                style,
                runtimeSettings != null && runtimeSettings.ReducedMotion);
        }

        private RectTransform EnsureCombatWorldRoot(Canvas canvas)
        {
            Transform existing = canvas.transform.Find("CombatWorldPresentationRoot");
            RectTransform worldRoot;
            if (existing is RectTransform existingRect)
            {
                worldRoot = existingRect;
            }
            else
            {
                var root = new GameObject(
                    "CombatWorldPresentationRoot", typeof(RectTransform));
                worldRoot = root.GetComponent<RectTransform>();
                worldRoot.SetParent(canvas.transform, false);
                worldRoot.anchorMin = Vector2.zero;
                worldRoot.anchorMax = Vector2.one;
                worldRoot.offsetMin = Vector2.zero;
                worldRoot.offsetMax = Vector2.zero;
                if (_sceneBackground != null)
                    _sceneBackground.transform.SetParent(worldRoot, true);
                if (_scenePlayer != null)
                    _scenePlayer.transform.SetParent(worldRoot, true);
                if (_sceneEnemy != null)
                    _sceneEnemy.transform.SetParent(worldRoot, true);
            }
            EnsureBackgroundTransitionLayer(worldRoot);
            return worldRoot;
        }

        private void EnsureBackgroundTransitionLayer(RectTransform worldRoot)
        {
            if (_sceneBackground == null || worldRoot == null) return;
            if (_sceneBackgroundTransition == null)
            {
                Transform existing = worldRoot.Find("bg_transition");
                if (existing != null)
                {
                    _sceneBackgroundTransition = existing.GetComponent<LegacyImage>();
                }
                else
                {
                    var faderGo = new GameObject("bg_transition", typeof(RectTransform), typeof(LegacyImage));
                    faderGo.transform.SetParent(worldRoot, false);
                    _sceneBackgroundTransition = faderGo.GetComponent<LegacyImage>();
                    _sceneBackgroundTransition.raycastTarget = false;
                }
            }

            if (_sceneBackgroundTransition != null)
            {
                RectTransform rt = _sceneBackgroundTransition.rectTransform;
                RectTransform bgRt = _sceneBackground.rectTransform;
                rt.anchorMin = bgRt.anchorMin;
                rt.anchorMax = bgRt.anchorMax;
                rt.anchoredPosition = bgRt.anchoredPosition;
                rt.sizeDelta = bgRt.sizeDelta;
                rt.pivot = bgRt.pivot;
                rt.localScale = bgRt.localScale;

                int bgIndex = _sceneBackground.transform.GetSiblingIndex();
                _sceneBackgroundTransition.transform.SetSiblingIndex(bgIndex + 1);
                _sceneBackgroundTransition.color = new Color(1f, 1f, 1f, 0f);
                _sceneBackgroundTransition.gameObject.SetActive(false);
            }
        }

        private IEnumerator CrossfadeBiomeBackground(string biomeId, float duration)
        {
            Sprite targetSprite = stageMapDefinition?.FindBiome(biomeId)?.BackgroundSprite;
            if (targetSprite == null || _sceneBackground == null)
            {
                yield break;
            }

            if (_sceneBackground.overrideSprite == targetSprite)
            {
                yield break;
            }

            bool reducedMotion = runtimeSettings != null && runtimeSettings.ReducedMotion;
            if (reducedMotion || _sceneBackgroundTransition == null)
            {
                _sceneBackground.overrideSprite = targetSprite;
                _sceneBackground.color = Color.white;
                yield break;
            }

            float fadeDuration = Mathf.Max(0.1f, duration);
            _sceneBackgroundTransition.overrideSprite = targetSprite;
            _sceneBackgroundTransition.color = new Color(1f, 1f, 1f, 0f);
            _sceneBackgroundTransition.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime > 0 ? Time.unscaledDeltaTime : 0.02f;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                _sceneBackgroundTransition.color = new Color(1f, 1f, 1f, smoothT);
                yield return null;
            }

            _sceneBackground.overrideSprite = targetSprite;
            _sceneBackground.color = Color.white;
            _sceneBackgroundTransition.color = new Color(1f, 1f, 1f, 0f);
            _sceneBackgroundTransition.gameObject.SetActive(false);
        }

        private void RenderBiomeOnCanvas(string biomeId)
        {
            Sprite sprite = stageMapDefinition?.FindBiome(biomeId)?.BackgroundSprite;
            if (_sceneBackground != null && sprite != null)
            {
                _sceneBackground.overrideSprite = sprite;
                _sceneBackground.color = Color.white;
            }
            if (_sceneBackgroundTransition != null)
            {
                _sceneBackgroundTransition.color = new Color(1f, 1f, 1f, 0f);
                _sceneBackgroundTransition.gameObject.SetActive(false);
            }
        }

        private void RenderEncounterOnCanvas(string encounterId)
        {
            EnemyDefinition monster = stageMapDefinition?.FindMonster(encounterId);
            Sprite sprite = monster?.EnemySprite;
            EventDefinition eventDefinition = stageMapDefinition?.FindEvent(encounterId);
            if (sprite == null) sprite = eventDefinition?.EventSprite;
            if (sprite == null) sprite = ResolveFallbackEnemySprite();
            if (_sceneEnemy != null && sprite != null)
            {
                _sceneEnemy.overrideSprite = sprite;
                _sceneEnemy.gameObject.SetActive(true);
            }
        }

        private string ResolveLocalizedEnemyName(string encounterId)
        {
            EnemyDefinition monster = stageMapDefinition?.FindMonster(encounterId);
            if (monster != null)
            {
                return PowerMath.Localization.LocalizationService.Locale == "th" && !string.IsNullOrWhiteSpace(monster.ThaiDisplayName)
                    ? monster.ThaiDisplayName
                    : monster.DisplayName;
            }
            return null;
        }

        private void OnLocaleChanged()
        {
            _view?.RefreshLocalizedEnemyName();
        }

        private Sprite ResolveFallbackEnemySprite()
        {
            if (enemyTexture != null)
            {
                if (_runtimeEnemySprite == null)
                {
                    _runtimeEnemySprite = Sprite.Create(
                        enemyTexture,
                        new Rect(0f, 0f, enemyTexture.width, enemyTexture.height),
                        new Vector2(0.5f, 0.5f));
                }
                return _runtimeEnemySprite;
            }
            if (enemyDefinition?.EnemySprite != null)
                return enemyDefinition.EnemySprite;
            return _sceneEnemy?.overrideSprite;
        }
        private static int ResolveStartingStage(PlayerSnapshot snapshot)
        {
            int stage = 1;
            if (snapshot.activeRun != null && snapshot.activeRun.currentStage > 0)
            {
                stage = snapshot.activeRun.currentStage;
            }
            else if (snapshot.progression != null &&
                     snapshot.progression.currentStage > 0)
            {
                stage = snapshot.progression.currentStage;
            }

            return Mathf.Clamp(stage, StageId.First, StageId.Final);
        }

        private static RankQuestionInventorySnapshot ToInventorySnapshot(
            PlayerSnapshot.RankInventoryData source)
        {
            source = source ?? new PlayerSnapshot.RankInventoryData();
            return new RankQuestionInventorySnapshot(
                source.cycle,
                ToQuestionIds(source.pendingIds),
                ToQuestionIds(source.failedIds),
                ToQuestionIds(source.attemptedInAuditIds),
                ToQuestionIds(source.clearedInCycleIds)
            );
        }

        private static QuestionId[] ToQuestionIds(long[] values)
        {
            if (values == null || values.Length == 0) return System.Array.Empty<QuestionId>();
            var result = new QuestionId[values.Length];
            for (int index = 0; index < values.Length; index++)
                result[index] = new QuestionId(values[index]);
            return result;
        }

        private static FirestoreAcademicProgressionStore TryCreateProgressionStore(
            GameApiSettings settings,
            PlayerSnapshot snapshot)
        {
            string playerId = snapshot?.playerId ?? string.Empty;
            int separator = playerId.IndexOf(':');
            if (separator <= 0 || separator >= playerId.Length - 1)
            {
                PowerMath.Diagnostics.AppLog.Warning("Combat", "Player ID cannot identify the Firestore level and student fields. Running with local persistence.");
                return null;
            }
            return new FirestoreAcademicProgressionStore(
                settings,
                playerId.Substring(0, separator),
                playerId.Substring(separator + 1),
                snapshot,
                Object.FindAnyObjectByType<ProfileActivityTracker>()
            );
        }

        private static bool TryBuildRestoredCombat(
            PlayerSnapshot snapshot,
            StageEncounterResolver resolver,
            string runId,
            out CombatSnapshot restored)
        {
            restored = null;
            PlayerSnapshot.ActiveRunData run = snapshot?.activeRun;
            if (run == null || run.enemyMaximumHp <= 0 ||
                !System.Enum.TryParse(run.phase, true, out CombatPhase phase))
                return false;

            try
            {
                StageId stage = new StageId(run.currentStage);
                EncounterSelection expected = resolver.Resolve(runId, stage);
                string savedId = string.IsNullOrWhiteSpace(run.encounterId)
                    ? run.enemyId : run.encounterId;
                if (!string.Equals(savedId, expected.EncounterId,
                    System.StringComparison.Ordinal)) return false;
                restored = new CombatSnapshot(
                    stage,
                    expected.EncounterId,
                    expected.DisplayName,
                    run.enemyCurrentHp,
                    run.enemyMaximumHp,
                    run.enemyRemainingCooldown,
                    run.enemyMaximumCooldown > 0
                        ? run.enemyMaximumCooldown
                        : expected.MaximumCooldown,
                    run.playerCurrentHearts,
                    run.playerMaximumHearts,
                    phase,
                    false,
                    expected.BiomeId,
                    expected.BiomeTitle,
                    expected.Kind,
                    expected.QuestionDocumentId,
                    run.eventAttemptOrdinal);
                return true;
            }
            catch (System.Exception exception) when (
                exception is System.ArgumentOutOfRangeException ||
                exception is System.InvalidOperationException)
            {
                return false;
            }
        }

        private static bool TryMapPendingPresentation(
            PlayerSnapshot.AttemptPresentationData source,
            out AttemptPresentationReceipt receipt,
            out string error)
        {
            receipt = null;
            error = string.Empty;
            if (source == null) return true;
            if (source.version != AttemptPresentationReceipt.CurrentVersion)
            {
                error = "This saved combat result requires a newer PowerMath version.";
                return false;
            }
            if (!System.Enum.TryParse(source.outcome, true,
                    out AttemptOutcomeKind outcome) ||
                !TryMapPresentationSnapshot(source.source, out var before) ||
                !TryMapPresentationSnapshot(source.destination, out var after) ||
                !AcademicRank.TryParseExact(source.previousRank, out AcademicRank previous) ||
                !AcademicRank.TryParseExact(source.currentRank, out AcademicRank current))
            {
                error = "The saved combat presentation receipt is invalid.";
                return false;
            }

            try
            {
                receipt = new AttemptPresentationReceipt(
                    source.presentationId,
                    source.attemptId,
                    outcome,
                    source.responseScore,
                    source.finalDamage,
                    source.isCritical,
                    before,
                    after,
                    source.resolvedEnemyHpAfter,
                    source.enemyDefeated,
                    source.enemyAttacked,
                    source.playerDefeated,
                    source.stageAdvanced,
                    source.biomeChanged,
                    new RankTransitionReceipt(previous, current),
                    source.version);
                return true;
            }
            catch (System.ArgumentException)
            {
                error = "The saved combat presentation receipt failed validation.";
                return false;
            }
        }

        private static bool TryMapPresentationSnapshot(
            PlayerSnapshot.CombatPresentationData source,
            out CombatPresentationSnapshot snapshot)
        {
            snapshot = null;
            if (source == null ||
                !System.Enum.TryParse(source.encounterKind, true,
                    out StageEncounterKind encounterKind) ||
                !System.Enum.TryParse(source.phase, true, out CombatPhase phase))
                return false;
            try
            {
                snapshot = new CombatPresentationSnapshot(
                    new StageId(source.stage),
                    source.biomeId,
                    source.encounterId,
                    encounterKind,
                    source.enemyCurrentHp,
                    source.enemyMaximumHp,
                    source.enemyRemainingCooldown,
                    source.enemyMaximumCooldown,
                    source.playerCurrentHearts,
                    source.playerMaximumHearts,
                    phase);
                return true;
            }
            catch (System.Exception exception) when (
                exception is System.ArgumentException ||
                exception is System.ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
