using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Gameplay.Pets
{
    [CreateAssetMenu(fileName = "PetGachaCatalog", menuName = "PowerMath/Pets/Gacha Catalog")]
    public sealed class PetGachaCatalogDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class RarityContent
        {
            public string rarityId;
            public string displayName;
            [Min(1)] public int rateBasisPoints;
            public Color displayColor = Color.white;
            public PetDefinition[] pets = Array.Empty<PetDefinition>();
        }

        [Header("Catalog")]
        [Tooltip("Increment whenever production rarity membership or rates change.")]
        [SerializeField] private string catalogVersion = string.Empty;

        [Tooltip("Rarity rates must total exactly 10,000 basis points.")]
        [SerializeField] private RarityContent[] rarities = Array.Empty<RarityContent>();

        [Header("Optional Feedback")]
        [Tooltip("Played after the player commits a pull.")]
        [SerializeField] private AudioClip commitClip;

        [Tooltip("Played when permanent ownership is added.")]
        [SerializeField] private AudioClip newPetClip;

        [Tooltip("Played for an empty duplicate result.")]
        [SerializeField] private AudioClip duplicateClip;

        [Tooltip("Played for an input or validation error.")]
        [SerializeField] private AudioClip errorClip;

        public string CatalogVersion => catalogVersion?.Trim() ?? string.Empty;
        public AudioClip CommitClip => commitClip;
        public AudioClip NewPetClip => newPetClip;
        public AudioClip DuplicateClip => duplicateClip;
        public AudioClip ErrorClip => errorClip;
        public IReadOnlyList<RarityContent> Rarities =>
            rarities ?? Array.Empty<RarityContent>();

        public bool TryBuildCatalog(out PetGachaCatalog catalog, out string error)
        {
            catalog = null;
            error = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(catalogVersion))
                    throw new InvalidOperationException("Pet gacha catalog version is required.");
                if (rarities == null || rarities.Length == 0)
                    throw new InvalidOperationException("Pet gacha needs at least one rarity.");

                var mappedRarities = new List<PetGachaRarity>(rarities.Length);
                foreach (RarityContent rarity in rarities)
                {
                    if (rarity == null)
                        throw new InvalidOperationException("Pet rarity entries cannot be null.");
                    if (rarity.pets == null || rarity.pets.Length == 0)
                        throw new InvalidOperationException("Every pet rarity needs at least one pet.");
                    var mappedPets = new List<PetGachaPet>(rarity.pets.Length);
                    foreach (PetDefinition pet in rarity.pets)
                    {
                        if (pet == null)
                            throw new InvalidOperationException(
                                "Pet rarity entries cannot be null.");
                        if (!pet.TryBuild(out PetGachaPet mappedPet, out string petError))
                        {
                            throw new InvalidOperationException(
                                string.IsNullOrEmpty(petError)
                                    ? "Pet definition is invalid."
                                    : petError);
                        }
                        mappedPets.Add(mappedPet);
                    }
                    mappedRarities.Add(new PetGachaRarity(
                        rarity.rarityId,
                        rarity.displayName,
                        rarity.rateBasisPoints,
                        mappedPets));
                }
                catalog = new PetGachaCatalog(catalogVersion, mappedRarities);
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is OverflowException)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TryResolvePet(
            string petId,
            out PetDefinition pet,
            out RarityContent rarity)
        {
            foreach (RarityContent candidateRarity in rarities ?? Array.Empty<RarityContent>())
            {
                if (candidateRarity == null) continue;
                foreach (PetDefinition candidatePet in
                    candidateRarity.pets ?? Array.Empty<PetDefinition>())
                {
                    if (candidatePet != null &&
                        string.Equals(candidatePet.PetId, petId, StringComparison.Ordinal))
                    {
                        pet = candidatePet;
                        rarity = candidateRarity;
                        return true;
                    }
                }
            }
            pet = null;
            rarity = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if ((rarities?.Length ?? 0) > 0 && !TryBuildCatalog(out _, out string error))
                Debug.LogError(error, this);
        }
#endif
    }
}
