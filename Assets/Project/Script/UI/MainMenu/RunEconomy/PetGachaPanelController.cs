using System;
using System.Collections;
using System.Collections.Generic;
using PowerMath.Gameplay.Pets;
using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class PetGachaPanelController : IDisposable
    {
        private const int ResultReadableHoldMilliseconds = 600;

        private readonly MonoBehaviour _host;
        private readonly PlayerSnapshot _player;
        private readonly PetGachaCatalogDefinition _definition;
        private readonly PetGachaCatalog _catalog;
        private readonly IPetGachaCommandStore _store;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly AudioSource _audio;
        private readonly bool _reducedMotion;
        private readonly string _unavailableReason;
        private readonly PetGachaProbabilityCalculator _calculator =
            new PetGachaProbabilityCalculator();

        private readonly Button _open;
        private readonly VisualElement _modal;
        private readonly Button _close;
        private readonly Label _balance;
        private readonly Label _cost;
        private readonly Label _projectedBalance;
        private readonly Label _catalogStatus;
        private readonly ScrollView _oddsList;
        private readonly Label _warning;
        private readonly Label _status;
        private readonly Button _pull;
        private readonly VisualElement _confirmation;
        private readonly Label _confirmationSummary;
        private readonly Button _cancel;
        private readonly Button _confirm;
        private readonly VisualElement _result;
        private readonly VisualElement _resultIcon;
        private readonly Label _resultRarity;
        private readonly Label _resultName;
        private readonly Label _resultState;
        private readonly Label _resultBalance;
        private readonly Button _continue;

        private string _pendingTransactionId;
        private long _previewRevision;
        private bool _busy;
        private bool _committed;

        public PetGachaPanelController(
            MonoBehaviour host,
            VisualElement root,
            PlayerSnapshot player,
            PetGachaCatalogDefinition definition,
            PetGachaCatalog catalog,
            IPetGachaCommandStore store,
            FirestoreLeaderboardProjectionPublisher publisher,
            AudioSource audio,
            bool reducedMotion,
            string unavailableReason)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _definition = definition;
            _catalog = catalog;
            _store = store;
            _publisher = publisher;
            _audio = audio;
            _reducedMotion = reducedMotion;
            _unavailableReason = unavailableReason ?? string.Empty;

            _open = Require<Button>(root, "pet-gacha-button");
            _modal = Require<VisualElement>(root, "pet-gacha-modal");
            _close = Require<Button>(root, "pet-gacha-close");
            _balance = Require<Label>(root, "pet-gacha-balance");
            _cost = Require<Label>(root, "pet-gacha-cost");
            _projectedBalance = Require<Label>(root, "pet-gacha-projected-balance");
            _catalogStatus = Require<Label>(root, "pet-gacha-catalog-status");
            _oddsList = Require<ScrollView>(root, "pet-gacha-odds-list");
            _warning = Require<Label>(root, "pet-gacha-warning");
            _status = Require<Label>(root, "pet-gacha-status");
            _pull = Require<Button>(root, "pet-gacha-pull");
            _confirmation = Require<VisualElement>(root, "pet-gacha-confirmation");
            _confirmationSummary = Require<Label>(root, "pet-gacha-confirmation-summary");
            _cancel = Require<Button>(root, "pet-gacha-cancel");
            _confirm = Require<Button>(root, "pet-gacha-confirm");
            _result = Require<VisualElement>(root, "pet-gacha-result");
            _resultIcon = Require<VisualElement>(root, "pet-gacha-result-icon");
            _resultRarity = Require<Label>(root, "pet-gacha-result-rarity");
            _resultName = Require<Label>(root, "pet-gacha-result-name");
            _resultState = Require<Label>(root, "pet-gacha-result-state");
            _resultBalance = Require<Label>(root, "pet-gacha-result-balance");
            _continue = Require<Button>(root, "pet-gacha-continue");

            _open.clicked += Open;
            _close.clicked += Close;
            _pull.clicked += BeginConfirmation;
            _cancel.clicked += CancelConfirmation;
            _confirm.clicked += Confirm;
            _continue.clicked += Continue;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;

            _open.text = "PET GACHA";
            CloseImmediate();
            RefreshAvailability();
        }

        public void Dispose()
        {
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _open.clicked -= Open;
            _close.clicked -= Close;
            _pull.clicked -= BeginConfirmation;
            _cancel.clicked -= CancelConfirmation;
            _confirm.clicked -= Confirm;
            _continue.clicked -= Continue;
        }

        private bool IsConfigured => _definition != null && _catalog != null && _store != null;

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            if (player == null || _busy) return;
            RefreshAvailability();
            if (_modal.resolvedStyle.display != DisplayStyle.None && !_committed)
                RenderPreview();
        }

        private void RefreshAvailability()
        {
            bool safe = string.IsNullOrEmpty(_player.activeRun?.committedAttemptId) &&
                !string.Equals(_player.activeRun?.phase, "RunDefeat", StringComparison.Ordinal);
            _open.SetEnabled(IsConfigured && safe && !_busy);
            _open.tooltip = IsConfigured
                ? safe
                    ? "Spend Power Coins on one transparent pet pull."
                    : "Finish the current question or run settlement first."
                : string.IsNullOrEmpty(_unavailableReason)
                    ? "Pet Gacha content is not configured."
                    : _unavailableReason;
        }

        private void Open()
        {
            if (!IsConfigured || _busy) return;
            _modal.style.display = DisplayStyle.Flex;
            _confirmation.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
            _status.text = string.Empty;
            _committed = false;
            _pendingTransactionId = string.Empty;
            RenderPreview();
        }

        private void Close()
        {
            if (_busy || _committed) return;
            CloseImmediate();
        }

        private void CloseImmediate()
        {
            _modal.style.display = DisplayStyle.None;
            _confirmation.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
            _pendingTransactionId = string.Empty;
            _committed = false;
        }

        private void RenderPreview()
        {
            if (!IsConfigured)
            {
                _status.text = _unavailableReason;
                _pull.SetEnabled(false);
                return;
            }

            _previewRevision = Math.Max(0, _player.revision);
            long coins = Math.Max(0, _player.wallet?.powerCoins ?? 0);
            _balance.text = $"YOUR POWER COINS: {coins:N0}";
            _cost.text = $"ONE PULL: {PetGachaTransactionPolicy.PullCost:N0} POWER COINS";
            _projectedBalance.text = coins >= PetGachaTransactionPolicy.PullCost
                ? $"AFTER PULL: {coins - PetGachaTransactionPolicy.PullCost:N0}"
                : $"NEED {PetGachaTransactionPolicy.PullCost - coins:N0} MORE";
            _catalogStatus.text = $"CURRENT ODDS - CATALOG {_catalog.Version}";
            _warning.text =
                "Owned pets can be pulled again. A duplicate grants no levels, items, or compensation.";
            RenderOdds(GetOwnedPetIds());

            if (!CanPull(out string reason))
            {
                _pull.SetEnabled(false);
                _status.text = reason;
                return;
            }
            if (coins < PetGachaTransactionPolicy.PullCost)
            {
                _pull.SetEnabled(false);
                _status.text =
                    $"You need {PetGachaTransactionPolicy.PullCost - coins:N0} more Power Coins.";
                return;
            }
            _pull.text = $"PULL FOR {PetGachaTransactionPolicy.PullCost}";
            _pull.SetEnabled(true);
            _status.text = string.Empty;
        }

        private void RenderOdds(IReadOnlyCollection<string> ownedPetIds)
        {
            _oddsList.Clear();
            PetChance[] chances = _calculator.Calculate(_catalog, ownedPetIds);
            int chanceIndex = 0;
            foreach (PetGachaRarity rarity in _catalog.Rarities)
            {
                var header = new Label(
                    $"{rarity.DisplayName.ToUpperInvariant()} - " +
                    $"{rarity.RateBasisPoints / 100m:0.##}% TOTAL");
                header.AddToClassList("pet-gacha-rarity-header");
                _oddsList.Add(header);

                foreach (PetGachaPet pet in rarity.Pets)
                {
                    PetChance chance = chances[chanceIndex++];
                    var row = new VisualElement();
                    row.AddToClassList("pet-gacha-odds-row");
                    var identity = new Label(
                        chance.IsOwned ? $"{pet.DisplayName}  OWNED" : pet.DisplayName);
                    identity.AddToClassList(chance.IsOwned
                        ? "pet-gacha-pet-owned"
                        : "pet-gacha-pet-unowned");
                    var probability = new Label(FormatPercent(chance.GetPercent()));
                    probability.AddToClassList("pet-gacha-probability");
                    row.Add(identity);
                    row.Add(probability);
                    _oddsList.Add(row);
                }
            }
        }

        private void BeginConfirmation()
        {
            if (!CanPull(out string reason))
            {
                _status.text = reason;
                Play(_definition?.ErrorClip);
                return;
            }
            long coins = Math.Max(0, _player.wallet?.powerCoins ?? 0);
            if (coins < PetGachaTransactionPolicy.PullCost)
            {
                RenderPreview();
                Play(_definition?.ErrorClip);
                return;
            }
            _confirmationSummary.text =
                $"Spend {PetGachaTransactionPolicy.PullCost} Power Coins?\n" +
                $"Balance: {coins:N0} -> {coins - PetGachaTransactionPolicy.PullCost:N0}\n" +
                "Duplicates grant no pet changes or compensation.";
            _confirmation.style.display = DisplayStyle.Flex;
            _result.style.display = DisplayStyle.None;
            _confirm.text = "CONFIRM PULL";
            _confirm.SetEnabled(true);
            _cancel.SetEnabled(true);
            _status.text = string.Empty;
        }

        private void CancelConfirmation()
        {
            if (_busy || _committed) return;
            _confirmation.style.display = DisplayStyle.None;
            _pendingTransactionId = string.Empty;
            RenderPreview();
        }

        private bool CanPull(out string reason)
        {
            reason = string.Empty;
            if (!IsConfigured)
            {
                reason = string.IsNullOrEmpty(_unavailableReason)
                    ? "Pet Gacha content is unavailable."
                    : _unavailableReason;
                return false;
            }
            if (_busy)
            {
                reason = "Saving the current pull...";
                return false;
            }
            if (!string.IsNullOrEmpty(_player.activeRun?.committedAttemptId))
            {
                reason = "Finish the current question before pulling.";
                return false;
            }
            if (string.Equals(_player.activeRun?.phase, "RunDefeat", StringComparison.Ordinal))
            {
                reason = "Finish run settlement before pulling.";
                return false;
            }
            return true;
        }

        private void Confirm()
        {
            if (_busy) return;
            if (!_committed && !CanPull(out string reason))
            {
                _status.text = reason;
                return;
            }
            if (string.IsNullOrEmpty(_pendingTransactionId))
                _pendingTransactionId = Guid.NewGuid().ToString("N");
            _committed = true;
            _host.StartCoroutine(Pull());
        }

        private IEnumerator Pull()
        {
            _busy = true;
            _open.SetEnabled(false);
            _close.SetEnabled(false);
            _cancel.SetEnabled(false);
            _confirm.SetEnabled(false);
            _status.text = "Saving this pull to Firebase...";
            Play(_definition?.CommitClip);

            PetGachaReceipt receipt = default;
            PetGachaFailure failure = default;
            bool success = false;
            var command = new PetGachaCommand(
                _pendingTransactionId,
                _catalog.Version,
                _previewRevision);
            yield return _store.Pull(
                command,
                value =>
                {
                    receipt = value;
                    success = true;
                },
                value => failure = value);

            _busy = false;
            if (!success)
            {
                HandleFailure(failure);
                yield break;
            }

            _committed = false;
            _pendingTransactionId = string.Empty;
            _close.SetEnabled(true);
            _confirmation.style.display = DisplayStyle.None;
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            ShowResult(receipt);
            RefreshAvailability();

            if (_publisher != null)
            {
                string warning = string.Empty;
                yield return _publisher.Publish(
                    _player,
                    () => { },
                    message => warning = message);
                if (!string.IsNullOrEmpty(warning)) Debug.LogWarning(warning);
            }
        }

        private void HandleFailure(PetGachaFailure failure)
        {
            Play(_definition?.ErrorClip);
            _status.text = failure.Message;
            if (failure.Code == PetGachaFailureCode.RecoverableTransport)
            {
                _committed = true;
                _confirm.text = "RECOVER PULL";
                _confirm.SetEnabled(true);
                _cancel.SetEnabled(false);
                _close.SetEnabled(false);
                return;
            }

            _committed = false;
            _pendingTransactionId = string.Empty;
            _close.SetEnabled(true);
            _cancel.SetEnabled(true);
            _confirmation.style.display = DisplayStyle.None;
            if (failure.Code == PetGachaFailureCode.StalePreview ||
                failure.Code == PetGachaFailureCode.InsufficientFunds)
            {
                PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
                RenderPreview();
                _status.text = failure.Message;
            }
            RefreshAvailability();
        }

        private void ShowResult(PetGachaReceipt receipt)
        {
            _result.style.display = DisplayStyle.Flex;
            _result.EnableInClassList("pet-gacha-result--new", receipt.WasNew);
            _result.EnableInClassList("pet-gacha-result--duplicate", !receipt.WasNew);
            _continue.SetEnabled(false);
            _close.SetEnabled(false);
            _resultBalance.text = $"POWER COINS: {receipt.ResultingPowerCoins:N0}";

            if (_definition != null &&
                _definition.TryResolvePet(
                    receipt.PetId,
                    out PetGachaCatalogDefinition.PetContent pet,
                    out PetGachaCatalogDefinition.RarityContent rarity))
            {
                _resultName.text = pet.displayName.ToUpperInvariant();
                _resultRarity.text = rarity.displayName.ToUpperInvariant();
                _resultRarity.style.color = rarity.displayColor;
                _resultIcon.style.backgroundImage = new StyleBackground(pet.icon);
            }
            else
            {
                _resultName.text = receipt.PetId.ToUpperInvariant();
                _resultRarity.text = "PET";
                _resultIcon.style.backgroundImage = StyleKeyword.None;
            }

            _resultState.text = receipt.WasNew
                ? "NEW PET - OWNERSHIP SAVED"
                : "DUPLICATE - NO PET CHANGES";
            _status.text = receipt.WasNew
                ? "Your new pet is now part of your permanent collection."
                : "This was an empty duplicate. No level, item, or compensation was granted.";
            Play(receipt.WasNew ? _definition?.NewPetClip : _definition?.DuplicateClip);

            if (!_reducedMotion)
            {
                _result.AddToClassList("pet-gacha-result--reveal");
                _result.schedule.Execute(() =>
                    _result.RemoveFromClassList("pet-gacha-result--reveal"))
                    .StartingIn(receipt.WasNew ? 900 : 450);
            }
            _continue.schedule.Execute(() =>
                {
                    _continue.SetEnabled(true);
                    _close.SetEnabled(true);
                })
                .StartingIn(ResultReadableHoldMilliseconds);
        }

        private void Continue()
        {
            if (_busy || _committed) return;
            _result.style.display = DisplayStyle.None;
            RenderPreview();
        }

        private HashSet<string> GetOwnedPetIds()
        {
            var owned = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlayerSnapshot.InventoryItemData item in
                _player.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
            {
                if (item != null && item.owned && _catalog.ContainsPet(item.itemId))
                    owned.Add(item.itemId);
            }
            return owned;
        }

        private void Play(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        private static string FormatPercent(decimal percent)
        {
            if (percent > 0m && percent < 0.0001m) return "<0.0001%";
            return percent.ToString(percent >= 1m ? "0.##'%'" : "0.####'%'");
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"Main Menu UI is missing '{name}'.");
        }
    }
}
