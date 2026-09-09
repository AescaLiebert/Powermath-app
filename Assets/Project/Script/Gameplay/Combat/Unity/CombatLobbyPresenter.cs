using System;
using System.Collections;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.UI.MainMenu;

namespace PowerMath.Gameplay.Combat.Unity
{
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
        private bool _isDucked;

        public event Action<CombatPhase> TerminalPresentationCompleted;

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
            UpdateMusicDucking(false);
        }

        private void UpdateMusicDucking(bool active)
        {
            if (_isDucked != active)
            {
                _isDucked = active;
                PowerMath.Audio.MusicController.Instance.SetDucking(active);
            }
        }

        public bool RecoverPendingPresentation()
        {
            AttemptPresentationReceipt receipt = _coordinator.PendingPresentation;
            if (!_bound || receipt == null || _saveInFlight) return false;
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
            if (_saveInFlight || (_interactionGate != null &&
                !_interactionGate.IsAllowed(InteractionScope.Lobby))) return;
            if (!_coordinator.TryBeginAttempt(out AttemptCommit commit))
            {
                return;
            }

            _audio.PlayCommit();
            _attemptLock = _interactionGate?.Acquire(
                "combat-attempt:" + commit.PresentationId,
                InteractionScope.Lobby | InteractionScope.Navigation |
                InteractionScope.ModalDismiss);
            _view.ArmEnemyAction(commit.PresentationId);
            _activeQuestion = commit.Question;
            _view.SetAttackEnabled(false);
            _view.SetResult("SAVING ATTEMPT...", true);
            _view.SetRetainedQuestionLayout(false);
            _view.SetAnswer(string.Empty, false);
            _view.SetAnswerInputEnabled(false);
            _academic.ShowQuestion(commit.Question);
            UpdateMusicDucking(true);
            Save(
                _saveRequests.CreateSaveRequest(
                    GameplaySavePoint.AttemptCommitted,
                    commit.Question),
                () =>
                {
                    _view.SetResult("PREPARING QUESTION...", true);
                    _questionPresentation.Begin(
                        commit.Question,
                        OnQuestionPresentationCompleted);
                });
        }

        private void OnQuestionPresentationCompleted(
            QuestionPresentationResult result)
        {
            if (!_bound || _coordinator.Phase != CombatPhase.Committed)
            {
                return;
            }

            if (!result.IsReady)
            {
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
                        UpdateMusicDucking(false);
                        _attemptLock?.Dispose();
                        _attemptLock = null;
                    });
                return;
            }

            if (!_coordinator.BeginAnswerWindow(out AnswerWindowReceipt window))
            {
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
            while (_coordinator.Phase == CombatPhase.Preparation ||
                   _coordinator.Phase == CombatPhase.Answering)
            {
                AnswerTiming timing = _coordinator.Tick(
                    UnityEngine.Time.realtimeSinceStartupAsDouble
                );
                _view.SetTimer(timing);
                yield return null;
            }
        }

        private IEnumerator ResolutionRoutine(AttemptResolution resolution)
        {
            string presentationId = resolution.Presentation?.PresentationId ?? string.Empty;
            yield return _feedback.PlayAnswerFeedback(resolution);
            _questionPresentation.Dismiss();
            _view.SetRetainedQuestionLayout(false);
            _view.ShowAttempt(false);
            UpdateMusicDucking(false);
            yield return _feedback.PlayBattleFeedback(resolution);
            while (!_feedback.AreActorsStable ||
                   !_view.IsEnemyActionQueueStable ||
                   !_view.IsBlockingUiStable)
                yield return null;
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
            if (!terminal && _view.IsEnemyActionQueueStable)
            {
                _resolutionLock?.Dispose();
                _resolutionLock = null;
            }
        }

        private IEnumerator RecoveryRoutine(AttemptPresentationReceipt receipt)
        {
            yield return _feedback.PlayRecoveredBattle(receipt);
            while (!_feedback.AreActorsStable ||
                   !_view.IsEnemyActionQueueStable ||
                   !_view.IsBlockingUiStable)
                yield return null;
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
            bool terminal = snapshot.Combat.Phase == CombatPhase.RunDefeat ||
                snapshot.Combat.Phase == CombatPhase.RunComplete;
            if (terminal)
            {
                TerminalPresentationCompleted?.Invoke(snapshot.Combat.Phase);
                _resolutionLock?.Dispose();
                _resolutionLock = null;
            }
            if (!terminal && _view.IsEnemyActionQueueStable)
            {
                _resolutionLock?.Dispose();
                _resolutionLock = null;
            }
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
                    if (_bound)
                    {
                        _view.SetAnswerInputEnabled(false);
                        _view.SetUnavailable(string.IsNullOrWhiteSpace(message)
                            ? "Progress could not be saved. Reconnect and restart PowerMath."
                            : message);
                    }
                });
        }
    }
}
