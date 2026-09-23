using System;
using PowerMath.PlayerData;

namespace PowerMath.Gameplay.Progression
{
    /// <summary>
    /// Durable unlock policy for the optional Hub and Pet Gacha tools.
    /// Rebirth is always available and is intentionally not governed here.
    /// </summary>
    public static class PlayerMenuUnlockPolicy
    {
        // Stage 31 means the player has beaten Stage 30.
        public const int RequiredHighestStage = 31;

        public static bool IsHubAndGachaUnlocked(PlayerSnapshot player) =>
            GetHighestReachedStage(player) >= RequiredHighestStage ||
            HasCompletedSettlement(player);

        public static bool HasCompletedSettlement(PlayerSnapshot player)
        {
            PlayerSnapshot.RunSettlementData settlement = player?.lastRunSettlement;
            return settlement != null &&
                !string.IsNullOrWhiteSpace(settlement.runId) &&
                (string.Equals(settlement.type, RunSettlementType.Death.ToString(),
                     StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(settlement.type, RunSettlementType.Rebirth.ToString(),
                     StringComparison.OrdinalIgnoreCase));
        }

        public static int GetHighestReachedStage(PlayerSnapshot player)
        {
            return Math.Max(1, Math.Max(
                player?.progression?.highestStage ?? 1,
                Math.Max(
                    player?.progression?.currentStage ?? 1,
                    player?.activeRun?.currentStage ?? 1)));
        }
    }
}
