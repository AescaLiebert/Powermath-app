using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public sealed class PetGachaPet
    {
        public PetGachaPet(string id, string displayName)
            : this(id, displayName, 0)
        {
        }

        public PetGachaPet(string id, string displayName, int attackBonus)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Pet ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Pet display name is required.", nameof(displayName));
            if (attackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(attackBonus));
            Id = id.Trim();
            DisplayName = displayName.Trim();
            AttackBonus = attackBonus;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int AttackBonus { get; }
    }

    public sealed class PetGachaRarity
    {
        private readonly PetGachaPet[] _pets;

        public PetGachaRarity(
            string id,
            string displayName,
            int rateBasisPoints,
            IReadOnlyList<PetGachaPet> pets)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Rarity ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Rarity display name is required.", nameof(displayName));
            if (rateBasisPoints <= 0 || rateBasisPoints > PetGachaCatalog.TotalRateBasisPoints)
                throw new ArgumentOutOfRangeException(nameof(rateBasisPoints));
            if (pets == null || pets.Count == 0)
                throw new ArgumentException("Every rarity needs at least one pet.", nameof(pets));

            Id = id.Trim();
            DisplayName = displayName.Trim();
            RateBasisPoints = rateBasisPoints;
            _pets = new PetGachaPet[pets.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < pets.Count; index++)
            {
                PetGachaPet pet = pets[index] ??
                    throw new ArgumentException("Rarity pets cannot be null.", nameof(pets));
                if (!ids.Add(pet.Id))
                    throw new ArgumentException("Pet IDs must be unique within a rarity.", nameof(pets));
                _pets[index] = pet;
            }
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int RateBasisPoints { get; }
        public IReadOnlyList<PetGachaPet> Pets => _pets;
    }

    public sealed class PetGachaCatalog
    {
        public const int TotalRateBasisPoints = 10000;

        private readonly PetGachaRarity[] _rarities;
        private readonly Dictionary<string, PetGachaPet> _petsById;

        public PetGachaCatalog(string version, IReadOnlyList<PetGachaRarity> rarities)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Catalog version is required.", nameof(version));
            if (rarities == null || rarities.Count == 0)
                throw new ArgumentException("At least one rarity is required.", nameof(rarities));

            Version = version.Trim();
            _rarities = new PetGachaRarity[rarities.Count];
            _petsById = new Dictionary<string, PetGachaPet>(StringComparer.Ordinal);
            var rarityIds = new HashSet<string>(StringComparer.Ordinal);
            int totalRate = 0;
            for (int rarityIndex = 0; rarityIndex < rarities.Count; rarityIndex++)
            {
                PetGachaRarity rarity = rarities[rarityIndex] ??
                    throw new ArgumentException("Catalog rarities cannot be null.", nameof(rarities));
                if (!rarityIds.Add(rarity.Id))
                    throw new ArgumentException("Rarity IDs must be globally unique.", nameof(rarities));
                totalRate = checked(totalRate + rarity.RateBasisPoints);
                _rarities[rarityIndex] = rarity;
                foreach (PetGachaPet pet in rarity.Pets)
                {
                    if (!_petsById.TryAdd(pet.Id, pet))
                        throw new ArgumentException("Pet IDs must be globally unique.", nameof(rarities));
                }
            }
            if (totalRate != TotalRateBasisPoints)
                throw new ArgumentException("Rarity rates must total exactly 10,000 basis points.", nameof(rarities));
        }

        public string Version { get; }
        public IReadOnlyList<PetGachaRarity> Rarities => _rarities;

        public bool ContainsPet(string petId) =>
            !string.IsNullOrEmpty(petId) && _petsById.ContainsKey(petId);

        public bool TryGetPet(string petId, out PetGachaPet pet) =>
            _petsById.TryGetValue(petId ?? string.Empty, out pet);
    }

    public readonly struct PetChance
    {
        public PetChance(
            string petId,
            string rarityId,
            int categoryRateBasisPoints,
            long weightNumerator,
            long weightDenominator,
            bool isOwned)
        {
            if (string.IsNullOrWhiteSpace(petId))
                throw new ArgumentException("Pet ID is required.", nameof(petId));
            if (string.IsNullOrWhiteSpace(rarityId))
                throw new ArgumentException("Rarity ID is required.", nameof(rarityId));
            if (categoryRateBasisPoints <= 0 ||
                categoryRateBasisPoints > PetGachaCatalog.TotalRateBasisPoints)
                throw new ArgumentOutOfRangeException(nameof(categoryRateBasisPoints));
            if (weightNumerator <= 0) throw new ArgumentOutOfRangeException(nameof(weightNumerator));
            if (weightDenominator <= 0 || weightNumerator > weightDenominator)
                throw new ArgumentOutOfRangeException(nameof(weightDenominator));

            PetId = petId;
            RarityId = rarityId;
            CategoryRateBasisPoints = categoryRateBasisPoints;
            WeightNumerator = weightNumerator;
            WeightDenominator = weightDenominator;
            IsOwned = isOwned;
        }

        public string PetId { get; }
        public string RarityId { get; }
        public int CategoryRateBasisPoints { get; }
        public long WeightNumerator { get; }
        public long WeightDenominator { get; }
        public bool IsOwned { get; }

        public decimal GetPercent()
        {
            return CategoryRateBasisPoints / 100m *
                WeightNumerator / WeightDenominator;
        }
    }

    public readonly struct PetGachaResult
    {
        public PetGachaResult(string petId, string rarityId, bool wasNew)
        {
            PetId = petId ?? throw new ArgumentNullException(nameof(petId));
            RarityId = rarityId ?? throw new ArgumentNullException(nameof(rarityId));
            WasNew = wasNew;
        }

        public string PetId { get; }
        public string RarityId { get; }
        public bool WasNew { get; }
    }

    public readonly struct PetGachaCommand
    {
        public PetGachaCommand(string transactionId, string catalogVersion, long previewRevision)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
            if (string.IsNullOrWhiteSpace(catalogVersion))
                throw new ArgumentException("Catalog version is required.", nameof(catalogVersion));
            if (previewRevision < 0) throw new ArgumentOutOfRangeException(nameof(previewRevision));
            TransactionId = transactionId;
            CatalogVersion = catalogVersion;
            PreviewRevision = previewRevision;
        }

        public string TransactionId { get; }
        public string CatalogVersion { get; }
        public long PreviewRevision { get; }
    }

    public readonly struct PetGachaReceipt
    {
        public PetGachaReceipt(
            string transactionId,
            string catalogVersion,
            string petId,
            bool wasNew,
            long cost,
            long resultingPowerCoins)
        {
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            CatalogVersion = catalogVersion ?? throw new ArgumentNullException(nameof(catalogVersion));
            PetId = petId ?? throw new ArgumentNullException(nameof(petId));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (resultingPowerCoins < 0) throw new ArgumentOutOfRangeException(nameof(resultingPowerCoins));
            WasNew = wasNew;
            Cost = cost;
            ResultingPowerCoins = resultingPowerCoins;
        }

        public string TransactionId { get; }
        public string CatalogVersion { get; }
        public string PetId { get; }
        public bool WasNew { get; }
        public long Cost { get; }
        public long ResultingPowerCoins { get; }
    }

    public enum PetGachaFailureCode
    {
        InsufficientFunds,
        StalePreview,
        UnavailableDuringAttempt,
        InvalidCatalog,
        RecoverableTransport,
        InvalidSavedState
    }

    public readonly struct PetGachaFailure
    {
        public PetGachaFailure(PetGachaFailureCode code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
        }

        public PetGachaFailureCode Code { get; }
        public string Message { get; }
    }

    public interface IPetGachaCommandStore
    {
        IEnumerator Pull(
            PetGachaCommand command,
            Action<PetGachaReceipt> completed,
            Action<PetGachaFailure> failed);
    }
}
