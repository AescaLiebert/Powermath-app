using System;
using PowerMath.PlayerData;

namespace PowerMath.Gameplay.Progression
{
    public enum RunSettlementType { Death, Rebirth }

    public readonly struct RunSettlementAward
    {
        public RunSettlementAward(int stage, long powerCoins, long legacyBasisPoints, int prestige)
        {
            StageReached = stage;
            PowerCoins = powerCoins;
            LegacyBasisPoints = legacyBasisPoints;
            Prestige = prestige;
        }

        public int StageReached { get; }
        public long PowerCoins { get; }
        public long LegacyBasisPoints { get; }
        public int Prestige { get; }
    }

    public readonly struct RunSettlementPresentationValues
    {
        public RunSettlementPresentationValues(
            long sourceEffectiveAttack,
            long resultingEffectiveAttack)
        {
            SourceEffectiveAttack = Math.Max(0, sourceEffectiveAttack);
            ResultingEffectiveAttack = Math.Max(0, resultingEffectiveAttack);
        }

        public long SourceEffectiveAttack { get; }
        public long ResultingEffectiveAttack { get; }
    }

    public static class RunSettlementPolicy
    {
        public const int MinimumRebirthStage = 50;
        public const int DefaultBonusBasisPoints = 10000;

        public static bool CanSettle(PlayerSnapshot player, RunSettlementType type, out string reason)
        {
            reason = string.Empty;
            if (player?.progression == null || player.activeRun == null)
            {
                reason = "Player run data is unavailable.";
                return false;
            }
            if (!string.IsNullOrEmpty(player.activeRun.committedAttemptId))
            {
                reason = "Finish the current question first.";
                return false;
            }
            int stage = Math.Min(200, Math.Max(1,
                Math.Max(player.progression.currentStage, player.activeRun.currentStage)));
            if (type == RunSettlementType.Death &&
                !string.Equals(player.activeRun.phase, "RunDefeat", StringComparison.Ordinal))
            {
                reason = "This run has not ended in defeat.";
                return false;
            }
            if (type == RunSettlementType.Rebirth && stage < MinimumRebirthStage)
            {
                reason = "Rebirth unlocks at Stage 50.";
                return false;
            }
            if (type == RunSettlementType.Rebirth &&
                !string.Equals(player.activeRun.phase, "EnemyReady", StringComparison.Ordinal) &&
                !string.Equals(player.activeRun.phase, "RunComplete", StringComparison.Ordinal))
            {
                reason = "Rebirth is only available between questions.";
                return false;
            }
            return true;
        }

        public static RunSettlementAward Calculate(PlayerSnapshot player, RunSettlementType type)
        {
            if (player?.progression == null || player.activeRun == null)
                throw new InvalidOperationException("Player run data is unavailable.");

            int stage = Math.Min(200, Math.Max(1,
                Math.Max(player.progression.currentStage, player.activeRun.currentStage)));
            long weightedTenths = checked(
                player.activeRun.silverEarned * 5L +
                player.activeRun.goldEarned * 7L +
                player.activeRun.diamondEarned * 10L);
            long bonusBasisPoints = Math.Max(
                DefaultBonusBasisPoints,
                player.activeRun.bonusMultiplierBasisPoints);
            long numerator = checked(checked(weightedTenths * stage * stage) * bonusBasisPoints);
            long powerCoins = numerator / 4000000000L;
            return new RunSettlementAward(
                stage,
                powerCoins,
                checked(stage * 10L),
                type == RunSettlementType.Rebirth ? 1 : 0);
        }
    }
}
