using System;
using System.Collections;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public enum PetStatType
    {
        PlayerAttack,
        PlayerAttackPercent,
        PetAttack,
        PetAttackPercent,
        CritRatePercent,
        CritDamagePercent,
        EncounterLuckPercent,
        PowerCoinBonusPercent,
        PlayerHeartUnit
    }

    public sealed class PetGachaPet
    {
        public PetGachaPet(string id, string displayName)
            : this(id, displayName, 0)
        {
        }

        public PetGachaPet(string id, string displayName, int attackBonus,
            double attackMultiplierPercent, int eventEncounterChanceBonusBasisPoints)
            : this(id, displayName,
                playerAttackBonus: attackBonus,
                playerAttackMultiplierPercent: attackMultiplierPercent,
                petAttackBonus: 0,
                petAttackMultiplierPercent: 0d,
                critRatePercent: 0d,
                critDamagePercent: 0d,
                encounterLuckPercent: eventEncounterChanceBonusBasisPoints / 100d,
                powerCoinBonusPercent: 0d,
                playerHeartUnit: 0,
                passive: null)
        {
        }

        public PetGachaPet(
            string id,
            string displayName,
            int playerAttackBonus = 0,
            double playerAttackMultiplierPercent = 0d,
            int petAttackBonus = 0,
            double petAttackMultiplierPercent = 0d,
            double critRatePercent = 0d,
            double critDamagePercent = 0d,
            double encounterLuckPercent = 0d,
            double powerCoinBonusPercent = 0d,
            int playerHeartUnit = 0,
            PetPassiveDefinition passive = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Pet ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Pet display name is required.", nameof(displayName));
            if (playerAttackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(playerAttackBonus));
            if (playerAttackMultiplierPercent < 0d)
                throw new ArgumentOutOfRangeException(nameof(playerAttackMultiplierPercent));
            if (petAttackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(petAttackBonus));
            if (petAttackMultiplierPercent < 0d)
                throw new ArgumentOutOfRangeException(nameof(petAttackMultiplierPercent));
            if (critRatePercent < 0d)
                throw new ArgumentOutOfRangeException(nameof(critRatePercent));
            if (critDamagePercent < 0d)
                throw new ArgumentOutOfRangeException(nameof(critDamagePercent));
            if (encounterLuckPercent < 0d)
                throw new ArgumentOutOfRangeException(nameof(encounterLuckPercent));
            if (powerCoinBonusPercent < 0d)
                throw new ArgumentOutOfRangeException(nameof(powerCoinBonusPercent));
            if (playerHeartUnit < 0)
                throw new ArgumentOutOfRangeException(nameof(playerHeartUnit));

            Id = id.Trim();
            DisplayName = displayName.Trim();
            PlayerAttackBonus = playerAttackBonus;
            PlayerAttackMultiplierPercent = playerAttackMultiplierPercent;
            PetAttackBonus = petAttackBonus;
            PetAttackMultiplierPercent = petAttackMultiplierPercent;
            CritRatePercent = critRatePercent;
            CritDamagePercent = critDamagePercent;
            EncounterLuckPercent = encounterLuckPercent;
            PowerCoinBonusPercent = powerCoinBonusPercent;
            PlayerHeartUnit = playerHeartUnit;
            Passive = passive;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int PlayerAttackBonus { get; }
        public double PlayerAttackMultiplierPercent { get; }
        public int PetAttackBonus { get; }
        public double PetAttackMultiplierPercent { get; }
        public double CritRatePercent { get; }
        public double CritDamagePercent { get; }
        public double EncounterLuckPercent { get; }
        public double PowerCoinBonusPercent { get; }
        public int PlayerHeartUnit { get; }
        public PetPassiveDefinition Passive { get; }
        public string PassiveDescription => Passive?.Description ?? string.Empty;

        /// <summary>Backwards-compatible: Flat ATK (Player or Pet).</summary>
        public int AttackBonus => PlayerAttackBonus > 0 ? PlayerAttackBonus : PetAttackBonus;
        /// <summary>Backwards-compatible: Percentage ATK multiplier (Player or Pet).</summary>
        public double AttackMultiplierPercent => PlayerAttackMultiplierPercent > 0d ? PlayerAttackMultiplierPercent : PetAttackMultiplierPercent;
        /// <summary>Additive account-wide Event chance multiplier in basis points (1% = 100 bp).</summary>
        public int EventEncounterChanceBonusBasisPoints => (int)Math.Round(EncounterLuckPercent * 100d);
    }

    public sealed class PetGachaRarity
    {
        private readonly PetGachaPet[] _pets;

        public PetGachaRarity(
            string id,
            string displayName,
            int rateBasisPoints,
            IReadOnlyList<PetGachaPet> pets,
            bool countsForTenPullGuarantee = false,
            bool resetsSsrPity = false)
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
            CountsForTenPullGuarantee = countsForTenPullGuarantee || resetsSsrPity;
            ResetsSsrPity = resetsSsrPity;
            _pets = new PetGachaPet[pets.Count];
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
        public bool CountsForTenPullGuarantee { get; }
        public bool ResetsSsrPity { get; }
        public IReadOnlyList<PetGachaPet> Pets => _pets;
    }

    public sealed class PetGachaCatalog
    {
        public const int TotalRateBasisPoints = 10000;

        private readonly PetGachaRarity[] _rarities;
        private readonly Dictionary<string, PetGachaPet> _petsById;
        private readonly Dictionary<string, PetGachaRarity> _raritiesByPetId;

        public PetGachaCatalog(string version, IReadOnlyList<PetGachaRarity> rarities)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Catalog version is required.", nameof(version));
            if (rarities == null || rarities.Count == 0)
                throw new ArgumentException("At least one rarity is required.", nameof(rarities));

            Version = version.Trim();
            _rarities = new PetGachaRarity[rarities.Count];
            _petsById = new Dictionary<string, PetGachaPet>(StringComparer.OrdinalIgnoreCase);
            _raritiesByPetId = new Dictionary<string, PetGachaRarity>(StringComparer.OrdinalIgnoreCase);
            var rarityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                    _raritiesByPetId[pet.Id] = rarity;
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

        public bool TryGetPet(string petId, out PetGachaPet pet, out PetGachaRarity rarity)
        {
            if (TryGetPet(petId, out pet) && _raritiesByPetId.TryGetValue(pet.Id, out rarity))
            {
                return true;
            }

            rarity = null;
            return false;
        }

        public bool TryGetRarityForPet(string petId, out PetGachaRarity rarity) =>
            _raritiesByPetId.TryGetValue(petId ?? string.Empty, out rarity);

        public bool TryGetRarity(string rarityId, out PetGachaRarity rarity)
        {
            foreach (PetGachaRarity candidate in _rarities)
            {
                if (string.Equals(candidate.Id, rarityId, StringComparison.OrdinalIgnoreCase))
                {
                    rarity = candidate;
                    return true;
                }
            }
            rarity = null;
            return false;
        }

        public PetGachaRarity GetTenPullGuaranteeRarity()
        {
            PetGachaRarity result = null;
            foreach (PetGachaRarity rarity in _rarities)
            {
                if (!rarity.CountsForTenPullGuarantee || rarity.ResetsSsrPity) continue;
                if (result != null)
                    throw new InvalidOperationException("Catalog has more than one SR guarantee rarity.");
                result = rarity;
            }
            return result ?? GetSsrPityRarity();
        }

        public PetGachaRarity GetSsrPityRarity()
        {
            PetGachaRarity result = null;
            foreach (PetGachaRarity rarity in _rarities)
            {
                if (!rarity.ResetsSsrPity) continue;
                if (result != null)
                    throw new InvalidOperationException("Catalog has more than one SSR pity rarity.");
                result = rarity;
            }
            return result ?? throw new InvalidOperationException("Catalog has no SSR pity rarity.");
        }
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
        public PetGachaResult(
            string petId,
            string rarityId,
            bool wasNew,
            int previousCount = 0,
            int resultingCount = 1)
        {
            PetId = petId ?? throw new ArgumentNullException(nameof(petId));
            RarityId = rarityId ?? throw new ArgumentNullException(nameof(rarityId));
            WasNew = wasNew;
            if (previousCount < 0 || resultingCount != checked(previousCount + 1))
                throw new ArgumentOutOfRangeException(nameof(resultingCount));
            PreviousCount = previousCount;
            ResultingCount = resultingCount;
        }

        public string PetId { get; }
        public string RarityId { get; }
        public bool WasNew { get; }
        public int PreviousCount { get; }
        public int ResultingCount { get; }
    }

    public readonly struct PetGachaCommand
    {
        public PetGachaCommand(string transactionId, string catalogVersion, long previewRevision, int pullCount = 1)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
            if (string.IsNullOrWhiteSpace(catalogVersion))
                throw new ArgumentException("Catalog version is required.", nameof(catalogVersion));
            if (previewRevision < 0) throw new ArgumentOutOfRangeException(nameof(previewRevision));
            if (pullCount != 1 && pullCount != 10)
                throw new ArgumentOutOfRangeException(
                    nameof(pullCount),
                    "Pet gacha supports only 1x or 10x pulls.");
            TransactionId = transactionId;
            CatalogVersion = catalogVersion;
            PreviewRevision = previewRevision;
            PullCount = pullCount;
        }

        public string TransactionId { get; }
        public string CatalogVersion { get; }
        public long PreviewRevision { get; }
        public int PullCount { get; }
    }

    public readonly struct PetGachaReceipt
    {
        private readonly PetGachaResult[] _results;

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
            if (string.IsNullOrEmpty(petId)) throw new ArgumentNullException(nameof(petId));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (resultingPowerCoins < 0) throw new ArgumentOutOfRangeException(nameof(resultingPowerCoins));
            PetId = petId;
            WasNew = wasNew;
            Cost = cost;
            ResultingPowerCoins = resultingPowerCoins;
            _results = new[] { new PetGachaResult(petId, string.Empty, wasNew) };
            PreviousPityCount = 0;
            ResultingPityCount = 0;
        }

        public PetGachaReceipt(
            string transactionId,
            string catalogVersion,
            IReadOnlyList<PetGachaResult> results,
            long cost,
            long resultingPowerCoins,
            int previousPityCount = 0,
            int resultingPityCount = 0)
        {
            TransactionId = transactionId ?? throw new ArgumentNullException(nameof(transactionId));
            CatalogVersion = catalogVersion ?? throw new ArgumentNullException(nameof(catalogVersion));
            if (results == null || results.Count == 0)
                throw new ArgumentException("At least one gacha result is required.", nameof(results));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (resultingPowerCoins < 0) throw new ArgumentOutOfRangeException(nameof(resultingPowerCoins));
            if (previousPityCount < 0 || previousPityCount >= PetGachaTransactionPolicy.SsrHardPityPulls)
                throw new ArgumentOutOfRangeException(nameof(previousPityCount));
            if (resultingPityCount < 0 || resultingPityCount >= PetGachaTransactionPolicy.SsrHardPityPulls)
                throw new ArgumentOutOfRangeException(nameof(resultingPityCount));
            _results = new PetGachaResult[results.Count];
            bool anyNew = false;
            for (int i = 0; i < results.Count; i++)
            {
                _results[i] = results[i];
                if (results[i].WasNew) anyNew = true;
            }
            PetId = _results[0].PetId;
            WasNew = anyNew;
            Cost = cost;
            ResultingPowerCoins = resultingPowerCoins;
            PreviousPityCount = previousPityCount;
            ResultingPityCount = resultingPityCount;
        }

        public string TransactionId { get; }
        public string CatalogVersion { get; }
        public string PetId { get; }
        public bool WasNew { get; }
        public long Cost { get; }
        public long ResultingPowerCoins { get; }
        public int PreviousPityCount { get; }
        public int ResultingPityCount { get; }
        public IReadOnlyList<PetGachaResult> Results => _results ?? Array.Empty<PetGachaResult>();
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
