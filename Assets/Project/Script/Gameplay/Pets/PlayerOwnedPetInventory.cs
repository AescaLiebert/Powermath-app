using System;
using System.Collections.Generic;
using PowerMath.PlayerData;
using UnityEngine;

namespace PowerMath.Gameplay.Pets
{
    public readonly struct OwnedPetEntry
    {
        public OwnedPetEntry(
            PetDefinition definition,
            string rarityId,
            string rarityName,
            Color rarityColor,
            bool isEquipped,
            int count = 1)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            RarityId = rarityId ?? string.Empty;
            RarityName = rarityName ?? string.Empty;
            RarityColor = rarityColor;
            IsEquipped = isEquipped;
            Count = count > 0 ? count : 1;
        }

        public PetDefinition Definition { get; }
        public string RarityId { get; }
        public string RarityName { get; }
        public Color RarityColor { get; }
        public bool IsEquipped { get; }
        public int Count { get; }
    }

    public sealed class PlayerOwnedPetInventory
    {
        private readonly OwnedPetEntry[] _entries;
        private readonly Dictionary<string, OwnedPetEntry> _entriesById;

        private PlayerOwnedPetInventory(
            OwnedPetEntry[] entries,
            Dictionary<string, OwnedPetEntry> entriesById,
            string equippedPetId)
        {
            _entries = entries;
            _entriesById = entriesById;
            EquippedPetId = equippedPetId;
        }

        public IReadOnlyList<OwnedPetEntry> Entries => _entries;
        public string EquippedPetId { get; }

        public bool TryGetOwned(string petId, out OwnedPetEntry entry) =>
            _entriesById.TryGetValue(petId ?? string.Empty, out entry);

        public static bool TryCreate(
            PlayerSnapshot player,
            PetGachaCatalogDefinition catalog,
            out PlayerOwnedPetInventory inventory,
            out string error)
        {
            inventory = null;
            error = string.Empty;
            if (player == null)
            {
                error = "Player data is unavailable.";
                return false;
            }
            if (catalog == null)
            {
                error = "Pet content is unavailable.";
                return false;
            }
            if (!catalog.TryBuildCatalog(out _, out string catalogError))
            {
                error = string.IsNullOrEmpty(catalogError)
                    ? "Pet content is unavailable."
                    : catalogError;
                return false;
            }

            var countById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (PlayerSnapshot.InventoryItemData item in
                player.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
            {
                if (item == null || !item.owned || !catalog.TryResolvePet(item.itemId, out PetDefinition petDef, out _))
                    continue;

                if (item.count < 0)
                {
                    error = "Saved pet copy count is invalid and requires data repair.";
                    return false;
                }
                // Zero is the pre-count schema's representation of one owned copy.
                int itemCount = item.count == 0 ? 1 : item.count;
                string key = petDef.PetId;
                if (countById.TryGetValue(key, out int existing))
                    countById[key] = checked(existing + itemCount);
                else
                    countById[key] = itemCount;
            }

            string equippedId = player.loadout?.petId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(equippedId) && !countById.ContainsKey(equippedId))
            {
                error = "The equipped pet is not owned or no longer exists in the catalog.";
                return false;
            }

            var entries = new List<OwnedPetEntry>(countById.Count);
            var byId = new Dictionary<string, OwnedPetEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (PetGachaCatalogDefinition.RarityContent rarity in catalog.Rarities)
            {
                if (rarity == null) continue;
                foreach (PetDefinition definition in rarity.pets ?? Array.Empty<PetDefinition>())
                {
                    if (definition == null || !countById.TryGetValue(definition.PetId, out int stackCount))
                        continue;
                    if (byId.ContainsKey(definition.PetId))
                        continue;

                    var entry = new OwnedPetEntry(
                        definition,
                        rarity.rarityId,
                        rarity.displayName,
                        rarity.displayColor,
                        string.Equals(equippedId, definition.PetId, StringComparison.OrdinalIgnoreCase),
                        stackCount);
                    entries.Add(entry);
                    byId.Add(definition.PetId, entry);
                }
            }

            inventory = new PlayerOwnedPetInventory(entries.ToArray(), byId, equippedId);
            return true;
        }
    }
}
