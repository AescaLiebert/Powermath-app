using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Tutorial
{
    public enum TutorialStatus
    {
        Eligible,
        Queued,
        Active,
        Completed
    }

    public enum TutorialStepKind
    {
        Dialogue,
        FocusAction,
        Wait
    }

    public enum TutorialSignalKind
    {
        SafeLobbyEntered,
        AdvanceRequested,
        TargetActivated,
        AttemptCommitted,
        AttemptPresentationCompleted,
        EncounterReady,
        ExternalEvent
    }

    public enum TutorialAttemptOutcome
    {
        None,
        Correct,
        Incorrect,
        Timeout,
        Abandoned
    }

    public sealed class TutorialSignal
    {
        public TutorialSignal(
            TutorialSignalKind kind,
            string targetId = "",
            string transactionId = "",
            string encounterId = "",
            TutorialAttemptOutcome outcome = TutorialAttemptOutcome.None,
            bool isStandardEncounter = false)
        {
            Kind = kind;
            TargetId = targetId ?? string.Empty;
            TransactionId = transactionId ?? string.Empty;
            EncounterId = encounterId ?? string.Empty;
            Outcome = outcome;
            IsStandardEncounter = isStandardEncounter;
        }

        public TutorialSignalKind Kind { get; }
        public string TargetId { get; }
        public string TransactionId { get; }
        public string EncounterId { get; }
        public TutorialAttemptOutcome Outcome { get; }
        public bool IsStandardEncounter { get; }
    }

    public sealed class TutorialRule
    {
        public TutorialRule(
            TutorialSignalKind signal,
            string nextStepId,
            string targetId = "",
            TutorialAttemptOutcome requiredOutcome = TutorialAttemptOutcome.None,
            bool requireMatchingTransaction = false,
            bool requireDifferentEncounter = false,
            bool completesSequence = false)
        {
            Signal = signal;
            NextStepId = nextStepId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            RequiredOutcome = requiredOutcome;
            RequireMatchingTransaction = requireMatchingTransaction;
            RequireDifferentEncounter = requireDifferentEncounter;
            CompletesSequence = completesSequence;
        }

        public TutorialSignalKind Signal { get; }
        public string NextStepId { get; }
        public string TargetId { get; }
        public TutorialAttemptOutcome RequiredOutcome { get; }
        public bool RequireMatchingTransaction { get; }
        public bool RequireDifferentEncounter { get; }
        public bool CompletesSequence { get; }
    }

    public sealed class TutorialStep
    {
        private readonly TutorialRule[] _rules;

        public TutorialStep(
            string id,
            TutorialStepKind kind,
            string speakerKey,
            string textKey,
            string emotionId,
            string spriteResourcePath,
            string audioCueId,
            string targetId,
            string fallbackTargetId,
            bool hidesPresentation,
            IReadOnlyList<TutorialRule> rules)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tutorial step ID is required.", nameof(id));
            Id = id;
            Kind = kind;
            SpeakerKey = speakerKey ?? string.Empty;
            TextKey = textKey ?? string.Empty;
            EmotionId = emotionId ?? string.Empty;
            SpriteResourcePath = spriteResourcePath ?? string.Empty;
            AudioCueId = audioCueId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            FallbackTargetId = fallbackTargetId ?? string.Empty;
            HidesPresentation = hidesPresentation;
            _rules = Copy(rules);
        }

        public string Id { get; }
        public TutorialStepKind Kind { get; }
        public string SpeakerKey { get; }
        public string TextKey { get; }
        public string EmotionId { get; }
        public string SpriteResourcePath { get; }
        public string AudioCueId { get; }
        public string TargetId { get; }
        public string FallbackTargetId { get; }
        public bool HidesPresentation { get; }
        public IReadOnlyList<TutorialRule> Rules => _rules;

        private static TutorialRule[] Copy(IReadOnlyList<TutorialRule> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<TutorialRule>();
            var result = new TutorialRule[source.Count];
            for (int index = 0; index < source.Count; index++)
                result[index] = source[index];
            return result;
        }
    }

    public sealed class TutorialSequence
    {
        private readonly Dictionary<string, TutorialStep> _steps;

        public TutorialSequence(
            string id,
            int version,
            string startStepId,
            string returningStartStepId,
            IReadOnlyList<TutorialStep> steps,
            IReadOnlyList<string> prerequisiteTutorialIds = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tutorial ID is required.", nameof(id));
            if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
            Id = id;
            Version = version;
            StartStepId = startStepId ?? string.Empty;
            ReturningStartStepId = returningStartStepId ?? string.Empty;
            PrerequisiteTutorialIds = CopyPrerequisiteTutorialIds(prerequisiteTutorialIds);
            _steps = new Dictionary<string, TutorialStep>(StringComparer.Ordinal);
            if (steps != null)
            {
                for (int index = 0; index < steps.Count; index++)
                {
                    TutorialStep step = steps[index] ??
                        throw new ArgumentException("Tutorial steps cannot contain null.", nameof(steps));
                    if (!_steps.TryAdd(step.Id, step))
                        throw new ArgumentException("Duplicate tutorial step: " + step.Id, nameof(steps));
                }
            }
            Validate();
        }

        public string Id { get; }
        public int Version { get; }
        public string StartStepId { get; }
        public string ReturningStartStepId { get; }
        /// <summary>
        /// Tutorials which must finish before this root sequence may take ownership of the UI.
        /// This permits authored tutorial trees without coupling individual directors together.
        /// </summary>
        public IReadOnlyList<string> PrerequisiteTutorialIds { get; }
        public IReadOnlyCollection<TutorialStep> Steps => _steps.Values;

        public bool TryGetStep(string stepId, out TutorialStep step) =>
            _steps.TryGetValue(stepId ?? string.Empty, out step);

        public string GetStartStepId(bool legacyPlayer) =>
            legacyPlayer && !string.IsNullOrWhiteSpace(ReturningStartStepId)
                ? ReturningStartStepId
                : StartStepId;

        private static IReadOnlyList<string> CopyPrerequisiteTutorialIds(
            IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0) return Array.Empty<string>();

            var result = new List<string>(source.Count);
            for (int index = 0; index < source.Count; index++)
            {
                string tutorialId = source[index];
                if (string.IsNullOrWhiteSpace(tutorialId)) continue;
                if (!result.Contains(tutorialId)) result.Add(tutorialId);
            }
            return result.ToArray();
        }

        private void Validate()
        {
            if (!_steps.ContainsKey(StartStepId))
                throw new ArgumentException("Tutorial start step is missing: " + StartStepId);
            if (!string.IsNullOrWhiteSpace(ReturningStartStepId) &&
                !_steps.ContainsKey(ReturningStartStepId))
                throw new ArgumentException("Returning-player start step is missing: " + ReturningStartStepId);
            for (int index = 0; index < PrerequisiteTutorialIds.Count; index++)
            {
                if (string.Equals(PrerequisiteTutorialIds[index], Id, StringComparison.Ordinal))
                    throw new ArgumentException("A tutorial cannot depend on itself: " + Id);
            }
            foreach (TutorialStep step in _steps.Values)
            {
                if (step.Kind != TutorialStepKind.Wait &&
                    (string.IsNullOrWhiteSpace(step.SpeakerKey) ||
                     string.IsNullOrWhiteSpace(step.TextKey)))
                    throw new ArgumentException("Visible tutorial step requires localization keys: " + step.Id);
                if (step.Kind == TutorialStepKind.FocusAction &&
                    string.IsNullOrWhiteSpace(step.TargetId))
                    throw new ArgumentException("Focus step requires a target: " + step.Id);
                foreach (TutorialRule rule in step.Rules)
                {
                    if (!rule.CompletesSequence && !_steps.ContainsKey(rule.NextStepId))
                        throw new ArgumentException(
                            $"Tutorial transition '{step.Id}' references missing step '{rule.NextStepId}'.");
                }
            }
        }
    }

    public sealed class TutorialProgress
    {
        public TutorialProgress(
            string tutorialId,
            int version,
            TutorialStatus status,
            string currentStepId,
            long triggerRecordedAt,
            long completedAt,
            bool rewardClaimed,
            string lastTransactionId,
            string lastOperationId,
            bool legacyPlayer,
            string guidedEncounterId,
            TutorialAttemptOutcome firstAttemptOutcome,
            string variant = "")
        {
            TutorialId = tutorialId ?? string.Empty;
            Version = version;
            Status = status;
            CurrentStepId = currentStepId ?? string.Empty;
            TriggerRecordedAt = triggerRecordedAt;
            CompletedAt = completedAt;
            RewardClaimed = rewardClaimed;
            LastTransactionId = lastTransactionId ?? string.Empty;
            LastOperationId = lastOperationId ?? string.Empty;
            LegacyPlayer = legacyPlayer;
            GuidedEncounterId = guidedEncounterId ?? string.Empty;
            FirstAttemptOutcome = firstAttemptOutcome;
            Variant = variant ?? string.Empty;
        }

        public string TutorialId { get; }
        public int Version { get; }
        public TutorialStatus Status { get; }
        public string CurrentStepId { get; }
        public long TriggerRecordedAt { get; }
        public long CompletedAt { get; }
        public bool RewardClaimed { get; }
        public string LastTransactionId { get; }
        public string LastOperationId { get; }
        public bool LegacyPlayer { get; }
        public string GuidedEncounterId { get; }
        public TutorialAttemptOutcome FirstAttemptOutcome { get; }
        public string Variant { get; }

        public TutorialProgress With(
            TutorialStatus? status = null,
            string currentStepId = null,
            long? triggerRecordedAt = null,
            long? completedAt = null,
            string lastTransactionId = null,
            string lastOperationId = null,
            string guidedEncounterId = null,
            TutorialAttemptOutcome? firstAttemptOutcome = null)
        {
            return new TutorialProgress(
                TutorialId,
                Version,
                status ?? Status,
                currentStepId ?? CurrentStepId,
                triggerRecordedAt ?? TriggerRecordedAt,
                completedAt ?? CompletedAt,
                RewardClaimed,
                lastTransactionId ?? LastTransactionId,
                lastOperationId ?? LastOperationId,
                LegacyPlayer,
                guidedEncounterId ?? GuidedEncounterId,
                firstAttemptOutcome ?? FirstAttemptOutcome,
                Variant);
        }
    }

    public readonly struct TutorialSafeState
    {
        public TutorialSafeState(
            bool sessionReady,
            bool characterCreationComplete,
            bool requestInFlight,
            bool pendingCombatPresentation,
            bool pendingTerminalPresentation,
            bool combatReady,
            bool standardEncounter,
            bool presentationStable,
            bool blockingPanelOpen,
            bool anotherTutorialActive)
        {
            SessionReady = sessionReady;
            CharacterCreationComplete = characterCreationComplete;
            RequestInFlight = requestInFlight;
            PendingCombatPresentation = pendingCombatPresentation;
            PendingTerminalPresentation = pendingTerminalPresentation;
            CombatReady = combatReady;
            StandardEncounter = standardEncounter;
            PresentationStable = presentationStable;
            BlockingPanelOpen = blockingPanelOpen;
            AnotherTutorialActive = anotherTutorialActive;
        }

        public bool SessionReady { get; }
        public bool CharacterCreationComplete { get; }
        public bool RequestInFlight { get; }
        public bool PendingCombatPresentation { get; }
        public bool PendingTerminalPresentation { get; }
        public bool CombatReady { get; }
        public bool StandardEncounter { get; }
        public bool PresentationStable { get; }
        public bool BlockingPanelOpen { get; }
        public bool AnotherTutorialActive { get; }
    }

    public static class TutorialSafeStatePolicy
    {
        public static bool CanPresent(TutorialSafeState state) =>
            state.SessionReady && state.CharacterCreationComplete &&
            !state.RequestInFlight && !state.PendingCombatPresentation &&
            !state.PendingTerminalPresentation && state.CombatReady &&
            state.StandardEncounter && state.PresentationStable &&
            !state.BlockingPanelOpen && !state.AnotherTutorialActive;
    }
}
