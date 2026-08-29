using NUnit.Framework;
using PowerMath.Gameplay.Combat.Presentation;

namespace PowerMath.Gameplay.Combat.Tests
{
    public sealed class CombatPresentationCoreTests
    {
        [Test]
        public void PlanBuilder_CorrectWalk_PresentsPlayerBeforeEnemyAction()
        {
            AttemptPresentationReceipt receipt = CreateReceipt(
                finalDamage: 3,
                resolvedEnemyHp: 7);

            CombatPresentationPlan plan = new CombatPresentationPlanBuilder().Build(receipt);

            Assert.That(IndexOf(plan, PresentationActionKind.PlayerPrimaryAttack),
                Is.LessThan(IndexOf(plan, PresentationActionKind.EnemyTakeDamage)));
            Assert.That(IndexOf(plan, PresentationActionKind.EnemyTakeDamage),
                Is.LessThan(IndexOf(plan, PresentationActionKind.ConsumeEnemyAction)));
            Assert.That(IndexOf(plan, PresentationActionKind.ConsumeEnemyAction),
                Is.LessThan(IndexOf(plan, PresentationActionKind.EnemyWalk)));
        }

        [Test]
        public void PlanBuilder_Critical_EmitsOneImpulseAndTargetedFct()
        {
            AttemptPresentationReceipt receipt = CreateReceipt(
                finalDamage: 3,
                resolvedEnemyHp: 7,
                isCritical: true);

            CombatPresentationPlan plan = new CombatPresentationPlanBuilder().Build(receipt);

            Assert.That(Count(plan, PresentationActionKind.PlayImpactImpulse), Is.EqualTo(1));
            Assert.That(Count(plan, PresentationActionKind.SpawnFloatingText), Is.EqualTo(1));
            CombatPresentationStep fct = Find(plan, PresentationActionKind.SpawnFloatingText);
            Assert.That(fct.TargetId, Is.EqualTo("enemy-a"));
            Assert.That(fct.Payload.IsCritical, Is.True);
        }

        [Test]
        public void PlanBuilder_EnemyDefeat_CancelsTokenAndNeverActs()
        {
            AttemptPresentationReceipt receipt = CreateReceipt(
                finalDamage: 10,
                resolvedEnemyHp: 0,
                enemyDefeated: true,
                stageAdvanced: true);

            CombatPresentationPlan plan = new CombatPresentationPlanBuilder().Build(receipt);

            Assert.That(Count(plan, PresentationActionKind.CancelEnemyAction), Is.EqualTo(1));
            Assert.That(Count(plan, PresentationActionKind.EnemyDie), Is.EqualTo(1));
            Assert.That(Count(plan, PresentationActionKind.EnemyWalk), Is.Zero);
            Assert.That(Count(plan, PresentationActionKind.EnemyAttack), Is.Zero);
            Assert.That(IndexOf(plan, PresentationActionKind.EnemyDie),
                Is.LessThan(IndexOf(plan, PresentationActionKind.EnemyAppear)));
        }

        [Test]
        public void PlanBuilder_PlayerDefeat_EndsActorFlowWithDieAndTransfersLock()
        {
            AttemptPresentationReceipt receipt = CreateReceipt(
                outcome: AttemptOutcomeKind.Timeout,
                finalDamage: 0,
                resolvedEnemyHp: 10,
                enemyAttacked: true,
                playerDefeated: true);

            CombatPresentationPlan plan = new CombatPresentationPlanBuilder().Build(receipt);

            Assert.That(IndexOf(plan, PresentationActionKind.EnemyAttack),
                Is.LessThan(IndexOf(plan, PresentationActionKind.PlayerTakeDamage)));
            Assert.That(IndexOf(plan, PresentationActionKind.PlayerTakeDamage),
                Is.LessThan(IndexOf(plan, PresentationActionKind.PlayerDie)));
            Assert.That(plan.TransfersToTerminalFlow, Is.True);
            Assert.That(plan.Steps[plan.Steps.Count - 1].Kind,
                Is.EqualTo(PresentationActionKind.PlayerDie));
            Assert.That(plan.Steps[plan.Steps.Count - 1].Barrier,
                Is.EqualTo(PresentationBarrier.Completion));
        }

        [Test]
        public void Receipt_RejectsUnknownVersion()
        {
            Assert.Throws<System.ArgumentException>(() => CreateReceipt(version: 99));
        }

        [Test]
        public void Readiness_RequiresAuthorityActorsQueueUiAndNoReceipt()
        {
            var ready = new CombatInteractionReadinessSnapshot(
                CombatPhase.EnemyReady, true, true, true, true, true, true, false);
            var pendingReceipt = new CombatInteractionReadinessSnapshot(
                CombatPhase.EnemyReady, true, true, true, true, true, true, true);
            var actorBusy = new CombatInteractionReadinessSnapshot(
                CombatPhase.EnemyReady, true, false, true, true, true, true, false);

            Assert.That(CombatInteractionReadinessPolicy.IsReady(ready), Is.True);
            Assert.That(CombatInteractionReadinessPolicy.IsReady(pendingReceipt), Is.False);
            Assert.That(CombatInteractionReadinessPolicy.IsReady(actorBusy), Is.False);
        }

        [Test]
        public void Recovery_PresentingWithoutReceipt_FailsClosed()
        {
            PresentationRecoveryKind result = PresentationRecoveryResolver.Resolve(
                CombatPhase.PresentingResult, null, null);

            Assert.That(result, Is.EqualTo(PresentationRecoveryKind.UnsupportedReceipt));
        }

        private static AttemptPresentationReceipt CreateReceipt(
            AttemptOutcomeKind outcome = AttemptOutcomeKind.Correct,
            int finalDamage = 0,
            int resolvedEnemyHp = 10,
            bool isCritical = false,
            bool enemyDefeated = false,
            bool enemyAttacked = false,
            bool playerDefeated = false,
            bool stageAdvanced = false,
            int version = AttemptPresentationReceipt.CurrentVersion)
        {
            int sourceHearts = playerDefeated ? 1 : 3;
            int destinationHearts = enemyAttacked ? (playerDefeated ? 0 : 2) : 3;
            CombatPhase phase = playerDefeated
                ? CombatPhase.RunDefeat
                : CombatPhase.PresentingResult;
            var source = new CombatPresentationSnapshot(
                new StageId(1), "biome-a", "enemy-a", StageEncounterKind.NormalMonster,
                10, 10, enemyAttacked ? 0 : 1, 2, sourceHearts, 3, CombatPhase.Committed);
            var destination = new CombatPresentationSnapshot(
                stageAdvanced ? new StageId(2) : new StageId(1),
                stageAdvanced ? "biome-b" : "biome-a",
                stageAdvanced ? "enemy-b" : "enemy-a",
                StageEncounterKind.NormalMonster,
                stageAdvanced ? 10 : resolvedEnemyHp,
                10,
                enemyAttacked ? 2 : 1,
                2,
                destinationHearts,
                3,
                phase);
            return new AttemptPresentationReceipt(
                "presentation-a",
                "attempt-a",
                outcome,
                outcome == AttemptOutcomeKind.Correct ? 10 : 0,
                finalDamage,
                isCritical,
                source,
                destination,
                resolvedEnemyHp,
                enemyDefeated,
                enemyAttacked,
                playerDefeated,
                stageAdvanced,
                stageAdvanced,
                default,
                version);
        }

        private static int IndexOf(
            CombatPresentationPlan plan,
            PresentationActionKind kind)
        {
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                if (plan.Steps[i].Kind == kind) return i;
            }
            return -1;
        }

        private static int Count(
            CombatPresentationPlan plan,
            PresentationActionKind kind)
        {
            int count = 0;
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                if (plan.Steps[i].Kind == kind) count++;
            }
            return count;
        }

        private static CombatPresentationStep Find(
            CombatPresentationPlan plan,
            PresentationActionKind kind)
        {
            int index = IndexOf(plan, kind);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            return plan.Steps[index];
        }
    }
}
