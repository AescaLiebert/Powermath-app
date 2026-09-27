using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public interface IPetGachaProbabilityCalculator
    {
        PetChance[] Calculate(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds);

        PetChance[] Calculate(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            int pullsSinceSsr);
    }

    public sealed class PetGachaProbabilityCalculator : IPetGachaProbabilityCalculator
    {
        public PetChance[] Calculate(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds)
        {
            return Calculate(catalog, ownedPetIds, 0);
        }

        public PetChance[] Calculate(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            int pullsSinceSsr)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var owned = new HashSet<string>(
                ownedPetIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            var result = new List<PetChance>();

            var effectiveRates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (pullsSinceSsr >= PetGachaTransactionPolicy.SsrSoftPityThreshold)
            {
                PetGachaRarity ssrRarity = catalog.GetSsrPityRarity();
                int effectiveSsrRate = PetGachaTransactionPolicy.GetEffectiveSsrRateBasisPoints(
                    ssrRarity.RateBasisPoints, pullsSinceSsr);
                int extraRate = effectiveSsrRate - ssrRarity.RateBasisPoints;

                int remainingDeduction = extraRate;
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
            }
            else
            {
                foreach (PetGachaRarity rarity in catalog.Rarities)
                {
                    effectiveRates[rarity.Id] = rarity.RateBasisPoints;
                }
            }

            foreach (PetGachaRarity rarity in catalog.Rarities)
            {
                int total = rarity.Pets.Count;
                int ownedCount = 0;
                foreach (PetGachaPet pet in rarity.Pets)
                    if (owned.Contains(pet.Id)) ownedCount++;
                int unownedCount = total - ownedCount;

                bool equal = ownedCount == 0 || unownedCount == 0;
                long denominator = equal
                    ? total
                    : checked(2L * total * unownedCount);
                long ownedWeight = equal ? 1L : unownedCount;
                long unownedWeight = equal ? 1L : checked(2L * total - ownedCount);

                int rateBasisPoints = effectiveRates[rarity.Id];

                foreach (PetGachaPet pet in rarity.Pets)
                {
                    bool isOwned = owned.Contains(pet.Id);
                    result.Add(new PetChance(
                        pet.Id,
                        rarity.Id,
                        rateBasisPoints,
                        isOwned ? ownedWeight : unownedWeight,
                        denominator,
                        isOwned));
                }
            }

            return result.ToArray();
        }
    }
}
