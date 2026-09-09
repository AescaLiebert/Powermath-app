using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PowerMath.Localization;
using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using PowerMath.UI.Shared;

namespace PowerMath.PlayerLifecycle
{
    public sealed class PlayerPreparationPresenter : MonoBehaviour
    {
        // Starting values. Tune against the onboarding playtest plan after final audio is authored.
        private const float TransitionCoverSeconds = 0.34f;
        private const float TransitionRevealSeconds = 0.48f;
        private const float DetailEnterSeconds = 0.66f;
        private const float DetailExitSeconds = 0.38f;
        private const float DetailHoldPulseSeconds = 1.65f;
        private const float DetailDotDriftSeconds = 5.4f;
        private const float CompletionFlashSeconds = 0.68f;
        private const float CompletionCurtainInSeconds = 0.46f;
        private const float CompletionCurtainOutSeconds = 0.55f;
        private const float VideoPrepareTimeoutSeconds = 12f;

        private enum VideoPurpose
        {
            None,
            Opening,
            Selection
        }

        private PlayerPreparationView _view;
        private PlayerSessionStore _store;
        private IPlayerLifecycleCommands _commands;
        private IUiMotionDriver _motion;
        private Action _completed;
        private PlayerLifecycleCommand _pending;
        private CharacterPresentationCatalog _catalog;
        private OpeningSequenceDefinition _opening;
        private Coroutine _visualSequence;
        private UiMotionHandle _characterHold;
        private UiMotionHandle _dotHold;
        private VideoPlayer _video;
        private RenderTexture _videoTexture;
        private VideoPurpose _videoPurpose;
        private bool _videoPrepared;
        private bool _videoEnded;
        private bool _videoFailed;
        private bool _busy;
        private bool _completionTransition;
        private bool _destinationLoaded;
        private bool _destinationLoadFailed;
        private bool _keepTransitionCovered;
        private bool _revealAfterSave;
        private bool _reducedMotion;
        private string _preview;
        private string _name;

        public PlayerPreparationSequenceState State =>
            _view?.State ?? PlayerPreparationSequenceState.Hidden;

        public event Action DestinationTransitionCompleted;

        public void Initialize(
            VisualElement root,
            PlayerSessionStore store,
            IPlayerLifecycleCommands commands,
            Action completed)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _completed = completed ?? throw new ArgumentNullException(nameof(completed));
            _catalog = Resources.Load<CharacterPresentationCatalog>(
                "CharacterPresentationCatalog");
            _opening = Resources.Load<OpeningSequenceDefinition>("OpeningSequence");
            var runtimeSettings = Resources.Load<
                PowerMath.Gameplay.Combat.Unity.CombatRuntimeSettingsDefinition>(
                "CombatRuntimeSettings");
            _reducedMotion = runtimeSettings != null && runtimeSettings.ReducedMotion;
            _motion = new LeanTweenUiDriver(this, _reducedMotion);
            _view = new PlayerPreparationView(root, _reducedMotion);
            BindView();
            _view.RefreshLocale();
            LocalizationService.Changed += OnLocaleChanged;
            Render();
        }

        private void BindView()
        {
            _view.OpeningSkipRequested += OnOpeningSkipRequested;
            _view.CharacterRequested += OnCharacterRequested;
            _view.CharacterAccepted += OnCharacterAccepted;
            _view.DetailBackRequested += OnDetailBackRequested;
            _view.NameBackRequested += OnNameBackRequested;
            _view.NameChanged += OnNameChanged;
            _view.NameConfirmed += OnNameConfirmed;
        }

        private void UnbindView()
        {
            if (_view == null) return;
            _view.OpeningSkipRequested -= OnOpeningSkipRequested;
            _view.CharacterRequested -= OnCharacterRequested;
            _view.CharacterAccepted -= OnCharacterAccepted;
            _view.DetailBackRequested -= OnDetailBackRequested;
            _view.NameBackRequested -= OnNameBackRequested;
            _view.NameChanged -= OnNameChanged;
            _view.NameConfirmed -= OnNameConfirmed;
        }

        private void Render()
        {
            if (_view == null || _store?.Snapshot == null) return;

            PlayerSnapshot player = _store.Snapshot;
            if (PlayerLifecyclePolicy.IsComplete(player))
            {
                if (!_completionTransition) Finish();
                return;
            }

            StopVisualSequence();
            _view.RefreshLocale();
            _view.SetInteractive(!_busy && _pending == null);
            if (!_keepTransitionCovered) _view.HideTransition();

            if (string.Equals(player.onboarding?.phase, "opening", StringComparison.Ordinal))
            {
                _visualSequence = StartCoroutine(PlayOpeningExperience());
                return;
            }

            if (!string.IsNullOrWhiteSpace(_preview))
            {
                ShowCharacterDetail(PlayerPreparationSequenceState.CharacterSelectedHolding);
                StartHoldAnimation();
                ShowPendingRecovery();
                return;
            }

            if (string.Equals(player.onboarding?.phase, "character", StringComparison.Ordinal))
            {
                ShowCharacterSelection();
                ShowPendingRecovery();
                return;
            }

            if (string.Equals(player.onboarding?.phase, "name", StringComparison.Ordinal))
            {
                ShowNameEntry();
                ShowPendingRecovery();
                return;
            }

            _view.ShowError(LocalizationService.Get("errors.updateRequired"));
        }

        private IEnumerator PlayOpeningExperience()
        {
            CleanupVideo();
            _view.SetInteractive(!_busy && _pending == null);
            string[] lines = _opening?.GetNarrativeLines(LocalizationService.Locale);
            if (lines == null || lines.Length == 0)
            {
                lines = new[] { LocalizationService.Get("onboarding.openingTitle") };
            }

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index]?.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                _view.ShowNarrative(string.Empty);
                yield return RevealNarrativeLine(line);
                yield return new WaitForSecondsRealtime(
                    _reducedMotion ? 0.2f : ResolveOpeningValue(
                        definition => definition.lineHoldSeconds,
                        1.15f));
                yield return FadeNarrativeLine();
            }

            if (_opening != null && _opening.HasVideo)
            {
                yield return PlayOpeningVideo();
                yield break;
            }

            _view.ShowOpeningVideo(
                null,
                LocalizationService.Get("onboarding.videoUnavailable"));
        }

        private IEnumerator RevealNarrativeLine(string line)
        {
            List<string> textElements = GetTextElements(line);
            if (_reducedMotion)
            {
                _view.SetNarrativeText(line);
                _view.SetNarrativeOpacity(1f);
                yield break;
            }

            var builder = new StringBuilder(line.Length);
            float delay = ResolveOpeningValue(
                definition => definition.characterRevealSeconds,
                0.025f);
            _view.SetNarrativeOpacity(1f);
            for (int index = 0; index < textElements.Count; index++)
            {
                builder.Append(textElements[index]);
                _view.SetNarrativeText(builder.ToString());
                if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            }
        }

        private IEnumerator FadeNarrativeLine()
        {
            float duration = _reducedMotion
                ? 0.06f
                : ResolveOpeningValue(definition => definition.lineFadeSeconds, 0.42f);
            UiMotionHandle fade = _motion.Tween(
                _view.NarrativeLine,
                UiMotionChannel.Lifecycle,
                duration,
                UiMotionEasing.OutCubic,
                progress => _view.SetNarrativeOpacity(1f - progress));
            yield return WaitFor(fade);
        }

        private IEnumerator PlayOpeningVideo()
        {
            if (!PrepareVideo(VideoPurpose.Opening, false))
            {
                _view.ShowOpeningVideo(
                    null,
                    LocalizationService.Get("onboarding.videoUnavailable"));
                yield break;
            }

            _view.ShowOpeningVideo(
                _videoTexture,
                LocalizationService.Get("onboarding.videoPreparing"));
            _video.Prepare();
            float elapsed = 0f;
            while (!_videoPrepared && !_videoFailed && elapsed < VideoPrepareTimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_videoPrepared || _videoFailed)
            {
                _view.SetVideoStatus(LocalizationService.Get("onboarding.videoUnavailable"));
                yield break;
            }

            _view.SetVideoStatus(string.Empty);
            _video.Play();
            while (!_videoEnded && !_videoFailed) yield return null;
            if (_videoFailed)
            {
                _view.SetVideoStatus(LocalizationService.Get("onboarding.videoUnavailable"));
                yield break;
            }

            yield return PlayWhiteFlash();
            Submit(PlayerLifecycleCommandKind.CompleteOpening);
        }

        private void ShowCharacterSelection()
        {
            CleanupVideo();
            CharacterPresentationCatalog.Character ricko = _catalog?.Find("ricko");
            CharacterPresentationCatalog.Character stellar = _catalog?.Find("stellar");
            Sprite rickoArt = CharacterPlaceholderSprites.Resolve(
                ricko?.overviewArt ?? ricko?.selectionArt,
                "ricko");
            Sprite stellarArt = CharacterPlaceholderSprites.Resolve(
                stellar?.overviewArt ?? stellar?.selectionArt,
                "stellar");
            _view.ShowSelection(null, rickoArt, stellarArt);
            _view.SetInteractive(!_busy && _pending == null);
            PowerMath.Audio.MusicController.Instance.PlayLoginMusic();

            if (_catalog?.selectionVideo != null &&
                PrepareVideo(VideoPurpose.Selection, true))
            {
                _video.Prepare();
            }
        }

        private void ShowCharacterDetail(PlayerPreparationSequenceState state)
        {
            CharacterPresentationCatalog.Character definition = _catalog?.Find(_preview);
            Sprite art = CharacterPlaceholderSprites.Resolve(
                definition?.selectionArt,
                _preview);
            _view.ShowCharacterDetail(
                _preview,
                art,
                LocalizationService.Get("onboarding." + _preview),
                LocalizationService.Get("onboarding." + _preview + "Description"),
                state);
        }

        private void ShowNameEntry()
        {
            string selected = _store.Snapshot.onboarding.selectedCharacterId;
            CharacterPresentationCatalog.Character definition = _catalog?.Find(selected);
            Sprite art = CharacterPlaceholderSprites.Resolve(
                definition?.overviewArt ?? definition?.selectionArt,
                selected);
            string initial = _name ?? (_store.Snapshot.onboarding.legacyPlayer
                ? _store.Snapshot.profile.displayName
                : string.Empty);
            _name = initial;
            _view.ShowNameEntry(selected, art, initial);
            _view.SetInteractive(!_busy && _pending == null);
        }

        private void OnOpeningSkipRequested()
        {
            if (_busy || _pending != null ||
                (_view.State != PlayerPreparationSequenceState.Narrative &&
                 _view.State != PlayerPreparationSequenceState.OpeningVideo)) return;
            StopVisualSequence();
            CleanupVideo();
            Submit(PlayerLifecycleCommandKind.CompleteOpening);
        }

        private void OnCharacterRequested(string characterId)
        {
            if (_busy || _pending != null ||
                _view.State != PlayerPreparationSequenceState.CharacterSelection ||
                !PlayerLifecyclePolicy.IsCharacter(characterId)) return;
            _preview = characterId;
            CleanupVideo();
            StartVisualSequence(PlayCharacterEnter());
        }

        private IEnumerator PlayCharacterEnter()
        {
            _view.SetInteractive(false);
            _view.SetState(PlayerPreparationSequenceState.CharacterSelectedEntering);
            _view.ApplyTransitionProgress(0f, true);
            UiMotionHandle cover = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Lifecycle,
                MotionSeconds(TransitionCoverSeconds),
                UiMotionEasing.OutCubic,
                progress => _view.ApplyTransitionProgress(progress, true));
            yield return WaitFor(cover);

            ShowCharacterDetail(PlayerPreparationSequenceState.CharacterSelectedEntering);
            _view.ApplyDetailProgress(0f);
            UiMotionHandle reveal = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Lifecycle,
                MotionSeconds(TransitionRevealSeconds),
                UiMotionEasing.OutCubic,
                progress => _view.ApplyTransitionProgress(progress, false));
            UiMotionHandle enter = _motion.Tween(
                _view.DetailArt,
                UiMotionChannel.Lifecycle,
                MotionSeconds(DetailEnterSeconds),
                UiMotionEasing.OutBack,
                _view.ApplyDetailProgress);
            yield return WaitFor(reveal, enter);
            _view.HideTransition();
            _view.SetState(PlayerPreparationSequenceState.CharacterSelectedHolding);
            _view.SetInteractive(true);
            StartHoldAnimation();
        }

        private void OnCharacterAccepted()
        {
            if (_busy || _pending != null ||
                _view.State != PlayerPreparationSequenceState.CharacterSelectedHolding ||
                !PlayerLifecyclePolicy.IsCharacter(_preview)) return;
            StartVisualSequence(PlayCharacterExit(true));
        }

        private void OnDetailBackRequested()
        {
            if (_busy || _pending != null ||
                _view.State != PlayerPreparationSequenceState.CharacterSelectedHolding) return;
            StartVisualSequence(PlayCharacterExit(false));
        }

        private void OnNameBackRequested()
        {
            if (_busy || _pending != null ||
                _view.State != PlayerPreparationSequenceState.NameEntry) return;
            _preview = _store.Snapshot.onboarding.selectedCharacterId;
            StartVisualSequence(PlayNameExitToDetail());
        }

        private void OnNameChanged(string value)
        {
            _name = value;
            if (_view.State == PlayerPreparationSequenceState.NameEntry)
            {
                if (!string.IsNullOrWhiteSpace(value) &&
                    !PlayerLifecyclePolicy.TryNormalizeName(value, out _, out var result) &&
                    result.Reason == DisplayNameDenialReason.InappropriateContent)
                {
                    _view.ShowError(LocalizationService.Get(result.LocalizationKey));
                }
                else
                {
                    _view.ClearError();
                }
            }
        }

        private void OnNameConfirmed()
        {
            if (_busy || _pending != null ||
                _view.State != PlayerPreparationSequenceState.NameEntry) return;
            if (!PlayerLifecyclePolicy.TryNormalizeName(_name, out string normalized, out DisplayNameValidationResult result))
            {
                string message = LocalizationService.Get(result.LocalizationKey);
                PowerMath.UI.Core.StatusMessageService.ShowWarning(message);
                _view.ShowError(message);
                return;
            }

            _name = normalized;
            _view.SetNameValue(normalized);
            _view.SetState(PlayerPreparationSequenceState.Confirming);
            Submit(
                PlayerLifecycleCommandKind.CompletePreparation,
                _store.Snapshot.onboarding.selectedCharacterId);
        }

        private IEnumerator PlayCharacterExit(bool commit)
        {
            CancelHoldAnimation();
            _view.SetInteractive(false);
            _view.SetState(PlayerPreparationSequenceState.CharacterSelectedExiting);
            UiMotionHandle exit = _motion.Tween(
                _view.DetailArt,
                UiMotionChannel.Lifecycle,
                MotionSeconds(DetailExitSeconds),
                UiMotionEasing.OutCubic,
                _view.ApplyDetailExitProgress);
            UiMotionHandle cover = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Lifecycle,
                MotionSeconds(TransitionCoverSeconds),
                UiMotionEasing.OutCubic,
                progress => _view.ApplyTransitionProgress(progress, true));
            yield return WaitFor(exit, cover);

            if (commit)
            {
                _keepTransitionCovered = true;
                _revealAfterSave = true;
                Submit(PlayerLifecycleCommandKind.SelectCharacter, _preview);
                yield break;
            }

            _preview = null;
            ShowCharacterSelection();
            yield return RevealTransition();
            _view.SetInteractive(true);
        }

        private IEnumerator PlayNameExitToDetail()
        {
            _view.SetInteractive(false);
            _view.ApplyTransitionProgress(0f, true);
            UiMotionHandle cover = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Lifecycle,
                MotionSeconds(TransitionCoverSeconds),
                UiMotionEasing.OutCubic,
                progress => _view.ApplyTransitionProgress(progress, true));
            yield return WaitFor(cover);
            ShowCharacterDetail(PlayerPreparationSequenceState.CharacterSelectedEntering);
            _view.ApplyDetailProgress(0f);
            UiMotionHandle reveal = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Lifecycle,
                MotionSeconds(TransitionRevealSeconds),
                UiMotionEasing.OutCubic,
                progress => _view.ApplyTransitionProgress(progress, false));
            UiMotionHandle enter = _motion.Tween(
                _view.DetailArt,
                UiMotionChannel.Lifecycle,
                MotionSeconds(DetailEnterSeconds),
                UiMotionEasing.OutBack,
                _view.ApplyDetailProgress);
            yield return WaitFor(reveal, enter);
            _view.SetState(PlayerPreparationSequenceState.CharacterSelectedHolding);
            _view.SetInteractive(true);
            StartHoldAnimation();
        }

        private IEnumerator RevealTransition()
        {
            UiMotionHandle reveal = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Lifecycle,
                MotionSeconds(TransitionRevealSeconds),
                UiMotionEasing.OutCubic,
                progress => _view.ApplyTransitionProgress(progress, false));
            yield return WaitFor(reveal);
            _keepTransitionCovered = false;
            _view.HideTransition();
        }

        private void StartHoldAnimation()
        {
            CancelHoldAnimation();
            _view.SetState(PlayerPreparationSequenceState.CharacterSelectedHolding);
            _view.SetInteractive(!_busy && _pending == null);
            if (_reducedMotion) return;
            _characterHold = _motion.Tween(
                _view.DetailArt,
                UiMotionChannel.Ambient,
                DetailHoldPulseSeconds,
                UiMotionEasing.InOutSine,
                _view.ApplyHoldProgress,
                loopPingPong: true);
            _dotHold = _motion.Tween(
                _view.DetailAccent,
                UiMotionChannel.Ambient,
                DetailDotDriftSeconds,
                UiMotionEasing.InOutSine,
                _view.ApplyDotHoldProgress,
                loopPingPong: true);
        }

        private void CancelHoldAnimation()
        {
            _characterHold?.Cancel();
            _characterHold = null;
            _dotHold?.Cancel();
            _dotHold = null;
        }

        private void Submit(PlayerLifecycleCommandKind kind, string value = null)
        {
            if (_busy || _pending != null || _store?.Snapshot == null) return;
            _pending = new PlayerLifecycleCommand
            {
                kind = kind,
                value = value,
                displayName = _name,
                playerId = _store.Snapshot.playerId,
                expectedRevision = _store.Snapshot.revision,
                operationId = Guid.NewGuid().ToString("N")
            };
            StartCoroutine(SendPending());
        }

        private IEnumerator SendPending()
        {
            _busy = true;
            _view.SetInteractive(false);
            _view.ClearError();
            PlayerLifecycleCommand command = _pending;
            FirestoreRestClient.Failure? failure = null;
            PlayerSnapshot saved = null;
            yield return _commands.Execute(
                command,
                result => saved = result,
                error => failure = error);
            _busy = false;

            if (_store?.Snapshot?.playerId != command.playerId) yield break;
            if (saved != null)
            {
                _pending = null;
                bool completing = command.kind ==
                    PlayerLifecycleCommandKind.CompletePreparation;
                if (!completing && command.kind !=
                    PlayerLifecycleCommandKind.SelectCharacter)
                {
                    _preview = null;
                }
                _store.TryHydrate(new BootstrapResponse
                {
                    player = saved,
                    schemaVersion = saved.schemaVersion,
                    remembered = _store.IsRemembered
                });

                if (completing)
                {
                    _completionTransition = true;
                    _visualSequence = StartCoroutine(CompleteSceneTransition());
                    yield break;
                }

                if (command.kind == PlayerLifecycleCommandKind.SelectCharacter)
                {
                    _preview = null;
                }
                Render();
                if (_revealAfterSave)
                {
                    _revealAfterSave = false;
                    _visualSequence = StartCoroutine(RevealAfterSave());
                }
                yield break;
            }

            string errorKey = failure?.Kind == FirestoreRestClient.FailureKind.Conflict
                ? "errors.conflict"
                : "errors.save";
            _keepTransitionCovered = false;
            _revealAfterSave = false;
            Render();
            _view.ShowError(LocalizationService.Get(errorKey));
            if (failure?.Kind == FirestoreRestClient.FailureKind.Conflict)
            {
                _view.ShowRecovery(
                    LocalizationService.Get("common.reload"),
                    ReloadCurrentScene);
            }
            else
            {
                _view.ShowRecovery(
                    LocalizationService.Get("common.retry"),
                    () => StartCoroutine(SendPending()));
            }
        }

        private IEnumerator RevealAfterSave()
        {
            _view.SetInteractive(false);
            yield return RevealTransition();
            _view.SetInteractive(true);
        }

        public void NotifyDestinationSceneLoaded()
        {
            if (!_completionTransition) return;
            _destinationLoaded = true;
            _destinationLoadFailed = false;
        }

        public void NotifyDestinationSceneLoadFailed()
        {
            if (!_completionTransition) return;
            _destinationLoadFailed = true;
            _destinationLoaded = false;
        }

        private IEnumerator CompleteSceneTransition()
        {
            CleanupVideo();
            CancelHoldAnimation();
            _view.SetState(PlayerPreparationSequenceState.Completing);
            _view.SetInteractive(false);
            _view.SetCurtainOpacity(0f);
            _destinationLoaded = false;
            _destinationLoadFailed = false;

            UiMotionHandle cover = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Feedback,
                MotionSeconds(CompletionCurtainInSeconds),
                UiMotionEasing.InOutSine,
                _view.SetCurtainOpacity);
            yield return WaitFor(cover);
            _view.SetCurtainOpacity(1f);

            Action requestScene = _completed;
            _completed = null;
            if (requestScene == null)
            {
                _destinationLoadFailed = true;
            }
            else
            {
                try
                {
                    requestScene.Invoke();
                }
                catch (Exception exception)
                {
                    PowerMath.Diagnostics.AppLog.Error(
                        "Lifecycle",
                        "Main-menu scene transition could not start: " + exception.Message,
                        this);
                    _destinationLoadFailed = true;
                }
            }

            while (!_destinationLoaded && !_destinationLoadFailed)
                yield return null;

            UiMotionHandle reveal = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Feedback,
                MotionSeconds(CompletionCurtainOutSeconds),
                UiMotionEasing.InOutSine,
                progress => _view.SetCurtainOpacity(1f - progress));
            yield return WaitFor(reveal);
            _view.SetCurtainOpacity(0f);
            _completionTransition = false;
            _view.SetState(PlayerPreparationSequenceState.Hidden);

            if (_destinationLoaded)
            {
                DestinationTransitionCompleted?.Invoke();
            }
            Destroy(this);
        }

        private IEnumerator PlayWhiteFlash()
        {
            UiMotionHandle flash = _motion.Tween(
                _view.TransitionLayer,
                UiMotionChannel.Feedback,
                MotionSeconds(CompletionFlashSeconds),
                UiMotionEasing.InOutSine,
                _view.ApplyFlash);
            yield return WaitFor(flash);
            _view.ApplyFlash(0f);
        }

        private void ShowPendingRecovery()
        {
            if (_pending == null || _busy) return;
            _view.SetInteractive(false);
            _view.ShowRecovery(
                LocalizationService.Get("common.retry"),
                () => StartCoroutine(SendPending()));
        }

        private bool PrepareVideo(VideoPurpose purpose, bool looping)
        {
            CleanupVideo();
            _videoTexture = new RenderTexture(
                1280,
                720,
                0,
                RenderTextureFormat.ARGB32)
            {
                name = "PlayerPreparationVideo"
            };
            _videoTexture.Create();
            _video = gameObject.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
            _video.waitForFirstFrame = true;
            _video.skipOnDrop = true;
            _video.isLooping = looping;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _videoTexture;
            _video.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPurpose = purpose;
            _videoPrepared = false;
            _videoEnded = false;
            _videoFailed = false;
            _video.prepareCompleted += OnVideoPrepared;
            _video.loopPointReached += OnVideoEnded;
            _video.errorReceived += OnVideoError;

            if (purpose == VideoPurpose.Selection)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (_catalog == null || !_catalog.HasHostedSelectionVideo)
                {
                    CleanupVideo();
                    return false;
                }
                _video.source = VideoSource.Url;
                StreamingVideoPath.TryResolve(
                    _catalog.selectionVideoUrl,
                    out string selectionVideoUrl);
                _video.url = selectionVideoUrl;
#else
                if (_catalog?.selectionVideo != null)
                {
                    _video.source = VideoSource.VideoClip;
                    _video.clip = _catalog.selectionVideo;
                }
                else if (_catalog != null && _catalog.HasHostedSelectionVideo)
                {
                    _video.source = VideoSource.Url;
                    _video.url = _catalog.selectionVideoUrl;
                }
                else
                {
                    CleanupVideo();
                    return false;
                }
#endif
                return true;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (_opening == null || !_opening.HasHostedVideo)
            {
                CleanupVideo();
                return false;
            }
            _video.source = VideoSource.Url;
            StreamingVideoPath.TryResolve(
                _opening.videoUrl,
                out string openingVideoUrl);
            _video.url = openingVideoUrl;
#else
            if (_opening?.videoClip != null)
            {
                _video.source = VideoSource.VideoClip;
                _video.clip = _opening.videoClip;
            }
            else if (_opening != null && _opening.HasHostedVideo)
            {
                _video.source = VideoSource.Url;
                _video.url = _opening.videoUrl;
            }
            else
            {
                CleanupVideo();
                return false;
            }
#endif
            return true;
        }

        private void OnVideoPrepared(VideoPlayer player)
        {
            if (player != _video) return;
            _videoPrepared = true;
            if (_videoPurpose == VideoPurpose.Selection)
            {
                CharacterPresentationCatalog.Character ricko = _catalog?.Find("ricko");
                CharacterPresentationCatalog.Character stellar = _catalog?.Find("stellar");
                _view.ShowSelection(
                    _videoTexture,
                    CharacterPlaceholderSprites.Resolve(
                        ricko?.overviewArt ?? ricko?.selectionArt,
                        "ricko"),
                    CharacterPlaceholderSprites.Resolve(
                        stellar?.overviewArt ?? stellar?.selectionArt,
                        "stellar"));
                _view.SetInteractive(!_busy && _pending == null);
                player.Play();
            }
        }

        private void OnVideoEnded(VideoPlayer player)
        {
            if (player == _video && _videoPurpose == VideoPurpose.Opening)
            {
                _videoEnded = true;
            }
        }

        private void OnVideoError(VideoPlayer player, string message)
        {
            if (player != _video) return;
            _videoFailed = true;
            PowerMath.Diagnostics.AppLog.Warning(
                "Lifecycle",
                "Player preparation video could not be played: " + message,
                this);
            if (_videoPurpose == VideoPurpose.Selection)
            {
                CharacterPresentationCatalog.Character ricko = _catalog?.Find("ricko");
                CharacterPresentationCatalog.Character stellar = _catalog?.Find("stellar");
                _view.ShowSelection(
                    null,
                    CharacterPlaceholderSprites.Resolve(
                        ricko?.overviewArt ?? ricko?.selectionArt,
                        "ricko"),
                    CharacterPlaceholderSprites.Resolve(
                        stellar?.overviewArt ?? stellar?.selectionArt,
                        "stellar"));
                _view.SetInteractive(true);
            }
        }

        private void CleanupVideo()
        {
            if (_video != null)
            {
                _video.prepareCompleted -= OnVideoPrepared;
                _video.loopPointReached -= OnVideoEnded;
                _video.errorReceived -= OnVideoError;
                _video.Stop();
                Destroy(_video);
                _video = null;
            }
            if (_videoTexture != null)
            {
                _videoTexture.Release();
                Destroy(_videoTexture);
                _videoTexture = null;
            }
            _videoPurpose = VideoPurpose.None;
            _videoPrepared = false;
            _videoEnded = false;
            _videoFailed = false;
        }

        private void OnLocaleChanged()
        {
            if (_view == null) return;
            _view.RefreshLocale();
            if (_view.State == PlayerPreparationSequenceState.Narrative && !_busy)
            {
                StartVisualSequence(PlayOpeningExperience());
                return;
            }
            if ((_view.State == PlayerPreparationSequenceState.CharacterSelectedHolding ||
                 _view.State == PlayerPreparationSequenceState.CharacterSelectedEntering) &&
                PlayerLifecyclePolicy.IsCharacter(_preview))
            {
                _view.SetCharacterCopy(
                    LocalizationService.Get("onboarding." + _preview),
                    LocalizationService.Get("onboarding." + _preview + "Description"));
            }
        }

        private void StartVisualSequence(IEnumerator sequence)
        {
            StopVisualSequence();
            _visualSequence = StartCoroutine(sequence);
        }

        private void StopVisualSequence()
        {
            if (_visualSequence != null)
            {
                StopCoroutine(_visualSequence);
                _visualSequence = null;
            }
            CancelHoldAnimation();
        }

        private IEnumerator WaitFor(params UiMotionHandle[] handles)
        {
            bool active;
            do
            {
                active = false;
                for (int index = 0; index < handles.Length; index++)
                {
                    active |= handles[index] != null && handles[index].IsActive;
                }
                if (active) yield return null;
            } while (active);
        }

        private float MotionSeconds(float normal)
        {
            return _reducedMotion ? 0.06f : normal;
        }

        private float ResolveOpeningValue(
            Func<OpeningSequenceDefinition, float> selector,
            float fallback)
        {
            return _opening == null ? fallback : Mathf.Max(0f, selector(_opening));
        }

        private static List<string> GetTextElements(string value)
        {
            var elements = new List<string>();
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(
                value ?? string.Empty);
            while (enumerator.MoveNext())
            {
                elements.Add(enumerator.GetTextElement());
            }
            return elements;
        }

        private static void ReloadCurrentScene()
        {
            UnityEngine.SceneManagement.Scene current =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(current.name);
        }

        private void Finish()
        {
            if (_view == null) return;
            _completionTransition = false;
            _view.SetState(PlayerPreparationSequenceState.Hidden);
            Action completed = _completed;
            _completed = null;
            completed?.Invoke();
            Destroy(this);
        }

        private void OnDestroy()
        {
            LocalizationService.Changed -= OnLocaleChanged;
            StopVisualSequence();
            CleanupVideo();
            UnbindView();
            _view?.Dispose();
            _view = null;
            _motion?.Dispose();
            _motion = null;
        }
    }
}
