using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public readonly struct PetOwnershipRecord
    {
        public PetOwnershipRecord(string petId, bool owned, int upgradeLevel)
            : this(petId, owned, upgradeLevel, owned ? 1 : 0)
        {
        }

        public PetOwnershipRecord(string petId, bool owned, int upgradeLevel, int count)
        {
            PetId = petId ?? string.Empty;
            Owned = owned;
            UpgradeLevel = upgradeLevel;
            Count = count;
        }

        public string PetId { get; }
        public bool Owned { get; }
        public int UpgradeLevel { get; }
        public int Count { get; }
    }

    public static class PetEquipPolicy
    {
        public static void Validate(
            string equippedPetId,
            PetGachaCatalog catalog,
            IEnumerable<PetOwnershipRecord> ownership)
        {
            if (string.IsNullOrWhiteSpace(equippedPetId)) return;
            if (catalog == null)
                throw new InvalidOperationException("Pet catalog is unavailable.");

            string trimmedEquipped = equippedPetId.Trim();
            if (!catalog.TryGetPet(trimmedEquipped, out PetGachaPet equippedPet))
                throw new InvalidOperationException("Equipped pet is missing from the active pet catalog.");

            int ownedCount = 0;
            foreach (PetOwnershipRecord record in ownership ?? Array.Empty<PetOwnershipRecord>())
            {
                if (!record.Owned || record.Count <= 0) continue;
                if (!catalog.TryGetPet(record.PetId, out PetGachaPet ownedPet)) continue;
                if (string.Equals(ownedPet.Id, equippedPet.Id, StringComparison.OrdinalIgnoreCase))
                    ownedCount = checked(ownedCount + record.Count);
            }

            if (ownedCount <= 0)
                throw new InvalidOperationException("Equipped pet is not owned in the active pet collection.");
        }
    }

    public static class EquippedPetAttackPolicy
    {
        public static int Resolve(
            string equippedPetId,
            PetGachaCatalog catalog,
            IEnumerable<PetOwnershipRecord> ownership,
            out bool hasConfiguredPetStats)
        {
            return Resolve(
                equippedPetId,
                catalog,
                ownership,
                out hasConfiguredPetStats,
                out _);
        }

        public static int Resolve(
            string equippedPetId,
            PetGachaCatalog catalog,
            IEnumerable<PetOwnershipRecord> ownership,
            out bool hasConfiguredPetStats,
            out double petMultiplierPercent)
        {
            hasConfiguredPetStats = false;
            petMultiplierPercent = 0d;
            if (catalog == null) return 0;

            var ownershipList = new List<PetOwnershipRecord>();
            foreach (PetOwnershipRecord record in ownership ?? Array.Empty<PetOwnershipRecord>())
            {
                if (string.IsNullOrWhiteSpace(record.PetId)) continue;
                ownershipList.Add(record);
            }
            PetEquipPolicy.Validate(equippedPetId, catalog, ownershipList);

            PetCollectionStats stats = PetCollectionPolicy.Calculate(catalog, ownershipList);
            if (stats.TotalOwnedPetsCount > 0)
            {
                hasConfiguredPetStats = true;
                petMultiplierPercent = stats.TotalPlayerAttackMultiplierPercent;
                return stats.TotalPlayerFlatAttack;
            }

            return 0;
        }
    }
}
