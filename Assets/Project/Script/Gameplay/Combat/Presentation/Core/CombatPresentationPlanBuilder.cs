using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Combat.Presentation
{
    public interface ICombatPresentationPlanBuilder
    {
        CombatPresentationPlan Build(AttemptPresentationReceipt receipt);
        CombatPresentationPlan BuildDeathRecovery(RunPresentationReceipt receipt);
        CombatPresentationPlan BuildRebirthRecovery(RunPresentationReceipt receipt);
    }

    public sealed class CombatPresentationPlanBuilder : ICombatPresentationPlanBuilder
    {
        public CombatPresentationPlan Build(AttemptPresentationReceipt receipt)
        {
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));
            if (!receipt.TryValidate(out string error))
                throw new ArgumentException(error, nameof(receipt));

            var steps = new List<CombatPresentationStep>(18);
            int ordinal = 0;
            Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                PresentationActionKind.AnswerFeedback, string.Empty,
                new PresentationPayload(receipt.ResponseScore, semantic: receipt.Outcome.ToString()));
            Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                PresentationActionKind.ArmEnemyAction, receipt.Source.EncounterId,
                new PresentationPayload(semantic: ResolveToken(receipt).ToString()));

            if (receipt.Outcome == AttemptOutcomeKind.Correct)
            {
                Add(steps, receipt, ref ordinal, PresentationActor.Player,
                    PresentationActionKind.PlayerPrimaryAttack, receipt.Source.EncounterId);
                Add(steps, receipt, ref ordinal, PresentationActor.Enemy,
                    PresentationActionKind.EnemyTakeDamage, receipt.Source.EncounterId,
                    new PresentationPayload(receipt.FinalDamage, isCritical: receipt.IsCritical));
                Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                    PresentationActionKind.InterpolateEnemyHp, receipt.Source.EncounterId,
                    new PresentationPayload(receipt.ResolvedEnemyHpAfter,
                        receipt.Source.EnemyCurrentHp, receipt.Source.EnemyMaximumHp));
                Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                    PresentationActionKind.SpawnFloatingText, receipt.Source.EncounterId,
                    new PresentationPayload(receipt.FinalDamage,
                        isCritical: receipt.IsCritical, semantic: "Damage"));
                if (receipt.IsCritical)
                {
                    Add(steps, receipt, ref ordinal, PresentationActor.System,
                        PresentationActionKind.PlayImpactImpulse, receipt.Source.EncounterId,
                        new PresentationPayload(receipt.FinalDamage, isCritical: true));
                }
            }
            else
            {
                Add(steps, receipt, ref ordinal, PresentationActor.Player,
                    PresentationActionKind.PlayerFailedAttack, receipt.Source.EncounterId,
                    new PresentationPayload(semantic: receipt.Outcome.ToString()));
            }

            if (receipt.EnemyDefeated)
            {
                Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                    PresentationActionKind.CancelEnemyAction, receipt.Source.EncounterId);
                Add(steps, receipt, ref ordinal, PresentationActor.Enemy,
                    PresentationActionKind.EnemyDie, receipt.Source.EncounterId);

                if (receipt.StageAdvanced)
                {
                    if (receipt.BiomeChanged)
                    {
                        Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                            PresentationActionKind.PlayBiomeTransition,
                            receipt.Destination.BiomeId);
                    }
                    Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                        PresentationActionKind.ShowStageResult,
                        receipt.Destination.Stage.Value.ToString());
                    Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                        PresentationActionKind.InitiateEnemyActions,
                        receipt.Destination.EncounterId,
                        new PresentationPayload(receipt.Destination.EnemyRemainingCooldown));
                    Add(steps, receipt, ref ordinal, PresentationActor.Enemy,
                        PresentationActionKind.EnemyAppear,
                        receipt.Destination.EncounterId);
                }
            }
            else
            {
                EnemyActionTokenKind token = ResolveToken(receipt);
                Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                    PresentationActionKind.ConsumeEnemyAction,
                    receipt.Source.EncounterId,
                    new PresentationPayload(semantic: token.ToString()));
                Add(steps, receipt, ref ordinal, PresentationActor.Enemy,
                    token == EnemyActionTokenKind.Walk
                        ? PresentationActionKind.EnemyWalk
                        : PresentationActionKind.EnemyAttack,
                    receipt.Source.EncounterId);

                if (receipt.EnemyAttacked)
                {
                    Add(steps, receipt, ref ordinal, PresentationActor.Player,
                        PresentationActionKind.PlayerTakeDamage, "player");
                    Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                        PresentationActionKind.InterpolatePlayerHearts, "player",
                        new PresentationPayload(receipt.Destination.PlayerCurrentHearts,
                            receipt.Source.PlayerCurrentHearts,
                            receipt.Source.PlayerMaximumHearts));
                    if (receipt.PlayerDefeated)
                    {
                        Add(steps, receipt, ref ordinal, PresentationActor.Player,
                            PresentationActionKind.PlayerDie, "player");
                    }
                    else
                    {
                        Add(steps, receipt, ref ordinal, PresentationActor.Ui,
                            PresentationActionKind.InitiateEnemyActions,
                            receipt.Destination.EncounterId,
                            new PresentationPayload(receipt.Destination.EnemyRemainingCooldown));
                    }
                }
            }

            if (receipt.RankTransition.IsPromotion)
            {
                Add(steps, receipt, ref ordinal, PresentationActor.Player,
                    PresentationActionKind.PlayerRankUp, "player");
            }
            else if (receipt.RankTransition.IsDemotion)
            {
                Add(steps, receipt, ref ordinal, PresentationActor.Player,
                    PresentationActionKind.PlayerRankDown, "player");
            }

            MarkCompletion(steps);
            return new CombatPresentationPlan(
                receipt.PresentationId,
                steps,
                receipt.PlayerDefeated || receipt.Destination.Phase == CombatPhase.RunComplete);
        }

        public CombatPresentationPlan BuildDeathRecovery(RunPresentationReceipt receipt)
        {
            ValidateRunReceipt(receipt, RunPresentationCause.Death);
            var steps = new List<CombatPresentationStep>(3)
            {
                RunStep(receipt, 0, PresentationActionKind.PlayerTakeDamage),
                RunStep(receipt, 1, PresentationActionKind.PlayerDie),
                RunStep(receipt, 2, PresentationActionKind.ShowStageResult)
            };
            MarkCompletion(steps);
            return new CombatPresentationPlan(receipt.PresentationId, steps, true);
        }

        public CombatPresentationPlan BuildRebirthRecovery(RunPresentationReceipt receipt)
        {
            ValidateRunReceipt(receipt, RunPresentationCause.Rebirth);
            var steps = new List<CombatPresentationStep>(2)
            {
                RunStep(receipt, 0, PresentationActionKind.PlayerRebirth),
                RunStep(receipt, 1, PresentationActionKind.ShowStageResult)
            };
            MarkCompletion(steps);
            return new CombatPresentationPlan(receipt.PresentationId, steps, true);
        }

        private static void Add(
            ICollection<CombatPresentationStep> steps,
            AttemptPresentationReceipt receipt,
            ref int ordinal,
            PresentationActor actor,
            PresentationActionKind kind,
            string targetId,
            PresentationPayload payload = null)
        {
            steps.Add(new CombatPresentationStep(
                receipt.PresentationId + ":" + ordinal++, actor, kind, targetId,
                payload, kind.ToString(), PresentationBarrier.Blocking));
        }

        private static EnemyActionTokenKind ResolveToken(AttemptPresentationReceipt receipt)
        {
            if (receipt.Source.EncounterKind == StageEncounterKind.ChallengeEvent)
                return EnemyActionTokenKind.EventRisk;
            return receipt.EnemyAttacked
                ? EnemyActionTokenKind.Attack
                : EnemyActionTokenKind.Walk;
        }

        private static CombatPresentationStep RunStep(
            RunPresentationReceipt receipt,
            int ordinal,
            PresentationActionKind kind)
        {
            PresentationActor actor = kind == PresentationActionKind.ShowStageResult
                ? PresentationActor.Ui
                : PresentationActor.Player;
            return new CombatPresentationStep(
                receipt.PresentationId + ":" + ordinal,
                actor,
                kind,
                kind == PresentationActionKind.ShowStageResult
                    ? receipt.SourceRunId
                    : "player",
                new PresentationPayload(semantic: receipt.Cause.ToString()),
                kind.ToString(),
                PresentationBarrier.Blocking);
        }

        private static void MarkCompletion(IList<CombatPresentationStep> steps)
        {
            if (steps.Count == 0) return;
            CombatPresentationStep last = steps[steps.Count - 1];
            steps[steps.Count - 1] = new CombatPresentationStep(
                last.StepId,
                last.Actor,
                last.Kind,
                last.TargetId,
                last.Payload,
                last.ProfileKey,
                PresentationBarrier.Completion);
        }

        private static void ValidateRunReceipt(
            RunPresentationReceipt receipt,
            RunPresentationCause expectedCause)
        {
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));
            if (!receipt.IsSupported || !receipt.IsPending || receipt.Cause != expectedCause ||
                string.IsNullOrWhiteSpace(receipt.PresentationId) ||
                string.IsNullOrWhiteSpace(receipt.SourceRunId))
                throw new ArgumentException("Run presentation receipt is invalid for recovery.", nameof(receipt));
        }
    }
}
