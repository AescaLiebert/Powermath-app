using System;
using NUnit.Framework;

namespace PowerMath.Gameplay.Tutorial.Tests
{
    public sealed class TutorialStateMachineTests
    {
        [Test]
        public void QueueThenSafeLobbyStartsCorrectVariant()
        {
            TutorialSequence sequence = BuildSequence();
            TutorialProgress progress = TutorialStateMachine.Queue(sequence, true, 100);

            bool changed = TutorialStateMachine.TryReduce(
                sequence,
                progress,
                new TutorialSignal(
                    TutorialSignalKind.SafeLobbyEntered,
                    encounterId: "enemy-9",
                    isStandardEncounter: true),
                101,
                out TutorialProgress next);

            Assert.That(changed, Is.True);
            Assert.That(next.Status, Is.EqualTo(TutorialStatus.Active));
            Assert.That(next.CurrentStepId, Is.EqualTo("returning"));
            Assert.That(next.GuidedEncounterId, Is.EqualTo("enemy-9"));
        }

        [Test]
        public void DemotionVariantStartsSupportiveBranchForNonLegacyPlayer()
        {
            TutorialSequence sequence = BuildSequence();
            TutorialProgress progress = TutorialStateMachine.Queue(
                sequence, false, 100, "demotion");

            Assert.That(TutorialStateMachine.TryReduce(
                sequence,
                progress,
                new TutorialSignal(
                    TutorialSignalKind.SafeLobbyEntered,
                    encounterId: "enemy-9",
                    isStandardEncounter: true),
                101,
                out TutorialProgress next), Is.True);

            Assert.That(next.Status, Is.EqualTo(TutorialStatus.Active));
            Assert.That(next.CurrentStepId, Is.EqualTo("returning"));
            Assert.That(next.Variant, Is.EqualTo("demotion"));
        }

        [Test]
        public void RestartRequeuesActiveFlowWithoutResettingRewardOrVariant()
        {
            var active = new TutorialProgress(
                "OnFirstRebirth", 1, TutorialStatus.Active, "reward-focus",
                100, 0, true, "attempt-4", "operation-8", false,
                "enemy-2", TutorialAttemptOutcome.Correct, "rewarded");

            TutorialProgress restarted = TutorialStateMachine.Restart(active);

            Assert.That(restarted.Status, Is.EqualTo(TutorialStatus.Queued));
            Assert.That(restarted.CurrentStepId, Is.Empty);
            Assert.That(restarted.LastTransactionId, Is.Empty);
            Assert.That(restarted.GuidedEncounterId, Is.Empty);
            Assert.That(restarted.FirstAttemptOutcome,
                Is.EqualTo(TutorialAttemptOutcome.None));
            Assert.That(restarted.RewardClaimed, Is.True);
            Assert.That(restarted.Variant, Is.EqualTo("rewarded"));
            Assert.That(restarted.TriggerRecordedAt, Is.EqualTo(100));
        }

        [Test]
        public void OutcomeBranchRequiresMatchingCommittedAttempt()
        {
            TutorialSequence sequence = BuildSequence();
            TutorialProgress progress = TutorialStateMachine.Queue(sequence, false, 100);
            TutorialStateMachine.TryReduce(
                sequence, progress,
                new TutorialSignal(TutorialSignalKind.SafeLobbyEntered,
                    encounterId: "enemy-1", isStandardEncounter: true),
                101, out progress);
            TutorialStateMachine.TryReduce(
                sequence, progress,
                new TutorialSignal(TutorialSignalKind.AdvanceRequested),
                102, out progress);
            TutorialStateMachine.TryReduce(
                sequence, progress,
                new TutorialSignal(TutorialSignalKind.AttemptCommitted,
                    transactionId: "attempt-1", encounterId: "enemy-1"),
                103, out progress);

            Assert.That(TutorialStateMachine.TryReduce(
                sequence, progress,
                new TutorialSignal(TutorialSignalKind.AttemptPresentationCompleted,
                    transactionId: "different", outcome: TutorialAttemptOutcome.Correct),
                104, out _), Is.False);
            Assert.That(TutorialStateMachine.TryReduce(
                sequence, progress,
                new TutorialSignal(TutorialSignalKind.AttemptPresentationCompleted,
                    transactionId: "attempt-1", outcome: TutorialAttemptOutcome.Timeout),
                104, out TutorialProgress next), Is.True);
            Assert.That(next.CurrentStepId, Is.EqualTo("timeout"));
            Assert.That(next.FirstAttemptOutcome, Is.EqualTo(TutorialAttemptOutcome.Timeout));
        }

        [Test]
        public void EncounterWaitRequiresDifferentStandardEncounter()
        {
            TutorialSequence sequence = BuildSequence();
            var progress = new TutorialProgress(
                sequence.Id, sequence.Version, TutorialStatus.Active, "wait-encounter",
                1, 0, false, "attempt-1", string.Empty, false, "enemy-1",
                TutorialAttemptOutcome.Correct);

            Assert.That(TutorialStateMachine.TryReduce(sequence, progress,
                new TutorialSignal(TutorialSignalKind.EncounterReady,
                    encounterId: "enemy-1", isStandardEncounter: true), 2, out _), Is.False);
            Assert.That(TutorialStateMachine.TryReduce(sequence, progress,
                new TutorialSignal(TutorialSignalKind.EncounterReady,
                    encounterId: "event-1", isStandardEncounter: false), 2, out _), Is.False);
            Assert.That(TutorialStateMachine.TryReduce(sequence, progress,
                new TutorialSignal(TutorialSignalKind.EncounterReady,
                    encounterId: "enemy-2", isStandardEncounter: true), 2,
                out TutorialProgress next), Is.True);
            Assert.That(next.CurrentStepId, Is.EqualTo("handoff"));
        }

        [Test]
        public void FinalAdvanceCompletesExactlyOnce()
        {
            TutorialSequence sequence = BuildSequence();
            var progress = new TutorialProgress(
                sequence.Id, sequence.Version, TutorialStatus.Active, "handoff",
                1, 0, false, "attempt-1", string.Empty, false, "enemy-1",
                TutorialAttemptOutcome.Correct);

            Assert.That(TutorialStateMachine.TryReduce(sequence, progress,
                new TutorialSignal(TutorialSignalKind.AdvanceRequested), 55,
                out TutorialProgress completed), Is.True);
            Assert.That(completed.Status, Is.EqualTo(TutorialStatus.Completed));
            Assert.That(completed.CompletedAt, Is.EqualTo(55));
            Assert.That(TutorialStateMachine.TryReduce(sequence, completed,
                new TutorialSignal(TutorialSignalKind.AdvanceRequested), 56,
                out _), Is.False);
        }

        [Test]
        public void SafeStateRejectsEveryUnsafeOwnershipCondition()
        {
            Assert.That(TutorialSafeStatePolicy.CanPresent(new TutorialSafeState(
                true, true, false, false, false, true, true, true, false, false)), Is.True);
            Assert.That(TutorialSafeStatePolicy.CanPresent(new TutorialSafeState(
                true, true, false, true, false, true, true, true, false, false)), Is.False);
            Assert.That(TutorialSafeStatePolicy.CanPresent(new TutorialSafeState(
                true, true, false, false, false, true, false, true, false, false)), Is.False);
            Assert.That(TutorialSafeStatePolicy.CanPresent(new TutorialSafeState(
                true, true, true, false, false, true, true, true, false, false)), Is.False);
        }

        [Test]
        public void SequencePreservesAuthoredPrerequisitesAndRejectsSelfDependency()
        {
            TutorialSequence sequence = new TutorialSequence(
                "OnFirstEnemySurvive", 1, "warning", string.Empty,
                new[] { Step("warning", TutorialStepKind.Dialogue,
                    new TutorialRule(TutorialSignalKind.AdvanceRequested, string.Empty,
                        completesSequence: true)) },
                new[] { "OnFirstCreate", "OnFirstCreate", string.Empty });

            Assert.That(sequence.PrerequisiteTutorialIds,
                Is.EqualTo(new[] { "OnFirstCreate" }));
            Assert.Throws<ArgumentException>(() => new TutorialSequence(
                "OnFirstEnemySurvive", 1, "warning", string.Empty,
                new[] { Step("warning", TutorialStepKind.Dialogue,
                    new TutorialRule(TutorialSignalKind.AdvanceRequested, string.Empty,
                        completesSequence: true)) },
                new[] { "OnFirstEnemySurvive" }));
        }

        private static TutorialSequence BuildSequence()
        {
            TutorialRule Advance(string next) =>
                new TutorialRule(TutorialSignalKind.AdvanceRequested, next);
            return new TutorialSequence(
                "OnFirstCreate", 1, "welcome", "returning",
                new[]
                {
                    Step("welcome", TutorialStepKind.Dialogue, Advance("attack")),
                    Step("returning", TutorialStepKind.Dialogue, Advance("attack")),
                    Step("attack", TutorialStepKind.FocusAction,
                        new TutorialRule(TutorialSignalKind.AttemptCommitted, "wait-result")),
                    Step("wait-result", TutorialStepKind.Wait,
                        new TutorialRule(TutorialSignalKind.AttemptPresentationCompleted,
                            "correct", requiredOutcome: TutorialAttemptOutcome.Correct,
                            requireMatchingTransaction: true),
                        new TutorialRule(TutorialSignalKind.AttemptPresentationCompleted,
                            "timeout", requiredOutcome: TutorialAttemptOutcome.Timeout,
                            requireMatchingTransaction: true)),
                    Step("correct", TutorialStepKind.Dialogue, Advance("wait-encounter")),
                    Step("timeout", TutorialStepKind.Dialogue, Advance("wait-encounter")),
                    Step("wait-encounter", TutorialStepKind.Wait,
                        new TutorialRule(TutorialSignalKind.EncounterReady, "handoff",
                            requireDifferentEncounter: true)),
                    Step("handoff", TutorialStepKind.Dialogue,
                        new TutorialRule(TutorialSignalKind.AdvanceRequested, string.Empty,
                            completesSequence: true))
                });
        }

        private static TutorialStep Step(
            string id,
            TutorialStepKind kind,
            params TutorialRule[] rules)
        {
            return new TutorialStep(
                id, kind,
                kind == TutorialStepKind.Wait ? string.Empty : "speaker",
                kind == TutorialStepKind.Wait ? string.Empty : "text",
                string.Empty, string.Empty, string.Empty,
                kind == TutorialStepKind.FocusAction ? "combat.enemy" : string.Empty,
                string.Empty,
                kind == TutorialStepKind.Wait,
                rules);
        }
    }
}
