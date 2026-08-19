using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public interface IPetGachaProbabilityCalculator
    {
        PetChance[] Calculate(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds);
    }

    public sealed class PetGachaProbabilityCalculator : IPetGachaProbabilityCalculator
    {
        public PetChance[] Calculate(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var owned = new HashSet<string>(
                ownedPetIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            var result = new List<PetChance>();

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

                foreach (PetGachaPet pet in rarity.Pets)
                {
                    bool isOwned = owned.Contains(pet.Id);
                    result.Add(new PetChance(
                        pet.Id,
                        rarity.Id,
                        rarity.RateBasisPoints,
                        isOwned ? ownedWeight : unownedWeight,
                        denominator,
                        isOwned));
                }
            }

            return result.ToArray();
        }
    }
}
