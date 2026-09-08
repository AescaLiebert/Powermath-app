using System;
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
        public readonly Label WeaponName;
        public readonly Label WeaponCurrent;
        public readonly Label WeaponNext;
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
        public readonly Image PetPreviewIcon;
        public readonly Label PetPreviewRarity;
        public readonly Label PetPreviewName;
        public readonly Label PetPreviewDescription;
        public readonly Label PetPreviewState;
        public readonly Image EquippedPet;
        public readonly Image EquippedWeapon;
        public readonly VisualElement Milestone;
        public readonly Label MilestoneName;

        private readonly Button _weaponTab;
        private readonly Button _petTab;
        private readonly VisualElement _weaponWorkspace;
        private readonly VisualElement _petWorkspace;
        private readonly VisualElement _petGrid;
        private readonly Image _weaponSprite;
        private readonly Label _weaponPlaceholder;

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
            WeaponName = Require<Label>(root, "player-hub-weapon-name");
            WeaponCurrent = Require<Label>(root, "player-hub-weapon-current");
            WeaponNext = Require<Label>(root, "player-hub-weapon-next");
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
            PetPreviewIcon = Require<Image>(root, "player-hub-pet-preview-icon");
            PetPreviewRarity = Require<Label>(root, "player-hub-pet-preview-rarity");
            PetPreviewName = Require<Label>(root, "player-hub-pet-preview-name");
            PetPreviewDescription = Require<Label>(root, "player-hub-pet-preview-description");
            PetPreviewState = Require<Label>(root, "player-hub-pet-preview-state");
            EquippedPet = Require<Image>(root, "player-hub-equipped-pet");
            EquippedWeapon = Require<Image>(root, "player-hub-equipped-weapon");
            Milestone = Require<VisualElement>(root, "player-hub-milestone");
            MilestoneName = Require<Label>(root, "player-hub-milestone-name");
            _weaponTab = Require<Button>(root, "player-hub-tab-weapon");
            _petTab = Require<Button>(root, "player-hub-tab-pets");
            _weaponWorkspace = Require<VisualElement>(root, "player-hub-weapon-workspace");
            _petWorkspace = Require<VisualElement>(root, "player-hub-pet-workspace");
            _petGrid = Require<VisualElement>(root, "player-hub-pet-grid");
            _weaponSprite = Require<Image>(root, "player-hub-weapon-sprite");
            _weaponPlaceholder = Require<Label>(root, "player-hub-weapon-placeholder");

            OpenButton.clicked += RaiseOpen;
            CloseButton.clicked += RaiseClose;
            UpgradeButton.clicked += RaiseUpgrade;
            _weaponTab.clicked += ShowWeapon;
            _petTab.clicked += ShowPets;
            SetSection(false);
        }

        public event Action OpenRequested;
        public event Action CloseRequested;
        public event Action UpgradeRequested;
        public event Action<string> PetEquipRequested;

        public void Dispose()
        {
            OpenButton.clicked -= RaiseOpen;
            CloseButton.clicked -= RaiseClose;
            UpgradeButton.clicked -= RaiseUpgrade;
            _weaponTab.clicked -= ShowWeapon;
            _petTab.clicked -= ShowPets;
        }

        public void SetSection(bool pets)
        {
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
        }

        public void RenderInventory(
            PlayerOwnedPetInventory inventory,
            string selectedPetId,
            string pendingPetId)
        {
            _petGrid.Clear();
            if (inventory == null) return;
            foreach (OwnedPetEntry entry in inventory.Entries)
            {
                OwnedPetEntry captured = entry;
                var tile = new Button { name = "pet-" + entry.Definition.PetId };
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
                tile.style.borderLeftColor = entry.RarityColor;
                tile.style.borderRightColor = entry.RarityColor;
                tile.style.borderTopColor = entry.RarityColor;
                tile.style.borderBottomColor = entry.RarityColor;
                var icon = new Image { sprite = entry.Definition.Icon };
                icon.AddToClassList("player-hub-pet-tile-icon");
                icon.pickingMode = PickingMode.Ignore;
                var rarity = new Label(entry.RarityName.ToUpperInvariant());
                rarity.AddToClassList("player-hub-pet-tile-rarity");
                rarity.style.color = entry.RarityColor;
                rarity.pickingMode = PickingMode.Ignore;
                tile.Add(icon);
                tile.Add(rarity);
                var badge = new Label(pending
                    ? "EQUIPPING"
                    : entry.IsEquipped ? "EQUIPPED" : string.Empty);
                badge.AddToClassList("player-hub-pet-tile-badge");
                badge.pickingMode = PickingMode.Ignore;
                badge.style.display = string.IsNullOrEmpty(badge.text)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
                tile.Add(badge);
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
            PetPreviewRarity.text = entry.RarityName.ToUpperInvariant();
            PetPreviewRarity.style.color = entry.RarityColor;
            PetPreviewName.text = definition.DisplayName.ToUpperInvariant();
            PetPreviewDescription.text = string.IsNullOrWhiteSpace(
                definition.AbilityRichText)
                ? "ABILITY DETAILS COMING SOON"
                : definition.AbilityRichText;
            PetPreviewState.text = pending
                ? "EQUIPPING…"
                : entry.IsEquipped ? "EQUIPPED" : "TAP TO EQUIP";
        }

        public void SetWeaponPresentation(
            WeaponAscensionCatalogDefinition.Tier tier)
        {
            Sprite icon = tier?.icon;
            _weaponSprite.sprite = icon;
            EquippedWeapon.sprite = icon;
            _weaponPlaceholder.style.display = icon == null
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void RaiseOpen() => OpenRequested?.Invoke();
        private void RaiseClose() => CloseRequested?.Invoke();
        private void RaiseUpgrade() => UpgradeRequested?.Invoke();
        private void ShowWeapon() => SetSection(false);
        private void ShowPets() => SetSection(true);

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return UiElementQuery.Require<T>(root, name, nameof(PlayerHubView));
        }
    }
}
