using System.Collections;
using System.Collections.Generic;
using PowerMath.Gameplay.Combat.Unity;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public interface IMainMenuTransitionPlayer
    {
        bool IsPlaying { get; }
        void NotifySessionReady();
        void CancelAndApplyFinalState();
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuTransitionController : MonoBehaviour,
        IMainMenuTransitionPlayer
    {
        [Header("Settings")]
        [Tooltip("Tunable presentation timings and optional transition audio.")]
        [SerializeField] private MainMenuTransitionSettingsDefinition settings;
        [Tooltip("Existing Main Menu accessibility source.")]
        [SerializeField] private CombatRuntimeSettingsDefinition runtimeSettings;

        [Header("Canvas Art")]
        [Tooltip("Optional left-side Player art. Missing art is skipped safely.")]
        [SerializeField] private RectTransform playerArt;
        [Tooltip("Optional alpha controller for Player art.")]
        [SerializeField] private CanvasGroup playerCanvasGroup;
        [Tooltip("Optional right-side Enemy art.")]
        [SerializeField] private RectTransform enemyArt;
        [Tooltip("Optional alpha controller for Enemy art.")]
        [SerializeField] private CanvasGroup enemyCanvasGroup;

        [Header("Audio")]
        [Tooltip("Optional shared UI audio source.")]
        [SerializeField] private AudioSource audioSource;

        private MainMenuTransitionView _view;
        private IUiMotionDriver _motionDriver;
        private Coroutine _activeRoutine;
        private readonly List<UiMotionHandle> _activeTweens =
            new List<UiMotionHandle>();
        private Vector2 _playerFinalPosition;
        private Vector2 _enemyFinalPosition;
        private bool _bootstrapPlayed;
        private int _generation;

        public bool IsPlaying => _activeRoutine != null;

        private void Awake()
        {
            if (settings == null)
                settings = Resources.Load<MainMenuTransitionSettingsDefinition>(
                    "MainMenuTransitionSettings");
            if (runtimeSettings == null)
                runtimeSettings = Resources.Load<CombatRuntimeSettingsDefinition>(
                    "CombatRuntimeSettings");
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            UiMotionDriverProvider motionProvider =
                GetComponent<UiMotionDriverProvider>();
            if (motionProvider == null)
                motionProvider = gameObject.AddComponent<UiMotionDriverProvider>();
            motionProvider.SetReducedMotion(IsReducedMotion());
            _motionDriver = motionProvider.Driver;

            try
            {
                _view = new MainMenuTransitionView(
                    GetComponent<UIDocument>().rootVisualElement);
            }
            catch (System.InvalidOperationException exception)
            {
                Debug.LogError(exception.Message);
                enabled = false;
                return;
            }

            CacheAuthoredCanvasState();
            _view.PrepareBootstrap(IsReducedMotion());
        }

        public Vector2 PlayerRestPosition => _playerFinalPosition;
        public Vector2 EnemyRestPosition => _enemyFinalPosition;

        private void Start()
        {
            StartCoroutine(EnforceTransitionStartupSafety());
        }

        private IEnumerator EnforceTransitionStartupSafety()
        {
            const float safetyTimeoutSeconds = 6f;
            float elapsed = 0f;
            while (elapsed < safetyTimeoutSeconds)
            {
                if (_bootstrapPlayed && _activeRoutine == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (isActiveAndEnabled && (_activeRoutine != null || !_bootstrapPlayed))
            {
                Debug.LogWarning("[MainMenuTransitionController] Transition did not complete within safety window. Forcing UI reveal.");
                CancelAndApplyFinalState();
            }
        }

        private void OnDisable()
        {
            CancelAndApplyFinalState();
        }

        public void NotifySessionReady()
        {
            if (_bootstrapPlayed || !isActiveAndEnabled || _view == null)
                return;

            _bootstrapPlayed = true;
            StartTransition(PlayBootstrap(++_generation));
        }

        public void CancelAndApplyFinalState()
        {
            _generation++;
            CancelActiveTweens();
            if (_activeRoutine != null)
            {
                StopCoroutine(_activeRoutine);
                _activeRoutine = null;
            }

            ApplyCanvasFinalState();
            _view?.ApplyFinalState();
        }

        private void StartTransition(IEnumerator routine)
        {
            CancelActiveTweens();
            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);
            ApplyCanvasFinalState();
            _view.ApplyFinalState();
            _activeRoutine = StartCoroutine(routine);
        }

        private IEnumerator PlayBootstrap(int generation)
        {
            try
            {
                bool reduced = IsReducedMotion();
                _motionDriver.SetReducedMotion(reduced);
                _view.PrepareBootstrap(reduced);
                PrepareCanvasEntrance(reduced);
                yield return null;
                yield return WaitUnscaled(settings.InitialSettleSeconds, generation);

                if (!IsCurrent(generation)) yield break;
                _view.ShowBattleTitle();
                PlayClip(settings.BattleStartImpact);
                yield return WaitUnscaled(settings.TitleEntrySeconds, generation);
                yield return WaitUnscaled(settings.TitleHoldSeconds, generation);

                if (!IsCurrent(generation)) yield break;
                _view.HideBattleTitle();
                yield return WaitUnscaled(settings.TitleExitSeconds, generation);

                if (!IsCurrent(generation)) yield break;
                _view.RevealScene();
                PlayClip(settings.CharacterWhoosh);
                if (reduced)
                {
                    yield return WaitUnscaled(
                        settings.ReducedCrossfadeSeconds,
                        generation);
                    ApplyCanvasFinalState();
                }
                else
                {
                    yield return AnimateCanvasEntrance(generation);
                }

                if (!IsCurrent(generation)) yield break;
                yield return AnimateSessionUi(generation, reduced);
            }
            finally
            {
                if (IsCurrent(generation))
                {
                    Complete(generation);
                }
            }
        }

        private IEnumerator AnimateSessionUi(int generation, bool reduced)
        {
            float duration = reduced
                ? settings.ReducedCrossfadeSeconds
                : settings.SessionEntrySeconds;

            PlayClip(settings.CharacterWhoosh);
            yield return TweenProgress(
                generation,
                duration,
                _view.ApplySessionUiProgress,
                reduced ? UiMotionEasing.OutCubic : UiMotionEasing.OutBack);
        }

        private IEnumerator AnimateCanvasEntrance(int generation)
        {
            float duration = Mathf.Max(0.01f, settings.CharacterEntrySeconds);
            float enemyDelay = Mathf.Max(0f, settings.CharacterStaggerSeconds);
            bool playerComplete = playerArt == null;
            bool enemyComplete = enemyArt == null;

            if (playerArt != null)
            {
                Track(_motionDriver.Tween(
                    playerArt,
                    UiMotionChannel.Lifecycle,
                    duration,
                    settings.CharacterEase,
                    ApplyPlayerCanvasSample,
                    () => playerComplete = true));
            }

            if (enemyArt != null)
            {
                Track(_motionDriver.Tween(
                    enemyArt,
                    UiMotionChannel.Lifecycle,
                    duration,
                    settings.CharacterEase,
                    ApplyEnemyCanvasSample,
                    () => enemyComplete = true,
                    enemyDelay));
            }

            float timeout = duration + enemyDelay + 1.0f;
            float elapsed = 0f;
            while ((!playerComplete || !enemyComplete) && elapsed < timeout)
            {
                if (!IsCurrent(generation)) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ApplyCanvasFinalState();
        }

        private IEnumerator TweenProgress(
            int generation,
            float seconds,
            System.Action<float> apply,
            UiMotionEasing ease)
        {
            if (seconds <= 0f)
            {
                apply(1f);
                yield break;
            }

            bool complete = false;
            UiMotionHandle tween = _motionDriver.Tween(
                _view.Screen,
                UiMotionChannel.Lifecycle,
                seconds,
                ease,
                apply,
                () => complete = true);
            Track(tween);

            while (!complete)
            {
                if (!IsCurrent(generation))
                {
                    tween.Cancel();
                    yield break;
                }
                yield return null;
            }

            apply(1f);
        }

        private IEnumerator WaitUnscaled(float seconds, int generation)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (!IsCurrent(generation)) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Complete(int generation)
        {
            if (!IsCurrent(generation)) return;
            ApplyCanvasFinalState();
            _view.ApplyFinalState();
            PlayClip(settings.SessionComplete);
            _activeTweens.Clear();
            _activeRoutine = null;
        }

        private void Track(UiMotionHandle tween)
        {
            if (tween != null && tween.IsActive)
                _activeTweens.Add(tween);
        }

        private void CancelActiveTweens()
        {
            for (int index = 0; index < _activeTweens.Count; index++)
                _activeTweens[index]?.Cancel();
            _activeTweens.Clear();
        }

        private void PrepareCanvasEntrance(bool reduced)
        {
            if (reduced)
            {
                SetCanvasAlpha(playerCanvasGroup, playerArt, 0f);
                SetCanvasAlpha(enemyCanvasGroup, enemyArt, 0f);
                return;
            }

            if (playerArt != null)
                playerArt.anchoredPosition = _playerFinalPosition +
                    Vector2.left * settings.PlayerTravelPixels;
            if (enemyArt != null)
                enemyArt.anchoredPosition = _enemyFinalPosition +
                    Vector2.right * settings.EnemyTravelPixels;
            SetCanvasAlpha(playerCanvasGroup, playerArt, 0f);
            SetCanvasAlpha(enemyCanvasGroup, enemyArt, 0f);
        }

        private void ApplyPlayerCanvasSample(float progress)
        {
            if (playerArt != null)
            {
                Vector2 start = _playerFinalPosition +
                    Vector2.left * settings.PlayerTravelPixels;
                playerArt.anchoredPosition = Vector2.LerpUnclamped(
                    start, _playerFinalPosition, progress);
            }
            SetCanvasAlpha(playerCanvasGroup, playerArt, progress);
        }

        private void ApplyEnemyCanvasSample(float progress)
        {
            if (enemyArt != null)
            {
                Vector2 start = _enemyFinalPosition +
                    Vector2.right * settings.EnemyTravelPixels;
                enemyArt.anchoredPosition = Vector2.LerpUnclamped(
                    start, _enemyFinalPosition, progress);
            }
            SetCanvasAlpha(enemyCanvasGroup, enemyArt, progress);
        }

        private void CacheAuthoredCanvasState()
        {
            if (playerArt == null || playerArt == enemyArt)
            {
                RectTransform autoPlayer = GameObject.Find("playerPresentation")?.GetComponent<RectTransform>();
                if (autoPlayer != null)
                {
                    playerArt = autoPlayer;
                    if (playerCanvasGroup == null)
                        playerCanvasGroup = autoPlayer.GetComponent<CanvasGroup>();
                }
            }

            if (enemyArt == null)
            {
                RectTransform autoEnemy = GameObject.Find("monsterPrefab")?.GetComponent<RectTransform>();
                if (autoEnemy != null)
                {
                    enemyArt = autoEnemy;
                    if (enemyCanvasGroup == null)
                        enemyCanvasGroup = autoEnemy.GetComponent<CanvasGroup>();
                }
            }

            if (playerArt != null)
                _playerFinalPosition = playerArt.anchoredPosition;
            if (enemyArt != null)
                _enemyFinalPosition = enemyArt.anchoredPosition;
        }

        private void ApplyCanvasFinalState()
        {
            if (playerArt != null)
                playerArt.anchoredPosition = _playerFinalPosition;
            if (enemyArt != null)
                enemyArt.anchoredPosition = _enemyFinalPosition;
            SetCanvasAlpha(playerCanvasGroup, playerArt, 1f);
            SetCanvasAlpha(enemyCanvasGroup, enemyArt, 1f);
        }

        private static void SetCanvasAlpha(
            CanvasGroup group,
            RectTransform target,
            float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
                return;
            }

            if (target != null && target.TryGetComponent(out UnityEngine.UI.Graphic graphic))
            {
                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }

        private bool IsReducedMotion()
        {
            return runtimeSettings != null && runtimeSettings.ReducedMotion;
        }

        private bool IsCurrent(int generation)
        {
            return generation == _generation && isActiveAndEnabled;
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

    }
}
