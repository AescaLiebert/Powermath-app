using System;
using System.Collections;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Combat.Unity;
using PowerMath.Gameplay.Tutorial;
using PowerMath.PlayerData;
using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.Tutorial
{
    public sealed class TutorialDirector : IDisposable
    {
        public const string OnFirstCreateId = "OnFirstCreate";
        public const string OnFirstRankChangeId = "OnFirstRankChange";
        public const string OnFirstEnemySurviveId = "OnFirstEnemySurvive";
        public const string OnFirstRebirthOpenId = "OnFirstRebirthOpen";
        public const string OnFirstRebirthId = "OnFirstRebirth";

        private readonly MonoBehaviour _host;
        private readonly TutorialSequence _sequence;
        private readonly ITutorialProgressStore _progressStore;
        private readonly PlayerSessionStore _sessionStore;
        private readonly CombatLobbyPresenter _combat;
        private readonly IMainMenuInteractionGate _interactionGate;
        private readonly TutorialOverlayView _view;
        private readonly TutorialTargetRegistry _targets;
        private readonly bool _autoQueueOnCreate;
        private readonly bool _autoQueueLegacyRankChange;
        private readonly bool _ownsCombatCheckpoints;
        private readonly Func<bool> _allowExternalGate;
        private readonly Func<bool> _allowNonCombatPresentation;
        private TutorialProgress _progress;
        private IInteractionLock _tutorialLock;
        private bool _busy;
        private bool _queueing;
        private bool _disposed;
        // Several state-machine directors share one physical overlay. Only the
        // director that rendered it may hide that shared surface.
        private bool _isPresenting;
        private bool _targetAccepted;
        private bool _failOpenInFlight;
        private TutorialFocusTarget _activeTarget;

        public event Action<TutorialStep> StepPresented;
        public event Action SequenceCompleted;

        public bool IsCompleted => _progress?.Status == TutorialStatus.Completed;

        public TutorialDirector(
            MonoBehaviour host,
            VisualElement root,
            TutorialSequence sequence,
            ITutorialProgressStore progressStore,
            PlayerSessionStore sessionStore,
            CombatLobbyPresenter combat,
            IMainMenuInteractionGate interactionGate,
            TutorialTargetRegistry targets,
            bool reducedMotion,
            bool autoQueueOnCreate = true,
            bool autoQueueLegacyRankChange = false,
            bool ownsCombatCheckpoints = true,
            Func<bool> allowExternalGate = null,
            Func<bool> allowNonCombatPresentation = null)
        {
            _host = host != null ? host : throw new ArgumentNullException(nameof(host));
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _progressStore = progressStore ?? throw new ArgumentNullException(nameof(progressStore));
            _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _interactionGate = interactionGate ?? throw new ArgumentNullException(nameof(interactionGate));
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));
            _autoQueueOnCreate = autoQueueOnCreate;
            _autoQueueLegacyRankChange = autoQueueLegacyRankChange;
            _ownsCombatCheckpoints = ownsCombatCheckpoints;
            _allowExternalGate = allowExternalGate;
            _allowNonCombatPresentation = allowNonCombatPresentation;
            _view = new TutorialOverlayView(root, reducedMotion);
        }

        public void Initialize()
        {
            _view.AdvanceRequested += OnAdvanceRequested;
            _view.TargetRequested += OnTargetRequested;
            _combat.TutorialLobbyStable += OnLobbyStable;
            _interactionGate.Changed += OnInteractionGateChanged;
            if (_ownsCombatCheckpoints)
            {
                _combat.TutorialCommitCheckpoint = CommitCheckpoint;
                _combat.TutorialResultCheckpoint = ResultCheckpoint;
            }
            // New-player onboarding is known from the loaded game state. Reserve
            // input before the asynchronous tutorial save/layout work starts.
            if (_autoQueueOnCreate && PlayerLifecyclePolicy.IsComplete(
                    _sessionStore.Snapshot))
                TryReservePendingOwnership();
            _host.StartCoroutine(InitializeRoutine());
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _view.AdvanceRequested -= OnAdvanceRequested;
            _view.TargetRequested -= OnTargetRequested;
            _combat.TutorialLobbyStable -= OnLobbyStable;
            _interactionGate.Changed -= OnInteractionGateChanged;
            if (_ownsCombatCheckpoints && _combat.TutorialCommitCheckpoint == CommitCheckpoint)
                _combat.TutorialCommitCheckpoint = null;
            if (_ownsCombatCheckpoints && _combat.TutorialResultCheckpoint == ResultCheckpoint)
                _combat.TutorialResultCheckpoint = null;
            ReleaseTutorialLock();
            _view.Dispose(_isPresenting);
        }

        private IEnumerator InitializeRoutine()
        {
            PlayerSnapshot player = _sessionStore.Snapshot;
            if (!TutorialProgressMapper.TryFind(
                player, _sequence.Id, out _progress, out string error))
            {
                ReleaseTutorialLock();
                StatusMessageService.ShowError(error);
                yield break;
            }
            if (_progress != null && _progress.Status == TutorialStatus.Completed)
            {
                HideAndRelease();
                yield break;
            }
            if (_progress != null && _progress.Version != _sequence.Version)
            {
                ReleaseTutorialLock();
                StatusMessageService.ShowError(
                    PowerMath.Localization.LocalizationService.Get(
                        "tutorial.updateRequired"));
                yield break;
            }
            if (_progress != null && _progress.Status == TutorialStatus.Active)
            {
                if (IsFailOpenTutorial)
                {
                    // Session 5 is optional guidance. If the player left while
                    // it was active, never replay a partially consumed reward
                    // flow that may no longer be actionable.
                    yield return CompleteSafelyRoutine("scene-reload");
                    yield break;
                }
                // Scene references and panel ownership do not survive an app exit.
                // Restart the authored flow from its safe entry while preserving
                // durable one-time reward state and the original trigger.
                yield return Persist(TutorialStateMachine.Restart(_progress));
            }
            if (_progress == null && _autoQueueOnCreate &&
                PlayerLifecyclePolicy.IsComplete(player))
            {
                bool legacy = player.onboarding?.legacyPlayer == true;
                TutorialProgress queued = TutorialStateMachine.Queue(
                    _sequence,
                    legacy,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                yield return Persist(queued);
            }
            else if (_progress == null && _autoQueueLegacyRankChange &&
                HasLegacyRankChange(player))
            {
                TutorialProgress queued = TutorialStateMachine.Queue(
                    _sequence,
                    true,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    "promotion");
                yield return Persist(queued);
            }
            if (_disposed || _progress == null)
            {
                ReleaseTutorialLock();
                yield break;
            }
            SynchronizeCurrentLobby();
        }

        private void OnAdvanceRequested()
        {
            if (_busy || _disposed) return;
            _host.StartCoroutine(AdvanceRoutine());
        }

        private IEnumerator AdvanceRoutine()
        {
            yield return ReduceAndPersist(
                new TutorialSignal(TutorialSignalKind.AdvanceRequested));
            if (!_busy) SynchronizeCurrentLobby();
        }

        private void OnTargetRequested()
        {
            if (_busy || _disposed || _targetAccepted || _progress == null ||
                !_sequence.TryGetStep(_progress.CurrentStepId, out TutorialStep step) ||
                step.Kind != TutorialStepKind.FocusAction)
                return;
            _targetAccepted = true;
            _host.StartCoroutine(ActivateTargetRoutine(step));
        }

        private IEnumerator ActivateTargetRoutine(TutorialStep step)
        {
            HideAndRelease();
            bool activated = false;
            try
            {
                activated = _activeTarget.TryActivate();
            }
            catch (Exception exception)
            {
                PowerMath.Diagnostics.AppLog.Exception(
                    "Tutorial", exception,
                    $"Tutorial target '{step.TargetId}' failed");
            }
            if (!activated)
            {
                _targetAccepted = false;
                if (IsFailOpenTutorial)
                {
                    yield return CompleteSafelyRoutine(
                        "target-unavailable:" + step.TargetId);
                    yield break;
                }
                RenderCurrent();
                _view.ShowError("tutorial.actionUnavailable");
                yield break;
            }
            yield return ReduceAndPersist(new TutorialSignal(
                TutorialSignalKind.TargetActivated,
                targetId: step.TargetId));
            if (_progress != null && _progress.Status != TutorialStatus.Active ||
                !_sequence.TryGetStep(_progress?.CurrentStepId, out TutorialStep current) ||
                !HasRule(current, TutorialSignalKind.AttemptCommitted))
                _targetAccepted = false;
            if (!_busy) SynchronizeCurrentLobby();
        }

        private IEnumerator CommitCheckpoint(
            string transactionId,
            CombatSnapshot snapshot)
        {
            if (_disposed || _progress == null ||
                _progress.Status != TutorialStatus.Active)
                yield break;
            yield return ReduceAndPersist(new TutorialSignal(
                TutorialSignalKind.AttemptCommitted,
                transactionId: transactionId,
                encounterId: snapshot?.EncounterId));
            _targetAccepted = false;
        }

        private IEnumerator ResultCheckpoint(CombatTutorialResult result)
        {
            if (_disposed || _progress == null ||
                _progress.Status != TutorialStatus.Active ||
                string.IsNullOrWhiteSpace(result.TransactionId))
                yield break;

            // Recovery may have the combat receipt even if the tutorial commit
            // checkpoint was interrupted. Reconstruct that semantic edge first.
            if (_sequence.TryGetStep(_progress.CurrentStepId, out TutorialStep current) &&
                current.Kind == TutorialStepKind.FocusAction)
            {
                yield return ReduceAndPersist(new TutorialSignal(
                    TutorialSignalKind.AttemptCommitted,
                    transactionId: result.TransactionId,
                    encounterId: result.SourceEncounterId));
            }

            yield return ReduceAndPersist(new TutorialSignal(
                TutorialSignalKind.AttemptPresentationCompleted,
                transactionId: result.TransactionId,
                encounterId: result.SourceEncounterId,
                outcome: MapOutcome(result.Outcome),
                isStandardEncounter: IsStandard(result.Destination)));
        }

        private void OnLobbyStable(CombatSnapshot snapshot)
        {
            if (_busy || _disposed || _progress == null) return;
            if (_progress.Status == TutorialStatus.Queued)
            {
                if (!CanActivateFromQueue()) return;
                if (!TryReservePendingOwnership()) return;
                _host.StartCoroutine(ReduceAndPersist(new TutorialSignal(
                    TutorialSignalKind.SafeLobbyEntered,
                    encounterId: snapshot?.EncounterId,
                    isStandardEncounter: IsStandard(snapshot))));
                return;
            }
            if (_progress.Status != TutorialStatus.Active) return;
            _host.StartCoroutine(EncounterReadyRoutine(snapshot));
        }

        public IEnumerator QueueFromTrigger(
            long triggerRecordedAt,
            string variant = "",
            bool useReturningStart = false)
        {
            TryReservePendingOwnership();
            // Combat result callbacks can be delivered more than once in the
            // same frame. Persist() has not set _busy until its child coroutine
            // starts, so serialize admission here as well.
            while ((_busy || _queueing) && !_disposed) yield return null;
            if (_disposed) yield break;
            _queueing = true;
            try
            {
                if (!TutorialProgressMapper.TryFind(
                        _sessionStore.Snapshot,
                        _sequence.Id,
                        out _progress,
                        out string error))
                {
                    ReleaseTutorialLock();
                    StatusMessageService.ShowError(error);
                    yield break;
                }
                if (_progress != null)
                {
                    if (_progress.Status != TutorialStatus.Active)
                        ReleaseTutorialLock();
                    yield break;
                }
                TutorialProgress queued = TutorialStateMachine.Queue(
                    _sequence,
                    useReturningStart,
                    triggerRecordedAt,
                    variant);
                yield return Persist(queued);
                if (!_busy && !_disposed) SynchronizeCurrentLobby();
            }
            finally
            {
                _queueing = false;
            }
        }

        public IEnumerator NotifyExternalEvent(
            string targetId,
            string transactionId = "",
            bool presentAfterPersist = true)
        {
            if (_disposed || string.IsNullOrWhiteSpace(targetId)) yield break;
            while (_busy && !_disposed) yield return null;
            if (_disposed || _progress == null) yield break;
            if (_progress.Status == TutorialStatus.Queued &&
                !CanActivateFromQueue()) yield break;
            if (_progress.Status == TutorialStatus.Queued)
                TryReservePendingOwnership();
            yield return ReduceAndPersist(new TutorialSignal(
                TutorialSignalKind.ExternalEvent,
                targetId: targetId,
                transactionId: transactionId),
                presentAfterPersist);
            if (!_busy && presentAfterPersist) SynchronizeCurrentLobby();
        }

        /// <summary>
        /// Reloads this director's cached progress after an adjacent durable
        /// operation (such as a tutorial-owned reward) updates the same entry.
        /// </summary>
        public bool RefreshProgressFromSession()
        {
            if (_disposed) return false;
            if (!TutorialProgressMapper.TryFind(
                    _sessionStore.Snapshot, _sequence.Id, out TutorialProgress value,
                    out string error))
            {
                if (!string.IsNullOrWhiteSpace(error)) StatusMessageService.ShowError(error);
                return false;
            }
            _progress = value;
            return true;
        }

        public IEnumerator CompleteSafely(string reason)
        {
            yield return CompleteSafelyRoutine(
                string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason);
        }

        /// <summary>
        /// Claims input synchronously when the game discovers a qualifying
        /// tutorial. The visual overlay may appear next frame, but no unrelated
        /// menu, attack, or raycast action can slip in during that gap.
        /// </summary>
        public bool TryReservePendingOwnership()
        {
            if (_disposed) return false;
            if (_tutorialLock != null && !_tutorialLock.IsReleased) return true;
            if (_interactionGate.Snapshot.IsBlocked &&
                !(_allowExternalGate?.Invoke() == true))
                return false;

            _tutorialLock = _interactionGate.Acquire(
                "tutorial-pending:" + _sequence.Id,
                InteractionScope.All);
            return true;
        }

        private void OnInteractionGateChanged(InteractionGateSnapshot snapshot)
        {
            if (_disposed || _busy || _targetAccepted || snapshot.IsBlocked) return;
            SynchronizeCurrentLobby();
        }

        private IEnumerator EncounterReadyRoutine(CombatSnapshot snapshot)
        {
            yield return ReduceAndPersist(new TutorialSignal(
                TutorialSignalKind.EncounterReady,
                encounterId: snapshot?.EncounterId,
                isStandardEncounter: IsStandard(snapshot)));
            if (!_busy) RenderCurrent();
        }

        private void SynchronizeCurrentLobby()
        {
            if (_busy || _disposed || _progress == null) return;
            CombatSnapshot snapshot = _combat.CurrentCombatSnapshot;
            bool canPresentOutsideCombat = _allowNonCombatPresentation?.Invoke() == true;
            if ((!_combat.IsStableForTutorial || !IsStandard(snapshot)) &&
                !canPresentOutsideCombat)
            {
                HideAndRelease();
                return;
            }
            if (_progress.Status == TutorialStatus.Queued)
            {
                if (canPresentOutsideCombat)
                {
                    _host.StartCoroutine(NotifyExternalEvent("tutorial.external-ready"));
                }
                else OnLobbyStable(snapshot);
                return;
            }
            if (_progress.Status == TutorialStatus.Active &&
                _sequence.TryGetStep(_progress.CurrentStepId, out TutorialStep step) &&
                step.Kind == TutorialStepKind.Wait)
            {
                _host.StartCoroutine(EncounterReadyRoutine(snapshot));
                return;
            }
            RenderCurrent();
        }

        private IEnumerator ReduceAndPersist(
            TutorialSignal signal,
            bool presentAfterPersist = true)
        {
            if (_busy || _progress == null || _disposed) yield break;
            if (!TutorialStateMachine.TryReduce(
                _sequence,
                _progress,
                signal,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                out TutorialProgress next))
                yield break;
            yield return Persist(next, presentAfterPersist);
        }

        private IEnumerator Persist(
            TutorialProgress next,
            bool presentAfterPersist = true)
        {
            if (_busy || _disposed) yield break;
            _busy = true;
            _view.SetBusy(true);
            PlayerSnapshot saved = null;
            FirestoreRestClient.Failure? failure = null;
            string operationId = Guid.NewGuid().ToString("N");
            yield return _progressStore.Execute(
                next,
                _sessionStore.Snapshot,
                operationId,
                value => saved = value,
                value => failure = value);
            if (_disposed) yield break;

            if (saved == null)
            {
                _busy = false;
                PowerMath.Diagnostics.AppLog.Warning(
                    "Tutorial",
                    $"Save failed for '{_sequence.Id}' at '{next.CurrentStepId}': " +
                    $"{failure?.Kind.ToString() ?? "No failure response"} — " +
                    (failure?.PlayerMessage ?? "No failure message."));
                string key = failure?.Kind == FirestoreRestClient.FailureKind.Conflict
                    ? "errors.conflict"
                    : "errors.save";
                if (_view.IsVisible) _view.ShowError(key);
                else StatusMessageService.ShowError(
                    PowerMath.Localization.LocalizationService.Get(key));
                if (!_view.IsVisible) ReleaseTutorialLock();
                yield break;
            }

            saved = MergeTutorialSaveIntoLiveSnapshot(saved);
            if (!TutorialProgressMapper.TryFind(
                saved, _sequence.Id, out _progress, out string error))
            {
                _busy = false;
                ReleaseTutorialLock();
                StatusMessageService.ShowError(error);
                yield break;
            }
            _busy = false;
            _view.SetBusy(false);
            if (_progress.Status == TutorialStatus.Completed)
                SequenceCompleted?.Invoke();
            TutorialStep persistedWaitStep = null;
            if (_progress.Status == TutorialStatus.Active &&
                _sequence.TryGetStep(_progress.CurrentStepId, out TutorialStep activeStep) &&
                activeStep.Kind == TutorialStepKind.Wait)
                persistedWaitStep = activeStep;
            if (presentAfterPersist) RenderCurrent();
            else HideAndRelease();
            // Clear the preceding focus proxy before wait-step side effects
            // (reward animation, reveal listeners, etc.) begin.
            if (persistedWaitStep != null)
                StepPresented?.Invoke(persistedWaitStep);
        }

        private PlayerSnapshot MergeTutorialSaveIntoLiveSnapshot(
            PlayerSnapshot saved)
        {
            PlayerSnapshot live = _sessionStore.Snapshot;
            if (live == null || ReferenceEquals(live, saved))
            {
                _sessionStore.NotifyAuthoritativeUpdate();
                return saved;
            }

            // Tutorial and combat share the player revision. Keep the live object
            // identity so the already-composed gameplay persistence adapter sees
            // the revision advanced by this tutorial command.
            live.schemaVersion = saved.schemaVersion;
            live.revision = saved.revision;
            live.tutorial = saved.tutorial;
            live.tutorialEntries = saved.tutorialEntries ??
                Array.Empty<PlayerSnapshot.TutorialEntryData>();
            _sessionStore.NotifyAuthoritativeUpdate();
            return live;
        }

        private void RenderCurrent()
        {
            if (_disposed || _progress == null ||
                _progress.Status == TutorialStatus.Completed)
            {
                HideAndRelease();
                return;
            }
            if (_progress.Status != TutorialStatus.Active ||
                !_sequence.TryGetStep(_progress.CurrentStepId, out TutorialStep step) ||
                step.Kind == TutorialStepKind.Wait ||
                (step.HidesPresentation && step.Kind != TutorialStepKind.FocusAction))
            {
                HideAndRelease();
                return;
            }

            PlayerSnapshot player = _sessionStore.Snapshot;
            bool ownsGate = _tutorialLock != null && !_tutorialLock.IsReleased;
            bool canPresentOutsideCombat = _allowNonCombatPresentation?.Invoke() == true;
            bool gateBlockedByOtherOwner = !ownsGate &&
                _interactionGate.Snapshot.IsBlocked &&
                !(_allowExternalGate?.Invoke() == true);
            var safeState = new TutorialSafeState(
                _sessionStore.IsReady,
                PlayerLifecyclePolicy.IsComplete(player),
                _busy,
                player?.activeRun?.pendingPresentation != null,
                string.Equals(player?.lastRunSettlement?.presentationStatus,
                    "Pending", StringComparison.Ordinal),
                canPresentOutsideCombat ||
                    _combat.CurrentCombatSnapshot?.Phase == CombatPhase.EnemyReady,
                canPresentOutsideCombat || IsStandard(_combat.CurrentCombatSnapshot),
                canPresentOutsideCombat || _combat.IsStableForTutorial || ownsGate,
                gateBlockedByOtherOwner,
                false);
            if (!TutorialSafeStatePolicy.CanPresent(safeState))
            {
                HideAndRelease();
                return;
            }

            TutorialFocusTarget target = default;
            if (step.Kind == TutorialStepKind.FocusAction &&
                !_targets.TryResolve(step.TargetId, step.FallbackTargetId, out target))
            {
                HideAndRelease();
                if (IsFailOpenTutorial)
                {
                    _host.StartCoroutine(CompleteSafelyRoutine(
                        "target-missing:" + step.TargetId));
                    return;
                }
                StatusMessageService.ShowError(
                    PowerMath.Localization.LocalizationService.Get(
                        "tutorial.targetUnavailable"));
                return;
            }
            _activeTarget = target;
            if (!_view.IsAttached)
            {
                HideAndRelease();
                StatusMessageService.ShowError(
                    PowerMath.Localization.LocalizationService.Get(
                        "tutorial.presentationUnavailable"));
                return;
            }

            try
            {
                // A qualifying trigger may already own a pending reservation.
                // Otherwise acquire the global gate only after the tutorial input
                // has been made visible.
                _isPresenting = true;
                _view.Show(step, player?.profile?.displayName, target);
                if (!ownsGate)
                {
                    _tutorialLock = _interactionGate.Acquire(
                        "tutorial:" + _sequence.Id + ":" + step.Id,
                        InteractionScope.All);
                }
                // Gate acquisition may append its transparent interaction shield
                // after the tutorial template. Restore the tutorial as the topmost
                // input and presentation layer.
                _view.BringToFront();
                PowerMath.Diagnostics.AppLog.Info(
                    "Tutorial",
                    $"Showing '{_sequence.Id}' step '{step.Id}'.");
                StepPresented?.Invoke(step);
            }
            catch (Exception exception)
            {
                HideAndRelease();
                PowerMath.Diagnostics.AppLog.Exception(
                    "Tutorial",
                    exception,
                    $"Tutorial step '{step.Id}' could not be presented");
                StatusMessageService.ShowError(
                    PowerMath.Localization.LocalizationService.Get(
                        "tutorial.presentationUnavailable"));
            }
        }

        private void HideAndRelease()
        {
            if (_isPresenting)
            {
                _view.Hide();
                _isPresenting = false;
            }
            ReleaseTutorialLock();
        }

        private void ReleaseTutorialLock()
        {
            IInteractionLock active = _tutorialLock;
            _tutorialLock = null;
            active?.Dispose();
        }

        private bool IsFailOpenTutorial => string.Equals(
            _sequence.Id, OnFirstRebirthId, StringComparison.Ordinal);

        private IEnumerator CompleteSafelyRoutine(string reason)
        {
            if (_failOpenInFlight || _disposed || _progress == null ||
                _progress.Status == TutorialStatus.Completed)
                yield break;
            _failOpenInFlight = true;
            HideAndRelease();
            TutorialProgress completed = _progress.With(
                status: TutorialStatus.Completed,
                completedAt: DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            PowerMath.Diagnostics.AppLog.Warning(
                "Tutorial",
                $"Fail-open completed '{_sequence.Id}' at " +
                $"'{_progress.CurrentStepId}' ({reason}).");
            yield return Persist(completed, presentAfterPersist: false);
            _failOpenInFlight = false;
        }

        private static bool IsStandard(CombatSnapshot snapshot) =>
            snapshot != null && snapshot.Phase == CombatPhase.EnemyReady &&
            snapshot.EncounterKind != StageEncounterKind.ChallengeEvent;

        private static TutorialAttemptOutcome MapOutcome(AttemptOutcomeKind value)
        {
            switch (value)
            {
                case AttemptOutcomeKind.Correct: return TutorialAttemptOutcome.Correct;
                case AttemptOutcomeKind.Timeout: return TutorialAttemptOutcome.Timeout;
                default: return TutorialAttemptOutcome.Incorrect;
            }
        }

        private static bool HasRule(
            TutorialStep step,
            TutorialSignalKind signal,
            TutorialAttemptOutcome outcome = TutorialAttemptOutcome.None)
        {
            if (step == null) return false;
            for (int index = 0; index < step.Rules.Count; index++)
            {
                TutorialRule rule = step.Rules[index];
                if (rule.Signal == signal &&
                    (outcome == TutorialAttemptOutcome.None ||
                     rule.RequiredOutcome == outcome))
                    return true;
            }
            return false;
        }

        private bool CanActivateFromQueue()
        {
            PlayerSnapshot.TutorialEntryData[] entries =
                _sessionStore.Snapshot?.tutorialEntries;
            if (entries == null) return true;

            // A prerequisite models a branch in the authored tutorial tree.  A child
            // trigger is persisted immediately, but it cannot interrupt its parent
            // while the parent still owns (or is waiting to own) the tutorial flow.
            for (int prerequisiteIndex = 0;
                 prerequisiteIndex < _sequence.PrerequisiteTutorialIds.Count;
                 prerequisiteIndex++)
            {
                string prerequisiteId = _sequence.PrerequisiteTutorialIds[prerequisiteIndex];
                foreach (PlayerSnapshot.TutorialEntryData entry in entries)
                {
                    if (entry == null || !string.Equals(entry.tutorialId, prerequisiteId,
                            StringComparison.Ordinal))
                        continue;
                    if (!string.Equals(entry.status, TutorialStatus.Completed.ToString(),
                            StringComparison.Ordinal))
                        return false;
                }
            }

            foreach (PlayerSnapshot.TutorialEntryData entry in entries)
            {
                if (entry == null || string.Equals(entry.tutorialId,
                        _sequence.Id, StringComparison.Ordinal))
                    continue;
                if (string.Equals(entry.status, TutorialStatus.Active.ToString(),
                        StringComparison.Ordinal))
                    return false;
                if (!string.Equals(entry.status, TutorialStatus.Queued.ToString(),
                        StringComparison.Ordinal))
                    continue;
                if (entry.triggerRecordedAtUnixSeconds < _progress.TriggerRecordedAt ||
                    entry.triggerRecordedAtUnixSeconds == _progress.TriggerRecordedAt &&
                    string.CompareOrdinal(entry.tutorialId, _sequence.Id) < 0)
                    return false;
            }
            return true;
        }

        private static bool HasLegacyRankChange(PlayerSnapshot player)
        {
            return player?.onboarding?.legacyPlayer == true &&
                !string.Equals(player.progression?.activeRank, "Silver",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
