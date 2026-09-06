using System;
using System.Collections;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class PlayerHubPanelController : IDisposable
    {
        private readonly MonoBehaviour _host;
        private readonly PlayerSnapshot _player;
        private readonly FirestoreProgressionCommandStore _weaponStore;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly WeaponAscensionCatalogDefinition _weaponCatalog;
        private readonly PetGachaCatalogDefinition _petDefinition;
        private readonly PetGachaCatalog _petCatalog;
        private readonly IPetEquipCommandStore _petEquipStore;
        private readonly int _baseAttack;
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
            int baseAttack,
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
            _baseAttack = baseAttack;
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
            _panelHost.PanelClosed -= OnPanelClosed;
            _feedback.Dispose();
            _view.Dispose();
            if (_panelHost.OpenPanel == MainMenuPanelId.PlayerHub)
                _panelHost.TryClose(MainMenuPanelId.PlayerHub, _view.OpenButton);
        }

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            if (player == null) return;
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
            _view.OpenButton.SetEnabled(safe && !_busy);
            if (_panelHost.OpenPanel == MainMenuPanelId.PlayerHub && !_busy)
                Render();
        }

        private void Open()
        {
            if (_busy || !_panelHost.TryOpen(
                    MainMenuPanelId.PlayerHub,
                    _view.Modal,
                    _view.OpenButton)) return;
            SetSemanticState();
            _view.Status.text = string.Empty;
            Render();
            _feedback.StartIdle();
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
        }

        private void OnPanelClosed(MainMenuPanelId panelId)
        {
            if (panelId != MainMenuPanelId.PlayerHub) return;
            _feedback.StopIdle();
            _pendingWeaponTransactionId = string.Empty;
        }

        private void Render()
        {
            _view.Status.text = string.Empty;
            try
            {
                PlayerStatProjection stats = ProjectStats();
                RenderStats(stats);
                RenderWeapon(stats.Weapon);
                RenderPets();
            }
            catch (Exception exception) when (IsProjectionFailure(exception))
            {
                _view.Status.text = "Player data is unavailable: " + exception.Message;
                _view.UpgradeButton.SetEnabled(false);
                SetSemanticState("is-error");
            }
        }

        private PlayerStatProjection ProjectStats()
        {
            return PlayerStatProjectionFactory.Create(
                _player,
                _baseAttack,
                _baseWeaponAttack,
                _baseCriticalRate,
                _baseCriticalDamagePercent,
                _petCatalog);
        }

        private void RenderStats(PlayerStatProjection stats)
        {
            RenderSummary(stats);
            _view.EffectiveAttack.text = $"{stats.EffectiveAttack:N0} ATK";
            _view.AttackBreakdown.text = stats.HasConfiguredPetStats
                ? $"BASE {stats.BaseAttack:N0}  •  WEAPON {stats.Weapon.Attack:N0}  •  PET {stats.PetAttack:N0}"
                : $"BASE {stats.BaseAttack:N0}  •  WEAPON {stats.Weapon.Attack:N0}";
            _view.LegacyBonus.text =
                $"REBIRTH +{stats.LegacyBasisPoints / 100d:0.0}%  •  +{stats.LegacyBonusAttack:N0} ATK";
            _view.PetStatus.text = string.Empty;
        }

        private void RenderSummary(PlayerStatProjection stats)
        {
            if (_view.SummaryAttack != null)
                _view.SummaryAttack.text = stats.EffectiveAttack.ToString("N0");
        }

        private void RenderWeapon(WeaponAscensionStats current)
        {
            WeaponAscensionCatalogDefinition.Tier tier =
                _weaponCatalog?.Resolve(current.Level);
            string currentName = tier?.displayName ?? "Sword";
            _view.SetWeaponPresentation(tier);
            _view.WeaponName.text = $"{currentName.ToUpperInvariant()}  LV.{current.Level}";
            _view.WeaponCurrent.text =
                $"ATK {current.Attack:N0}\nCR +{current.CriticalRatePercent}%   CD +{current.CriticalDamagePercent}%";
            long coins = _player.wallet?.powerCoins ?? 0;
            _view.Balance.text = $"⚡ {coins:N0} POWER COINS";

            if (current.Level >= WeaponAscensionPolicy.MaximumLevel)
            {
                _view.WeaponNext.text = "MAXIMUM POWER REACHED";
                _view.WeaponCost.text = "NO FURTHER ASCENSION";
                _view.UpgradeButton.text = "MAX LEVEL";
                _view.UpgradeButton.SetEnabled(false);
                return;
            }

            WeaponAscensionStats next = WeaponAscensionPolicy.GetStats(
                current.Level + 1,
                _baseWeaponAttack);
            long cost = WeaponAscensionPolicy.GetNextCost(current.Level);
            WeaponAscensionCatalogDefinition.Tier nextTier =
                _weaponCatalog?.Resolve(next.Level);
            string nextName = nextTier?.displayName ?? currentName;
            _view.WeaponNext.text =
                $"{nextName.ToUpperInvariant()}  LV.{next.Level}\nATK {next.Attack:N0}   CR +{next.CriticalRatePercent}%   CD +{next.CriticalDamagePercent}%";
            _view.WeaponCost.text = $"{cost:N0} POWER COINS";
            _view.UpgradeButton.text = $"ASCEND  ⚡{cost:N0}";
            _view.UpgradeButton.SetEnabled(CanMutate(out _));
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
                _view.RenderPetPreview(
                    selected,
                    _busy && string.Equals(
                        _pendingPetId,
                        _selectedPetId,
                        StringComparison.Ordinal));
            if (inventory.TryGetOwned(inventory.EquippedPetId, out OwnedPetEntry equipped))
                _view.EquippedPet.sprite = equipped.Definition.Icon;
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

        private void Upgrade()
        {
            _feedback.PlayWeaponPress();
            if (!CanMutate(out string reason))
            {
                Warn(reason);
                return;
            }
            PlayerStatProjection current = ProjectStats();
            if (current.Weapon.Level >= WeaponAscensionPolicy.MaximumLevel) return;
            long cost = WeaponAscensionPolicy.GetNextCost(current.Weapon.Level);
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
            _sharedOverlay?.Publish(
                $"{name} reached Lv.{stats.Level} — ATK {stats.Attack:N0}",
                MainMenuNoticeKind.Success,
                milestone ? 3400 : 2200);
        }

        private void SelectAndEquipPet(string petId)
        {
            if (string.IsNullOrWhiteSpace(petId) || _busy) return;
            _selectedPetId = petId;
            _view.SetSection(true);
            _feedback.PlayPetPressed();
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
            _player.loadout = _player.loadout ?? new PlayerSnapshot.LoadoutData();
            _player.loadout.petId = petId;
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            Render();
            _sharedOverlay?.Publish("Pet equipped — saving…", MainMenuNoticeKind.Information);

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
            SetBusy(true, "Saving equipped pet…");
            RenderPets();
            bool success = false;
            PetEquipFailure failure = default;
            yield return _petEquipStore.Equip(
                new PetEquipCommand(transactionId, targetPetId, previewRevision),
                _ => success = true,
                value => failure = value);
            SetBusy(false);
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
            Render();
            SetSemanticState("is-success");
            _feedback.PlayPetSuccess();
            _sharedOverlay?.Publish("Pet equipped and saved.", MainMenuNoticeKind.Success);
        }

        private IEnumerator PublishLeaderboard()
        {
            if (_publisher == null) yield break;
            string warning = string.Empty;
            yield return _publisher.Publish(
                _player,
                () => { },
                message => warning = message);
            if (!string.IsNullOrEmpty(warning)) Debug.LogWarning(warning);
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
            if (!string.IsNullOrEmpty(message)) _view.Status.text = message;
        }

        private void Warn(string message)
        {
            _view.Status.text = message ?? string.Empty;
            SetSemanticState("is-error");
            _sharedOverlay?.Publish(
                string.IsNullOrWhiteSpace(message) ? "Action unavailable." : message,
                MainMenuNoticeKind.Warning);
        }

        private void HideInitially()
        {
            _view.Modal.EnableInClassList("is-hidden", true);
            _view.Modal.style.display = DisplayStyle.None;
        }

        private void SetSemanticState(string state = null)
        {
            _view.Modal.EnableInClassList("is-busy", state == "is-busy");
            _view.Modal.EnableInClassList("is-success", state == "is-success");
            _view.Modal.EnableInClassList("is-error", state == "is-error");
        }

        private static bool IsProjectionFailure(Exception exception)
        {
            return exception is InvalidOperationException ||
                exception is ArgumentOutOfRangeException ||
                exception is OverflowException;
        }
    }
}
