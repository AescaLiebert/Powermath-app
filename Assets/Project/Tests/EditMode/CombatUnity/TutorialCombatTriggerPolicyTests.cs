using NUnit.Framework;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class TutorialCombatTriggerPolicyTests
    {
        [Test]
        public void EnemySurvive_RequiresDamagingCorrectStandardResult()
        {
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result()), Is.True);
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result(damage: 0)), Is.False);
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result(outcome: AttemptOutcomeKind.Incorrect, damage: 0)), Is.False);
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result(enemyDefeated: true)), Is.False);
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result(enemyFled: true)), Is.False);
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result(actionConsumed: false)), Is.False);
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result(kind: StageEncounterKind.ChallengeEvent)), Is.False);
            Assert.That(TutorialCombatTriggerPolicy.IsFirstEnemySurvive(
                Result(destinationEncounterId: "enemy-2")), Is.False);
        }

        private static CombatTutorialResult Result(
            AttemptOutcomeKind outcome = AttemptOutcomeKind.Correct,
            int damage = 3,
            bool enemyDefeated = false,
            bool enemyFled = false,
            bool actionConsumed = true,
            StageEncounterKind kind = StageEncounterKind.NormalMonster,
            string destinationEncounterId = "enemy-1")
        {
            var destination = new CombatSnapshot(
                new StageId(1),
                destinationEncounterId,
                "Monster",
                enemyDefeated ? 0 : 7,
                10,
                2,
                3,
                3,
                3,
                CombatPhase.EnemyReady,
                false,
                "biome-1",
                "Biome",
                kind,
                string.Empty,
                0);
            return new CombatTutorialResult(
                "attempt-1",
                "enemy-1",
                destination,
                outcome,
                default,
                damage,
                enemyDefeated,
                enemyFled,
                actionConsumed);
        }
    }
}
