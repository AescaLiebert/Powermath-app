namespace PowerMath.Gameplay.Combat.Unity
{
    public static class TutorialCombatTriggerPolicy
    {
        public static bool IsFirstEnemySurvive(CombatTutorialResult result)
        {
            return result.Destination != null &&
                result.Destination.EncounterKind != StageEncounterKind.ChallengeEvent &&
                result.Outcome == AttemptOutcomeKind.Correct &&
                result.FinalDamage > 0 &&
                !result.EnemyDefeated &&
                !result.EnemyFled &&
                result.EnemyActionConsumed &&
                string.Equals(result.SourceEncounterId,
                    result.Destination.EncounterId,
                    System.StringComparison.Ordinal);
        }
    }
}
