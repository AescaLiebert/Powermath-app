using System;
using System.Text;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class PlayerHubView : IDisposable
    {
        public readonly VisualElement Modal;
        public readonly Button OpenButton;
        public readonly VisualElement LockOverlay;
        public readonly Button CloseButton;
        public readonly Button UpgradeButton;
        public readonly Label EffectiveAttack;
        public readonly Label AttackBreakdown;
        public readonly Label LegacyBonus;
        public readonly Label PetStatus;
        public readonly Label CompactAttack;
        public readonly Label CompactCritRate;
        public readonly Label CompactCritDamage;
        public readonly Label CompactPetAttack;
        public readonly Label CompactLuck;
        public readonly Label CompactCoinBonus;
        public readonly Label WeaponName;
        public readonly Label WeaponCurrent;
        public readonly Label WeaponNext;
        public readonly Label WeaponSubLevel;
        public readonly Label EquippedWeaponSubLevel;
        public readonly Label LevelTransition;
        public readonly Label AscendSubtitle;
        public readonly Label CurrentAttack;
        public readonly Label NextAttack;
        public readonly Label CurrentCritRate;
        public readonly Label NextCritRate;
        public readonly Label CurrentCritDamage;
        public readonly Label NextCritDamage;
        public readonly Label WeaponCost;
        public readonly Label Balance;
        public readonly Label Status;
        public readonly Label SummaryAttack;
        public readonly VisualElement WeaponIcon;
        public readonly VisualElement WeaponAura;
        public readonly VisualElement CurrentCard;
        public readonly VisualElement NextCard;
        public readonly VisualElement ParticleLayer;
        public readonly VisualElement PetPreview;
        public readonly VisualElement PetPreviewInfo;
        public readonly VisualElement PetPreviewArtwork;
        public readonly Image PetPreviewIcon;
        public readonly Label PetPreviewRarity;
        public readonly Label PetPreviewName;
        public readonly Label PetPreviewAttack;
        public readonly VisualElement PetPreviewStatRow;
        public readonly VisualElement PetPreviewStatIcon;
        public readonly VisualElement PetPreviewPassive;
        public readonly Label PetPreviewPassiveName;
        public readonly Label PetPreviewPassiveDescription;
        public readonly Label PetPreviewDescription;
        public readonly Label PetPreviewState;
        public readonly Image EquippedPet;
        public readonly Image EquippedWeapon;
        public readonly VisualElement Milestone;
        public readonly Label MilestoneName;
        public readonly VisualElement StarFrame;
        public readonly Label StarLabel;
        public readonly Label EquippedStarLabel;
        public readonly VisualElement RowAttack;
        public readonly VisualElement RowCritRate;
        public readonly VisualElement RowCritDamage;
        public readonly VisualElement PlayerAvatar;

        private readonly Button _weaponTab;
        private readonly Button _petTab;
        private readonly VisualElement _weaponWorkspace;
        private readonly VisualElement _petWorkspace;
        private readonly ScrollView _petScroll;
        private readonly VisualElement _petGrid;
        private readonly Button _petScrollPrevious;
        private readonly Button _petScrollNext;
        private readonly Image _weaponSprite;

        private const float PetTileStride = 225f;

        public PlayerHubView(VisualElement root)
        {
            Modal = Require<VisualElement>(root, "player-hub-modal");
            OpenButton = Require<Button>(root, "player-hub-button");
            LockOverlay = OpenButton.Q<VisualElement>("player-hub-lock");
            CloseButton = Require<Button>(root, "player-hub-close");
            UpgradeButton = Require<Button>(root, "player-hub-weapon-upgrade");
            EffectiveAttack = Require<Label>(root, "player-hub-effective-atk");
            AttackBreakdown = Require<Label>(root, "player-hub-atk-breakdown");
            LegacyBonus = Require<Label>(root, "player-hub-legacy-bonus");
            PetStatus = Require<Label>(root, "player-hub-pet-status");
            CompactAttack = Require<Label>(root, "player-hub-compact-attack");
            CompactCritRate = Require<Label>(root, "player-hub-compact-crit-rate");
            CompactCritDamage = Require<Label>(root, "player-hub-compact-crit-damage");
            CompactPetAttack = Require<Label>(root, "player-hub-compact-pet-attack");
            CompactLuck = Require<Label>(root, "player-hub-compact-luck");
            CompactCoinBonus = Require<Label>(root, "player-hub-compact-coin-bonus");
            WeaponName = Require<Label>(root, "player-hub-weapon-name");
            WeaponCurrent = Require<Label>(root, "player-hub-weapon-current");
            WeaponNext = Require<Label>(root, "player-hub-weapon-next");
            WeaponSubLevel = Require<Label>(root, "player-hub-weapon-sublevel");
            EquippedWeaponSubLevel = Require<Label>(root, "player-hub-equipped-sublevel");
            LevelTransition = Require<Label>(root, "player-hub-level-transition");
            AscendSubtitle = Require<Label>(root, "player-hub-ascend-subtitle");
            CurrentAttack = Require<Label>(root, "player-hub-current-attack");
            NextAttack = Require<Label>(root, "player-hub-next-attack");
            CurrentCritRate = Require<Label>(root, "player-hub-current-crit-rate");
            NextCritRate = Require<Label>(root, "player-hub-next-crit-rate");
            CurrentCritDamage = Require<Label>(root, "player-hub-current-crit-damage");
            NextCritDamage = Require<Label>(root, "player-hub-next-crit-damage");
            WeaponCost = Require<Label>(root, "player-hub-weapon-cost");
            Balance = Require<Label>(root, "player-hub-balance");
            Status = Require<Label>(root, "player-hub-status");
            SummaryAttack = root.Q<Label>("player-menu-atk");
            WeaponIcon = Require<VisualElement>(root, "player-hub-weapon-icon");
            WeaponAura = Require<VisualElement>(root, "player-hub-weapon-aura");
            CurrentCard = Require<VisualElement>(root, "player-hub-current-card");
            NextCard = Require<VisualElement>(root, "player-hub-next-card");
            ParticleLayer = Require<VisualElement>(root, "player-hub-particle-layer");
            PetPreview = Require<VisualElement>(root, "player-hub-pet-preview");
            PetPreviewInfo = PetPreview.Q<VisualElement>(
                className: "player-hub-pet-selected-info--metadata") ??
                throw new InvalidOperationException(
                    "PlayerHubView requires the pet preview metadata group.");
            PetPreviewArtwork = PetPreview.Q<VisualElement>(
                className: "player-hub-pet-selected-info--artwork") ??
                throw new InvalidOperationException(
                    "PlayerHubView requires the pet preview artwork group.");
            PetPreviewIcon = Require<Image>(root, "player-hub-pet-preview-icon");
            PetPreviewRarity = Require<Label>(root, "player-hub-pet-preview-rarity");
            PetPreviewName = Require<Label>(root, "player-hub-pet-preview-name");
            PetPreviewAttack = Require<Label>(root, "player-hub-pet-preview-attack");
            PetPreviewStatRow = Require<VisualElement>(PetPreview, "Compact Stat / Attack");
            PetPreviewStatIcon = PetPreviewStatRow.Q<VisualElement>("Icon_ATK");
            PetPreviewPassive = Require<VisualElement>(PetPreview, "Passive");
            PetPreviewPassiveName = Require<Label>(PetPreview, "Passive Name");
            PetPreviewPassiveDescription = Require<Label>(PetPreview, "player-hub-pet-passive-description");
            PetPreviewDescription = Require<Label>(PetPreview, "player-hub-pet-preview-description");
            PetPreviewState = Require<Label>(PetPreview, "player-hub-pet-preview-state");
            EquippedPet = Require<Image>(root, "player-hub-equipped-pet");
            EquippedWeapon = Require<Image>(root, "player-hub-equipped-weapon");
            Milestone = Require<VisualElement>(root, "player-hub-milestone");
            MilestoneName = Require<Label>(root, "player-hub-milestone-name");
            StarFrame = root.Q<VisualElement>("Frame-Star") ?? root.Q<VisualElement>("player-hub-star-frame");
            StarLabel = StarFrame?.Q<Label>("Star") ?? root.Q<Label>("player-hub-star");
            EquippedStarLabel = root.Q<Label>("player-hub-equipped-star");
            RowAttack = root.Q<VisualElement>("Weapon / Stat Comparison / Attack");
            RowCritRate = root.Q<VisualElement>("Weapon / Stat Comparison / Crit Rate");
            RowCritDamage = root.Q<VisualElement>("Weapon / Stat Comparison / Crit Damage");
            PlayerAvatar = root.Q("player-hub-avatar") ?? root.Q("Hub_Stand_Stellar 1") ?? root.Q(className: "player-hub-player-sprite");
            _weaponTab = Require<Button>(root, "player-hub-tab-weapon");
            _petTab = Require<Button>(root, "player-hub-tab-pets");
            _weaponWorkspace = Require<VisualElement>(root, "player-hub-weapon-workspace");
            _petWorkspace = Require<VisualElement>(root, "player-hub-pet-workspace");
            _petScroll = Require<ScrollView>(root, "player-hub-pet-scroll");
            _petGrid = Require<VisualElement>(root, "player-hub-pet-grid");
            var petScrollButtons = root.Query<Button>(name: "scroll").ToList();
            if (petScrollButtons.Count < 2)
                throw new InvalidOperationException(
                    "PlayerHubView requires both Figma 'scroll' controls.");
            _petScrollPrevious = petScrollButtons[0];
            _petScrollNext = petScrollButtons[1];
            _weaponSprite = Require<Image>(root, "player-hub-weapon-sprite");

            HidePetPreview();

            OpenButton.clicked += RaiseOpen;
            CloseButton.clicked += RaiseClose;
            UpgradeButton.clicked += RaiseUpgrade;
            _weaponTab.clicked += ShowWeapon;
            _petTab.clicked += ShowPets;
            _petScrollPrevious.clicked += ScrollPetsPrevious;
            _petScrollNext.clicked += ScrollPetsNext;
            SetSection(false);
        }

        public event Action OpenRequested;
        public event Action CloseRequested;
        public event Action UpgradeRequested;
        public event Action<string> PetEquipRequested;
        public event Action PetsSectionShown;

        public bool IsPetSectionVisible { get; private set; }

        public void Dispose()
        {
            OpenButton.clicked -= RaiseOpen;
            CloseButton.clicked -= RaiseClose;
            UpgradeButton.clicked -= RaiseUpgrade;
            _weaponTab.clicked -= ShowWeapon;
            _petTab.clicked -= ShowPets;
            _petScrollPrevious.clicked -= ScrollPetsPrevious;
            _petScrollNext.clicked -= ScrollPetsNext;
        }

        public void SetSection(bool pets)
        {
            IsPetSectionVisible = pets;
            _weaponWorkspace.style.display = pets ? DisplayStyle.None : DisplayStyle.Flex;
            _petWorkspace.style.display = pets ? DisplayStyle.Flex : DisplayStyle.None;
            _weaponTab.EnableInClassList("is-selected", !pets);
            _petTab.EnableInClassList("is-selected", pets);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _weaponTab.SetEnabled(enabled);
            _petTab.SetEnabled(enabled);
            _petGrid.SetEnabled(enabled);
            _petScrollPrevious.SetEnabled(enabled);
            _petScrollNext.SetEnabled(enabled);
        }

        public void RenderInventory(
            PlayerOwnedPetInventory inventory,
            string selectedPetId,
            string pendingPetId)
        {
            // A tile click can trigger multiple renders while the equip request begins.
            // Rebuild the tiles in place: do not schedule ScrollTo for a tile that may be
            // detached by the next render, and do not overwrite the player's scroll offset.
            if (inventory == null)
            {
                _petGrid.Clear();
                return;
            }

            if (_petGrid.childCount == inventory.Entries.Count && _petGrid.childCount > 0)
            {
                bool allMatch = true;
                for (int i = 0; i < inventory.Entries.Count; i++)
                {
                    if (!(_petGrid[i] is Button existingTile) ||
                        !string.Equals(existingTile.userData as string, inventory.Entries[i].Definition.PetId, StringComparison.Ordinal))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    for (int i = 0; i < inventory.Entries.Count; i++)
                    {
                        OwnedPetEntry entry = inventory.Entries[i];
                        Button tile = (Button)_petGrid[i];
                        tile.EnableInClassList("is-equipped", entry.IsEquipped);
                        bool isPending = string.Equals(pendingPetId, entry.Definition.PetId, StringComparison.Ordinal);
                        tile.EnableInClassList("is-pending", isPending);
                        bool isSelected = string.Equals(selectedPetId, entry.Definition.PetId, StringComparison.Ordinal);
                        tile.EnableInClassList("is-selected", isSelected);
                        var marker = tile.Q<VisualElement>("generic_selected");
                        if (marker != null)
                        {
                            marker.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                        var countBadge = tile.Q<Label>(className: "player-hub-pet-tile-count");
                        if (countBadge != null)
                        {
                            countBadge.text = entry.Count > 1 ? $"x{entry.Count}" : string.Empty;
                            countBadge.style.display = entry.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                    }
                    return;
                }
            }

            _petGrid.Clear();
            foreach (OwnedPetEntry entry in inventory.Entries)
            {
                OwnedPetEntry captured = entry;
                var tile = new Button
                {
                    name = "PetSlot",
                    tooltip = entry.Definition.DisplayName,
                    userData = entry.Definition.PetId
                };
                tile.AddToClassList("player-hub-pet-tile");
                tile.EnableInClassList("is-equipped", entry.IsEquipped);
                bool pending = string.Equals(
                    pendingPetId,
                    entry.Definition.PetId,
                    StringComparison.Ordinal);
                tile.EnableInClassList("is-pending", pending);
                tile.EnableInClassList(
                    "is-selected",
                    string.Equals(
                        selectedPetId,
                        entry.Definition.PetId,
                        StringComparison.Ordinal));
                var icon = new Image { sprite = entry.Definition.Icon };
                icon.name = "Icon_pet";
                icon.AddToClassList("player-hub-pet-tile-icon");
                icon.pickingMode = PickingMode.Ignore;
                var selectedMarker = new VisualElement
                {
                    name = "generic_selected",
                    pickingMode = PickingMode.Ignore
                };
                selectedMarker.AddToClassList("player-hub-pet-tile-selected-marker");
                selectedMarker.style.display = tile.ClassListContains("is-selected")
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                var rarity = new Label(BuildRarityStars(entry))
                {
                    name = "Frame-Star",
                    tooltip = entry.RarityName
                };
                rarity.AddToClassList("player-hub-pet-tile-rarity");
                rarity.style.color = entry.RarityColor;
                rarity.pickingMode = PickingMode.Ignore;
                tile.Add(icon);
                tile.Add(rarity);
                var countBadge = new Label(entry.Count > 1 ? $"x{entry.Count}" : string.Empty);
                countBadge.AddToClassList("player-hub-pet-tile-count");
                countBadge.pickingMode = PickingMode.Ignore;
                countBadge.style.display = entry.Count > 1
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                tile.Add(selectedMarker);
                tile.Add(countBadge);
                tile.clicked += () => PetEquipRequested?.Invoke(
                    captured.Definition.PetId);
                _petGrid.Add(tile);
            }
        }

        public void RenderPetPreview(OwnedPetEntry entry, bool pending)
        {
            PetDefinition definition = entry.Definition;
            PetPreviewIcon.sprite = definition.PreviewSprite != null
                ? definition.PreviewSprite
                : definition.Icon;
            PetPreviewRarity.text = BuildRarityStars(entry);
            PetPreviewRarity.tooltip = entry.RarityName;
            PetPreviewRarity.style.color = entry.RarityColor;
            string nameText = definition.DisplayName.ToUpperInvariant();
            if (entry.Count > 1) nameText += $"  (x{entry.Count})";
            PetPreviewName.text = nameText;
            PetPreviewStatKind statKind = ResolvePetStat(definition);
            PetPreviewStatRow.style.display = DisplayStyle.Flex;
            SetPetStatIcon(statKind);
            PetPreviewAttack.text = FormatPetStat(definition, entry.Count, statKind);
            PetPreviewDescription.text = string.Empty;
            PetPreviewDescription.style.display = DisplayStyle.None;

            bool hasPassive = IsSsr(entry) && definition.PassiveType != PetPassiveEffectType.None;
            PetPreviewPassive.style.display = hasPassive
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            PetPreviewPassiveName.text = hasPassive
                ? FormatPassiveName(definition)
                : string.Empty;
            PetPreviewPassiveDescription.text = hasPassive
                ? definition.PassiveDescription.Trim()
                : string.Empty;
            PetPreviewPassiveDescription.style.display = hasPassive &&
                !string.IsNullOrWhiteSpace(definition.PassiveDescription)
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            PetPreviewState.text = string.Empty;
            PetPreviewState.style.display = DisplayStyle.None;
        }

        public void HidePetPreview()
        {
            PetPreviewInfo.style.visibility = Visibility.Hidden;
            PetPreviewInfo.style.opacity = 0f;
            PetPreviewInfo.style.translate = new Translate(0f, 20f, 0f);
            PetPreviewIcon.style.visibility = Visibility.Hidden;
            PetPreviewIcon.style.opacity = 0f;
            PetPreviewIcon.style.scale = new Scale(Vector3.one);
            PetPreviewIcon.style.translate = new Translate(-50f, 0f, 0f);
        }

        public void PrimePetPreviewEntrance()
        {
            PetPreviewInfo.style.visibility = Visibility.Visible;
            PetPreviewInfo.style.opacity = 0f;
            PetPreviewInfo.style.translate = new Translate(0f, 20f, 0f);
            PetPreviewIcon.style.visibility = Visibility.Visible;
            PetPreviewIcon.style.opacity = 0f;
            PetPreviewIcon.style.scale = new Scale(Vector3.one);
            PetPreviewIcon.style.translate = new Translate(-50f, 0f, 0f);
        }

        public void ShowPetPreviewImmediate()
        {
            PetPreviewInfo.style.visibility = Visibility.Visible;
            PetPreviewInfo.style.opacity = 1f;
            PetPreviewInfo.style.translate = new Translate(0f, 0f, 0f);
            PetPreviewIcon.style.visibility = Visibility.Visible;
            PetPreviewIcon.style.opacity = 1f;
            PetPreviewIcon.style.scale = new Scale(Vector3.one);
            PetPreviewIcon.style.translate = new Translate(0f, 0f, 0f);
        }

        public void SetWeaponPresentation(
            WeaponAscensionCatalogDefinition.Tier tier)
        {
            Sprite icon = tier?.icon;
            _weaponSprite.sprite = icon;
            EquippedWeapon.sprite = icon;
        }

        private void RaiseOpen() => OpenRequested?.Invoke();
        private void RaiseClose() => CloseRequested?.Invoke();
        private void RaiseUpgrade() => UpgradeRequested?.Invoke();
        private void ShowWeapon() => SetSection(false);
        private void ShowPets()
        {
            SetSection(true);
            PetsSectionShown?.Invoke();
        }

        private void ScrollPetsPrevious() => ScrollPets(-PetTileStride);
        private void ScrollPetsNext() => ScrollPets(PetTileStride);

        private void ScrollPets(float delta)
        {
            Vector2 offset = _petScroll.scrollOffset;
            _petScroll.scrollOffset = new Vector2(
                Mathf.Max(0f, offset.x + delta),
                0f);
        }

        private static string BuildRarityStars(OwnedPetEntry entry)
        {
            string rarity = (entry.RarityId + " " + entry.RarityName)
                .ToUpperInvariant();
            int count = rarity.Contains("SSR") || rarity.Contains("LEGENDARY")
                ? 5
                : rarity.Contains("SR") || rarity.Contains("EPIC")
                    ? 4
                    : rarity.Contains("RARE") || rarity.StartsWith("R ", StringComparison.Ordinal)
                        ? 3
                        : rarity.Contains("UNCOMMON")
                            ? 2
                            : 1;
            return new string('★', count);
        }

        private enum PetPreviewStatKind
        {
            None,
            PlayerAttack,
            PetAttack,
            CritRate,
            CritDamage,
            EncounterLuck,
            PowerCoinBonus,
            PlayerHearts
        }

        private static PetPreviewStatKind ResolvePetStat(PetDefinition definition)
        {
            if (definition.PlayerAttackBonus > 0 || definition.PlayerAttackMultiplierPercent > 0f)
                return PetPreviewStatKind.PlayerAttack;
            if (definition.PetAttackBonus > 0 || definition.PetAttackMultiplierPercent > 0f)
                return PetPreviewStatKind.PetAttack;
            if (definition.CritRatePercent > 0f) return PetPreviewStatKind.CritRate;
            if (definition.CritDamagePercent > 0f) return PetPreviewStatKind.CritDamage;
            if (definition.EncounterLuckPercent > 0f) return PetPreviewStatKind.EncounterLuck;
            if (definition.PowerCoinBonusPercent > 0f) return PetPreviewStatKind.PowerCoinBonus;
            if (definition.PlayerHeartUnit > 0) return PetPreviewStatKind.PlayerHearts;
            return PetPreviewStatKind.None;
        }

        private void SetPetStatIcon(PetPreviewStatKind statKind)
        {
            PetPreviewStatIcon.EnableInClassList("player-hub-icon-atk", false);
            PetPreviewStatIcon.EnableInClassList("player-hub-icon-pet", false);
            PetPreviewStatIcon.EnableInClassList("player-hub-icon-cr", false);
            PetPreviewStatIcon.EnableInClassList("player-hub-icon-cd", false);
            PetPreviewStatIcon.EnableInClassList("player-hub-icon-luck", false);
            PetPreviewStatIcon.EnableInClassList("player-hub-icon-coin", false);
            PetPreviewStatIcon.EnableInClassList("player-hub-icon-heart", false);
            switch (statKind)
            {
                case PetPreviewStatKind.PlayerAttack:
                    PetPreviewStatIcon.AddToClassList("player-hub-icon-atk");
                    break;
                case PetPreviewStatKind.PetAttack:
                    PetPreviewStatIcon.AddToClassList("player-hub-icon-pet");
                    break;
                case PetPreviewStatKind.CritRate:
                    PetPreviewStatIcon.AddToClassList("player-hub-icon-cr");
                    break;
                case PetPreviewStatKind.CritDamage:
                    PetPreviewStatIcon.AddToClassList("player-hub-icon-cd");
                    break;
                case PetPreviewStatKind.EncounterLuck:
                    PetPreviewStatIcon.AddToClassList("player-hub-icon-luck");
                    break;
                case PetPreviewStatKind.PowerCoinBonus:
                    PetPreviewStatIcon.AddToClassList("player-hub-icon-coin");
                    break;
                case PetPreviewStatKind.PlayerHearts:
                    PetPreviewStatIcon.AddToClassList("player-hub-icon-heart");
                    break;
            }
        }

        private static string FormatPetStat(
            PetDefinition definition,
            int count,
            PetPreviewStatKind statKind)
        {
            int safeCount = Mathf.Max(1, count);
            switch (statKind)
            {
                case PetPreviewStatKind.PlayerAttack:
                    int flat = definition.PlayerAttackBonus * safeCount;
                    float multiplier = definition.PlayerAttackMultiplierPercent * safeCount;
                    if (flat > 0 && multiplier > 0f) return $"+{flat:N0} (+{multiplier:0.##}%)";
                    return flat > 0 ? $"+{flat:N0}" : $"+{multiplier:0.##}%";
                case PetPreviewStatKind.PetAttack:
                    int petFlat = definition.PetAttackBonus * safeCount;
                    float petMultiplier = definition.PetAttackMultiplierPercent * safeCount;
                    if (petFlat > 0 && petMultiplier > 0f) return $"+{petFlat:N0} (+{petMultiplier:0.##}%)";
                    return petFlat > 0 ? $"+{petFlat:N0}" : $"+{petMultiplier:0.##}%";
                case PetPreviewStatKind.CritRate:
                    return $"+{definition.CritRatePercent * safeCount:0.##}%";
                case PetPreviewStatKind.CritDamage:
                    return $"+{definition.CritDamagePercent * safeCount:0.##}%";
                case PetPreviewStatKind.EncounterLuck:
                    return $"+{definition.EncounterLuckPercent * safeCount:0.##}%";
                case PetPreviewStatKind.PowerCoinBonus:
                    return $"+{definition.PowerCoinBonusPercent * safeCount:0.##}%";
                case PetPreviewStatKind.PlayerHearts:
                    return $"+{definition.PlayerHeartUnit * safeCount:N0}";
                default:
                    return string.Empty;
            }
        }

        private static bool IsSsr(OwnedPetEntry entry)
        {
            string rarity = (entry.RarityId + " " + entry.RarityName).ToUpperInvariant();
            return rarity.Contains("SSR") || rarity.Contains("LEGENDARY");
        }

        private static string FormatPassiveName(PetDefinition definition)
        {
            if (definition.PassiveType == PetPassiveEffectType.None)
                return "COLLECTION BONUS";
            string raw = definition.PassiveType.ToString();
            var label = new StringBuilder(raw.Length + 8);
            for (int index = 0; index < raw.Length; index++)
            {
                char current = raw[index];
                if (index > 0 && char.IsUpper(current) && char.IsLower(raw[index - 1]))
                    label.Append(' ');
                label.Append(current);
            }
            return label.ToString().ToUpperInvariant();
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return UiElementQuery.Require<T>(root, name, nameof(PlayerHubView));
        }
    }
}
