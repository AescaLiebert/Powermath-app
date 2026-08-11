using System;
using System.Collections;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class PlayerHubPanelController : IDisposable
    {
        private readonly MonoBehaviour _host;
        private readonly PlayerSnapshot _player;
        private readonly FirestoreProgressionCommandStore _store;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly WeaponAscensionCatalogDefinition _catalog;
        private readonly int _baseAttack;
        private readonly int _baseWeaponAttack;
        private readonly double _baseCriticalRate;
        private readonly double _baseCriticalDamagePercent;
        private readonly VisualElement _modal;
        private readonly Button _open;
        private readonly Button _close;
        private readonly Button _upgrade;
        private readonly Label _effectiveAttack;
        private readonly Label _attackBreakdown;
        private readonly Label _legacyBonus;
        private readonly Label _petStatus;
        private readonly Label _weaponName;
        private readonly Label _weaponCurrent;
        private readonly Label _weaponNext;
        private readonly Label _weaponCost;
        private readonly Label _balance;
        private readonly Label _status;
        private string _pendingTransactionId;
        private bool _busy;

        public PlayerHubPanelController(
            MonoBehaviour host,
            VisualElement root,
            PlayerSnapshot player,
            FirestoreProgressionCommandStore store,
            FirestoreLeaderboardProjectionPublisher publisher,
            WeaponAscensionCatalogDefinition catalog,
            int baseAttack,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _catalog = catalog;
            _baseAttack = baseAttack;
            _baseWeaponAttack = baseWeaponAttack;
            _baseCriticalRate = baseCriticalRate;
            _baseCriticalDamagePercent = baseCriticalDamagePercent;
            _modal = Require<VisualElement>(root, "player-hub-modal");
            _open = Require<Button>(root, "player-hub-button");
            _close = Require<Button>(root, "player-hub-close");
            _upgrade = Require<Button>(root, "player-hub-weapon-upgrade");
            _effectiveAttack = Require<Label>(root, "player-hub-effective-atk");
            _attackBreakdown = Require<Label>(root, "player-hub-atk-breakdown");
            _legacyBonus = Require<Label>(root, "player-hub-legacy-bonus");
            _petStatus = Require<Label>(root, "player-hub-pet-status");
            _weaponName = Require<Label>(root, "player-hub-weapon-name");
            _weaponCurrent = Require<Label>(root, "player-hub-weapon-current");
            _weaponNext = Require<Label>(root, "player-hub-weapon-next");
            _weaponCost = Require<Label>(root, "player-hub-weapon-cost");
            _balance = Require<Label>(root, "player-hub-balance");
            _status = Require<Label>(root, "player-hub-status");

            _open.clicked += Open;
            _close.clicked += Close;
            _upgrade.clicked += Upgrade;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;
            _open.text = "PLAYER HUB";
            Close();
            OnPlayerChanged(_player);
        }

        public void Dispose()
        {
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _open.clicked -= Open;
            _close.clicked -= Close;
            _upgrade.clicked -= Upgrade;
        }

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            if (player == null || _busy) return;
            _open.SetEnabled(!string.Equals(
                player.activeRun?.phase,
                "RunDefeat",
                StringComparison.Ordinal));
            if (_modal.resolvedStyle.display != DisplayStyle.None)
                Render();
        }

        private void Open()
        {
            if (_busy) return;
            _modal.style.display = DisplayStyle.Flex;
            _status.text = string.Empty;
            Render();
        }

        private void Close()
        {
            if (_busy) return;
            _modal.style.display = DisplayStyle.None;
            _pendingTransactionId = string.Empty;
        }

        private void Render()
        {
            _status.text = string.Empty;
            try
            {
                PlayerStatProjection stats = ProjectStats();
                RenderStats(stats);
                RenderWeapon(stats.Weapon);
            }
            catch (Exception exception) when (
                exception is InvalidOperationException ||
                exception is ArgumentOutOfRangeException ||
                exception is OverflowException)
            {
                _status.text = $"Player stats are unavailable: {exception.Message}";
                _upgrade.SetEnabled(false);
            }
        }

        private PlayerStatProjection ProjectStats()
        {
            return PlayerStatProjectionFactory.Create(
                _player,
                _baseAttack,
                _baseWeaponAttack,
                _baseCriticalRate,
                _baseCriticalDamagePercent);
        }

        private void RenderStats(PlayerStatProjection stats)
        {
            _effectiveAttack.text = $"{stats.EffectiveAttack:N0} ATK";
            _attackBreakdown.text = stats.HasConfiguredPetStats
                ? $"BASE {stats.BaseAttack:N0} + WEAPON {stats.Weapon.Attack:N0} + " +
                    $"PET {stats.PetAttack:N0} = {stats.PermanentAttackSubtotal:N0}"
                : $"BASE {stats.BaseAttack:N0} + WEAPON {stats.Weapon.Attack:N0} = " +
                    $"{stats.PermanentAttackSubtotal:N0}";
            _legacyBonus.text =
                $"+{stats.LegacyBasisPoints / 100d:0.0}% REBIRTH BONUS " +
                $"(+{stats.LegacyBonusAttack:N0} ATK)";
            _petStatus.text = stats.HasConfiguredPetStats
                ? $"PET ATK +{stats.PetAttack:N0}"
                : "PET ATK: NO STAT CONFIGURED";
        }

        private void RenderWeapon(WeaponAscensionStats current)
        {
            string currentName =
                _catalog?.Resolve(current.Level)?.displayName ?? "Sword";
            _weaponName.text = $"{currentName.ToUpperInvariant()}  LV.{current.Level}";
            _weaponCurrent.text =
                $"CURRENT   ATK {current.Attack:N0}   " +
                $"CR +{current.CriticalRatePercent}%   " +
                $"CD +{current.CriticalDamagePercent}%";
            long coins = _player.wallet?.powerCoins ?? 0;
            _balance.text = $"YOUR POWER COINS: {coins:N0}";

            if (current.Level >= WeaponAscensionPolicy.MaximumLevel)
            {
                _weaponNext.text = "NEXT   MAXIMUM LEVEL REACHED";
                _weaponCost.text = "NO FURTHER UPGRADE";
                _upgrade.text = "MAX LEVEL";
                _upgrade.SetEnabled(false);
                return;
            }

            WeaponAscensionStats next = WeaponAscensionPolicy.GetStats(
                current.Level + 1,
                _baseWeaponAttack);
            long cost = WeaponAscensionPolicy.GetNextCost(current.Level);
            string nextName =
                _catalog?.Resolve(next.Level)?.displayName ?? currentName;
            _weaponNext.text =
                $"NEXT   {nextName.ToUpperInvariant()} LV.{next.Level}   " +
                $"ATK {next.Attack:N0}   CR +{next.CriticalRatePercent}%   " +
                $"CD +{next.CriticalDamagePercent}%";
            _weaponCost.text = $"UPGRADE COST: {cost:N0} POWER COINS";
            _upgrade.text = $"UPGRADE FOR {cost:N0}";

            if (!CanUpgrade(out string reason))
            {
                _upgrade.SetEnabled(false);
                if (string.IsNullOrEmpty(_status.text)) _status.text = reason;
                return;
            }

            if (coins < cost)
            {
                _upgrade.SetEnabled(false);
                _status.text = $"Need {(cost - coins):N0} more Power Coins.";
                return;
            }

            _upgrade.SetEnabled(!_busy);
        }

        private bool CanUpgrade(out string reason)
        {
            reason = string.Empty;
            if (_busy)
            {
                reason = "Saving the current upgrade...";
                return false;
            }
            if (!string.IsNullOrEmpty(_player.activeRun?.committedAttemptId))
            {
                reason = "Finish the current question before upgrading.";
                return false;
            }
            if (string.Equals(
                    _player.activeRun?.phase,
                    "RunDefeat",
                    StringComparison.Ordinal))
            {
                reason = "Finish run settlement before upgrading.";
                return false;
            }
            return true;
        }

        private void Upgrade()
        {
            if (!CanUpgrade(out string reason))
            {
                _status.text = reason;
                return;
            }
            if (string.IsNullOrEmpty(_pendingTransactionId))
                _pendingTransactionId = Guid.NewGuid().ToString("N");
            _host.StartCoroutine(Ascend());
        }

        private IEnumerator Ascend()
        {
            _busy = true;
            _status.text = "Saving weapon upgrade to Firebase...";
            _upgrade.SetEnabled(false);
            _close.SetEnabled(false);
            WeaponAscensionStats stats = default;
            bool success = false;
            string failure = string.Empty;
            yield return _store.AscendWeapon(
                _pendingTransactionId,
                (value, _) =>
                {
                    stats = value;
                    success = true;
                },
                message => failure = message);
            _busy = false;
            _close.SetEnabled(true);
            if (!success)
            {
                Render();
                _status.text = failure;
                yield break;
            }

            yield return Publish();
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            _pendingTransactionId = string.Empty;
            Render();
            string name = _catalog?.Resolve(stats.Level)?.displayName ?? "Sword";
            _status.text =
                $"Saved: {name} reached Lv.{stats.Level} with {stats.Attack} Weapon ATK.";
        }

        private IEnumerator Publish()
        {
            string warning = string.Empty;
            yield return _publisher.Publish(
                _player,
                () => { },
                message => warning = message);
            if (!string.IsNullOrEmpty(warning)) Debug.LogWarning(warning);
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"Main Menu UI is missing '{name}'.");
        }
    }
}
