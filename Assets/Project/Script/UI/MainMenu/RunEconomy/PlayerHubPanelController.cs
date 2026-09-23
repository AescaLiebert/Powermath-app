using System;
using System.Collections;
using System.Globalization;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using PowerMath.Localization;
using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class PlayerHubPanelController : IDisposable
    {
        public event Action TutorialPanelOpened;
        public event Action TutorialWeaponAscendSucceeded;
        public event Action TutorialPetsSectionShown;

        private readonly MonoBehaviour _host;
        private PlayerSnapshot _player;
        private readonly FirestoreProgressionCommandStore _weaponStore;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly WeaponAscensionCatalogDefinition _weaponCatalog;
        private readonly PetGachaCatalogDefinition _petDefinition;
        private readonly PetGachaCatalog _petCatalog;
        private readonly IPetEquipCommandStore _petEquipStore;
        private readonly int _baseWeaponAttack;
        private readonly double _baseCriticalRate;
        private readonly double _baseCriticalDamagePercent;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly MainMenuSharedOverlayController _sharedOverlay;
        private readonly PlayerHubView _view;
        private readonly PlayerHubFeedbackPlayer _feedback;
        private string _pendingWeaponTransactionId;
        private string _pendingPetTransactionId;
        private string _pendingPetId;
        private string _selectedPetId;
        private bool _busy;
        private int _lastRenderedStarCount = -1;
        private string _renderedCharacterId;
        private bool _wasPlayerMenuUnlocked;

        public PlayerHubPanelController(
            MonoBehaviour host,
            VisualElement root,
            PlayerSnapshot player,
            FirestoreProgressionCommandStore weaponStore,
            FirestoreLeaderboardProjectionPublisher publisher,
            WeaponAscensionCatalogDefinition weaponCatalog,
            PetGachaCatalogDefinition petDefinition,
            PetGachaCatalog petCatalog,
            IPetEquipCommandStore petEquipStore,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            AudioSource audioSource,
            IUiMotionDriver motionDriver,
            IMainMenuPanelHost panelHost,
            MainMenuSharedOverlayController sharedOverlay)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _weaponStore = weaponStore;
            _publisher = publisher;
            _weaponCatalog = weaponCatalog;
            _petDefinition = petDefinition;
            _petCatalog = petCatalog;
            _petEquipStore = petEquipStore;
            _baseWeaponAttack = baseWeaponAttack;
            _baseCriticalRate = baseCriticalRate;
            _baseCriticalDamagePercent = baseCriticalDamagePercent;
            _panelHost = panelHost ?? throw new ArgumentNullException(nameof(panelHost));
            _sharedOverlay = sharedOverlay;
            _view = new PlayerHubView(root ?? throw new ArgumentNullException(nameof(root)));
            PlayerHubJuiceProfileDefinition profile =
                Resources.Load<PlayerHubJuiceProfileDefinition>("PlayerHubJuiceProfile");
            _feedback = new PlayerHubFeedbackPlayer(
                _view,
                profile,
                audioSource,
                motionDriver);

            _view.OpenRequested += Open;
            _view.CloseRequested += Close;
            _view.UpgradeRequested += Upgrade;
            _view.PetEquipRequested += SelectAndEquipPet;
            _view.PetsSectionShown += OnPetsSectionShown;
            _panelHost.PanelClosed += OnPanelClosed;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;
            _view.OpenButton.tooltip = "Player Hub";
            HideInitially();
            OnPlayerChanged(_player);
        }

        public void Dispose()
        {
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _view.OpenRequested -= Open;
            _view.CloseRequested -= Close;
            _view.UpgradeRequested -= Upgrade;
            _view.PetEquipRequested -= SelectAndEquipPet;
            _view.PetsSectionShown -= OnPetsSectionShown;
            _panelHost.PanelClosed -= OnPanelClosed;
            _feedback.Dispose();
            _view.Dispose();
            if (_panelHost.OpenPanel == MainMenuPanelId.PlayerHub)
                _panelHost.TryClose(MainMenuPanelId.PlayerHub, _view.OpenButton);
        }

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            if (player == null) return;
            _player = player;
            RenderCharacterAvatar();
            try
            {
                RenderSummary(ProjectStats());
            }
            catch (Exception exception) when (IsProjectionFailure(exception))
            {
                if (_view.SummaryAttack != null) _view.SummaryAttack.text = "—";
            }
            bool safe = !string.Equals(
                player.activeRun?.phase,
                "RunDefeat",
                StringComparison.Ordinal);

            bool hubAvailable = PlayerMenuUnlockPolicy.IsHubAndGachaUnlocked(player);
            if (!hubAvailable)
            {
                _view.OpenButton.SetEnabled(true);
                _view.OpenButton.pickingMode = PickingMode.Position;
                _view.OpenButton.tooltip =
                    "Unlocks after reaching Stage 31 or completing a run settlement.";
                _view.OpenButton.AddToClassList("is-feature-locked");
                _view.LockOverlay?.RemoveFromClassList("is-hidden");
                if (_view.LockOverlay != null)
                    _view.LockOverlay.style.display = DisplayStyle.Flex;
                _wasPlayerMenuUnlocked = false;
            }
            else
            {
                _view.OpenButton.SetEnabled(safe && !_busy);
                _view.OpenButton.pickingMode = PickingMode.Position;
                _view.OpenButton.tooltip = "Player Hub";
                _view.OpenButton.RemoveFromClassList("is-feature-locked");
                _view.LockOverlay?.AddToClassList("is-hidden");
                if (_view.LockOverlay != null)
                    _view.LockOverlay.style.display = DisplayStyle.None;
                if (!_wasPlayerMenuUnlocked)
                {
                    _view.OpenButton.AddToClassList("is-unlocking");
                    _view.OpenButton.schedule.Execute(() =>
                        _view.OpenButton.RemoveFromClassList("is-unlocking")).StartingIn(500);
                }
                _wasPlayerMenuUnlocked = true;
            }

            if (_panelHost.OpenPanel == MainMenuPanelId.PlayerHub && !_busy)
                Render();
        }

        private void Open()
        {
            if (!PlayerMenuUnlockPolicy.IsHubAndGachaUnlocked(_player))
            {
                StatusMessageService.ShowWarning(
                    "Reach Stage 31 or complete a run settlement to unlock Player Hub.");
                return;
            }

            if (_busy ||
                !string.IsNullOrEmpty(_player.activeRun?.committedAttemptId) ||
                !_panelHost.TryOpen(
                    MainMenuPanelId.PlayerHub,
                    _view.Modal,
                    _view.OpenButton)) return;
            SetSemanticState();
            SetStatus(string.Empty);
            Render();
            _feedback.StartIdle();
            TutorialPanelOpened?.Invoke();
        }

        public bool TryOpenForTutorial()
        {
            if (_busy || _panelHost.OpenPanel != MainMenuPanelId.None) return false;
            Open();
            return _panelHost.OpenPanel == MainMenuPanelId.PlayerHub;
        }

        public bool TryAscendForTutorial()
        {
            if (_busy || _panelHost.OpenPanel != MainMenuPanelId.PlayerHub) return false;
            if (!CanMutate(out _)) return false;
            PlayerStatProjection current = ProjectStats();
            if (current.Weapon.Level >= MaximumWeaponLevel) return false;
            long cost = WeaponAscensionPolicy.GetNextCost(
                current.Weapon.Level, MaximumWeaponLevel);
            if ((_player.wallet?.powerCoins ?? 0) < cost) return false;
            Upgrade();
            return true;
        }

        public bool TryShowPetsForTutorial()
        {
            if (_busy || _panelHost.OpenPanel != MainMenuPanelId.PlayerHub) return false;
            _view.SetSection(true);
            return true;
        }

        private void Close()
        {
            if (_busy) return;
            _feedback.StopIdle();
            if (_panelHost.OpenPanel == MainMenuPanelId.PlayerHub)
                _panelHost.TryClose(MainMenuPanelId.PlayerHub, _view.OpenButton);
            else if (_panelHost.OpenPanel == MainMenuPanelId.None)
                HideInitially();
            _pendingWeaponTransactionId = string.Empty;
            _lastRenderedStarCount = -1;
        }

        private void OnPanelClosed(MainMenuPanelId panelId)
        {
            if (panelId != MainMenuPanelId.PlayerHub) return;
            _feedback.StopIdle();
            _feedback.StopPetPreviewTweens();
            _pendingWeaponTransactionId = string.Empty;
            _lastRenderedStarCount = -1;
            _view.HidePetPreview();
            _view.Modal.style.display = DisplayStyle.None;
            _view.Modal.style.visibility = Visibility.Hidden;
            _view.Modal.EnableInClassList("is-hidden", true);
        }

        private void Render()
        {
            SetStatus(string.Empty);
            _view.Balance.text = (_player?.wallet?.powerCoins ?? 0).ToString("N0");
            try
            {
                RenderCharacterAvatar();
                PlayerStatProjection stats = ProjectStats();
                RenderStats(stats);
                RenderWeapon(stats);
                RenderPets();
            }
            catch (Exception exception) when (IsProjectionFailure(exception))
            {
                StatusMessageService.ShowError("Player data is unavailable: " + exception.Message);
                _view.UpgradeButton.SetEnabled(false);
                SetSemanticState("is-error");
            }
        }

        private PlayerStatProjection ProjectStats()
        {
            return PlayerStatProjectionFactory.Create(
                _player,
                _baseWeaponAttack,
                _baseCriticalRate,
                _baseCriticalDamagePercent,
                _petCatalog);
        }

        private void RenderStats(PlayerStatProjection stats)
        {
            RenderSummary(stats);
            _view.EffectiveAttack.text = $"{stats.EffectiveAttack:N0} ATK";
            if (stats.HasConfiguredPetStats)
            {
                string petText = stats.PetMultiplierPercent > 0d
                    ? $"  -  PET +{stats.PetFlatAttack:N0} / ×{1d + stats.PetMultiplierPercent / 100d:0.##}"
                    : $"  -  PET +{stats.PetFlatAttack:N0}";
                _view.AttackBreakdown.text = $"WEAPON {stats.Weapon.Attack:N0}{petText}";
            }
            else
            {
                _view.AttackBreakdown.text = $"WEAPON {stats.Weapon.Attack:N0}";
            }
            _view.LegacyBonus.text =
                $"REBIRTH +{stats.LegacyBasisPoints / 100d:0.0}%  -  +{stats.LegacyBonusAttack:N0} ATK";
            _view.PetStatus.text = string.Empty;

            _view.CompactAttack.text = stats.EffectiveAttack.ToString("N0", CultureInfo.InvariantCulture);
            _view.CompactCritRate.text = $"{stats.CriticalRate * 100d:0.##}%";
            _view.CompactCritDamage.text = $"{stats.CriticalDamagePercent:0.##}%";
            _view.CompactPetAttack.text = stats.PetStats.EffectivePetAttack.ToString("N0", CultureInfo.InvariantCulture);
            _view.CompactLuck.text = $"{stats.PetStats.TotalEncounterLuckPercent:0.##}%";
            _view.CompactCoinBonus.text = $"{stats.PetStats.TotalPowerCoinBonusPercent:0.##}%";
        }

        private void RenderSummary(PlayerStatProjection stats)
        {
            if (_view.SummaryAttack != null)
                _view.SummaryAttack.text = stats.EffectiveAttack.ToString("N0");
        }

        private void RenderWeapon(PlayerStatProjection projection)
        {
            WeaponAscensionStats current = projection.Weapon;
            WeaponAscensionCatalogDefinition.Tier tier =
                _weaponCatalog?.Resolve(current.Level);
            string currentName = tier?.displayName ?? "Sword";
            _view.SetWeaponPresentation(tier);
            _view.WeaponName.text = currentName.ToUpperInvariant();
            _view.WeaponName.tooltip = $"Global weapon level {current.Level}";
            _view.WeaponCurrent.text =
                $"ATK {current.Attack:N0}\nCR +{current.CriticalRatePercent}%   CD +{current.CriticalDamagePercent}%";

            int maxLevel = MaximumWeaponLevel;
            int levelsPerTier = _weaponCatalog?.LevelsPerTier ??
                WeaponAscensionPolicy.DefaultLevelsPerTier;
            int currentSubLevel = _weaponCatalog != null
                ? _weaponCatalog.GetSubLevelInTier(current.Level)
                : GetFallbackSubLevel(current.Level, maxLevel, levelsPerTier);
            string pips = BuildPips(currentSubLevel, levelsPerTier);
            _view.WeaponSubLevel.text = pips;
            _view.EquippedWeaponSubLevel.text = pips;

            int starCount = ResolveStarCount(tier, current.Level);
            string starText = ResolveStarString(starCount);
            if (_view.StarLabel != null) _view.StarLabel.text = starText;
            if (_view.EquippedStarLabel != null) _view.EquippedStarLabel.text = starText;

            if (_lastRenderedStarCount > 0 && starCount > _lastRenderedStarCount)
            {
                _feedback.PlayStarIncrease();
            }
            _lastRenderedStarCount = starCount;

            _view.CurrentAttack.text = current.Attack.ToString("N0", CultureInfo.InvariantCulture);
            _view.CurrentCritRate.text = $"{current.CriticalRatePercent:0.##}%";
            _view.CurrentCritDamage.text = $"{current.CriticalDamagePercent:0.##}%";

            long coins = _player.wallet?.powerCoins ?? 0;
            _view.Balance.text = coins.ToString("N0");

            if (current.Level >= maxLevel)
            {
                _view.WeaponNext.text = PowerMath.Localization.LocalizationService.Get("menu.maxPower");
                _view.WeaponCost.text = PowerMath.Localization.LocalizationService.Get("menu.noAscension");
                _view.UpgradeButton.text = PowerMath.Localization.LocalizationService.Get("menu.maxLevel");
                _view.LevelTransition.text = "MAX";
                _view.AscendSubtitle.text = "MAXIMUM FORM AWAKENED";
                _view.NextAttack.text = "MAX";
                _view.NextCritRate.text = "MAX";
                _view.NextCritDamage.text = "MAX";
                _view.UpgradeButton.SetEnabled(false);
                UpdateStatRowVisibility(current.CriticalRatePercent > 0, current.CriticalDamagePercent > 0);
                return;
            }

            WeaponAscensionStats next = WeaponAscensionPolicy.GetStats(
                current.Level + 1,
                _baseWeaponAttack,
                maxLevel);
            long cost = WeaponAscensionPolicy.GetNextCost(current.Level, maxLevel);
            WeaponAscensionCatalogDefinition.Tier nextTier =
                _weaponCatalog?.Resolve(next.Level);
            string nextName = nextTier?.displayName ?? currentName;
            _view.WeaponNext.text =
                $"{nextName.ToUpperInvariant()}  LV.{next.Level}\nATK {next.Attack:N0}   CR +{next.CriticalRatePercent}%   CD +{next.CriticalDamagePercent}%";
            _view.NextAttack.text = next.Attack.ToString("N0", CultureInfo.InvariantCulture);
            _view.NextCritRate.text = $"{next.CriticalRatePercent:0.##}%";
            _view.NextCritDamage.text = $"{next.CriticalDamagePercent:0.##}%";

            bool awakensNextForm = _weaponCatalog != null
                ? _weaponCatalog.IsMilestoneAwakening(next.Level)
                : next.Level > 0 && next.Level % levelsPerTier == 0;
            _view.LevelTransition.text = $"Lv.{current.Level} → Lv.{next.Level}";
            _view.AscendSubtitle.text = awakensNextForm
                ? $"AWAKEN {nextName.ToUpperInvariant()}"
                : "POWER UP CURRENT FORM";
            _view.WeaponCost.text = cost.ToString("N0", CultureInfo.InvariantCulture);
            _view.UpgradeButton.text = "Upgrade";
            _view.UpgradeButton.SetEnabled(CanMutate(out _));

            bool showCritRate = current.CriticalRatePercent > 0 || next.CriticalRatePercent > 0;
            bool showCritDamage = current.CriticalDamagePercent > 0 || next.CriticalDamagePercent > 0;
            UpdateStatRowVisibility(showCritRate, showCritDamage);
        }

        private void UpdateStatRowVisibility(bool showCritRate, bool showCritDamage)
        {
            if (_view.RowAttack != null)
                _view.RowAttack.style.display = DisplayStyle.Flex;
            if (_view.RowCritRate != null)
                _view.RowCritRate.style.display = showCritRate ? DisplayStyle.Flex : DisplayStyle.None;
            if (_view.RowCritDamage != null)
                _view.RowCritDamage.style.display = showCritDamage ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static int ResolveStarCount(WeaponAscensionCatalogDefinition.Tier tier, int level)
        {
            if (tier != null && !string.IsNullOrEmpty(tier.milestoneFeedbackKey))
            {
                string key = tier.milestoneFeedbackKey;
                if (key.IndexOf("t1", StringComparison.OrdinalIgnoreCase) >= 0) return 5;
                if (key.IndexOf("t2", StringComparison.OrdinalIgnoreCase) >= 0) return 3;
                if (key.IndexOf("t3", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            }
            if (level >= 85) return 5;
            if (level >= 45) return 3;
            return 1;
        }

        public static string ResolveStarString(int starCount)
        {
            return new string('★', Mathf.Clamp(starCount, 1, 5));
        }

        private static int GetFallbackSubLevel(
            int level,
            int maximumLevel,
            int levelsPerTier)
        {
            if (level <= 0) return 0;
            if (maximumLevel > 0 && level >= maximumLevel) return levelsPerTier;
            return level % levelsPerTier;
        }

        private static string BuildPips(int filled, int total)
        {
            int safeTotal = Math.Max(1, total);
            int safeFilled = Math.Max(0, Math.Min(filled, safeTotal));
            var pips = new string[safeTotal];
            for (int index = 0; index < safeTotal; index++)
                pips[index] = index < safeFilled ? "◆" : "◇";
            return string.Join(" ", pips);
        }

        private void RenderPets()
        {
            if (!PlayerOwnedPetInventory.TryCreate(
                    _player,
                    _petDefinition,
                    out PlayerOwnedPetInventory inventory,
                    out string error))
            {
                _view.PetPreviewState.text = error;
                _view.EquippedPet.sprite = null;
                _view.HidePetPreview();
                _feedback.StopPetPreviewTweens();
                return;
            }

            if (string.IsNullOrEmpty(_selectedPetId) ||
                !inventory.TryGetOwned(_selectedPetId, out _))
            {
                _selectedPetId = !string.IsNullOrEmpty(inventory.EquippedPetId)
                    ? inventory.EquippedPetId
                    : inventory.Entries.Count > 0
                        ? inventory.Entries[0].Definition.PetId
                        : string.Empty;
            }
            _view.RenderInventory(
                inventory,
                _selectedPetId,
                _busy ? _pendingPetId : string.Empty);
            if (inventory.TryGetOwned(_selectedPetId, out OwnedPetEntry selected))
            {
                _view.RenderPetPreview(
                    selected,
                    _busy && string.Equals(
                        _pendingPetId,
                        _selectedPetId,
                        StringComparison.Ordinal));
            }
            else
            {
                _view.HidePetPreview();
                _feedback.StopPetPreviewTweens();
            }
            if (inventory.TryGetOwned(inventory.EquippedPetId, out OwnedPetEntry equipped))
            {
                if (equipped.Definition.Icon != null)
                {
                    _view.EquippedPet.sprite = equipped.Definition.Icon;
                }
                else if (!string.IsNullOrEmpty(equipped.Definition.IconAddressableKey))
                {
                    UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(equipped.Definition.IconAddressableKey).Completed += handle =>
                    {
                        if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && handle.Result != null)
                        {
                            if (_view?.EquippedPet != null) _view.EquippedPet.sprite = handle.Result;
                        }
                    };
                }
                else
                {
                    _view.EquippedPet.sprite = null;
                }
            }
            else
                _view.EquippedPet.sprite = null;
        }

        private bool CanMutate(out string reason)
        {
            reason = string.Empty;
            if (_busy)
            {
                reason = "Saving your last choice…";
                return false;
            }
            if (!string.IsNullOrEmpty(_player.activeRun?.committedAttemptId))
            {
                reason = "Finish the current question first.";
                return false;
            }
            if (string.Equals(_player.activeRun?.phase, "RunDefeat", StringComparison.Ordinal))
            {
                reason = "Finish run settlement first.";
                return false;
            }
            return true;
        }

        private int MaximumWeaponLevel => _weaponCatalog != null && _weaponCatalog.MaximumLevel > 0
            ? _weaponCatalog.MaximumLevel
            : WeaponAscensionPolicy.DefaultMaximumLevel;

        private void Upgrade()
        {
            _feedback.PlayWeaponPress();
            if (!CanMutate(out string reason))
            {
                Warn(reason);
                return;
            }
            PlayerStatProjection current = ProjectStats();
            int maxLevel = MaximumWeaponLevel;
            if (current.Weapon.Level >= maxLevel) return;
            long cost = WeaponAscensionPolicy.GetNextCost(current.Weapon.Level, maxLevel);
            long coins = _player.wallet?.powerCoins ?? 0;
            if (coins < cost)
            {
                string message = $"Need {(cost - coins):N0} more Power Coins.";
                Warn(message);
                _feedback.PlayInsufficient();
                return;
            }
            if (string.IsNullOrEmpty(_pendingWeaponTransactionId))
                _pendingWeaponTransactionId = Guid.NewGuid().ToString("N");
            _host.StartCoroutine(Ascend(current.Weapon.Level));
        }

        private IEnumerator Ascend(int previousLevel)
        {
            if (_weaponStore == null)
            {
                Warn("Weapon ascension is not available in offline mode.");
                _feedback.PlayInsufficient();
                yield break;
            }

            SetBusy(true, "Forging weapon ascension…");
            WeaponAscensionStats stats = default;
            bool success = false;
            string failure = string.Empty;
            yield return _weaponStore.AscendWeapon(
                _pendingWeaponTransactionId,
                (value, _) => { stats = value; success = true; },
                message => failure = message);
            SetBusy(false);
            if (!success)
            {
                Render();
                Warn(failure);
                _feedback.PlayInsufficient();
                yield break;
            }

            yield return PublishLeaderboard();
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            _pendingWeaponTransactionId = string.Empty;
            WeaponAscensionCatalogDefinition.Tier previousTier =
                _weaponCatalog?.Resolve(previousLevel);
            WeaponAscensionCatalogDefinition.Tier newTier =
                _weaponCatalog?.Resolve(stats.Level);
            bool milestone = newTier != null &&
                !ReferenceEquals(previousTier, newTier);
            Render();
            SetSemanticState("is-success");
            _feedback.PlayWeaponSuccess(milestone, newTier?.displayName);
            string name = newTier?.displayName ?? "Sword";
            StatusMessageService.ShowSuccess(
                $"{name} reached Lv.{stats.Level} — ATK {stats.Attack:N0}",
                milestone ? 3400 : 2200);
            TutorialWeaponAscendSucceeded?.Invoke();
        }

        private void OnPetsSectionShown()
        {
            TutorialPetsSectionShown?.Invoke();
            if (!string.IsNullOrEmpty(_selectedPetId))
            {
                _feedback.PlayPetPreviewEntrance();
            }
            else
            {
                _view.HidePetPreview();
                _feedback.StopPetPreviewTweens();
            }
        }

        private void SelectAndEquipPet(string petId)
        {
            if (string.IsNullOrWhiteSpace(petId) || _busy) return;
            _feedback.PlayPetPressed();
            _selectedPetId = petId;
            _view.SetSection(true);
            if (!CanMutate(out string reason))
            {
                Warn(reason);
                return;
            }
            if (_petEquipStore == null)
            {
                Warn("Pet equipment is unavailable right now.");
                _feedback.PlayPetFailure();
                return;
            }
            if (!PlayerOwnedPetInventory.TryCreate(
                    _player,
                    _petDefinition,
                    out PlayerOwnedPetInventory inventory,
                    out string error) ||
                !inventory.TryGetOwned(petId, out _))
            {
                Warn(string.IsNullOrEmpty(error)
                    ? "Only owned pets can be equipped."
                    : error);
                _feedback.PlayPetFailure();
                return;
            }

            string previousPetId = _player.loadout?.petId ?? string.Empty;
            if (string.Equals(previousPetId, petId, StringComparison.Ordinal))
            {
                RenderPets();
                _feedback.PlayPetPreviewEntrance();
                return;
            }

            _player.loadout = _player.loadout ?? new PlayerSnapshot.LoadoutData();
            _player.loadout.petId = petId;
            Render();
            _feedback.PlayPetPreviewEntrance();

            if (!string.Equals(_pendingPetId, petId, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(_pendingPetTransactionId))
            {
                _pendingPetId = petId;
                _pendingPetTransactionId = Guid.NewGuid().ToString("N");
            }
            _host.StartCoroutine(EquipPet(previousPetId, _player.revision));
        }

        private IEnumerator EquipPet(string previousPetId, long previewRevision)
        {
            string transactionId = _pendingPetTransactionId;
            string targetPetId = _pendingPetId;
            bool success = false;
            PetEquipFailure failure = default;
            yield return _petEquipStore.Equip(
                new PetEquipCommand(transactionId, targetPetId, previewRevision),
                _ => success = true,
                value => failure = value);
            if (!success)
            {
                _player.loadout = _player.loadout ?? new PlayerSnapshot.LoadoutData();
                if (failure.Code != PetEquipFailureCode.StaleState)
                    _player.loadout.petId = previousPetId;
                PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
                if (failure.Code != PetEquipFailureCode.RecoverableTransport)
                {
                    _pendingPetId = string.Empty;
                    _pendingPetTransactionId = string.Empty;
                }
                Render();
                Warn(failure.Message);
                _feedback.PlayPetFailure();
                yield break;
            }

            _pendingPetId = string.Empty;
            _pendingPetTransactionId = string.Empty;
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            RenderPets();
            SetSemanticState("is-success");
            _feedback.PlayPetSuccess();
        }

        private IEnumerator PublishLeaderboard()
        {
            if (_publisher == null) yield break;
            string warning = string.Empty;
            yield return _publisher.Publish(
                _player,
                () => { },
                message => warning = message);
            if (!string.IsNullOrEmpty(warning)) PowerMath.Diagnostics.AppLog.Warning("PlayerHub", warning);
        }

        private void SetBusy(bool busy, string message = null)
        {
            _busy = busy;
            SetSemanticState(busy ? "is-busy" : null);
            _view.UpgradeButton.SetEnabled(!busy);
            _view.SetInteractionEnabled(!busy);
            _view.OpenButton.SetEnabled(!busy && !string.Equals(
                _player.activeRun?.phase,
                "RunDefeat",
                StringComparison.Ordinal));
            _sharedOverlay?.SetBackEnabled(!busy);
        }

        private void Warn(string message)
        {
            SetSemanticState("is-error");
            StatusMessageService.ShowWarning(
                string.IsNullOrWhiteSpace(message) ? "Action unavailable." : message);
        }

        private void SetStatus(string message)
        {
            if (_view.Status != null)
            {
                _view.Status.text = string.Empty;
                _view.Status.style.display = DisplayStyle.None;
            }
        }

        private void HideInitially()
        {
            _view.Modal.EnableInClassList("is-hidden", true);
            _view.Modal.style.display = DisplayStyle.None;
            _view.HidePetPreview();
            _feedback.StopPetPreviewTweens();
        }

        private void SetSemanticState(string state = null)
        {
            _view.Modal.EnableInClassList("is-busy", state == "is-busy");
            _view.Modal.EnableInClassList("is-success", state == "is-success");
            _view.Modal.EnableInClassList("is-error", state == "is-error");
        }

        private void RenderCharacterAvatar()
        {
            if (_view?.PlayerAvatar == null) return;
            string id = _player?.profile?.characterId;
            if (!PlayerLifecyclePolicy.IsCharacter(id)) id = "ricko";
            if (string.Equals(_renderedCharacterId, id, StringComparison.Ordinal)) return;
            var catalog = CharacterPresentationCatalog.Load();
            var definition = catalog?.Find(id);
            Sprite sprite = CharacterPlaceholderSprites.Resolve(definition?.hubSprite, id);
            if (sprite != null)
            {
                _view.PlayerAvatar.style.backgroundImage = new StyleBackground(sprite);
                _renderedCharacterId = id;
            }
        }

        private static bool IsProjectionFailure(Exception exception)
        {
            return exception is InvalidOperationException ||
                exception is ArgumentOutOfRangeException ||
                exception is OverflowException;
        }
    }
}
