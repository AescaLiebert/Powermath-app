using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public readonly struct PetCollectionStats
    {
        public PetCollectionStats(
            int totalPlayerFlatAttack,
            double totalPlayerAttackMultiplierPercent,
            int totalPetFlatAttack,
            double totalPetAttackMultiplierPercent,
            int effectivePetAttack,
            double totalCritRatePercent,
            double totalCritDamagePercent,
            double totalEncounterLuckPercent,
            int totalEncounterLuckBasisPoints,
            double totalPowerCoinBonusPercent,
            int totalPlayerHeartBonus,
            ActivePetPassiveSet activePassives,
            int totalOwnedPetsCount)
        {
            TotalPlayerFlatAttack = totalPlayerFlatAttack;
            TotalPlayerAttackMultiplierPercent = totalPlayerAttackMultiplierPercent;
            TotalPetFlatAttack = totalPetFlatAttack;
            TotalPetAttackMultiplierPercent = totalPetAttackMultiplierPercent;
            EffectivePetAttack = effectivePetAttack;
            TotalCritRatePercent = totalCritRatePercent;
            TotalCritDamagePercent = totalCritDamagePercent;
            TotalEncounterLuckPercent = totalEncounterLuckPercent;
            TotalEncounterLuckBasisPoints = totalEncounterLuckBasisPoints;
            TotalPowerCoinBonusPercent = totalPowerCoinBonusPercent;
            TotalPlayerHeartBonus = totalPlayerHeartBonus;
            ActivePassives = activePassives ?? ActivePetPassiveSet.Empty;
            TotalOwnedPetsCount = totalOwnedPetsCount;
        }

        public int TotalPlayerFlatAttack { get; }
        public double TotalPlayerAttackMultiplierPercent { get; }
        public int TotalPetFlatAttack { get; }
        public double TotalPetAttackMultiplierPercent { get; }
        public int EffectivePetAttack { get; }
        public double TotalCritRatePercent { get; }
        public double TotalCritDamagePercent { get; }
        public double TotalEncounterLuckPercent { get; }
        public int TotalEncounterLuckBasisPoints { get; }
        public double TotalPowerCoinBonusPercent { get; }
        public int TotalPlayerHeartBonus { get; }

        public ActivePetPassiveSet ActivePassives { get; }
        public int TotalOwnedPetsCount { get; }

        public static PetCollectionStats Empty => new PetCollectionStats(
            0, 0d, 0, 0d, 0, 0d, 0d, 0d, 0, 0d, 0, ActivePetPassiveSet.Empty, 0);
    }

    public static class PetCollectionPolicy
    {
        public static PetCollectionStats Calculate(
            PetGachaCatalog catalog,
            IEnumerable<PetOwnershipRecord> ownership)
        {
            if (catalog == null) return PetCollectionStats.Empty;

            var aggregatedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (PetOwnershipRecord record in ownership ?? Array.Empty<PetOwnershipRecord>())
            {
                if (!record.Owned) continue;
                if (record.Count <= 0)
                    throw new InvalidOperationException("Owned pet copy counts must be positive.");
                if (!catalog.TryGetPet(record.PetId, out PetGachaPet pet)) continue;
                if (aggregatedCounts.TryGetValue(pet.Id, out int current))
                    aggregatedCounts[pet.Id] = checked(current + record.Count);
                else
                    aggregatedCounts[pet.Id] = record.Count;
            }

            var pairs = new List<KeyValuePair<PetGachaPet, int>>();
            foreach (var kvp in aggregatedCounts)
            {
                if (catalog.TryGetPet(kvp.Key, out PetGachaPet pet))
                {
                    pairs.Add(new KeyValuePair<PetGachaPet, int>(pet, kvp.Value));
                }
            }
            return Calculate(pairs);
        }

        public static PetCollectionStats Calculate(
            IEnumerable<KeyValuePair<PetGachaPet, int>> collection)
        {
            if (collection == null) return PetCollectionStats.Empty;

            var aggregatedCollection = new Dictionary<string, KeyValuePair<PetGachaPet, int>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<PetGachaPet, int> entry in collection)
            {
                PetGachaPet pet = entry.Key;
                if (pet == null) continue;
                if (entry.Value <= 0)
                    throw new InvalidOperationException("Owned pet copy counts must be positive.");
                if (aggregatedCollection.TryGetValue(pet.Id, out KeyValuePair<PetGachaPet, int> existing))
                {
                    aggregatedCollection[pet.Id] = new KeyValuePair<PetGachaPet, int>(
                        existing.Key,
                        checked(existing.Value + entry.Value));
                }
                else
                {
                    aggregatedCollection.Add(pet.Id, entry);
                }
            }

            int totalPlayerFlatAttack = 0;
            double totalPlayerAttackMultiplierPercent = 0d;
            int totalPetFlatAttack = 0;
            double totalPetAttackMultiplierPercent = 0d;
            double totalCritRatePercent = 0d;
            double totalCritDamagePercent = 0d;
            double totalEncounterLuckPercent = 0d;
            double totalPowerCoinBonusPercent = 0d;
            int totalPlayerHeartBonus = 0;

            var activePassives = new List<ActivePetPassive>();
            int totalOwnedPetsCount = 0;

            foreach (KeyValuePair<string, KeyValuePair<PetGachaPet, int>> aggregate in aggregatedCollection)
            {
                PetGachaPet pet = aggregate.Value.Key;
                int count = aggregate.Value.Value;

                totalOwnedPetsCount = checked(totalOwnedPetsCount + count);

                totalPlayerFlatAttack = checked(totalPlayerFlatAttack + checked(pet.PlayerAttackBonus * count));
                totalPlayerAttackMultiplierPercent += pet.PlayerAttackMultiplierPercent * count;
                totalPetFlatAttack = checked(totalPetFlatAttack + checked(pet.PetAttackBonus * count));
                totalPetAttackMultiplierPercent += pet.PetAttackMultiplierPercent * count;
                totalCritRatePercent += pet.CritRatePercent * count;
                totalCritDamagePercent += pet.CritDamagePercent * count;
                totalEncounterLuckPercent += pet.EncounterLuckPercent * count;
                totalPowerCoinBonusPercent += pet.PowerCoinBonusPercent * count;
                totalPlayerHeartBonus = checked(totalPlayerHeartBonus + checked(pet.PlayerHeartUnit * count));

                if (pet.Passive != null)
                {
                    int passiveStacks = pet.Passive.ResolveStackCount(count);
                    if (passiveStacks > 0)
                        activePassives.Add(new ActivePetPassive(pet.Passive, passiveStacks));
                }
            }

            int effectivePetAttack = totalPetFlatAttack > 0
                ? Math.Max(1, (int)Math.Round(
                    totalPetFlatAttack * (1d + totalPetAttackMultiplierPercent / 100d),
                    MidpointRounding.AwayFromZero))
                : 0;

            int encounterLuckBasisPoints = (int)Math.Round(
                totalEncounterLuckPercent * 100d,
                MidpointRounding.AwayFromZero);

            return new PetCollectionStats(
                totalPlayerFlatAttack,
                totalPlayerAttackMultiplierPercent,
                totalPetFlatAttack,
                totalPetAttackMultiplierPercent,
                effectivePetAttack,
                totalCritRatePercent,
                totalCritDamagePercent,
                totalEncounterLuckPercent,
                encounterLuckBasisPoints,
                totalPowerCoinBonusPercent,
                totalPlayerHeartBonus,
                new ActivePetPassiveSet(activePassives),
                totalOwnedPetsCount);
        }
    }
}
