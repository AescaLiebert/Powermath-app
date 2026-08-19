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
    }

    public sealed class PetGachaRoller : IPetGachaRoller
    {
        public PetGachaResult Roll(
            PetGachaCatalog catalog,
            IReadOnlyCollection<string> ownedPetIds,
            IPetGachaRandomSource random)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (random == null) throw new ArgumentNullException(nameof(random));
            var owned = new HashSet<string>(
                ownedPetIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);

            int categoryRoll = random.NextExclusive(PetGachaCatalog.TotalRateBasisPoints);
            PetGachaRarity selectedRarity = null;
            int boundary = 0;
            foreach (PetGachaRarity rarity in catalog.Rarities)
            {
                boundary = checked(boundary + rarity.RateBasisPoints);
                if (categoryRoll < boundary)
                {
                    selectedRarity = rarity;
                    break;
                }
            }
            if (selectedRarity == null)
                throw new InvalidOperationException("The rarity roll did not resolve against the catalog total.");

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
        public const long PullCost = 25;

        public static bool TryRecover(
            PetGachaCommand command,
            PetGachaReceipt lastReceipt,
            out PetGachaReceipt receipt)
        {
            if (!string.IsNullOrEmpty(lastReceipt.TransactionId) &&
                string.Equals(
                    lastReceipt.TransactionId,
                    command.TransactionId,
                    StringComparison.Ordinal))
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
            IPetGachaRandomSource random)
        {
            if (currentPowerCoins < PullCost)
                throw new InvalidOperationException("Insufficient Power Coins.");
            if (!string.Equals(command.CatalogVersion, catalog?.Version, StringComparison.Ordinal))
                throw new InvalidOperationException("The gacha catalog version changed.");
            PetGachaResult result = new PetGachaRoller().Roll(catalog, ownedPetIds, random);
            return new PetGachaReceipt(
                command.TransactionId,
                command.CatalogVersion,
                result.PetId,
                result.WasNew,
                PullCost,
                checked(currentPowerCoins - PullCost));
        }
    }
}
