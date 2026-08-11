using System;
using System.Collections;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Unity;

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
        private QuestionPresentationDescriptor _activeQuestion;

        public CombatLobbyPresenter(
            CombatLobbyView view,
            AcademicProgressionPresenter academic,
            CombatAttemptCoordinator coordinator,
            AttemptFeedbackSequence feedback,
            CombatAudioPlayer audio,
            IQuestionPresentation questionPresentation,
            ICombatCoroutineRunner runner,
            IGameplayPersistence persistence,
            IGameplaySaveRequestFactory saveRequests)
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
            _bound = false;
        }

        private void OnAttack()
        {
            if (_saveInFlight) return;
            if (!_coordinator.TryBeginAttempt(out AttemptCommit commit))
            {
                return;
            }

            _audio.PlayCommit();
            _activeQuestion = commit.Question;
            _view.SetResult("SAVING ATTEMPT...", true);
            _view.SetAnswer(string.Empty, false);
            _view.SetAnswerInputEnabled(false);
            _view.ShowAttempt(true);
            _academic.ShowQuestion(commit.Question);
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
                        _academic.ClearAttemptPresentation();
                        _view.SetResult(
                            string.IsNullOrWhiteSpace(result.PlayerMessage)
                                ? "QUESTION CONTENT UNAVAILABLE - ATTEMPT RESTORED"
                                : result.PlayerMessage,
                            false);
                        _view.ShowAttempt(false);
                    });
                return;
            }

            if (!_coordinator.BeginAnswerWindow(out AnswerWindowReceipt window))
            {
                return;
            }

            _view.SetResult("SAVING ANSWER WINDOW...", true);
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
            if (_coordinator.TryAppendDigit(digit))
            {
                _audio.PlayKey();
            }
        }

        private void OnBackspace()
        {
            if (_coordinator.TryBackspace())
            {
                _audio.PlayKey();
            }
        }

        private void OnClear()
        {
            if (_coordinator.TryClear())
            {
                _audio.PlayKey();
            }
        }

        private void OnSubmit()
        {
            _coordinator.TrySubmit(UnityEngine.Time.realtimeSinceStartupAsDouble);
        }

        private void OnSnapshotChanged(GameplaySnapshot snapshot)
        {
            _view.Render(snapshot.Combat);
            _academic.Render(snapshot.Academic);
        }

        private void OnAnswerChanged(string displayValue, bool canSubmit)
        {
            _view.SetAnswer(displayValue, canSubmit);
        }

        private void OnAttemptResolved(AttemptResolution resolution)
        {
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
            yield return _feedback.Play(resolution);
            if (resolution.Combat.BiomeChanged)
                yield return _view.PlayBiomeTransition(resolution.Snapshot.Combat);
            GameplaySnapshot snapshot = _coordinator.CompletePresentation();
            bool saved = false;
            Save(
                _saveRequests.CreateSaveRequest(GameplaySavePoint.PresentationCompleted),
                () => saved = true);
            while (_saveInFlight) yield return null;
            if (!saved) yield break;

            _activeQuestion = null;
            _view.Render(snapshot.Combat);
            _academic.Render(snapshot.Academic);

            bool terminal = snapshot.Combat.Phase == CombatPhase.RunDefeat ||
                snapshot.Combat.Phase == CombatPhase.RunComplete;
            if (!terminal)
            {
                _view.ShowAttempt(false);
                _academic.ClearAttemptPresentation();
            }
            else
            {
                _view.SetAnswerInputEnabled(false);
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
