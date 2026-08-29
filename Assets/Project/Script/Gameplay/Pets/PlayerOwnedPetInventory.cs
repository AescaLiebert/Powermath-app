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
            bool isEquipped)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            RarityId = rarityId ?? string.Empty;
            RarityName = rarityName ?? string.Empty;
            RarityColor = rarityColor;
            IsEquipped = isEquipped;
        }

        public PetDefinition Definition { get; }
        public string RarityId { get; }
        public string RarityName { get; }
        public Color RarityColor { get; }
        public bool IsEquipped { get; }
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

            var savedById = new Dictionary<string, PlayerSnapshot.InventoryItemData>(
                StringComparer.Ordinal);
            foreach (PlayerSnapshot.InventoryItemData item in
                player.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
            {
                if (item == null || !catalog.TryResolvePet(item.itemId, out _, out _))
                    continue;
                if (!item.owned || item.upgradeLevel != 0 ||
                    !savedById.TryAdd(item.itemId, item))
                {
                    error = "Saved pet ownership is inconsistent and requires data repair.";
                    return false;
                }
            }

            string equippedId = player.loadout?.petId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(equippedId) && !savedById.ContainsKey(equippedId))
            {
                error = "The equipped pet is not owned or no longer exists in the catalog.";
                return false;
            }

            var entries = new List<OwnedPetEntry>(savedById.Count);
            var byId = new Dictionary<string, OwnedPetEntry>(StringComparer.Ordinal);
            foreach (PetGachaCatalogDefinition.RarityContent rarity in catalog.Rarities)
            {
                if (rarity == null) continue;
                foreach (PetDefinition definition in rarity.pets ?? Array.Empty<PetDefinition>())
                {
                    if (definition == null || !savedById.ContainsKey(definition.PetId))
                        continue;
                    var entry = new OwnedPetEntry(
                        definition,
                        rarity.rarityId,
                        rarity.displayName,
                        rarity.displayColor,
                        string.Equals(equippedId, definition.PetId, StringComparison.Ordinal));
                    entries.Add(entry);
                    byId.Add(definition.PetId, entry);
                }
            }

            inventory = new PlayerOwnedPetInventory(entries.ToArray(), byId, equippedId);
            return true;
        }
    }
}
