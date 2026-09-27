using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public interface IPetGachaRandomSource
    {
        int NextExclusive(int maximumExclusive);
    }

    public interface IPetGachaRoller
    {
        PetGachaResult Roll(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            IPetGachaRandomSource random);

        PetGachaResult Roll(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            IPetGachaRandomSource random,
            int pullsSinceSsr);
    }

    public sealed class PetGachaRoller : IPetGachaRoller
    {
        public PetGachaResult Roll(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            IPetGachaRandomSource random)
        {
            return Roll(catalog, ownedPetIds, random, 0);
        }

        public PetGachaResult Roll(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            IPetGachaRandomSource random,
            int pullsSinceSsr)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var owned = new HashSet<string>(
                ownedPetIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            int categoryRoll = random.NextExclusive(PetGachaCatalog.TotalRateBasisPoints);
            PetGachaRarity selectedRarity = null;
            int boundary = 0;

            if (pullsSinceSsr >= PetGachaTransactionPolicy.SsrSoftPityThreshold)
            {
                PetGachaRarity ssrRarity = catalog.GetSsrPityRarity();
                int effectiveSsrRate = PetGachaTransactionPolicy.GetEffectiveSsrRateBasisPoints(
                    ssrRarity.RateBasisPoints, pullsSinceSsr);
                int extraRate = effectiveSsrRate - ssrRarity.RateBasisPoints;

                int remainingDeduction = extraRate;
                var effectiveRates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (PetGachaRarity rarity in catalog.Rarities)
                {
                    if (rarity.ResetsSsrPity)
                    {
                        effectiveRates[rarity.Id] = effectiveSsrRate;
                    }
                    else if (!rarity.CountsForTenPullGuarantee)
                    {
                        int deduction = Math.Min(rarity.RateBasisPoints, remainingDeduction);
                        effectiveRates[rarity.Id] = rarity.RateBasisPoints - deduction;
                        remainingDeduction -= deduction;
                    }
                    else
                    {
                        effectiveRates[rarity.Id] = rarity.RateBasisPoints;
                    }
                }

                if (remainingDeduction > 0)
                {
                    foreach (PetGachaRarity rarity in catalog.Rarities)
                    {
                        if (rarity.ResetsSsrPity) continue;
                        int current = effectiveRates[rarity.Id];
                        int deduction = Math.Min(current, remainingDeduction);
                        effectiveRates[rarity.Id] = current - deduction;
                        remainingDeduction -= deduction;
                        if (remainingDeduction <= 0) break;
                    }
                }

                foreach (PetGachaRarity rarity in catalog.Rarities)
                {
                    boundary = checked(boundary + effectiveRates[rarity.Id]);
                    if (categoryRoll < boundary)
                    {
                        selectedRarity = rarity;
                        break;
                    }
                }
            }
            else
            {
                foreach (PetGachaRarity rarity in catalog.Rarities)
                {
                    boundary = checked(boundary + rarity.RateBasisPoints);
                    if (categoryRoll < boundary)
                    {
                        selectedRarity = rarity;
                        break;
                    }
                }
            }

            if (selectedRarity == null)
                throw new InvalidOperationException("The rarity roll did not resolve against the catalog total.");

            return RollFromRarity(selectedRarity, owned, random);
        }

        public PetGachaResult RollFromRarity(
            PetGachaRarity selectedRarity,
            IReadOnlyCollection<string> ownedPetIds,
            IPetGachaRandomSource random)
        {
            if (selectedRarity == null) throw new ArgumentNullException(nameof(selectedRarity));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var owned = new HashSet<string>(
                ownedPetIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            int total = selectedRarity.Pets.Count;
            int ownedCount = 0;
            foreach (PetGachaPet pet in selectedRarity.Pets)
                if (owned.Contains(pet.Id)) ownedCount++;
            int unownedCount = total - ownedCount;
            bool equal = ownedCount == 0 || unownedCount == 0;
            int ownedWeight = equal ? 1 : unownedCount;
            int unownedWeight = equal ? 1 : checked(2 * total - ownedCount);
            int totalWeight = equal ? total : checked(2 * total * unownedCount);
            int petRoll = random.NextExclusive(totalWeight);
            int petBoundary = 0;
            foreach (PetGachaPet pet in selectedRarity.Pets)
            {
                petBoundary = checked(petBoundary +
                    (owned.Contains(pet.Id) ? ownedWeight : unownedWeight));
                if (petRoll < petBoundary)
                {
                    return new PetGachaResult(
                        pet.Id,
                        selectedRarity.Id,
                        !owned.Contains(pet.Id));
                }
            }

            throw new InvalidOperationException("The pet roll did not resolve against the rarity weights.");
        }
    }

    public static class PetGachaTransactionPolicy
    {
        public const long PullCost = 180;
        public const long SinglePullCost = 180;
        public const int MultiPullCount = 10;
        public const long MultiPullCost = PullCost * MultiPullCount;
        public const int SsrHardPityPulls = 90;
        public const int SsrSoftPityThreshold = 74;
        public const int SsrSoftPityStepBasisPoints = 600;
        public const int SrHardPityPulls = 10;
        public const string FirstPullGuaranteedPetId = "sapphire";

        public static int GetEffectiveSsrRateBasisPoints(int baseSsrRateBasisPoints, int pullsSinceSsr)
        {
            if (pullsSinceSsr < SsrSoftPityThreshold)
                return baseSsrRateBasisPoints;
            if (pullsSinceSsr >= SsrHardPityPulls - 1)
                return PetGachaCatalog.TotalRateBasisPoints;

            int steps = pullsSinceSsr - SsrSoftPityThreshold + 1;
            int calculated = checked(baseSsrRateBasisPoints + steps * SsrSoftPityStepBasisPoints);
            return Math.Min(PetGachaCatalog.TotalRateBasisPoints, calculated);
        }

        public static long GetCost(int pullCount)
        {
            if (pullCount != 1 && pullCount != MultiPullCount)
                throw new ArgumentOutOfRangeException(
                    nameof(pullCount),
                    "Pet gacha supports only 1x or 10x pulls.");
            return checked(PullCost * pullCount);
        }

        public static bool TryRecover(
            PetGachaCommand command,
            PetGachaReceipt lastReceipt,
            out PetGachaReceipt receipt)
        {
            if (!string.IsNullOrEmpty(lastReceipt.TransactionId) &&
                string.Equals(
                    lastReceipt.TransactionId,
                    command.TransactionId,
                    StringComparison.Ordinal) &&
                string.Equals(
                    lastReceipt.CatalogVersion,
                    command.CatalogVersion,
                    StringComparison.Ordinal) &&
                lastReceipt.Results.Count == command.PullCount &&
                lastReceipt.Cost == GetCost(command.PullCount))
            {
                receipt = lastReceipt;
                return true;
            }
            receipt = default;
            return false;
        }

        public static PetGachaReceipt CreateReceipt(
            PetGachaCommand command,
            long currentPowerCoins,
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            IPetGachaRandomSource random,
            bool isFirstPull = false,
            string firstPullPetId = FirstPullGuaranteedPetId)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string petId in ownedPetIds ?? Array.Empty<string>())
                counts[petId] = 1;
            return CreateReceipt(command, currentPowerCoins, catalog, counts, 0, 0, random, isFirstPull, firstPullPetId);
        }

        public static PetGachaReceipt CreateReceipt(
            PetGachaCommand command,
            long currentPowerCoins,
            PetGachaCatalog catalog,
            IReadOnlyDictionary<string, int> ownedPetCounts,
            int pullsSinceSsr,
            IPetGachaRandomSource random,
            bool isFirstPull = false,
            string firstPullPetId = FirstPullGuaranteedPetId)
        {
            return CreateReceipt(command, currentPowerCoins, catalog, ownedPetCounts, pullsSinceSsr, 0, random, isFirstPull, firstPullPetId);
        }

        public static PetGachaReceipt CreateReceipt(
            PetGachaCommand command,
            long currentPowerCoins,
            PetGachaCatalog catalog,
            IReadOnlyDictionary<string, int> ownedPetCounts,
            int pullsSinceSsr,
            int pullsSinceSr,
            IPetGachaRandomSource random,
            bool isFirstPull = false,
            string firstPullPetId = FirstPullGuaranteedPetId)
        {
            int pullCount = command.PullCount;
            long totalCost = GetCost(pullCount);

            if (currentPowerCoins < totalCost)
                throw new InvalidOperationException("Insufficient Power Coins.");
            if (!string.Equals(command.CatalogVersion, catalog?.Version, StringComparison.Ordinal))
                throw new InvalidOperationException("The gacha catalog version changed.");
            if (pullsSinceSsr < 0 || pullsSinceSsr >= SsrHardPityPulls)
                throw new InvalidOperationException("Saved SSR pity progress is invalid.");
            if (pullsSinceSr < 0 || pullsSinceSr >= SrHardPityPulls)
                throw new InvalidOperationException("Saved SR pity progress is invalid.");

            var roller = new PetGachaRoller();
            var rollingCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, int> pair in
                ownedPetCounts ?? new Dictionary<string, int>())
            {
                if (pair.Value <= 0) throw new InvalidOperationException("Owned pet counts must be positive.");
                rollingCounts[pair.Key] = pair.Value;
            }
            var results = new List<PetGachaResult>(pullCount);
            int pitySsr = pullsSinceSsr;
            int pitySr = pullsSinceSr;

            for (int i = 0; i < pullCount; i++)
            {
                PetGachaResult rolled;
                PetGachaRarity rarity;
                if (isFirstPull && i == 0 && !string.IsNullOrEmpty(firstPullPetId))
                {
                    if (!catalog.TryGetPet(firstPullPetId, out PetGachaPet guaranteedPet, out rarity))
                        throw new InvalidOperationException($"Guaranteed pet '{firstPullPetId}' is missing from the catalog.");

                    rolled = new PetGachaResult(
                        guaranteedPet.Id,
                        rarity.Id,
                        !rollingCounts.ContainsKey(guaranteedPet.Id));
                }
                else
                {
                    bool forceSsr = pitySsr == SsrHardPityPulls - 1;
                    bool forceSr = pitySr == SrHardPityPulls - 1;
                    PetGachaRarity forcedRarity = forceSsr
                        ? catalog.GetSsrPityRarity()
                        : forceSr ? catalog.GetTenPullGuaranteeRarity() : null;
                    rolled = forcedRarity == null
                        ? roller.Roll(catalog, rollingCounts.Keys, random, pitySsr)
                        : roller.RollFromRarity(forcedRarity, rollingCounts.Keys, random);
                    if (!catalog.TryGetRarity(rolled.RarityId, out rarity))
                        throw new InvalidOperationException("Rolled rarity is missing from the catalog.");
                }

                int previousCount = rollingCounts.TryGetValue(rolled.PetId, out int count)
                    ? count
                    : 0;
                int resultingCount = checked(previousCount + 1);
                rollingCounts[rolled.PetId] = resultingCount;
                results.Add(new PetGachaResult(
                    rolled.PetId,
                    rolled.RarityId,
                    previousCount == 0,
                    previousCount,
                    resultingCount));

                pitySsr = rarity.ResetsSsrPity ? 0 : checked(pitySsr + 1);
                pitySr = rarity.CountsForTenPullGuarantee ? 0 : checked(pitySr + 1);
            }

            return new PetGachaReceipt(
                command.TransactionId,
                command.CatalogVersion,
                results,
                totalCost,
                checked(currentPowerCoins - totalCost),
                pullsSinceSsr,
                pitySsr,
                pullsSinceSr,
                pitySr);
        }
    }
}
