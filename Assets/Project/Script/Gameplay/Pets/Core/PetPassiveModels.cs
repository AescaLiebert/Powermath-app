using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Pets
{
    public enum PetPassiveEffectType
    {
        None = 0,
        FollowUpAfterSuccessfulPlayerAttack = 1,
        ModifyEveryNthPlayerAttack = 2,
        EnablePetFollowUpCritical = 3,
        GrantPowerCoinsOnRebirth = 4,
        RestoreHeartsOnEncounterDefeat = 5,
        CounterAttackAfterHeartLoss = 6
    }

    public enum PetPassiveStackRule
    {
        UniquePerDefinition = 0,
        PerCopy = 1,
        CappedCopies = 2
    }

    public enum PetPassiveResetScope
    {
        Never = 0,
        Encounter = 1,
        Stage = 2,
        Run = 3
    }

    public enum PetEncounterFilter
    {
        Any = 0,
        BigBoss = 1
    }

    public sealed class PetPassiveDefinition
    {
        public PetPassiveDefinition(
            string definitionId,
            PetPassiveEffectType effectType,
            double magnitude,
            int triggerCount = 1,
            PetPassiveStackRule stackRule = PetPassiveStackRule.UniquePerDefinition,
            int maximumStacks = 1,
            PetPassiveResetScope resetScope = PetPassiveResetScope.Never,
            PetEncounterFilter encounterFilter = PetEncounterFilter.Any,
            string description = "")
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("Passive definition ID is required.", nameof(definitionId));
            if (effectType == PetPassiveEffectType.None)
                throw new ArgumentOutOfRangeException(nameof(effectType));
            if (double.IsNaN(magnitude) || double.IsInfinity(magnitude) || magnitude < 0d)
                throw new ArgumentOutOfRangeException(nameof(magnitude));
            if (triggerCount <= 0) throw new ArgumentOutOfRangeException(nameof(triggerCount));
            if (maximumStacks <= 0) throw new ArgumentOutOfRangeException(nameof(maximumStacks));

            DefinitionId = definitionId.Trim();
            EffectType = effectType;
            Magnitude = magnitude;
            TriggerCount = triggerCount;
            StackRule = stackRule;
            MaximumStacks = maximumStacks;
            ResetScope = resetScope;
            EncounterFilter = encounterFilter;
            Description = description ?? string.Empty;
        }

        public string DefinitionId { get; }
        public PetPassiveEffectType EffectType { get; }
        public double Magnitude { get; }
        public int TriggerCount { get; }
        public PetPassiveStackRule StackRule { get; }
        public int MaximumStacks { get; }
        public PetPassiveResetScope ResetScope { get; }
        public PetEncounterFilter EncounterFilter { get; }
        public string Description { get; }

        public int ResolveStackCount(int ownedCopyCount)
        {
            if (ownedCopyCount <= 0) return 0;
            switch (StackRule)
            {
                case PetPassiveStackRule.UniquePerDefinition:
                    return 1;
                case PetPassiveStackRule.PerCopy:
                    return ownedCopyCount;
                case PetPassiveStackRule.CappedCopies:
                    return Math.Min(ownedCopyCount, MaximumStacks);
                default:
                    throw new InvalidOperationException("Unsupported pet passive stack rule.");
            }
        }
    }

    public readonly struct ActivePetPassive
    {
        public ActivePetPassive(PetPassiveDefinition definition, int stackCount)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (stackCount <= 0) throw new ArgumentOutOfRangeException(nameof(stackCount));
            StackCount = stackCount;
        }

        public PetPassiveDefinition Definition { get; }
        public int StackCount { get; }
        public double EffectiveMagnitude => Definition.Magnitude * StackCount;
    }

    public sealed class ActivePetPassiveSet
    {
        private readonly ActivePetPassive[] _items;

        public ActivePetPassiveSet(IReadOnlyList<ActivePetPassive> items)
        {
            if (items == null || items.Count == 0)
            {
                _items = Array.Empty<ActivePetPassive>();
                return;
            }

            _items = new ActivePetPassive[items.Count];
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < items.Count; index++)
            {
                ActivePetPassive item = items[index];
                if (item.Definition == null)
                    throw new ArgumentException("Active passive definitions cannot be null.", nameof(items));
                if (!ids.Add(item.Definition.DefinitionId))
                    throw new ArgumentException("Active passive definition IDs must be unique.", nameof(items));
                _items[index] = item;
            }
        }

        public static ActivePetPassiveSet Empty { get; } =
            new ActivePetPassiveSet(Array.Empty<ActivePetPassive>());

        public IReadOnlyList<ActivePetPassive> Items => _items;
        public int Count => _items.Length;

        public bool HasEffect(PetPassiveEffectType effectType) =>
            TryGetFirst(effectType, out _);

        public bool TryGetFirst(
            PetPassiveEffectType effectType,
            out ActivePetPassive passive)
        {
            for (int index = 0; index < _items.Length; index++)
            {
                if (_items[index].Definition.EffectType == effectType)
                {
                    passive = _items[index];
                    return true;
                }
            }

            passive = default;
            return false;
        }

        public double SumMagnitude(PetPassiveEffectType effectType)
        {
            double total = 0d;
            for (int index = 0; index < _items.Length; index++)
            {
                if (_items[index].Definition.EffectType == effectType)
                    total += _items[index].EffectiveMagnitude;
            }
            return total;
        }
    }
}
