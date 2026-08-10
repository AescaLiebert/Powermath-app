using System.Collections;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Combat.Unity;
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

        [Tooltip("Development simulation and answer-window settings.")]
        [SerializeField] private CombatRuntimeSettingsDefinition runtimeSettings;

        [Tooltip("Optional explicit enemy texture. The existing scene monster image is used when empty.")]
        [SerializeField] private Texture2D enemyTexture;

        private CombatLobbyPresenter _presenter;
        private CombatLobbyView _view;
        private IQuestionCatalogRepository _questionCatalogRepository;

        private void Start()
        {
            UIDocument document = GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null)
            {
                Debug.LogError("Combat Lobby requires the Main Menu UIDocument.");
                return;
            }

            try
            {
                _view = new CombatLobbyView(document.rootVisualElement);
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
            if (!TryLoadDevelopmentCatalog(out QuestionCatalog catalog))
            {
                _view.SetUnavailable(
                    "Development question catalog is invalid. See the Unity Console."
                );
                return;
            }

            InitializeRuntime(
                snapshot,
                catalog,
                new SimulationQuestionPresentation(),
                null
            );
        }

        private void InitializeLive(PlayerSnapshot snapshot, GameApiSettings settings)
        {
            if (settings == null)
            {
                _view.SetUnavailable("Question service configuration is missing.");
                return;
            }

            _questionCatalogRepository = new FirestoreQuestionCatalogRepository(this, settings);
            _questionCatalogRepository.Load(result =>
            {
                if (result == null || !result.IsSuccess)
                {
                    if (result != null)
                    {
                        foreach (string error in result.Errors) Debug.LogError($"Question catalog: {error}");
                    }
                    _view?.SetUnavailable("Questions could not be loaded. Please try again.");
                    return;
                }

                IQuestionPresentation presentation;
#if UNITY_EDITOR
                // Editor cannot host the browser DOM iframe. It still uses the
                // live question catalog and player persistence for E2E testing.
                presentation = new SimulationQuestionPresentation();
#else
                WebGlYouTubeQuestionPresentation webPresentation =
                    GetComponent<WebGlYouTubeQuestionPresentation>();
                if (webPresentation == null)
                    webPresentation = gameObject.AddComponent<WebGlYouTubeQuestionPresentation>();
                presentation = webPresentation;
#endif
                FirestoreAcademicProgressionStore progressionStore =
                    TryCreateProgressionStore(settings, snapshot);
                InitializeRuntime(snapshot, result.Catalog, presentation, progressionStore);
            });
        }

        private void InitializeRuntime(
            PlayerSnapshot snapshot,
            QuestionCatalog catalog,
            IQuestionPresentation questionPresentation,
            FirestoreAcademicProgressionStore progressionStore)
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
            EnemyDefinitionData enemyData = enemyDefinition == null
                ? new EnemyDefinitionData("rock-titan", "Rock Titan", 40, 3)
                : enemyDefinition.ToDomainData();

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

            var random = new SeededRandomSource(seed);
            LocalCombatEngine engine;
            if (TryBuildRestoredCombat(snapshot, enemyData, out CombatSnapshot restoredCombat))
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
                engine = new LocalCombatEngine(
                    restoredCombat,
                    enemyData,
                    random,
                    criticalRate,
                    criticalDamage);
            }
            else
            {
                engine = new LocalCombatEngine(
                    new StageId(startingStage),
                    enemyData,
                    random,
                    maximumHearts,
                    criticalRate,
                    criticalDamage);
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
                runtimeSettings == null ? 10d : runtimeSettings.AnswerSeconds
            );
            var gateway = new LocalDevelopmentAttemptGateway(transactionEngine);
            var coordinator = new CombatAttemptCoordinator(gateway);
            IGameplayPersistence persistence = progressionStore == null
                ? new ImmediateGameplayPersistence()
                : new FirestoreGameplayPersistence(this, progressionStore, snapshot);

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
            Texture2D resolvedTexture = ResolveEnemyTexture();
            _view.SetEnemyTexture(resolvedTexture);

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
        }

        private bool TryLoadDevelopmentCatalog(out QuestionCatalog catalog)
        {
            catalog = null;
            QuestionCatalogLoadResult loadResult = null;
            _questionCatalogRepository = new InMemoryQuestionCatalogRepository();
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

        private Texture2D ResolveEnemyTexture()
        {
            if (enemyTexture != null)
            {
                return enemyTexture;
            }

            if (enemyDefinition != null && enemyDefinition.EnemySprite != null)
            {
                return enemyDefinition.EnemySprite.texture;
            }

            GameObject legacyEnemy = GameObject.Find("monsterPrefab");
            if (legacyEnemy == null)
            {
                return null;
            }

            LegacyImage image = legacyEnemy.GetComponent<LegacyImage>();
            Texture2D texture = image != null && image.sprite != null
                ? image.sprite.texture
                : null;

            if (texture != null)
            {
                // The scene image is a compatibility source, not a second enemy view.
                legacyEnemy.SetActive(false);
            }

            return texture;
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
                snapshot.progression == null ? 1 : snapshot.progression.highestStage
            );
        }

        private static bool TryBuildRestoredCombat(
            PlayerSnapshot snapshot,
            EnemyDefinitionData enemy,
            out CombatSnapshot restored)
        {
            restored = null;
            PlayerSnapshot.ActiveRunData run = snapshot?.activeRun;
            if (run == null || run.enemyMaximumHp <= 0 ||
                !string.Equals(run.enemyId, enemy.EnemyId, System.StringComparison.Ordinal) ||
                !System.Enum.TryParse(run.phase, true, out CombatPhase phase))
                return false;

            try
            {
                restored = new CombatSnapshot(
                    new StageId(run.currentStage),
                    run.enemyId,
                    enemy.DisplayName,
                    run.enemyCurrentHp,
                    run.enemyMaximumHp,
                    run.enemyRemainingCooldown,
                    run.enemyMaximumCooldown > 0
                        ? run.enemyMaximumCooldown
                        : enemy.MaximumCooldown,
                    run.playerCurrentHearts,
                    run.playerMaximumHearts,
                    phase,
                    false);
                return true;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
