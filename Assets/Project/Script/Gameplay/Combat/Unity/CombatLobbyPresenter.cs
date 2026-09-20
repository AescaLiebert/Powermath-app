using System;
using System.Collections;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.UI.MainMenu;

namespace PowerMath.Gameplay.Combat.Unity
{
    public readonly struct CombatTutorialResult
    {
        public CombatTutorialResult(
            string transactionId,
            string sourceEncounterId,
            CombatSnapshot destination,
            AttemptOutcomeKind outcome,
            RankTransitionReceipt rankTransition,
            int finalDamage,
            bool enemyDefeated,
            bool enemyFled,
            bool enemyActionConsumed)
        {
            TransactionId = transactionId ?? string.Empty;
            SourceEncounterId = sourceEncounterId ?? string.Empty;
            Destination = destination;
            Outcome = outcome;
            RankTransition = rankTransition;
            FinalDamage = finalDamage;
            EnemyDefeated = enemyDefeated;
            EnemyFled = enemyFled;
            EnemyActionConsumed = enemyActionConsumed;
        }

        public string TransactionId { get; }
        public string SourceEncounterId { get; }
        public CombatSnapshot Destination { get; }
        public AttemptOutcomeKind Outcome { get; }
        public RankTransitionReceipt RankTransition { get; }
        public int FinalDamage { get; }
        public bool EnemyDefeated { get; }
        public bool EnemyFled { get; }
        public bool EnemyActionConsumed { get; }
    }

    public interface ICombatCoroutineRunner
    {
        void RunCombatRoutine(IEnumerator routine);
        void StopCombatRoutines();
    }

    public sealed class CombatLobbyPresenter : IDisposable
    {
        private readonly CombatLobbyView _view;
        private readonly AcademicProgressionPresenter _academic;
        private readonly CombatAttemptCoordinator _coordinator;
        private readonly AttemptFeedbackSequence _feedback;
        private readonly CombatAudioPlayer _audio;
        private readonly IQuestionPresentation _questionPresentation;
        private readonly ICombatCoroutineRunner _runner;
        private readonly IGameplayPersistence _persistence;
        private readonly IGameplaySaveRequestFactory _saveRequests;
        private bool _bound;
        private bool _saveInFlight;
        private bool _presentationInFlight;
        private QuestionPresentationDescriptor _activeQuestion;
        private readonly IMainMenuInteractionGate _interactionGate;
        private IInteractionLock _attemptLock;
        private IInteractionLock _resolutionLock;
        private bool _musicDucked;

        public event Action<CombatPhase> TerminalPresentationCompleted;
        public event Action<string, CombatSnapshot> TutorialAttemptCommitted;
        public event Action<CombatTutorialResult> TutorialAttemptPresentationCompleted;
        public event Action<CombatSnapshot> TutorialLobbyStable;
        public Func<string, CombatSnapshot, IEnumerator> TutorialCommitCheckpoint { get; set; }
        public Func<CombatTutorialResult, IEnumerator> TutorialResultCheckpoint { get; set; }

        public CombatSnapshot CurrentCombatSnapshot => _coordinator.Snapshot.Combat;
        public bool IsStableForTutorial => _bound && !_saveInFlight &&
            !_presentationInFlight && _feedback.AreActorsStable &&
            _view.IsEnemyActionQueueStable && _view.IsBlockingUiStable &&
            CurrentCombatSnapshot.Phase == CombatPhase.EnemyReady;

        public CombatLobbyPresenter(
            CombatLobbyView view,
            AcademicProgressionPresenter academic,
            CombatAttemptCoordinator coordinator,
            AttemptFeedbackSequence feedback,
            CombatAudioPlayer audio,
            IQuestionPresentation questionPresentation,
            ICombatCoroutineRunner runner,
            IGameplayPersistence persistence,
            IGameplaySaveRequestFactory saveRequests,
            IMainMenuInteractionGate interactionGate = null)
        {
            _view = view;
            _academic = academic;
            _coordinator = coordinator;
            _feedback = feedback;
            _audio = audio;
            _questionPresentation = questionPresentation ??
                throw new ArgumentNullException(nameof(questionPresentation));
            _runner = runner;
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _saveRequests = saveRequests ?? throw new ArgumentNullException(nameof(saveRequests));
            _interactionGate = interactionGate;
        }

        public void Initialize()
        {
            if (_bound)
            {
                return;
            }

            _view.AttackRequested += OnAttack;
            _view.DigitRequested += OnDigit;
            _view.BackspaceRequested += OnBackspace;
            _view.ClearRequested += OnClear;
            _view.SubmitRequested += OnSubmit;
            _coordinator.SnapshotChanged += OnSnapshotChanged;
            _coordinator.AnswerChanged += OnAnswerChanged;
            _coordinator.AttemptResolved += OnAttemptResolved;
            _view.Bind();
            _view.Render(_coordinator.Snapshot.Combat);
            _academic.Initialize(_coordinator.Snapshot.Academic);
            _view.ShowAttempt(false);
            _view.HideDamage();
            _view.HideBattleBanner();
            _bound = true;
        }

        public void Dispose()
        {
            if (!_bound)
            {
                return;
            }

            SetMusicDucked(false);
            _runner.StopCombatRoutines();
            _feedback.Cancel();
            _questionPresentation.Cancel();
            _persistence.Cancel();
            _view.AttackRequested -= OnAttack;
            _view.DigitRequested -= OnDigit;
            _view.BackspaceRequested -= OnBackspace;
            _view.ClearRequested -= OnClear;
            _view.SubmitRequested -= OnSubmit;
            _coordinator.SnapshotChanged -= OnSnapshotChanged;
            _coordinator.AnswerChanged -= OnAnswerChanged;
            _coordinator.AttemptResolved -= OnAttemptResolved;
            _academic.Dispose();
            _view.Dispose();
            _resolutionLock?.Dispose();
            _resolutionLock = null;
            _attemptLock?.Dispose();
            _attemptLock = null;
            _bound = false;
            _presentationInFlight = false;
        }

        public bool RecoverPendingPresentation()
        {
            AttemptPresentationReceipt receipt = _coordinator.PendingPresentation;
            if (!_bound || receipt == null || _saveInFlight) return false;
            SetMusicDucked(false);
            _presentationInFlight = true;
            _resolutionLock = _interactionGate?.Acquire(
                "combat-recovery:" + receipt.PresentationId,
                InteractionScope.All);
            _view.SetAnswerInputEnabled(false);
            _view.ShowAttempt(false);
            _runner.RunCombatRoutine(RecoveryRoutine(receipt));
            return true;
        }

        private void OnAttack()
        {
            TryBeginAttack(false);
        }

        public bool RequestTutorialAttack()
        {
            return TryBeginAttack(true);
        }

        private bool TryBeginAttack(bool tutorialAuthorized)
        {
            if (_saveInFlight)
            {
                PowerMath.Diagnostics.AppLog.Warning(
                    "Combat",
                    "[CombatLobbyPresenter] TryBeginAttack rejected: _saveInFlight is true.");
                return false;
            }
            if (!tutorialAuthorized && _interactionGate != null && !_interactionGate.IsAllowed(InteractionScope.Lobby))
            {
                PowerMath.Diagnostics.AppLog.Warning(
                    "Combat",
                    $"[CombatLobbyPresenter] TryBeginAttack rejected: interaction gate blocked for Lobby. BlockedScopes={_interactionGate.Snapshot.BlockedScopes}, LockCount={_interactionGate.Snapshot.LockCount}");
                return false;
            }
            if (!_coordinator.TryBeginAttempt(out AttemptCommit commit))
            {
                PowerMath.Diagnostics.AppLog.Warning(
                    "Combat",
                    $"[CombatLobbyPresenter] TryBeginAttempt rejected: coordinator phase is {_coordinator.Phase}.");
                return false;
            }

            _audio.PlayCommit();
            _attemptLock = _interactionGate?.Acquire(
                "combat-attempt:" + commit.PresentationId,
                InteractionScope.Lobby | InteractionScope.Navigation |
                InteractionScope.ModalDismiss | InteractionScope.TerminalAction);
            _view.ArmEnemyAction(commit.PresentationId);
            _activeQuestion = commit.Question;
            _view.SetAttackEnabled(false);
            _view.SetResult("SAVING ATTEMPT...", true);
            _view.SetRetainedQuestionLayout(false);
            _view.SetAnswer(string.Empty, false);
            _view.SetAnswerInputEnabled(false);
            _academic.ShowQuestion(commit.Question);
            Save(
                _saveRequests.CreateSaveRequest(
                    GameplaySavePoint.AttemptCommitted,
                    commit.Question),
                () =>
                {
                    TutorialAttemptCommitted?.Invoke(
                        commit.PresentationId,
                        commit.Snapshot.Combat);
                    _runner.RunCombatRoutine(BeginQuestionRoutine(commit));
                });
            return true;
        }

        private IEnumerator BeginQuestionRoutine(AttemptCommit commit)
        {
            if (TutorialCommitCheckpoint != null)
                yield return TutorialCommitCheckpoint(
                    commit.PresentationId,
                    commit.Snapshot.Combat);
            if (!_bound || _coordinator.Phase != CombatPhase.Committed)
            {
                _attemptLock?.Dispose();
                _attemptLock = null;
                _view.SetAttackEnabled(true);
                yield break;
            }
            _view.SetResult("PREPARING QUESTION...", true);
            SetMusicDucked(true);
            _questionPresentation.Begin(
                commit.Question,
                OnQuestionPresentationCompleted);
        }

        private void OnQuestionPresentationCompleted(
            QuestionPresentationResult result)
        {
            if (!_bound || _coordinator.Phase != CombatPhase.Committed)
            {
                _attemptLock?.Dispose();
                _attemptLock = null;
                _view.SetAttackEnabled(true);
                return;
            }

            if (!result.IsReady)
            {
                SetMusicDucked(false);
                _coordinator.VoidContentFailure();
                Save(
                    _saveRequests.CreateSaveRequest(GameplaySavePoint.ContentFailureVoided),
                    () =>
                    {
                        _activeQuestion = null;
                        _view.SetRetainedQuestionLayout(false);
                        _academic.ClearAttemptPresentation();
                        _view.SetResult(
                            string.IsNullOrWhiteSpace(result.PlayerMessage)
                                ? "QUESTION CONTENT UNAVAILABLE - ATTEMPT RESTORED"
                                : result.PlayerMessage,
                            false);
                        _view.ShowAttempt(false);
                        _attemptLock?.Dispose();
                        _attemptLock = null;
                    });
                return;
            }

            if (!_coordinator.BeginAnswerWindow(out AnswerWindowReceipt window))
            {
                _attemptLock?.Dispose();
                _attemptLock = null;
                _view.SetAttackEnabled(true);
                return;
            }

            _view.SetResult("SAVING ANSWER WINDOW...", true);
            _view.SetRetainedQuestionLayout(
                result.RetainsPresentationSurface);
            _view.ShowAttempt(true);
            _audio.PlayPopUp();
            Save(
                _saveRequests.CreateSaveRequest(
                    GameplaySavePoint.AnswerWindowOpened,
                    _activeQuestion,
                    window),
                () =>
                {
                    _view.SetResult(result.PlayerMessage, true);
                    _view.SetAnswerInputEnabled(true);
                    _runner.RunCombatRoutine(TimerRoutine());
                });
        }

        private void OnDigit(int digit)
        {
            if (_interactionGate != null &&
                !_interactionGate.IsAllowed(InteractionScope.Question)) return;
            if (_coordinator.TryAppendDigit(digit))
            {
                _audio.PlayKey();
            }
        }

        private void OnBackspace()
        {
            if (_interactionGate != null &&
                !_interactionGate.IsAllowed(InteractionScope.Question)) return;
            if (_coordinator.TryBackspace())
            {
                _audio.PlayKey();
            }
        }

        private void OnClear()
        {
            if (_interactionGate != null &&
                !_interactionGate.IsAllowed(InteractionScope.Question)) return;
            if (_coordinator.TryClear())
            {
                _audio.PlayKey();
            }
        }

        private void OnSubmit()
        {
            if (_interactionGate != null &&
                !_interactionGate.IsAllowed(InteractionScope.Question)) return;
            _coordinator.TrySubmit(UnityEngine.Time.realtimeSinceStartupAsDouble);
        }

        private void OnSnapshotChanged(GameplaySnapshot snapshot)
        {
            // The presentation owns encounter visuals until its acknowledgement
            // succeeds, including the ready snapshot emitted by CompletePresentation.
            if (_presentationInFlight) return;
            _view.Render(snapshot.Combat);
            _academic.Render(snapshot.Academic);
        }

        private void OnAnswerChanged(string displayValue, bool canSubmit)
        {
            _view.SetAnswer(displayValue, canSubmit);
        }

        private void OnAttemptResolved(AttemptResolution resolution)
        {
            SetMusicDucked(false);
            _presentationInFlight = true;
            _resolutionLock = _interactionGate?.Acquire(
                "combat-presentation:" + resolution.Presentation?.PresentationId,
                InteractionScope.All);
            _attemptLock?.Dispose();
            _attemptLock = null;
            _runner.StopCombatRoutines();
            _view.SetAnswerInputEnabled(false);
            _view.SetResult("SAVING RESULT...", true);
            Save(
                _saveRequests.CreateSaveRequest(
                    GameplaySavePoint.AttemptResolved,
                    resolution: resolution),
                () => _runner.RunCombatRoutine(ResolutionRoutine(resolution)));
        }

        private IEnumerator TimerRoutine()
        {
            int lastSecond = -1;
            while (_coordinator.Phase == CombatPhase.Preparation ||
                   _coordinator.Phase == CombatPhase.Answering)
            {
                AnswerTiming timing = _coordinator.Tick(
                    UnityEngine.Time.realtimeSinceStartupAsDouble
                );
                _view.SetTimer(timing);
                if (!timing.IsPreparation && timing.DisplayedSeconds > 0 && timing.DisplayedSeconds != lastSecond)
                {
                    lastSecond = timing.DisplayedSeconds;
                    bool isWarning = timing.RemainingSeconds <= 3.0d;
                    _audio?.PlayCountdownTick(isWarning);
                }
                yield return null;
            }
        }

        private IEnumerator ResolutionRoutine(AttemptResolution resolution)
        {
            SetMusicDucked(false);
            string presentationId = resolution.Presentation?.PresentationId ?? string.Empty;
            yield return _feedback.PlayAnswerFeedback(resolution);
            _questionPresentation.Dismiss();
            _view.SetRetainedQuestionLayout(false);
            _view.ShowAttempt(false);
            yield return _feedback.PlayBattleFeedback(resolution);
            while (!_feedback.AreActorsStable ||
                   !_view.IsEnemyActionQueueStable ||
                   !_view.IsBlockingUiStable)
                yield return null;
            CombatTutorialResult tutorialResult = CreateTutorialResult(
                resolution.Presentation,
                resolution.Snapshot.Combat);
            if (TutorialResultCheckpoint != null)
                yield return TutorialResultCheckpoint(tutorialResult);
            GameplaySnapshot snapshot = _coordinator.CompletePresentation();
            bool saved = false;
            Save(
                _saveRequests.CreateSaveRequest(
                    GameplaySavePoint.PresentationCompleted,
                    presentationId: presentationId),
                () => saved = true);
            while (_saveInFlight) yield return null;
            if (!saved) yield break;

            _presentationInFlight = false;
            _activeQuestion = null;
            _view.Render(snapshot.Combat);
            _academic.Render(snapshot.Academic);
            PublishTutorialResult(resolution.Presentation, snapshot.Combat);

            bool terminal = snapshot.Combat.Phase == CombatPhase.RunDefeat ||
                snapshot.Combat.Phase == CombatPhase.RunComplete;
            if (terminal)
            {
                TerminalPresentationCompleted?.Invoke(snapshot.Combat.Phase);
                _resolutionLock?.Dispose();
                _resolutionLock = null;
            }
            if (!terminal)
            {
                _view.ShowAttempt(false);
                _view.HideBattleBanner();
                _academic.ClearAttemptPresentation();
            }
            else
            {
                _view.SetAnswerInputEnabled(false);
            }
            if (!terminal)
            {
                while (!_view.IsEnemyActionQueueStable) yield return null;
                _resolutionLock?.Dispose();
                _resolutionLock = null;
                PublishTutorialLobbyStable();
            }
        }

        private IEnumerator RecoveryRoutine(AttemptPresentationReceipt receipt)
        {
            if (receipt != null && receipt.Outcome == AttemptOutcomeKind.Timeout)
            {
                _view.SetResult("BATTLE RESUMED: UNFINISHED QUESTION TIMED OUT", false);
            }
            else
            {
                _view.SetResult("RECOVERING BATTLE...", true);
            }
            yield return _feedback.PlayRecoveredBattle(receipt);
            while (!_feedback.AreActorsStable ||
                   !_view.IsEnemyActionQueueStable ||
                   !_view.IsBlockingUiStable)
                yield return null;
            CombatTutorialResult tutorialResult = CreateTutorialResult(
                receipt,
                ToCombatSnapshot(receipt.Destination));
            if (TutorialResultCheckpoint != null)
                yield return TutorialResultCheckpoint(tutorialResult);
            GameplaySnapshot snapshot = _coordinator.CompletePresentation();
            bool saved = false;
            Save(
                _saveRequests.CreateSaveRequest(
                    GameplaySavePoint.PresentationCompleted,
                    presentationId: receipt.PresentationId),
                () => saved = true);
            while (_saveInFlight) yield return null;
            if (!saved) yield break;

            _presentationInFlight = false;
            _view.Render(snapshot.Combat);
            _academic.Render(snapshot.Academic);
            PublishTutorialResult(receipt, snapshot.Combat);
            bool terminal = snapshot.Combat.Phase == CombatPhase.RunDefeat ||
                snapshot.Combat.Phase == CombatPhase.RunComplete;
            if (terminal)
            {
                TerminalPresentationCompleted?.Invoke(snapshot.Combat.Phase);
                _resolutionLock?.Dispose();
                _resolutionLock = null;
            }
            if (!terminal)
            {
                while (!_view.IsEnemyActionQueueStable) yield return null;
                _resolutionLock?.Dispose();
                _resolutionLock = null;
                PublishTutorialLobbyStable();
            }
        }

        public void PublishTutorialLobbyStable()
        {
            if (IsStableForTutorial)
                TutorialLobbyStable?.Invoke(CurrentCombatSnapshot);
        }

        private void PublishTutorialResult(
            AttemptPresentationReceipt receipt,
            CombatSnapshot destination)
        {
            if (receipt == null || destination == null) return;
            TutorialAttemptPresentationCompleted?.Invoke(
                CreateTutorialResult(receipt, destination));
        }

        private static CombatTutorialResult CreateTutorialResult(
            AttemptPresentationReceipt receipt,
            CombatSnapshot destination)
        {
            return receipt == null
                ? default
                : new CombatTutorialResult(
                    receipt.PresentationId,
                    receipt.Source.EncounterId,
                    destination,
                    receipt.Outcome,
                    receipt.RankTransition,
                    receipt.FinalDamage,
                    receipt.EnemyDefeated,
                    receipt.EnemyFled,
                    !receipt.EnemyDefeated &&
                    !receipt.EnemyFled &&
                    receipt.Source.EncounterKind != StageEncounterKind.ChallengeEvent);
        }

        private static CombatSnapshot ToCombatSnapshot(
            CombatPresentationSnapshot value)
        {
            if (value == null) return null;
            return new CombatSnapshot(
                value.Stage,
                value.EncounterId,
                value.EncounterId,
                value.EnemyCurrentHp,
                value.EnemyMaximumHp,
                value.EnemyRemainingCooldown,
                value.EnemyMaximumCooldown,
                value.PlayerCurrentHearts,
                value.PlayerMaximumHearts,
                value.Phase,
                false,
                value.BiomeId,
                string.Empty,
                value.EncounterKind,
                string.Empty,
                0);
        }

        private void Save(GameplaySaveRequest request, Action completed)
        {
            if (_saveInFlight)
                throw new InvalidOperationException("A gameplay save is already in progress.");
            _saveInFlight = true;
            _persistence.Save(
                request,
                () =>
                {
                    _saveInFlight = false;
                    if (_bound) completed?.Invoke();
                },
                message =>
                {
                    _saveInFlight = false;
                    _attemptLock?.Dispose();
                    _attemptLock = null;
                    if (_bound)
                    {
                        PowerMath.Diagnostics.AppLog.Warning(
                            "Combat",
                            $"[CombatLobbyPresenter] Save failed for {request.SavePoint}: {message}");
                        _view.SetAttackEnabled(true);
                        _view.SetAnswerInputEnabled(false);
                        _view.SetUnavailable(string.IsNullOrWhiteSpace(message)
                            ? "Progress could not be saved. Reconnect and restart PowerMath."
                            : message);
                    }
                });
        }

        private void SetMusicDucked(bool ducked)
        {
            if (_musicDucked == ducked) return;
            _musicDucked = ducked;
            if (PowerMath.Audio.MusicController.Instance != null)
            {
                PowerMath.Audio.MusicController.Instance.SetDucking(ducked);
            }
        }
    }
}
