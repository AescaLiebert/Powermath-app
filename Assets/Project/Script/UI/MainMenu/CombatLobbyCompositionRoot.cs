using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Combat.Unity;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;
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

        private CombatLobbyPresenter _presenter;
        private CombatLobbyView _view;
        private IMainMenuPanelHost _panelHost;
        private IQuestionCatalogRepository _questionCatalogRepository;
        private FirestoreEventQuestionCatalogRepository _eventQuestionRepository;
        private RunEconomyPanelController _runEconomyController;
        private LegacyImage _sceneBackground;
        private LegacyImage _sceneEnemy;
        private Sprite _runtimeEnemySprite;

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
            UIDocument document = GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null)
            {
                Debug.LogError("Combat Lobby requires the Main Menu UIDocument.");
                return;
            }

            try
            {
                MainMenuPanelHostProvider provider =
                    GetComponent<MainMenuPanelHostProvider>();
                if (provider == null)
                    provider = gameObject.AddComponent<MainMenuPanelHostProvider>();
                _panelHost = provider.Host;
                _view = new CombatLobbyView(
                    document.rootVisualElement,
                    _panelHost
                );
            }
            catch (System.InvalidOperationException exception)
            {
                Debug.LogError(exception.Message);
                return;
            }

            PlayerSessionStore sessionStore = PlayerSessionStore.Instance;
            if (sessionStore == null || !sessionStore.IsReady ||
                sessionStore.Snapshot == null)
            {
                _view.SetUnavailable("Combat unavailable until player data is loaded.");
                return;
            }

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
            _presenter?.Dispose();
            _presenter = null;
            _questionCatalogRepository?.Cancel();
            _questionCatalogRepository = null;
            _eventQuestionRepository?.Cancel();
            _eventQuestionRepository = null;
            _runEconomyController?.Dispose();
            _runEconomyController = null;
            _panelHost?.ForceCloseAll();
            _panelHost = null;
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

        private void InitializeSimulation(PlayerSnapshot snapshot)
        {
            if (!TryLoadDevelopmentCatalog(snapshot, out QuestionCatalog catalog))
            {
                _view.SetUnavailable(
                    "Development question catalog is invalid. See the Unity Console."
                );
                return;
            }

            InitializeRuntime(
                snapshot,
                catalog,
                BuildDevelopmentEventQuestions(catalog),
                new SimulationQuestionPresentation(),
                null
            );
        }

        private void InitializeLive(PlayerSnapshot snapshot, GameApiSettings settings)
        {
            if (settings == null)
            {
                _view.SetUnavailable("Firebase player configuration is missing.");
                return;
            }

            FirestoreAcademicProgressionStore progressionStore =
                TryCreateProgressionStore(settings, snapshot);
            if (progressionStore == null)
            {
                _view.SetUnavailable("Firebase player saves could not be initialized.");
                return;
            }

            if (!TryResolveStageMap(out StageMapData map, out string mapError))
            {
                _view.SetUnavailable(mapError);
                return;
            }

            _questionCatalogRepository = new FirestoreQuestionCatalogRepository(this, settings);
            _questionCatalogRepository.Load(result =>
            {
                if (result == null || !result.IsSuccess)
                {
                    if (result != null)
                    {
                        foreach (string error in result.Errors)
                            Debug.LogWarning($"Question catalog fallback: {error}");
                    }

                    InitializeLiveQuestionFallback(snapshot, progressionStore, map);
                    return;
                }

                IQuestionPresentation presentation = CreateLiveQuestionPresentation();
                _eventQuestionRepository = new FirestoreEventQuestionCatalogRepository(this, settings);
                _eventQuestionRepository.Load(map.EventQuestionDocumentIds, (eventCatalog, eventError) =>
                {
                    if (eventCatalog == null)
                    {
                        Debug.LogWarning(
                            $"Event question catalog fallback: {eventError}");
                        eventCatalog = BuildDevelopmentEventQuestions(
                            result.Catalog,
                            map.EventQuestionDocumentIds);
                    }

                    InitializeRuntime(snapshot, result.Catalog, eventCatalog,
                        presentation, progressionStore, map);
                });
            });
        }

        private void InitializeLiveQuestionFallback(
            PlayerSnapshot snapshot,
            FirestoreAcademicProgressionStore progressionStore,
            StageMapData map)
        {
            if (!TryLoadDevelopmentCatalog(snapshot, out QuestionCatalog catalog))
            {
                _view?.SetUnavailable(
                    "Questions are offline and the development fallback is invalid.");
                return;
            }

            Debug.LogWarning(
                "Shared Question Firebase is unavailable. Using local development " +
                "questions and simulated video while keeping Firebase player saves active.");
            InitializeRuntime(
                snapshot,
                catalog,
                BuildDevelopmentEventQuestions(catalog, map.EventQuestionDocumentIds),
                new SimulationQuestionPresentation(),
                progressionStore,
                map,
                "QUESTION FIREBASE OFFLINE - DEVELOPMENT QUESTIONS ACTIVE; PLAYER PROGRESS SAVES TO FIREBASE"
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
            string startupNotice = "")
        {

            if (snapshot.progression == null ||
                !AcademicRank.TryParseExact(
                    snapshot.progression.activeRank,
                    out AcademicRank activeRank))
            {
                _view.SetUnavailable("Player Rank data is unavailable.");
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
                Debug.LogError(exception.Message);
                _view.SetUnavailable("Player Rank Currency data is invalid.");
                return;
            }

            int startingStage = ResolveStartingStage(snapshot);
            if (resolvedMap == null && !TryResolveStageMap(out resolvedMap, out string mapError))
            {
                _view.SetUnavailable(mapError);
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
            PlayerCombatStats combatStats;
            try
            {
                combatStats = PlayerCombatStatsFactory.Create(
                    snapshot,
                    baseAttack,
                    baseWeaponAttack,
                    criticalRate,
                    criticalDamage);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Player combat stats are invalid: {exception.Message}");
                _view.SetUnavailable("Saved weapon progression is invalid.");
                return;
            }

            var random = new SeededRandomSource(seed);
            string runId = ResolveRunId(snapshot);
            ILocalEncounterEngine engine;
            if (TryBuildRestoredCombat(snapshot, encounterResolver, runId,
                out CombatSnapshot restoredCombat))
            {
                if (restoredCombat.Phase == CombatPhase.Committed ||
                    restoredCombat.Phase == CombatPhase.Preparation ||
                    restoredCombat.Phase == CombatPhase.Answering ||
                    restoredCombat.Phase == CombatPhase.Resolving)
                {
                    _view.SetUnavailable(
                        "An unfinished saved attempt needs recovery before combat can continue.");
                    return;
                }
                engine = new LocalRunEncounterEngine(restoredCombat, runId,
                    encounterResolver, random, combatStats);
            }
            else
            {
                engine = new LocalRunEncounterEngine(new StageId(startingStage),
                    runId, encounterResolver, random, maximumHearts, combatStats);
            }
            var academicEngine = new AcademicProgressionEngine(catalog);
            AcademicProgressionState academicState;
            try
            {
                academicState = snapshot.academic == null
                    ? academicEngine.CreateInitialState(activeRank, balances)
                    : academicEngine.Rehydrate(new AcademicPersistenceSnapshot(
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
                Debug.LogError($"Academic progression data is invalid: {exception.Message}");
                _view.SetUnavailable("Saved question progress is invalid.");
                return;
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
                runId
            );
            var gateway = new LocalDevelopmentAttemptGateway(transactionEngine);
            var coordinator = new CombatAttemptCoordinator(gateway);
            IGameplayPersistence persistence;
            if (progressionStore == null)
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

            CombatAudioPlayer audio = new CombatAudioPlayer(source);
            CombatFeedbackPlayer combatFeedback = new CombatFeedbackPlayer(
                _view,
                audio,
                runtimeSettings != null && runtimeSettings.ReducedMotion
            );
            var academicView = new AcademicProgressionView(
                GetComponent<UIDocument>().rootVisualElement
            );
            var academicPresenter = new AcademicProgressionPresenter(academicView);
            var academicAudio = new AcademicAudioPlayer(source);
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
            BindSceneCanvas();
            _view.ConfigureStageMap(resolvedMap, RenderBiomeOnCanvas,
                RenderEncounterOnCanvas);

            _presenter = new CombatLobbyPresenter(
                _view,
                academicPresenter,
                coordinator,
                feedback,
                audio,
                questionPresentation,
                this,
                persistence,
                transactionEngine
            );
            _presenter.Initialize();
            if (!string.IsNullOrWhiteSpace(startupNotice))
                _view.SetResult(startupNotice, true);
            if (progressionStore != null)
            {
                try
                {
                    _runEconomyController = new RunEconomyPanelController(
                        this,
                        GetComponent<UIDocument>().rootVisualElement,
                        GetComponent<MainMenuPresenter>()?.ApiSettings,
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
                        _panelHost);
                }
                catch (System.Exception exception)
                {
                    Debug.LogError($"Run progression controls could not start: {exception.Message}");
                }
            }
            else
            {
                GetComponent<UIDocument>().rootVisualElement.Q<Button>("rebirth-button").style.display = DisplayStyle.None;
                GetComponent<UIDocument>().rootVisualElement.Q<Button>("player-hub-button").style.display = DisplayStyle.None;
                GetComponent<UIDocument>().rootVisualElement.Q<Button>("pet-gacha-button").style.display = DisplayStyle.None;
            }
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
                        Debug.LogError($"Question catalog: {error}");
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
            if (_sceneBackground != null) _sceneBackground.raycastTarget = false;
            if (_sceneEnemy != null)
            {
                _sceneEnemy.raycastTarget = false;
                _sceneEnemy.gameObject.SetActive(true);
            }
        }

        private void RenderBiomeOnCanvas(string biomeId)
        {
            Sprite sprite = stageMapDefinition?.FindBiome(biomeId)?.BackgroundSprite;
            if (_sceneBackground != null && sprite != null)
                _sceneBackground.overrideSprite = sprite;
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
                Debug.LogError("Player ID cannot identify the Firestore level and student fields.");
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
    }
}
