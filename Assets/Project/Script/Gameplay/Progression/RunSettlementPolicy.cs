using System;
using System.Collections.Generic;
using PowerMath.Gameplay.Combat;
using PowerMath.PlayerData;

namespace PowerMath.Gameplay.Progression
{
    public enum RunSettlementType { Death, Rebirth }

    public readonly struct RunSettlementAward
    {
        public RunSettlementAward(
            int stage,
            long powerCoins,
            long legacyBasisPoints,
            int prestige,
            double powerCoinBonusPercent = 0d,
            bool wasTeleported = false)
        {
            StageReached = stage;
            PowerCoins = powerCoins;
            LegacyBasisPoints = legacyBasisPoints;
            Prestige = prestige;
            PowerCoinBonusPercent = Math.Max(0d, powerCoinBonusPercent);
            WasTeleported = wasTeleported;
        }

        public int StageReached { get; }
        public long PowerCoins { get; }
        public long LegacyBasisPoints { get; }
        public int Prestige { get; }
        public double PowerCoinBonusPercent { get; }
        public bool WasTeleported { get; }
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
        public const int MinimumRebirthStage = 30;
        public const int DefaultBonusBasisPoints = 10000;
        public const double TeleportPenaltyMultiplier = 0.10d;

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
            int stage = Math.Min(StageId.Final, Math.Max(1,
                Math.Max(player.progression.currentStage, player.activeRun.currentStage)));
            if (type == RunSettlementType.Death &&
                !string.Equals(player.activeRun.phase, "RunDefeat", StringComparison.Ordinal))
            {
                reason = "This run has not ended in defeat.";
                return false;
            }
            if (type == RunSettlementType.Rebirth && stage < MinimumRebirthStage)
            {
                reason = "Rebirth unlocks at Stage 30.";
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
            return Calculate(player, type, null);
        }

        public static RunSettlementAward Calculate(PlayerSnapshot player, RunSettlementType type, PowerMath.Gameplay.Pets.PetGachaCatalog petCatalog)
        {
            if (player?.progression == null || player.activeRun == null)
                throw new InvalidOperationException("Player run data is unavailable.");

            int stage = Math.Min(StageId.Final, Math.Max(1,
                Math.Max(player.progression.currentStage, player.activeRun.currentStage)));
            long weightedTenths = checked(
                player.activeRun.silverEarned * 10L +
                player.activeRun.goldEarned * 12L +
                player.activeRun.diamondEarned * 15L);
            long bonusBasisPoints = Math.Max(
                DefaultBonusBasisPoints,
                player.activeRun.bonusMultiplierBasisPoints);
            long depthBonusBasisPoints = 10000L;
            if (stage > 100)
            {
                depthBonusBasisPoints += (long)(stage - 100) * 150L;
            }
            long combinedMultiplier = checked((bonusBasisPoints * depthBonusBasisPoints) / 10000L);
            double petPowerCoinMultiplier = 1d;
            double passiveCoins = 0d;

            if (petCatalog != null && player.inventory != null)
            {
                var records = new List<PowerMath.Gameplay.Pets.PetOwnershipRecord>();
                foreach (var item in player.inventory)
                {
                    if (item == null) continue;
                    records.Add(new PowerMath.Gameplay.Pets.PetOwnershipRecord(
                        item.itemId,
                        item.owned,
                        item.upgradeLevel,
                        item.count > 0 ? item.count : (item.owned ? 1 : 0)));
                }

                PowerMath.Gameplay.Pets.PetCollectionStats petStats =
                    PowerMath.Gameplay.Pets.PetCollectionPolicy.Calculate(petCatalog, records);
                petPowerCoinMultiplier += petStats.TotalPowerCoinBonusPercent / 100d;

                if (type == RunSettlementType.Rebirth)
                {
                    passiveCoins = petStats.ActivePassives.SumMagnitude(
                        PowerMath.Gameplay.Pets.PetPassiveEffectType.GrantPowerCoinsOnRebirth);
                    if (passiveCoins > long.MaxValue)
                        throw new OverflowException("Pet rebirth reward exceeds the supported range.");
                }
            }

            long numerator = checked(checked(checked(weightedTenths * stage) * stage) * combinedMultiplier);
            long weightedCoins = numerator / 70000000L;
            long flatStageCoins = checked(stage * 1L);
            long powerCoins = checked(flatStageCoins + weightedCoins);
            if (petPowerCoinMultiplier > 1d)
            {
                powerCoins = checked((long)Math.Round(
                    powerCoins * petPowerCoinMultiplier,
                    MidpointRounding.AwayFromZero));
            }
            powerCoins = checked(powerCoins + (long)Math.Round(
                passiveCoins,
                MidpointRounding.AwayFromZero));

            double totalPercentMultiplier =
                combinedMultiplier / 10000d * petPowerCoinMultiplier;

            bool wasTeleported = player.activeRun != null && player.activeRun.wasTeleported;
            long legacyBasisPoints = checked(stage * 50L);
            if (wasTeleported)
            {
                legacyBasisPoints = checked((long)Math.Round(
                    legacyBasisPoints * TeleportPenaltyMultiplier,
                    MidpointRounding.AwayFromZero));
                powerCoins = checked((long)Math.Round(
                    powerCoins * TeleportPenaltyMultiplier,
                    MidpointRounding.AwayFromZero));
            }

            return new RunSettlementAward(
                stage,
                powerCoins,
                legacyBasisPoints,
                type == RunSettlementType.Rebirth ? 1 : 0,
                Math.Round(
                    Math.Max(0d, (totalPercentMultiplier - 1d) * 100d),
                    6,
                    MidpointRounding.AwayFromZero),
                wasTeleported);
        }
    }
}
