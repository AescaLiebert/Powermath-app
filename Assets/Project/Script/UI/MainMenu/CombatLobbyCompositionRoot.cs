using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PowerMath.Bootstrap;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Combat.Presentation;
using PowerMath.Gameplay.Combat.Unity;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using PowerMath.UI.Core;
using PowerMath.UI.Settings;
using PowerMath.UI.MainMenu.Tutorial;
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

        [SerializeField] private FloatingRewardTextView floatingRewardTextPrefab;

        [Tooltip("Floating reward text style, currency colors, and celebratory Pop/Drift/Exit motion.")]
        [SerializeField] private FloatingRewardTextStyleDefinition floatingRewardTextStyle;

        [Tooltip("Actor timing/motion and critical-impact tuning. Runtime defaults are used until assigned.")]
        [SerializeField] private CombatJuiceProfileDefinition combatJuiceProfile;
        [Tooltip("Optional authored battle clips; safe generated cues fill missing slots.")]
        [SerializeField] private BattleSfxLibraryDefinition battleSfxLibrary;

        [Header("Result Stickers")]
        [SerializeField] private Sprite stickerCorrect;
        [SerializeField] private Sprite stickerFail;

        [Header("Combat Text Anchoring")]
        [Tooltip("Normalized anchor within enemy RectTransform for FCT spawn (0.5, 0.5 = center).")]
        [SerializeField] private Vector2 enemyFctNormalizedAnchor = new Vector2(0.5f, 0.5f);

        [Tooltip("Pixel offset added to the enemy FCT spawn position.")]
        [SerializeField] private Vector2 enemyFctOffset = Vector2.zero;

        [Header("Tutorial Focus")]
        [Tooltip("Normalized visible region inside the enemy RectTransform. " +
            "X/Y start at its bottom-left; Width/Height define the guided hit area.")]
        [SerializeField] private Rect enemyTutorialFocusNormalizedRect =
            new Rect(0.28f, 0.30f, 0.48f, 0.42f);

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
        private LegacyImage _scenePet;
        private FloatingCombatTextService _floatingText;
        private FloatingRewardTextService _floatingRewardText;
        private ICombatAnchor _enemyDamageAnchor;
        private CombatWorldImpulsePlayer _impactImpulse;
        private CombatImpactBurstPlayer _impactBurst;
        private ActorPresentationController _playerActor;
        private ActorPresentationController _enemyActor;
        private ActorPresentationController _petActor;
        private IMainMenuInteractionGate _interactionGate;
        private InteractionShieldView _interactionShield;
        private UiSceneContext _uiContext;
        private Sprite _runtimeEnemySprite;
        private RewardMagnetFeedbackPlayer _rewardMagnet;
        private CombatAudioPlayer _combatAudio;
        private TutorialDirector _tutorialDirector;
        private TutorialDirector _rankTutorialDirector;
        private TutorialDirector _enemySurviveTutorialDirector;
        private readonly List<ResultDrivenTutorialBinding> _resultDrivenTutorials =
            new List<ResultDrivenTutorialBinding>();
        private int _questionCatalogLoadGeneration;
        private bool _questionCatalogLoadCompleted;
        private LocalAttemptTransactionEngine _transactionEngine;
        private ILocalEncounterEngine _encounterEngine;
        private StageMapData _runtimeStageMap;
        private PetGachaCatalog _runtimePetCatalog;
        private string _runtimeRunId = string.Empty;
        private string _boundPetId = string.Empty;
        private int _runtimeBaseWeaponAttack;
        private double _runtimeCriticalRate;
        private double _runtimeCriticalDamagePercent;

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
                petGachaCatalog = Resources.Load<PetGachaCatalogDefinition>("Pets/PetGachaCatalog") ??
                                  Resources.Load<PetGachaCatalogDefinition>("PetGachaCatalog");
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
            PowerMath.Audio.UiSfxAudioBinder.Bind(root);

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
                _view.BiomeMapLockedRequested += OnBiomeMapLockedRequested;
                ResolveResultStickers();
                _view.SetResultStickers(stickerCorrect, stickerFail);
                var characterBinding = GetComponent<PowerMath.PlayerLifecycle.CharacterPresentationBinding>() ??
                    gameObject.AddComponent<PowerMath.PlayerLifecycle.CharacterPresentationBinding>();
                characterBinding.Bind(root);
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
            if (_presenter != null)
                _presenter.TutorialAttemptPresentationCompleted -=
                    OnTutorialAttemptPresentationCompleted;
            _resultDrivenTutorials.Clear();
            _tutorialDirector?.Dispose();
            _tutorialDirector = null;
            _rankTutorialDirector?.Dispose();
            _rankTutorialDirector = null;
            _enemySurviveTutorialDirector?.Dispose();
            _enemySurviveTutorialDirector = null;
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
            _transactionEngine = null;
            _encounterEngine = null;
            _runtimeStageMap = null;
            _runtimePetCatalog = null;
            _runtimeRunId = string.Empty;
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
            if (_view != null)
            {
                _view.BiomeMapLockedRequested -= OnBiomeMapLockedRequested;
            }
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
            PowerMath.Audio.MusicController.Instance.SetBossBattleActive(false);
        }

        private void OnBiomeMapLockedRequested()
        {
            PowerMath.UI.Core.StatusMessageService.ShowWarning(
                PowerMath.Localization.LocalizationService.Get("menu.lockedFeatureUpdate"));
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
            if (snapshot?.wallet != null)
            {
                _transactionEngine?.SynchronizePowerCoins(snapshot.wallet.powerCoins);
            }
            _view?.SetPowerCoins(snapshot?.wallet?.powerCoins ?? 0);
            _view?.SetBiomeMapAvailable(GameVersionChecker.IsFeatureAvailable(GameFeature.BiomeMap));
            RefreshRuntimePetCollection(snapshot);
            string equippedPetId = snapshot?.loadout?.petId ?? string.Empty;
            if (_scenePet != null && !string.Equals(
                    equippedPetId,
                    _boundPetId,
                    StringComparison.OrdinalIgnoreCase))
            {
                BindEquippedPetPresentation();
                if (_petActor != null)
                {
                    _petActor.ConfigureSprites(_scenePet.sprite);
                    _petActor.CancelAndApply(_scenePet.sprite == null
                        ? PowerMath.Gameplay.Combat.Presentation.ActorVisualState.Hidden
                        : PowerMath.Gameplay.Combat.Presentation.ActorVisualState.Idle);
                }
            }
        }

        private void RefreshRuntimePetCollection(PlayerSnapshot snapshot)
        {
            if (snapshot == null || _encounterEngine == null ||
                _runtimePetCatalog == null)
                return;

            try
            {
                PlayerCombatStats stats = PlayerCombatStatsFactory.Create(
                    snapshot,
                    _runtimeBaseWeaponAttack,
                    _runtimeCriticalRate,
                    _runtimeCriticalDamagePercent,
                    _runtimePetCatalog);
                _encounterEngine.RefreshPlayerStats(stats);

                EventScheduleSnapshot current = _encounterEngine.Snapshot.EventSchedule;
                if (_runtimeStageMap == null || current == null ||
                    string.IsNullOrWhiteSpace(_runtimeRunId))
                    return;

                int multiplier = PetCollectionEventMultiplierPolicy.Calculate(
                    snapshot,
                    _runtimePetCatalog);
                if (current.PetMultiplierBasisPoints == multiplier)
                    return;

                EventScheduleSnapshot regenerated = EventScheduleGenerator.Create(
                    _runtimeRunId,
                    _runtimeStageMap,
                    multiplier);
                EventScheduleSnapshot merged =
                    EventScheduleRefreshPolicy.PreserveReachedStages(
                        _runtimeStageMap,
                        current,
                        regenerated,
                        _encounterEngine.Snapshot.Stage);
                _encounterEngine.RefreshEventSchedule(merged);
            }
            catch (System.Exception exception) when (
                exception is System.ArgumentException ||
                exception is System.InvalidOperationException ||
                exception is System.OverflowException)
            {
                PowerMath.Diagnostics.AppLog.Error(
                    "Pets",
                    $"Runtime pet collection refresh failed: {exception.Message}");
            }
        }

        private void RefreshCombatPresentationAfterEconomyPanelClosed()
        {
            PlayerSnapshot snapshot = PlayerSessionStore.Instance?.Snapshot;
            RefreshRuntimePetCollection(snapshot);
            if (_encounterEngine != null && _view != null)
            {
                CombatSnapshot combat = _encounterEngine.Snapshot;
                _view.SetPlayerHearts(
                    combat.PlayerCurrentHearts,
                    combat.PlayerMaximumHearts);
            }

            BindEquippedPetPresentation();
            if (_petActor != null && _scenePet != null)
            {
                _petActor.ConfigureSprites(_scenePet.sprite);
                _petActor.CancelAndApply(_scenePet.sprite == null
                    ? PowerMath.Gameplay.Combat.Presentation.ActorVisualState.Hidden
                    : PowerMath.Gameplay.Combat.Presentation.ActorVisualState.Idle);
            }
        }

        private void InitializeSimulation(PlayerSnapshot snapshot)
        {
            bool catalogLoaded = TryLoadDevelopmentCatalog(out QuestionCatalog catalog);
            if (!catalogLoaded)
            {
                SetUnavailable(
                    "Development question catalog is invalid. See the Unity Console."
                );
                return;
            }

            GameApiSettings settings = GetComponent<MainMenuPresenter>()?.ApiSettings;
            FirestoreAcademicProgressionStore progressionStore =
                settings != null
                    ? TryCreateProgressionStore(settings, snapshot)
                    : null;

            InitializeRuntime(
                snapshot,
                catalog,
                BuildDevelopmentEventQuestions(catalog),
                new SimulationQuestionPresentation(),
                progressionStore,
                startupNotice: string.Empty,
                isolateQuestionFallback: false
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

                IQuestionPresentation presentation = CreateLiveQuestionPresentation(snapshot);
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
                CreateLiveQuestionPresentation(snapshot),
                progressionStore,
                map,
                string.Empty,
                isolateQuestionFallback: false
            );
        }

        private IQuestionPresentation CreateLiveQuestionPresentation(PlayerSnapshot snapshot)
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

            return new AdminBypassQuestionPresentation(
                () => IsAdminQuestionBypassActive(snapshot),
                presentation,
                new SimulationQuestionPresentation()
            );
#endif
        }

        private static bool IsAdminQuestionBypassActive(PlayerSnapshot snapshot)
        {
            if (!AdminAccountAccessPolicy.IsAuthorized(snapshot))
            {
                return false;
            }

            if (snapshot?.adminTuning != null && snapshot.adminTuning.bypassVideoQuestion)
            {
                return true;
            }

            return PlayerPrefs.GetInt("PowerMath.Admin.BypassVideoQuestion", 0) == 1;
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
            int baseWeaponAttack = runtimeSettings == null
                ? WeaponAscensionPolicy.DefaultBaseWeaponAttack
                : runtimeSettings.BaseWeaponAttack;
            bool adminInvincible = false;
            PlayerSnapshot.AdminTuningData adminTuning = snapshot.adminTuning;
            if (AdminAccountAccessPolicy.IsAuthorized(snapshot) &&
                adminTuning != null && adminTuning.combatOverrideEnabled &&
                adminTuning.attack >= 1 && adminTuning.attack <= 1000000 &&
                adminTuning.criticalRateBasisPoints >= 0 && adminTuning.criticalRateBasisPoints <= 10000 &&
                adminTuning.criticalDamageBasisPoints >= 0 && adminTuning.criticalDamageBasisPoints <= 1000000)
            {
                baseWeaponAttack = adminTuning.attack;
                criticalRate = adminTuning.criticalRateBasisPoints / 10000d;
                criticalDamage = adminTuning.criticalDamageBasisPoints / 100d;
                adminInvincible = adminTuning.invincible;
                startupNotice = string.IsNullOrWhiteSpace(startupNotice)
                    ? "FIREBASE ADMIN COMBAT SETTINGS ACTIVE"
                    : startupNotice + " · FIREBASE ADMIN COMBAT SETTINGS ACTIVE";
            }
            if (IsAdminQuestionBypassActive(snapshot))
            {
                startupNotice = string.IsNullOrWhiteSpace(startupNotice)
                    ? "ADMIN DIRECT QUESTION ACTIVE"
                    : startupNotice + " · ADMIN DIRECT QUESTION ACTIVE";
            }
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
            EventScheduleSnapshot eventSchedule;
            try
            {
                int petEventMultiplier = PetCollectionEventMultiplierPolicy.Calculate(
                    snapshot, runtimePetCatalog);
                EventScheduleSnapshot restoredSchedule = null;
                bool hasRestoredSchedule = !isolateQuestionFallback &&
                    TryMapEventSchedule(
                        snapshot.activeRun,
                        resolvedMap,
                        out restoredSchedule);
                bool staleStageOneMultiplier = hasRestoredSchedule &&
                    startingStage == StageId.First &&
                    restoredSchedule.PetMultiplierBasisPoints != petEventMultiplier;
                eventSchedule = hasRestoredSchedule && !staleStageOneMultiplier
                    ? restoredSchedule
                    : EventScheduleGenerator.Create(
                        runId,
                        resolvedMap,
                        petEventMultiplier);
            }
            catch (System.Exception exception) when (
                exception is System.ArgumentException ||
                exception is System.InvalidOperationException ||
                exception is System.OverflowException)
            {
                PowerMath.Diagnostics.AppLog.Error("Combat",
                    $"Event schedule is invalid: {exception.Message}");
                SetUnavailable("The saved Event schedule is unavailable.");
                return;
            }
            var encounterResolver = new StageEncounterResolver(resolvedMap, eventSchedule);
            AttemptPresentationReceipt pendingPresentation = null;
            if (!isolateQuestionFallback &&
                snapshot.activeRun?.pendingPresentation != null &&
                !TryMapPendingPresentation(
                    snapshot.activeRun.pendingPresentation,
                    out pendingPresentation,
                    out string presentationError))
            {
                PowerMath.Diagnostics.AppLog.Warning("Combat",
                    $"Saved combat presentation could not be restored ({presentationError}). Interrupted presentation discarded.");
                pendingPresentation = null;
            }
            ILocalEncounterEngine engine;
            if (!isolateQuestionFallback &&
                TryBuildRestoredCombat(snapshot, encounterResolver, runId,
                out CombatSnapshot restoredCombat))
            {
                bool hasCommittedAttempt = !string.IsNullOrEmpty(snapshot.activeRun?.committedAttemptId);
                PresentationRecoveryKind recoveryKind = PresentationRecoveryResolver.Resolve(
                    restoredCombat.Phase,
                    pendingPresentation,
                    null,
                    hasCommittedAttempt);

                if (recoveryKind == PresentationRecoveryKind.InterruptedAttempt)
                {
                    PowerMath.Diagnostics.AppLog.Warning("Combat",
                        "An unfinished saved attempt was interrupted. Resolving authoritatively as Timeout/Forfeit.");

                    int safeHearts = restoredCombat.PlayerCurrentHearts <= 0 || restoredCombat.Phase == CombatPhase.RunDefeat
                        ? 0
                        : Math.Min(restoredCombat.PlayerCurrentHearts, maximumHearts);

                    int safeCooldown = Math.Max(0, Math.Min(
                        restoredCombat.EnemyRemainingCooldown,
                        restoredCombat.EnemyMaximumCooldown));

                    CombatSnapshot committedCombat = new CombatSnapshot(
                        restoredCombat.Stage,
                        restoredCombat.EnemyId,
                        restoredCombat.EnemyName,
                        restoredCombat.EnemyCurrentHp,
                        restoredCombat.EnemyMaximumHp,
                        safeCooldown,
                        restoredCombat.EnemyMaximumCooldown,
                        safeHearts,
                        restoredCombat.PlayerMaximumHearts > 0 ? restoredCombat.PlayerMaximumHearts : maximumHearts,
                        CombatPhase.Committed,
                        false,
                        restoredCombat.BiomeId,
                        restoredCombat.BiomeTitle,
                        restoredCombat.EncounterKind,
                        restoredCombat.QuestionDocumentId,
                        restoredCombat.EventAttemptOrdinal,
                        eventSchedule,
                        restoredCombat.StageAttackCount,
                        restoredCombat.BigBossesDefeated,
                        restoredCombat.PendingPetFollowUpDamage);

                    engine = new LocalRunEncounterEngine(committedCombat, runId,
                        encounterResolver, random, combatStats, adminInvincible,
                        maximumHearts);

                    CombatResolution resolution = engine.ResolveIncorrect(timedOut: true);

                    string attemptId = !string.IsNullOrEmpty(snapshot.activeRun?.committedAttemptId)
                        ? snapshot.activeRun.committedAttemptId
                        : Guid.NewGuid().ToString("N");

                    bool isChallengeEvent = committedCombat.EncounterKind == StageEncounterKind.ChallengeEvent;
                    int challengeBiomeIndex = Math.Min(7, Math.Max(1, (committedCombat.Stage.Value - 1) / 30 + 1));
                    int eventCoinsGranted = isChallengeEvent
                        ? ChallengeRewardPolicy.Calculate(QuestionOutcome.Timeout, 0, challengeBiomeIndex)
                        : 0;
                    long currentCoins = snapshot.wallet?.powerCoins ?? 0;
                    long resultingCoins = checked(currentCoins + eventCoinsGranted);

                    pendingPresentation = new AttemptPresentationReceipt(
                        "interrupted-attempt-" + attemptId,
                        attemptId,
                        AttemptOutcomeKind.Timeout,
                        0,
                        0,
                        false,
                        CombatPresentationSnapshot.From(committedCombat),
                        CombatPresentationSnapshot.From(engine.Snapshot),
                        committedCombat.EnemyCurrentHp,
                        false,
                        resolution.EnemyAttacked,
                        resolution.PlayerDefeated,
                        resolution.StageAdvanced,
                        resolution.BiomeChanged,
                        default,
                        AttemptPresentationReceipt.CurrentVersion,
                        resolution.EnemyFled,
                        eventCoinsGranted,
                        resultingCoins,
                        0,
                        committedCombat.EnemyCurrentHp,
                        null);

                    if (snapshot.activeRun != null)
                    {
                        snapshot.activeRun.pendingPresentation = new PlayerSnapshot.AttemptPresentationData
                        {
                            version = pendingPresentation.Version,
                            presentationId = pendingPresentation.PresentationId,
                            attemptId = pendingPresentation.AttemptId,
                            outcome = pendingPresentation.Outcome.ToString(),
                            responseScore = 0,
                            finalDamage = 0,
                            playerDamage = 0,
                            playerEnemyHpAfter = committedCombat.EnemyCurrentHp,
                            isCritical = false,
                            resolvedEnemyHpAfter = committedCombat.EnemyCurrentHp,
                            enemyDefeated = false,
                            enemyAttacked = resolution.EnemyAttacked,
                            playerDefeated = resolution.PlayerDefeated,
                            stageAdvanced = resolution.StageAdvanced,
                            biomeChanged = resolution.BiomeChanged,
                            enemyFled = resolution.EnemyFled,
                            powerCoinsGranted = eventCoinsGranted,
                            resultingPowerCoins = resultingCoins
                        };
                    }
                }
                else
                {
                    if (restoredCombat.PlayerCurrentHearts <= 0 || restoredCombat.Phase == CombatPhase.RunDefeat)
                    {
                        restoredCombat = new CombatSnapshot(
                            restoredCombat.Stage,
                            restoredCombat.EnemyId,
                            restoredCombat.EnemyName,
                            restoredCombat.EnemyCurrentHp,
                            restoredCombat.EnemyMaximumHp,
                            Math.Max(0, restoredCombat.EnemyRemainingCooldown),
                            restoredCombat.EnemyMaximumCooldown,
                            0,
                            restoredCombat.PlayerMaximumHearts > 0 ? restoredCombat.PlayerMaximumHearts : maximumHearts,
                            CombatPhase.RunDefeat,
                            false,
                            restoredCombat.BiomeId,
                            restoredCombat.BiomeTitle,
                            restoredCombat.EncounterKind,
                            restoredCombat.QuestionDocumentId,
                            restoredCombat.EventAttemptOrdinal,
                            eventSchedule,
                            restoredCombat.StageAttackCount,
                            restoredCombat.BigBossesDefeated,
                            restoredCombat.PendingPetFollowUpDamage);
                    }
                    else if (restoredCombat.Phase == CombatPhase.PresentingResult && pendingPresentation == null)
                    {
                        restoredCombat = new CombatSnapshot(
                            restoredCombat.Stage,
                            restoredCombat.EnemyId,
                            restoredCombat.EnemyName,
                            restoredCombat.EnemyCurrentHp,
                            restoredCombat.EnemyMaximumHp,
                            Math.Max(0, restoredCombat.EnemyRemainingCooldown),
                            restoredCombat.EnemyMaximumCooldown,
                            Math.Min(restoredCombat.PlayerCurrentHearts, maximumHearts),
                            restoredCombat.PlayerMaximumHearts > 0 ? restoredCombat.PlayerMaximumHearts : maximumHearts,
                            restoredCombat.EncounterKind == StageEncounterKind.ChallengeEvent ? CombatPhase.EventReady : CombatPhase.EnemyReady,
                            false,
                            restoredCombat.BiomeId,
                            restoredCombat.BiomeTitle,
                            restoredCombat.EncounterKind,
                            restoredCombat.QuestionDocumentId,
                            restoredCombat.EventAttemptOrdinal,
                            eventSchedule,
                            restoredCombat.StageAttackCount,
                            restoredCombat.BigBossesDefeated,
                            restoredCombat.PendingPetFollowUpDamage);
                    }

                    engine = new LocalRunEncounterEngine(restoredCombat, runId,
                        encounterResolver, random, combatStats, adminInvincible,
                        maximumHearts);
                }
            }
            else
            {
                engine = new LocalRunEncounterEngine(new StageId(startingStage),
                    runId, encounterResolver, random, maximumHearts, combatStats,
                    adminInvincible);
            }

            _encounterEngine = engine;
            _runtimeStageMap = resolvedMap;
            _runtimePetCatalog = runtimePetCatalog;
            _runtimeRunId = runId;
            _runtimeBaseWeaponAttack = baseWeaponAttack;
            _runtimeCriticalRate = criticalRate;
            _runtimeCriticalDamagePercent = criticalDamage;

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
                        ToInventorySnapshot(snapshot.academic.diamond),
                        snapshot.academic.auditCorrectCount
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
                pendingPresentation,
                ToChallengeQuestionSnapshot(snapshot.activeRun?.challengeQuestions),
                (pendingPresentation != null && pendingPresentation.PowerCoinsGranted > 0)
                    ? pendingPresentation.ResultingPowerCoins
                    : (snapshot.wallet?.powerCoins ?? 0),
                string.Equals(snapshot.activeRun?.questionContentKind,
                    QuestionContentKind.EventQuestion.ToString(),
                    System.StringComparison.Ordinal)
                    ? snapshot.activeRun?.committedAttemptId
                    : string.Empty
            );
            _transactionEngine = transactionEngine;
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
            CombatAudioPlayer audio = new CombatAudioPlayer(source, battleSfxLibrary);
            _combatAudio = audio;
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
                _petActor,
                _rewardMagnet,
                this,
                _floatingRewardText,
                _impactBurst,
                combatJuiceProfile
            );
            var academicView = new AcademicProgressionView(
                rootVisualElement
            );
            var academicPresenter = new AcademicProgressionPresenter(
                academicView,
                () => IsAdminQuestionBypassActive(snapshot)
            );
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
                ResolveLocalizedEnemyName, RequiresBackgroundTransition,
                StartBiomeMusic);
            StartCoroutine(PrewarmBiomePresentationAssets());

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
            InitializeTutorial(
                rootVisualElement,
                snapshot,
                runtimeSettings != null && runtimeSettings.ReducedMotion);
            MainMenuTransitionController transition =
                GetComponent<MainMenuTransitionController>();
            if (pendingPresentation != null)
            {
                _presenter.RecoverPendingPresentation();
                transition?.NotifyRecoveryReady();
            }
            else if (!string.IsNullOrWhiteSpace(startupNotice))
                _view.SetResult(startupNotice, true);
            if (pendingPresentation == null)
                StartCoroutine(PublishTutorialReadyWhenStable());
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
                    baseWeaponAttack,
                    criticalRate,
                    criticalDamage,
                    source,
                    runtimeSettings != null && runtimeSettings.ReducedMotion,
                    _uiContext.MotionDriver,
                    _panelHost,
                    _interactionGate,
                    _playerActor,
                    _rewardMagnet,
                    _floatingRewardText,
                    RefreshCombatPresentationAfterEconomyPanelClosed);
                _presenter.TerminalPresentationCompleted +=
                    _runEconomyController.NotifyTerminalPresentationCompleted;
            }
            catch (System.Exception exception)
            {
                PowerMath.Diagnostics.AppLog.Error("Combat", $"Run progression controls could not start: {exception.Message}");
            }

            if (pendingPresentation == null)
                transition?.NotifySessionReady();
        }

        private void SetUnavailable(string playerMessage)
        {
            PowerMath.Diagnostics.AppLog.Error(
                "Combat",
                $"Combat Lobby unavailable: {playerMessage}");
            StatusMessageService.ShowError(playerMessage);
            _view.SetUnavailable(playerMessage);
            GetComponent<MainMenuTransitionController>()?
                .NotifyRecoveryReady();
        }

        private void InitializeTutorial(
            VisualElement root,
            PlayerSnapshot snapshot,
            bool reducedMotion)
        {
            if (_presenter == null || root == null || snapshot == null ||
                _interactionGate == null || PlayerLifecycleRuntime.Commands == null)
                return;
            TutorialCatalogDefinition catalog =
                Resources.Load<TutorialCatalogDefinition>("Tutorial/TutorialCatalog");
            if (catalog == null || !catalog.TryGet(
                TutorialDirector.OnFirstCreateId,
                out PowerMath.Gameplay.Tutorial.TutorialSequence sequence))
            {
                PowerMath.Diagnostics.AppLog.Warning(
                    "Tutorial",
                    "OnFirstCreate tutorial content is unavailable; gameplay remains enabled.");
                return;
            }
            _presenter.TutorialAttemptPresentationCompleted -=
                OnTutorialAttemptPresentationCompleted;
            _resultDrivenTutorials.Clear();
            _tutorialDirector?.Dispose();
            var targets = new TutorialTargetRegistry()
                .Register("combat.enemy",
                    _sceneEnemy != null ? _sceneEnemy.rectTransform : null,
                    enemyTutorialFocusNormalizedRect,
                    () => _presenter != null && _presenter.RequestTutorialAttack())
                .Register("combat.attack",
                    root.Q<VisualElement>("combat-attack-button"),
                    () => _presenter != null && _presenter.RequestTutorialAttack());
            _tutorialDirector = new TutorialDirector(
                this,
                root,
                sequence,
                new LifecycleTutorialProgressStore(PlayerLifecycleRuntime.Commands),
                PlayerSessionStore.Instance,
                _presenter,
                _interactionGate,
                targets,
                reducedMotion);
            _tutorialDirector.Initialize();

            SocialProfile.SocialProfileCompositionRoot social =
                GetComponent<SocialProfile.SocialProfileCompositionRoot>();
            MainMenuPanelHostProvider panelProvider =
                GetComponent<MainMenuPanelHostProvider>();
            _rankTutorialDirector?.Dispose();
            _rankTutorialDirector = null;
            if (catalog.TryGet(TutorialDirector.OnFirstRankChangeId,
                    out PowerMath.Gameplay.Tutorial.TutorialSequence rankSequence))
            {
                var rankTargets = new TutorialTargetRegistry()
                    .Register("profile.open", root.Q<VisualElement>("profile-panel"),
                        () => social != null && social.TryOpenProfileForTutorial())
                    .Register("profile.currency", root.Q<VisualElement>("profile-ranks"),
                        () => true)
                    .Register("profile.close", root.Q<VisualElement>("profile-analytics-close"),
                        () => social != null && social.TryCloseProfileForTutorial())
                    .Register("leaderboard.open", root.Q<VisualElement>("leaderboard"),
                        () => social != null && social.TryOpenLeaderboardForTutorial())
                    .Register("leaderboard.currency",
                        root.Q<VisualElement>("leaderboard-self-currencies"),
                        () => true);
                _rankTutorialDirector = new TutorialDirector(
                    this,
                    root,
                    rankSequence,
                    new LifecycleTutorialProgressStore(PlayerLifecycleRuntime.Commands),
                    PlayerSessionStore.Instance,
                    _presenter,
                    _interactionGate,
                    rankTargets,
                    reducedMotion,
                    autoQueueOnCreate: false,
                    autoQueueLegacyRankChange: true,
                    ownsCombatCheckpoints: false,
                    allowExternalGate: () => panelProvider?.Host?.OpenPanel ==
                            MainMenuPanelId.ProfileAnalytics ||
                        panelProvider?.Host?.OpenPanel == MainMenuPanelId.Leaderboard);
                _rankTutorialDirector.Initialize();
                _resultDrivenTutorials.Add(new ResultDrivenTutorialBinding(
                    _rankTutorialDirector,
                    result => result.RankTransition.Changed
                        ? result.RankTransition.IsDemotion ? "demotion" : "promotion"
                        : null));
            }

            _enemySurviveTutorialDirector?.Dispose();
            _enemySurviveTutorialDirector = null;
            if (catalog.TryGet(TutorialDirector.OnFirstEnemySurviveId,
                    out PowerMath.Gameplay.Tutorial.TutorialSequence surviveSequence))
            {
                var surviveTargets = new TutorialTargetRegistry()
                    .Register("combat.enemy-actions",
                        root.Q<VisualElement>("combat-enemy-actions"),
                        () => true);
                _enemySurviveTutorialDirector = new TutorialDirector(
                    this,
                    root,
                    surviveSequence,
                    new LifecycleTutorialProgressStore(PlayerLifecycleRuntime.Commands),
                    PlayerSessionStore.Instance,
                    _presenter,
                    _interactionGate,
                    surviveTargets,
                    reducedMotion,
                    autoQueueOnCreate: false,
                    ownsCombatCheckpoints: false);
                _enemySurviveTutorialDirector.Initialize();
                _resultDrivenTutorials.Add(new ResultDrivenTutorialBinding(
                    _enemySurviveTutorialDirector,
                    result => TutorialCombatTriggerPolicy.IsFirstEnemySurvive(result)
                        ? string.Empty
                        : null));
            }

            if (_resultDrivenTutorials.Count > 0)
                _presenter.TutorialAttemptPresentationCompleted +=
                    OnTutorialAttemptPresentationCompleted;
        }

        private void OnTutorialAttemptPresentationCompleted(
            CombatTutorialResult result)
        {
            // This callback runs in the same frame as the authoritative result.
            // Reserve the first qualifying tutorial before its queue coroutine
            // performs persistence work on a later frame.
            for (int index = 0; index < _resultDrivenTutorials.Count; index++)
            {
                ResultDrivenTutorialBinding binding = _resultDrivenTutorials[index];
                if (binding.ResolveVariant(result) == null) continue;
                binding.Director.TryReservePendingOwnership();
                break;
            }
            StartCoroutine(QueueResultDrivenTutorials(result));
        }

        private IEnumerator QueueResultDrivenTutorials(CombatTutorialResult result)
        {
            long triggerRecordedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            for (int index = 0; index < _resultDrivenTutorials.Count; index++)
            {
                ResultDrivenTutorialBinding binding = _resultDrivenTutorials[index];
                string variant = binding.ResolveVariant(result);
                if (variant == null) continue;
                yield return binding.Director.QueueFromTrigger(
                    triggerRecordedAt, variant);
            }
        }

        private sealed class ResultDrivenTutorialBinding
        {
            public ResultDrivenTutorialBinding(
                TutorialDirector director,
                Func<CombatTutorialResult, string> resolveVariant)
            {
                Director = director ?? throw new ArgumentNullException(nameof(director));
                ResolveVariant = resolveVariant ??
                    throw new ArgumentNullException(nameof(resolveVariant));
            }

            public TutorialDirector Director { get; }
            public Func<CombatTutorialResult, string> ResolveVariant { get; }
        }

        private IEnumerator PublishTutorialReadyWhenStable()
        {
            while (_presenter != null && !_presenter.IsStableForTutorial)
                yield return null;
            _presenter?.PublishTutorialLobbyStable();
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
            var questions = new List<ChallengeQuestionDefinition>();
            foreach (AcademicRank rank in new[]
            {
                AcademicRank.Silver,
                AcademicRank.Gold,
                AcademicRank.Diamond
            })
            {
                QuestionDefinition[] rankQuestions = catalog.GetRankQuestions(rank).ToArray();
                for (int index = 0; index < rankQuestions.Length; index++)
                {
                    QuestionDefinition question = rankQuestions[index];
                    questions.Add(new ChallengeQuestionDefinition(
                        ChallengeQuestionId.Create(rank, index + 1),
                        question.VideoUri,
                        question.CorrectAnswer,
                        question.YouTubeVideoId));
                }
            }
            var documents = new Dictionary<string, IReadOnlyList<ChallengeQuestionDefinition>>(
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
            _scenePet = GameObject.Find("petPresentation")?.GetComponent<LegacyImage>();
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
            if (_scenePet != null)
            {
                _scenePet.raycastTarget = false;
                BindEquippedPetPresentation();
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
                _playerActor.ConfigureInteractionEligibility(() =>
                {
                    if (_interactionGate == null) return true;
                    bool allowed = _interactionGate.IsAllowed(InteractionScope.Lobby);
                    if (!allowed)
                    {
                        PowerMath.Diagnostics.AppLog.Warning(
                            "Combat",
                            $"[PlayerActor] Eligibility rejected: Gate active leases: {_interactionGate.ActiveLeasesSummary}");
                    }
                    return allowed;
                });
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
                _enemyActor.ConfigureInteractionEligibility(() =>
                {
                    if (_interactionGate == null) return true;
                    bool allowed = _interactionGate.IsAllowed(InteractionScope.Lobby);
                    if (!allowed)
                    {
                        PowerMath.Diagnostics.AppLog.Warning(
                            "Combat",
                            $"[EnemyActor] Eligibility rejected: Gate active leases: {_interactionGate.ActiveLeasesSummary}");
                    }
                    return allowed;
                });
                _enemyDamageAnchor = _enemyActor.DamageTextAnchor;
            }
            if (_scenePet != null)
            {
                _petActor = _scenePet.GetComponent<ActorPresentationController>();
                if (_petActor == null)
                    _petActor = _scenePet.gameObject.AddComponent<ActorPresentationController>();
                _petActor.Initialize(
                    PowerMath.Gameplay.Combat.Presentation.PresentationActor.Pet,
                    reducedMotion,
                    combatJuiceProfile);
                _petActor.ConfigureSprites(_scenePet.sprite);
                if (_scenePet.sprite == null)
                    _petActor.CancelAndApply(
                        PowerMath.Gameplay.Combat.Presentation.ActorVisualState.Hidden);
            }
        }

        private void BindEquippedPetPresentation()
        {
            if (_scenePet == null) return;
            string equippedPetId = PlayerSessionStore.Instance?.Snapshot?.loadout?.petId;
            _boundPetId = equippedPetId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(equippedPetId) || petGachaCatalog == null ||
                !petGachaCatalog.TryResolvePet(
                    equippedPetId,
                    out PetDefinition pet,
                    out _))
            {
                _scenePet.sprite = null;
                _scenePet.gameObject.SetActive(false);
                return;
            }

            _scenePet.sprite = pet.PreviewSprite;
            _scenePet.preserveAspect = true;
            _scenePet.gameObject.SetActive(_scenePet.sprite != null);
        }

        private void OnActorTapped()
        {
            _combatAudio?.PlayActorClick();
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

            _impactBurst = GetComponent<CombatImpactBurstPlayer>();
            if (_impactBurst == null)
                _impactBurst = gameObject.AddComponent<CombatImpactBurstPlayer>();
            _impactBurst.Initialize(
                fxRoot,
                uiCamera,
                combatJuiceProfile,
                runtimeSettings != null && runtimeSettings.ReducedMotion);

            _floatingText.Initialize(
                fxRoot,
                uiCamera,
                floatingCombatTextPrefab,
                style,
                runtimeSettings != null && runtimeSettings.ReducedMotion);

            _floatingRewardText = GetComponent<FloatingRewardTextService>();
            if (_floatingRewardText == null)
                _floatingRewardText = gameObject.AddComponent<FloatingRewardTextService>();
            FloatingRewardTextStyleDefinition rewardStyle = floatingRewardTextStyle != null
                ? floatingRewardTextStyle
                : Resources.Load<FloatingRewardTextStyleDefinition>(
                    "FloatingRewardTextStyle");
            _floatingRewardText.Initialize(
                fxRoot,
                uiCamera,
                floatingRewardTextPrefab,
                rewardStyle,
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
            EnsurePetPresentationObject(worldRoot);
            EnsureBackgroundTransitionLayer(worldRoot);
            return worldRoot;
        }

        private void EnsurePetPresentationObject(RectTransform worldRoot)
        {
            if (worldRoot == null) return;
            if (_scenePet == null)
            {
                Transform inRoot = worldRoot.Find("petPresentation");
                if (inRoot != null)
                {
                    _scenePet = inRoot.GetComponent<LegacyImage>();
                }
                else
                {
                    GameObject outside = GameObject.Find("petPresentation");
                    if (outside != null)
                    {
                        outside.transform.SetParent(worldRoot, true);
                        _scenePet = outside.GetComponent<LegacyImage>();
                    }
                    else
                    {
                        var petGo = new GameObject(
                            "petPresentation",
                            typeof(RectTransform),
                            typeof(CanvasRenderer),
                            typeof(LegacyImage));
                        petGo.transform.SetParent(worldRoot, false);
                        RectTransform rect = petGo.GetComponent<RectTransform>();
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.sizeDelta = new Vector2(160f, 160f);
                        if (_scenePlayer != null)
                        {
                            rect.anchoredPosition = _scenePlayer.rectTransform.anchoredPosition + new Vector2(130f, -40f);
                        }
                        else
                        {
                            rect.anchoredPosition = new Vector2(-150f, -100f);
                        }
                        _scenePet = petGo.GetComponent<LegacyImage>();
                    }
                }
            }
            else if (_scenePet.transform.parent != worldRoot)
            {
                _scenePet.transform.SetParent(worldRoot, true);
            }

            if (_scenePet != null)
            {
                _scenePet.raycastTarget = false;
                _scenePet.preserveAspect = true;
                if (_sceneEnemy != null)
                {
                    int enemyIndex = _sceneEnemy.transform.GetSiblingIndex();
                    _scenePet.transform.SetSiblingIndex(Mathf.Max(0, enemyIndex));
                }
                BindEquippedPetPresentation();
            }
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

        private IEnumerator CrossfadeBiomeBackground(
            CombatSnapshot destination,
            float duration)
        {
            Sprite targetSprite = ResolveBackground(destination);
            if (targetSprite == null || _sceneBackground == null)
            {
                yield break;
            }

            if (_sceneBackground.overrideSprite == targetSprite)
            {
                yield break;
            }

            float safeDuration = Mathf.Max(0.1f, duration);
            try
            {
                if (_sceneBackgroundTransition != null)
                {
                    _sceneBackgroundTransition.overrideSprite = targetSprite;
                    _sceneBackgroundTransition.color =
                        new Color(1f, 1f, 1f, 0f);
                    _sceneBackgroundTransition.gameObject.SetActive(true);

                    float overlayElapsed = 0f;
                    while (overlayElapsed < safeDuration)
                    {
                        overlayElapsed += Time.unscaledDeltaTime > 0f
                            ? Time.unscaledDeltaTime : 0.02f;
                        float alpha = Mathf.SmoothStep(
                            0f, 1f,
                            Mathf.Clamp01(overlayElapsed / safeDuration));
                        _sceneBackgroundTransition.color =
                            new Color(1f, 1f, 1f, alpha);
                        yield return null;
                    }
                    yield break;
                }

                float halfDuration = safeDuration * 0.5f;
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime > 0f
                        ? Time.unscaledDeltaTime : 0.02f;
                    float alpha = 1f - Mathf.SmoothStep(
                        0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
                    _sceneBackground.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }

                // Switch only after the outgoing image is fully invisible.
                _sceneBackground.color = new Color(1f, 1f, 1f, 0f);
                _sceneBackground.overrideSprite = targetSprite;

                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime > 0f
                        ? Time.unscaledDeltaTime : 0.02f;
                    float alpha = Mathf.SmoothStep(
                        0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
                    _sceneBackground.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
            }
            finally
            {
                // The destination already belongs to an accepted result. If a
                // presentation coroutine is interrupted, leave a stable saved view.
                _sceneBackground.overrideSprite = targetSprite;
                _sceneBackground.color = Color.white;
                if (_sceneBackgroundTransition != null)
                {
                    _sceneBackgroundTransition.color =
                        new Color(1f, 1f, 1f, 0f);
                    _sceneBackgroundTransition.overrideSprite = null;
                    _sceneBackgroundTransition.gameObject.SetActive(false);
                }
            }
        }

        private static void StartBiomeMusic(CombatSnapshot destination)
        {
            if (destination == null) return;
            PowerMath.Audio.MusicController music = PowerMath.Audio.MusicController.Instance;
            if (music == null) return;

            bool isStage1 = destination.Stage.Value == 1;
            bool isDifferentBiome = !string.Equals(music.CurrentBattleBiomeId, destination.BiomeId, System.StringComparison.OrdinalIgnoreCase);

            // Request new biome music if the biome changed or if returning to stage 1
            if (isDifferentBiome || isStage1)
            {
                music.PlayBattleMusic(destination.BiomeId, forceRestart: isStage1);
            }
        }

        private IEnumerator PrewarmBiomePresentationAssets()
        {
            if (stageMapDefinition == null)
            {
                yield break;
            }

            var textures = new HashSet<Texture>();
            PowerMath.Audio.MusicController music =
                PowerMath.Audio.MusicController.Instance;
            foreach (BiomeDefinition biome in stageMapDefinition.Biomes)
            {
                if (biome == null)
                {
                    continue;
                }

                foreach (EnemyDefinition monster in biome.NormalMonsters)
                {
                    music.PreloadClip(monster?.BattleMusic?.Clip);
                }
                foreach (BiomeDefinition.BossBinding binding in biome.BossBindings)
                {
                    music.PreloadClip(binding?.monster?.BattleMusic?.Clip);
                }

                Sprite[] backgrounds =
                {
                    biome.BackgroundSprite,
                    biome.SecondaryBackgroundSprite
                };
                for (int index = 0; index < backgrounds.Length; index++)
                {
                    Texture texture = backgrounds[index]?.texture;
                    if (texture == null || !textures.Add(texture))
                    {
                        continue;
                    }

                    // Force the first GPU upload away from the actual biome
                    // transition, spreading large backgrounds across frames.
                    texture.GetNativeTexturePtr();
                    yield return null;
                }
            }
        }

        private void RenderBiomeOnCanvas(CombatSnapshot snapshot)
        {
            Sprite sprite = ResolveBackground(snapshot);
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
            bool isStage1 = snapshot != null && snapshot.Stage.Value == 1;
            PowerMath.Audio.MusicController.Instance.PlayBattleMusic(snapshot?.BiomeId, forceRestart: isStage1);
        }

        private bool RequiresBackgroundTransition(CombatSnapshot snapshot)
        {
            if (snapshot == null) return false;
            BiomeDefinition biome = stageMapDefinition?.FindBiome(snapshot.BiomeId);
            if (biome != null && biome.MidpointStage > 0 &&
                snapshot.Stage.Value == biome.MidpointStage &&
                biome.SecondaryBackgroundSprite != null)
            {
                return true;
            }
            Sprite desired = ResolveBackground(snapshot);
            return desired != null && _sceneBackground != null &&
                _sceneBackground.overrideSprite != desired;
        }

        private Sprite ResolveBackground(CombatSnapshot snapshot)
        {
            if (snapshot == null) return null;
            BiomeDefinition biome = stageMapDefinition?.FindBiome(snapshot.BiomeId);
            return biome?.ResolveBackground(snapshot.Stage.Value);
        }

        private void RenderEncounterOnCanvas(string encounterId)
        {
            EnemyDefinition monster = stageMapDefinition?.FindMonster(encounterId);
            Sprite sprite = monster?.EnemySprite;
            EventDefinition eventDefinition = stageMapDefinition?.FindEvent(encounterId);
            if (sprite == null) sprite = eventDefinition?.EventSprite;
            if (sprite == null) sprite = ResolveFallbackEnemySprite();
            StageEncounterKind kind = monster?.EncounterKind ??
                StageEncounterKind.ChallengeEvent;
            bool isBigBoss = kind == StageEncounterKind.BigBoss ||
                kind == StageEncounterKind.FinalBoss;
            PowerMath.Audio.MusicController.Instance.SetEncounterMusicOverride(
                monster?.BattleMusic,
                isBigBoss);
            if (_sceneEnemy != null && sprite != null)
            {
                _enemyActor?.ConfigureDeathProfile(isBigBoss);
                PowerMath.Audio.IEnemySfxProfile customProfile = (PowerMath.Audio.IEnemySfxProfile)monster ?? (PowerMath.Audio.IEnemySfxProfile)eventDefinition;
                _enemyActor?.ConfigureSfxProfile(customProfile, kind.ToString());
                // Actor animation chooses its own state sprite and clears Image's
                // override. Rebind the actor too so Appear/Idle retain this encounter.
                _enemyActor?.ConfigureSprites(sprite);
                _sceneEnemy.overrideSprite = sprite;
                _sceneEnemy.gameObject.SetActive(true);
            }
        }

        private string ResolveLocalizedEnemyName(string encounterId)
        {
            EnemyDefinition monster = stageMapDefinition?.FindMonster(encounterId);
            if (monster != null)
            {
                return monster.GetDisplayName(PowerMath.Localization.LocalizationService.Locale);
            }
            EventDefinition eventDefinition = stageMapDefinition?.FindEvent(encounterId);
            if (eventDefinition != null)
                return eventDefinition.GetDisplayName(PowerMath.Localization.LocalizationService.Locale);
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

        private static ChallengeQuestionSequenceSnapshot ToChallengeQuestionSnapshot(
            PlayerSnapshot.ChallengeQuestionSequenceData source)
        {
            return source == null
                ? default
                : new ChallengeQuestionSequenceSnapshot(
                    source.silverCursor,
                    source.goldCursor,
                    source.diamondCursor,
                    source.reservedDocumentId,
                    source.reservedQuestionId);
        }

        private static bool TryMapEventSchedule(
            PlayerSnapshot.ActiveRunData run,
            StageMapData map,
            out EventScheduleSnapshot schedule)
        {
            schedule = null;
            if (run == null || run.eventScheduleVersion == 0)
                return false;
            if (run.eventScheduleVersion != EventScheduleSnapshot.CurrentVersion)
                throw new System.InvalidOperationException(
                    "The active run uses an unsupported Event schedule version.");
            if (string.IsNullOrWhiteSpace(run.eventScheduleCatalogVersion) ||
                string.IsNullOrWhiteSpace(run.eventScheduleEventId) ||
                run.eventScheduleStages == null)
                throw new System.InvalidOperationException(
                    "The active run Event schedule is incomplete.");
            if (!string.Equals(run.eventScheduleCatalogVersion, map.CatalogVersion,
                    System.StringComparison.Ordinal))
                throw new System.InvalidOperationException(
                    "The active run uses a different Stage Map catalog version.");
            schedule = new EventScheduleSnapshot(
                run.eventScheduleCatalogVersion,
                run.eventScheduleEventId,
                run.eventScheduleStages,
                run.eventChanceBasisPoints,
                run.petEventMultiplierBasisPoints,
                run.eventScheduleVersion);
            return true;
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
                UnityEngine.Object.FindAnyObjectByType<ProfileActivityTracker>()
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
                    run.eventAttemptOrdinal,
                    resolver.Schedule,
                    run.stageAttackCount,
                    run.bigBossesDefeated,
                    run.pendingPetFollowUpDamage);
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
            if (source.version < 0 ||
                source.version > AttemptPresentationReceipt.CurrentVersion)
            {
                error = "This saved combat result requires a newer PowerMath version.";
                return false;
            }
            // V1 receipts predate explicit persisted versioning in some player
            // documents. Missing version is therefore normalized to the oldest
            // schema whose invariant fields are still fully represented.
            int receiptVersion = source.version == 0
                ? AttemptPresentationReceipt.MinimumSupportedVersion
                : source.version;
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
                PetFollowUpPresentationReceipt petFollowUp = null;
                if (source.petFollowUp != null)
                {
                    if (!TryMapPresentationSnapshot(
                            source.petFollowUp.target,
                            out CombatPresentationSnapshot petTarget))
                    {
                        error = "The saved pet follow-up target is invalid.";
                        return false;
                    }
                    petFollowUp = new PetFollowUpPresentationReceipt(
                        source.petFollowUp.damage,
                        source.petFollowUp.isCritical,
                        petTarget,
                        source.petFollowUp.enemyHpAfter,
                        source.petFollowUp.enemyDefeated,
                        source.petFollowUp.stageAdvanced,
                        source.petFollowUp.carried);
                }
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
                    receiptVersion,
                    source.enemyFled,
                    source.powerCoinsGranted,
                    source.resultingPowerCoins,
                    receiptVersion >= 3 ? source.playerDamage : source.finalDamage,
                    receiptVersion >= 3
                        ? source.playerEnemyHpAfter
                        : source.resolvedEnemyHpAfter,
                    petFollowUp);
                return true;
            }
            catch (System.ArgumentException ex)
            {
                error = $"The saved combat presentation receipt failed validation: {ex.Message}";
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

        private void ResolveResultStickers()
        {
            if (stickerCorrect == null)
            {
#if UNITY_EDITOR
                stickerCorrect = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Project/Art/Character/Sticker_Power_Correct.PNG");
#endif
                if (stickerCorrect == null)
                {
                    stickerCorrect = Resources.Load<Sprite>("Character/Sticker_Power_Correct")
                        ?? Resources.Load<Sprite>("Sticker_Power_Correct");
                }
            }

            if (stickerFail == null)
            {
#if UNITY_EDITOR
                stickerFail = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Project/Art/Character/Sticker_Power_Fail.PNG");
#endif
                if (stickerFail == null)
                {
                    stickerFail = Resources.Load<Sprite>("Character/Sticker_Power_Fail")
                        ?? Resources.Load<Sprite>("Sticker_Power_Fail");
                }
            }
        }
    }
}
