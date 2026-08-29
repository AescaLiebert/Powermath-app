namespace PowerMath.Gameplay.Combat.Presentation
{
    public readonly struct CombatInteractionReadinessSnapshot
    {
        public CombatInteractionReadinessSnapshot(
            CombatPhase authorityPhase,
            bool planEmpty,
            bool playerIdle,
            bool enemyIdle,
            bool enemyAlive,
            bool actionQueueStable,
            bool blockingUiStable,
            bool hasPendingRequiredReceipt)
        {
            AuthorityPhase = authorityPhase;
            PlanEmpty = planEmpty;
            PlayerIdle = playerIdle;
            EnemyIdle = enemyIdle;
            EnemyAlive = enemyAlive;
            ActionQueueStable = actionQueueStable;
            BlockingUiStable = blockingUiStable;
            HasPendingRequiredReceipt = hasPendingRequiredReceipt;
        }

        public CombatPhase AuthorityPhase { get; }
        public bool PlanEmpty { get; }
        public bool PlayerIdle { get; }
        public bool EnemyIdle { get; }
        public bool EnemyAlive { get; }
        public bool ActionQueueStable { get; }
        public bool BlockingUiStable { get; }
        public bool HasPendingRequiredReceipt { get; }
    }

    public static class CombatInteractionReadinessPolicy
    {
        public static bool IsReady(CombatInteractionReadinessSnapshot value)
        {
            bool authorityReady = value.AuthorityPhase == CombatPhase.EnemyReady ||
                value.AuthorityPhase == CombatPhase.EventReady;
            return authorityReady &&
                value.PlanEmpty &&
                value.PlayerIdle &&
                (!value.EnemyAlive || value.EnemyIdle) &&
                value.ActionQueueStable &&
                value.BlockingUiStable &&
                !value.HasPendingRequiredReceipt;
        }
    }
}
