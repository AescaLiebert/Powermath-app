using System;

namespace PowerMath.Gameplay.Tutorial
{
    public static class TutorialStateMachine
    {
        public static TutorialProgress Queue(
            TutorialSequence sequence,
            bool legacyPlayer,
            long triggerRecordedAt,
            string variant = "")
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            return new TutorialProgress(
                sequence.Id,
                sequence.Version,
                TutorialStatus.Queued,
                string.Empty,
                Math.Max(0, triggerRecordedAt),
                0,
                false,
                string.Empty,
                string.Empty,
                legacyPlayer,
                string.Empty,
                TutorialAttemptOutcome.None,
                variant);
        }

        public static TutorialProgress Restart(TutorialProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (progress.Status != TutorialStatus.Active) return progress;
            return progress.With(
                status: TutorialStatus.Queued,
                currentStepId: string.Empty,
                lastTransactionId: string.Empty,
                guidedEncounterId: string.Empty,
                firstAttemptOutcome: TutorialAttemptOutcome.None);
        }

        public static bool TryReduce(
            TutorialSequence sequence,
            TutorialProgress progress,
            TutorialSignal signal,
            long transitionTime,
            out TutorialProgress next)
        {
            next = progress;
            if (sequence == null || progress == null || signal == null ||
                progress.Status == TutorialStatus.Completed ||
                !string.Equals(sequence.Id, progress.TutorialId, StringComparison.Ordinal) ||
                sequence.Version != progress.Version)
                return false;

            if (progress.Status == TutorialStatus.Queued)
            {
                if (signal.Kind != TutorialSignalKind.SafeLobbyEntered ||
                    !signal.IsStandardEncounter || string.IsNullOrWhiteSpace(signal.EncounterId))
                    return false;
                next = progress.With(
                    status: TutorialStatus.Active,
                    currentStepId: string.Equals(progress.Variant, "demotion",
                        StringComparison.Ordinal)
                        ? sequence.GetStartStepId(true)
                        : sequence.GetStartStepId(progress.LegacyPlayer),
                    guidedEncounterId: signal.EncounterId);
                return true;
            }

            if (progress.Status != TutorialStatus.Active ||
                !sequence.TryGetStep(progress.CurrentStepId, out TutorialStep step))
                return false;

            for (int index = 0; index < step.Rules.Count; index++)
            {
                TutorialRule rule = step.Rules[index];
                if (!Matches(rule, progress, signal)) continue;
                string transactionId = signal.Kind == TutorialSignalKind.AttemptCommitted
                    ? signal.TransactionId
                    : progress.LastTransactionId;
                string encounterId = string.IsNullOrWhiteSpace(progress.GuidedEncounterId)
                    ? signal.EncounterId
                    : progress.GuidedEncounterId;
                TutorialAttemptOutcome outcome =
                    signal.Kind == TutorialSignalKind.AttemptPresentationCompleted
                        ? signal.Outcome
                        : progress.FirstAttemptOutcome;
                next = progress.With(
                    status: rule.CompletesSequence
                        ? TutorialStatus.Completed
                        : TutorialStatus.Active,
                    currentStepId: rule.CompletesSequence
                        ? progress.CurrentStepId
                        : rule.NextStepId,
                    completedAt: rule.CompletesSequence
                        ? Math.Max(0, transitionTime)
                        : progress.CompletedAt,
                    lastTransactionId: transactionId,
                    guidedEncounterId: encounterId,
                    firstAttemptOutcome: outcome);
                return true;
            }
            return false;
        }

        private static bool Matches(
            TutorialRule rule,
            TutorialProgress progress,
            TutorialSignal signal)
        {
            if (rule.Signal != signal.Kind) return false;
            if (!string.IsNullOrWhiteSpace(rule.TargetId) &&
                !string.Equals(rule.TargetId, signal.TargetId, StringComparison.Ordinal))
                return false;
            if (rule.RequiredOutcome != TutorialAttemptOutcome.None &&
                rule.RequiredOutcome != signal.Outcome)
                return false;
            if (rule.RequireMatchingTransaction &&
                (!string.IsNullOrWhiteSpace(progress.LastTransactionId) &&
                 !string.Equals(progress.LastTransactionId, signal.TransactionId,
                     StringComparison.Ordinal)))
                return false;
            if (rule.RequireDifferentEncounter &&
                (!signal.IsStandardEncounter || string.IsNullOrWhiteSpace(signal.EncounterId) ||
                 string.Equals(progress.GuidedEncounterId, signal.EncounterId,
                     StringComparison.Ordinal)))
                return false;
            return true;
        }
    }
}
