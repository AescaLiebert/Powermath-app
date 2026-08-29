using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public readonly struct PetOwnershipRecord
    {
        public PetOwnershipRecord(string petId, bool owned, int upgradeLevel)
        {
            PetId = petId ?? string.Empty;
            Owned = owned;
            UpgradeLevel = upgradeLevel;
        }

        public string PetId { get; }
        public bool Owned { get; }
        public int UpgradeLevel { get; }
    }

    public static class EquippedPetAttackPolicy
    {
        public static int Resolve(
            string equippedPetId,
            PetGachaCatalog catalog,
            IEnumerable<PetOwnershipRecord> ownership,
            out bool hasConfiguredPetStats)
        {
            hasConfiguredPetStats = false;
            if (string.IsNullOrWhiteSpace(equippedPetId)) return 0;
            if (catalog == null ||
                !catalog.TryGetPet(equippedPetId, out PetGachaPet pet))
            {
                throw new InvalidOperationException(
                    "Equipped pet is missing from the active pet catalog.");
            }

            int matches = 0;
            PetOwnershipRecord match = default;
            foreach (PetOwnershipRecord record in
                ownership ?? Array.Empty<PetOwnershipRecord>())
            {
                if (!string.Equals(
                        record.PetId,
                        equippedPetId,
                        StringComparison.Ordinal)) continue;
                matches++;
                match = record;
            }
            if (matches != 1 || !match.Owned || match.UpgradeLevel != 0)
            {
                throw new InvalidOperationException(
                    "Equipped pet ownership data is invalid.");
            }

            hasConfiguredPetStats = true;
            return pet.AttackBonus;
        }
    }
}
