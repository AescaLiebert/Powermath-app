using System;
using System.Collections;
using System.Collections.Generic;
using PowerMath.Bootstrap;
using PowerMath.Gameplay.Pets;
using PowerMath.Localization;
using PowerMath.PlayerData;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class PetGachaPanelController : IDisposable
    {
        private const int ResultReadableHoldMilliseconds = 600;

        private readonly MonoBehaviour _host;
        private PlayerSnapshot _player;
        private readonly PetGachaCatalogDefinition _definition;
        private readonly PetGachaCatalog _catalog;
        private readonly IPetGachaCommandStore _store;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly AudioSource _audio;
        private readonly bool _reducedMotion;
        private readonly string _unavailableReason;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly PetGachaProbabilityCalculator _calculator =
            new PetGachaProbabilityCalculator();

        private readonly Button _open;
        private readonly VisualElement _lockOverlay;
        private readonly VisualElement _modal;
        private readonly Button _close;
        private readonly Label _balance;
        private readonly Label _cost;
        private readonly Label _projectedBalance;
        private readonly Label _catalogStatus;
        private readonly ScrollView _oddsList;
        private readonly Label _warning;
        private readonly Label _status;
        private readonly Button _detailsButton;
        private readonly Button _detailsClose;
        private readonly Button _historyButton;
        private readonly VisualElement _detailsDrawer;
        private readonly Button _pull1;
        private readonly Button _pull10;
        private readonly Label _singlePullLabel;
        private readonly Label _singlePullCost;
        private readonly Label _multiPullLabel;
        private readonly Label _multiPullCost;
        private readonly bool _multiPullAvailable;
        private readonly Button _pull;
        private readonly VisualElement _confirmation;
        private readonly Label _confirmationTitle;
        private readonly Label _confirmationSummary;
        private readonly Button _cancel;
        private readonly Button _confirm;
        private readonly VisualElement _result;
        private readonly VisualElement _resultSingle;
        private readonly VisualElement _resultMulti;
        private readonly Label _multiBalance;
        private readonly VisualElement _multiGrid;
        private readonly Button _multiContinue;
        private readonly Image _resultIcon;
        private readonly List<VisualElement> _resultStars;
        private readonly Label _resultRarity;
        private readonly Label _resultName;
        private readonly Label _resultState;
        private readonly Label _resultBalance;
        private readonly Button _continue;

        private string _pendingTransactionId;
        private long _previewRevision;
        private int _selectedPullCount = 1;
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
            string unavailableReason,
            IMainMenuPanelHost panelHost)
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
            _panelHost = panelHost ?? throw new ArgumentNullException(nameof(panelHost));

            _open = Require<Button>(root, "pet-gacha-button");
            _lockOverlay = _open.Q<VisualElement>("pet-gacha-lock");
            _modal = Require<VisualElement>(root, "pet-gacha-modal");
            _close = Require<Button>(root, "pet-gacha-close");
            _balance = Require<Label>(root, "pet-gacha-balance");
            _cost = Require<Label>(root, "pet-gacha-cost");
            _projectedBalance = Require<Label>(root, "pet-gacha-projected-balance");
            _catalogStatus = Require<Label>(root, "pet-gacha-catalog-status");
            _oddsList = Require<ScrollView>(root, "pet-gacha-odds-list");
            _warning = Require<Label>(root, "pet-gacha-warning");
            _status = Require<Label>(root, "pet-gacha-status");
            _detailsButton = root.Q<Button>("pet-gacha-details");
            _detailsClose = root.Q<Button>("pet-gacha-details-close");
            _historyButton = root.Q<Button>("pet-gacha-history");
            _detailsDrawer = root.Q<VisualElement>("pet-gacha-details-drawer");
            _pull1 = root.Q<Button>("pet-gacha-pull-1") ?? root.Q<Button>("pet-gacha-pull");
            _pull10 = root.Q<Button>("pet-gacha-pull-10");
            _singlePullLabel = root.Q<Label>("pet-gacha-single-label");
            _singlePullCost = root.Q<Label>("pet-gacha-single-cost");
            _multiPullLabel = root.Q<Label>("pet-gacha-multi-label");
            _multiPullCost = root.Q<Label>("pet-gacha-multi-cost");
            _multiPullAvailable = _pull10 != null && !_pull10.ClassListContains("hub-lock");
            _pull = _pull1;
            _confirmation = Require<VisualElement>(root, "pet-gacha-confirmation");
            _confirmationTitle = root.Q<Label>("pet-gacha-confirmation-title");
            _confirmationSummary = Require<Label>(root, "pet-gacha-confirmation-summary");
            _cancel = Require<Button>(root, "pet-gacha-cancel");
            _confirm = Require<Button>(root, "pet-gacha-confirm");
            _result = Require<VisualElement>(root, "pet-gacha-result");
            _resultSingle = root.Q<VisualElement>("pet-gacha-result-single");
            _resultMulti = root.Q<VisualElement>("pet-gacha-result-multi");
            _multiBalance = root.Q<Label>("pet-gacha-result-multi-balance");
            _multiGrid = root.Q<VisualElement>("pet-gacha-multi-grid");
            _multiContinue = root.Q<Button>("pet-gacha-multi-continue");
            _resultIcon = root.Q<Image>("pet-gacha-result-icon");
            if (_resultIcon != null)
                _resultIcon.scaleMode = ScaleMode.ScaleToFit;
            _resultStars = root.Query<VisualElement>(
                className: "pet-gacha-result-star").ToList();
            _resultRarity = root.Q<Label>("pet-gacha-result-rarity");
            _resultName = root.Q<Label>("pet-gacha-result-name");
            _resultState = root.Q<Label>("pet-gacha-result-state");
            _resultBalance = root.Q<Label>("pet-gacha-result-balance");
            _continue = Require<Button>(root, "pet-gacha-continue");

            _open.clicked += Open;
            _close.clicked += Close;
            if (_detailsButton != null) _detailsButton.clicked += ShowDetails;
            if (_detailsClose != null) _detailsClose.clicked += HideDetails;
            if (_historyButton != null) _historyButton.clicked += ShowHistoryUnavailable;
            if (_pull1 != null) _pull1.clicked += OnPull1Clicked;
            if (_multiPullAvailable) _pull10.clicked += OnPull10Clicked;
            _cancel.clicked += CancelConfirmation;
            _confirm.clicked += Confirm;
            _continue.clicked += Continue;
            if (_multiContinue != null) _multiContinue.clicked += Continue;
            _panelHost.PanelClosed += OnPanelClosed;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;

            _open.tooltip = "Pet Gacha";
            if (_historyButton != null)
                _historyButton.tooltip = "Summon history is coming soon.";
            if (_pull10 != null && !_multiPullAvailable)
            {
                _pull10.SetEnabled(false);
                _pull10.tooltip = "10x summon is locked in this hub release.";
            }
            _modal.EnableInClassList("is-reduced-motion", _reducedMotion);
            CloseImmediate();
            RefreshAvailability();
        }

        public void Dispose()
        {
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _panelHost.PanelClosed -= OnPanelClosed;
            _open.clicked -= Open;
            _close.clicked -= Close;
            if (_detailsButton != null) _detailsButton.clicked -= ShowDetails;
            if (_detailsClose != null) _detailsClose.clicked -= HideDetails;
            if (_historyButton != null) _historyButton.clicked -= ShowHistoryUnavailable;
            if (_pull1 != null) _pull1.clicked -= OnPull1Clicked;
            if (_multiPullAvailable) _pull10.clicked -= OnPull10Clicked;
            _cancel.clicked -= CancelConfirmation;
            _confirm.clicked -= Confirm;
            _continue.clicked -= Continue;
            if (_multiContinue != null) _multiContinue.clicked -= Continue;
            if (_panelHost.OpenPanel == MainMenuPanelId.PetGacha)
                _panelHost.TryClose(MainMenuPanelId.PetGacha, _open);
        }

        private bool IsConfigured => _definition != null && _catalog != null && _store != null;

        private bool IsFirstGachaPull =>
            _player?.economy == null ||
            (!_player.economy.firstGachaPullCompleted &&
             string.IsNullOrEmpty(_player.economy.lastPetGachaTransactionId));

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            if (player == null || _busy) return;
            _player = player;
            RefreshAvailability();
            if (_modal.resolvedStyle.display != DisplayStyle.None && !_committed)
                RenderPreview();
        }

        private void OnPanelClosed(MainMenuPanelId panelId)
        {
            if (panelId != MainMenuPanelId.PetGacha) return;
            _modal.style.display = DisplayStyle.None;
            _modal.style.visibility = Visibility.Hidden;
            _modal.EnableInClassList("is-hidden", true);
            HideDetails();
        }

        private void RefreshAvailability()
        {
            bool gachaAvailable = GameVersionChecker.IsFeatureAvailable(GameFeature.PetGacha);
            if (!gachaAvailable)
            {
                _open.SetEnabled(true);
                _open.pickingMode = PickingMode.Position;
                _open.tooltip = "Pet Gacha (Locked in v1.0)";
                _open.AddToClassList("is-feature-locked");
                _lockOverlay?.RemoveFromClassList("is-hidden");
                return;
            }

            _open.RemoveFromClassList("is-feature-locked");
            _open.pickingMode = PickingMode.Position;
            _lockOverlay?.AddToClassList("is-hidden");

            bool safe = string.IsNullOrEmpty(_player.activeRun?.committedAttemptId) &&
                !string.Equals(_player.activeRun?.phase, "RunDefeat", StringComparison.Ordinal);
            _open.SetEnabled(IsConfigured && safe && !_busy);
            _open.tooltip = IsConfigured
                ? safe
                    ? "Spend Power Coins on transparent 1x or 10x pet pulls."
                    : "Finish the current question or run settlement first."
                : string.IsNullOrEmpty(_unavailableReason)
                    ? "Pet Gacha content is not configured."
                    : _unavailableReason;
        }

        private void Open()
        {
            if (!GameVersionChecker.IsFeatureAvailable(GameFeature.PetGacha))
            {
                StatusMessageService.ShowWarning(
                    LocalizationService.Get("menu.lockedFeatureUpdate"));
                return;
            }
            if (!IsConfigured || _busy) return;
            if (!_panelHost.TryOpen(
                    MainMenuPanelId.PetGacha,
                    _modal,
                    _open)) return;
            _confirmation.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
            HideDetails();
            _status.text = string.Empty;
            _committed = false;
            _pendingTransactionId = string.Empty;
            SetSemanticState();
            RenderPreview();
        }

        private void Close()
        {
            if (_busy || _committed) return;
            CloseImmediate();
        }

        private void CloseImmediate()
        {
            if (_panelHost.OpenPanel == MainMenuPanelId.PetGacha)
                _panelHost.TryClose(MainMenuPanelId.PetGacha, _open);
            else
            {
                _modal.EnableInClassList("is-hidden", true);
                _modal.style.display = DisplayStyle.None;
            }
            _confirmation.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
            HideDetails();
            _pendingTransactionId = string.Empty;
            _committed = false;
            SetSemanticState();
        }

        private void RenderPreview()
        {
            if (!IsConfigured)
            {
                _status.text = _unavailableReason;
                _pull1?.SetEnabled(false);
                _pull10?.SetEnabled(false);
                return;
            }

            _previewRevision = Math.Max(0, _player.revision);
            long coins = Math.Max(0, _player.wallet?.powerCoins ?? 0);
            _balance.text = coins.ToString("N0");
            long singleCost = PetGachaTransactionPolicy.SinglePullCost;
            long multiCost = PetGachaTransactionPolicy.MultiPullCost;
            if (_singlePullLabel != null) _singlePullLabel.text = "x1";
            if (_singlePullCost != null) _singlePullCost.text = singleCost.ToString("N0");
            if (_multiPullLabel != null)
                _multiPullLabel.text = $"x{PetGachaTransactionPolicy.MultiPullCount}";
            if (_multiPullCost != null) _multiPullCost.text = multiCost.ToString("N0");
            _cost.text = $"1x: {singleCost:N0} PC  |  10x: {multiCost:N0} PC";
            _projectedBalance.text = coins >= singleCost
                ? $"AFTER PULL: {coins - singleCost:N0} PC"
                : $"NEED {singleCost - coins:N0} MORE";

            _catalogStatus.text = $"CURRENT ODDS - CATALOG {_catalog.Version}";
            bool isFirst = IsFirstGachaPull;
            _warning.text = isFirst
                ? "FIRST PULL GUARANTEE: SSR Sapphire! " +
                  "10x guarantees SR or better. SSR hard pity: " +
                  $"{Math.Max(0, _player.economy?.petGachaPullsSinceSsr ?? 0)}/90. " +
                  "Duplicate pets stack by count."
                : "10x guarantees SR or better. SSR hard pity: " +
                  $"{Math.Max(0, _player.economy?.petGachaPullsSinceSsr ?? 0)}/90. " +
                  "Duplicate pets stack by count.";
            RenderOdds(GetOwnedPetIds());

            bool canPull = CanPull(out string reason);
            if (!canPull)
            {
                _pull1?.SetEnabled(false);
                _pull10?.SetEnabled(false);
                _status.text = reason;
                return;
            }

            bool canAfford1 = coins >= singleCost;
            if (_pull1 != null)
            {
                if (_singlePullLabel == null) _pull1.text = $"1x PULL ({singleCost:N0})";
                _pull1.SetEnabled(canAfford1);
            }
            if (_pull10 != null)
            {
                if (_multiPullLabel == null) _pull10.text = $"10x PULL ({multiCost:N0})";
                _pull10.SetEnabled(_multiPullAvailable && coins >= multiCost);
            }

            if (!canAfford1)
            {
                _status.text = $"You need {singleCost - coins:N0} more Power Coins.";
            }
            else
            {
                _status.text = string.Empty;
            }
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

        private void OnPull1Clicked() => BeginConfirmation(1);
        private void OnPull10Clicked()
        {
            if (!_multiPullAvailable)
            {
                _status.text = "10x summon is locked in this hub release.";
                Play(_definition?.ErrorClip);
                return;
            }
            BeginConfirmation(PetGachaTransactionPolicy.MultiPullCount);
        }

        private void ShowDetails()
        {
            if (_detailsDrawer == null || _busy || _committed) return;
            _detailsDrawer.RemoveFromClassList("is-hidden");
            _detailsDrawer.style.display = DisplayStyle.Flex;
            _detailsDrawer.Focus();
        }

        private void HideDetails()
        {
            if (_detailsDrawer == null) return;
            _detailsDrawer.AddToClassList("is-hidden");
            _detailsDrawer.style.display = DisplayStyle.None;
        }

        private void ShowHistoryUnavailable()
        {
            _status.text = "Summon history is coming soon.";
        }

        private void BeginConfirmation(int pullCount = 1)
        {
            _selectedPullCount = pullCount;
            if (!CanPull(out string reason))
            {
                _status.text = reason;
                Play(_definition?.ErrorClip);
                return;
            }
            long totalCost = PetGachaTransactionPolicy.GetCost(_selectedPullCount);
            long coins = Math.Max(0, _player.wallet?.powerCoins ?? 0);
            if (coins < totalCost)
            {
                RenderPreview();
                Play(_definition?.ErrorClip);
                return;
            }
            if (_confirmationTitle != null)
            {
                _confirmationTitle.text = _selectedPullCount > 1
                    ? $"CONFIRM {_selectedPullCount}x PULL"
                    : "CONFIRM 1x PULL";
            }
            bool isFirst = IsFirstGachaPull;
            _confirmationSummary.text =
                $"Spend {totalCost:N0} Power Coins for {_selectedPullCount} pull{(_selectedPullCount > 1 ? "s" : "")}?\n" +
                $"Balance: {coins:N0} -> {coins - totalCost:N0}\n" +
                (isFirst
                    ? "★ FIRST PULL BONUS: Guaranteed SSR Sapphire!\n"
                    : string.Empty) +
                (_selectedPullCount == PetGachaTransactionPolicy.MultiPullCount
                    ? "Guarantees at least one SR or SSR. SSR is guaranteed by pull 90.\n"
                    : string.Empty) +
                "Duplicates will stack by count in your pet inventory.";
            _confirmation.style.display = DisplayStyle.Flex;
            _result.style.display = DisplayStyle.None;
            _confirmation.Focus();
            _confirm.text = PowerMath.Localization.LocalizationService.Get("menu.confirmPull");
            _confirm.SetEnabled(true);
            _cancel.SetEnabled(true);
            _status.text = string.Empty;
            SetSemanticState("is-confirming");
        }

        private void CancelConfirmation()
        {
            if (_busy || _committed) return;
            _confirmation.style.display = DisplayStyle.None;
            _pendingTransactionId = string.Empty;
            SetSemanticState();
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
            _pull1?.SetEnabled(false);
            _pull10?.SetEnabled(false);
            _status.text = PowerMath.Localization.LocalizationService.Get("menu.savingPull");
            _confirmationSummary.text =
                "Saving this pull…\nYour result will appear after the transaction is accepted.";
            SetSemanticState("is-busy");
            Play(_definition?.CommitClip);

            PetGachaReceipt receipt = default;
            PetGachaFailure failure = default;
            bool success = false;
            var command = new PetGachaCommand(
                _pendingTransactionId,
                _catalog.Version,
                _previewRevision,
                _selectedPullCount);
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
                if (!string.IsNullOrEmpty(warning)) PowerMath.Diagnostics.AppLog.Warning("Pets", warning);
            }
        }

        private void HandleFailure(PetGachaFailure failure)
        {
            Play(_definition?.ErrorClip);
            _status.text = failure.Message;
            SetSemanticState("is-error");
            if (failure.Code == PetGachaFailureCode.RecoverableTransport)
            {
                _committed = true;
                _confirm.text = PowerMath.Localization.LocalizationService.Get("menu.recoverPull");
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
            _result.Focus();
            SetSemanticState("is-success");
            _result.EnableInClassList("pet-gacha-result--new", receipt.WasNew);
            _result.EnableInClassList("pet-gacha-result--duplicate", !receipt.WasNew);
            _continue.SetEnabled(false);
            if (_multiContinue != null) _multiContinue.SetEnabled(false);
            _close.SetEnabled(false);

            if (receipt.Results.Count > 1)
            {
                if (_resultSingle != null) _resultSingle.style.display = DisplayStyle.None;
                if (_resultMulti != null) _resultMulti.style.display = DisplayStyle.Flex;
                RenderMultiResults(receipt);
            }
            else
            {
                if (_resultMulti != null) _resultMulti.style.display = DisplayStyle.None;
                if (_resultSingle != null) _resultSingle.style.display = DisplayStyle.Flex;
                RenderSingleResult(receipt);
            }

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
                    if (_multiContinue != null) _multiContinue.SetEnabled(true);
                    _close.SetEnabled(true);
                })
                .StartingIn(ResultReadableHoldMilliseconds);
        }

        private void RenderSingleResult(PetGachaReceipt receipt)
        {
            if (_resultBalance != null)
                _resultBalance.text = $"POWER COINS: {receipt.ResultingPowerCoins:N0}";

            if (_definition != null &&
                _definition.TryResolvePet(
                    receipt.PetId,
                    out PetDefinition pet,
                    out PetGachaCatalogDefinition.RarityContent rarity))
            {
                if (_resultName != null) _resultName.text = pet.DisplayName.ToUpperInvariant();
                if (_resultRarity != null)
                {
                    _resultRarity.text = rarity.displayName.ToUpperInvariant();
                    _resultRarity.style.color = rarity.displayColor;
                }
                RenderShowcaseStars(rarity.showcaseStarCount);
                if (_resultIcon != null)
                    _resultIcon.sprite = pet.PreviewSprite;
            }
            else
            {
                if (_resultName != null) _resultName.text = receipt.PetId.ToUpperInvariant();
                if (_resultRarity != null) _resultRarity.text = PowerMath.Localization.LocalizationService.Get("menu.pet");
                RenderShowcaseStars(_resultStars.Count);
                if (_resultIcon != null) _resultIcon.sprite = null;
            }

            if (_resultState != null)
            {
                _resultState.text = receipt.WasNew
                    ? "NEW ✦"
                    : "DUPLICATE";
            }
            _status.text = receipt.WasNew
                ? "Your new pet is now part of your permanent collection."
                : "This pet was already owned. Inventory count incremented.";
        }

        private void RenderShowcaseStars(int requestedCount)
        {
            int visibleCount = Mathf.Clamp(requestedCount, 0, _resultStars.Count);
            for (int i = 0; i < _resultStars.Count; i++)
            {
                _resultStars[i].style.display = i < visibleCount
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void RenderMultiResults(PetGachaReceipt receipt)
        {
            if (_multiBalance != null)
                _multiBalance.text = $"POWER COINS: {receipt.ResultingPowerCoins:N0}";

            if (_multiGrid == null) return;
            _multiGrid.Clear();

            int newCount = 0;
            foreach (PetGachaResult roll in receipt.Results)
            {
                if (roll.WasNew) newCount++;

                var card = new VisualElement();
                card.AddToClassList("pet-gacha-multi-card");

                var badge = new Label();
                badge.AddToClassList("pet-gacha-multi-card-badge");
                if (roll.WasNew)
                {
                    badge.text = "NEW!";
                    badge.AddToClassList("pet-gacha-badge--new");
                }
                else
                {
                    badge.text = "DUPE";
                    badge.AddToClassList("pet-gacha-badge--duplicate");
                }

                var icon = new VisualElement();
                icon.AddToClassList("pet-gacha-multi-card-icon");

                var rarityLabel = new Label();
                rarityLabel.AddToClassList("pet-gacha-multi-card-rarity");

                var name = new Label();
                name.AddToClassList("pet-gacha-multi-card-name");

                if (_definition != null &&
                    _definition.TryResolvePet(
                        roll.PetId,
                        out PetDefinition pet,
                        out PetGachaCatalogDefinition.RarityContent rarity))
                {
                    name.text = pet.DisplayName;
                    rarityLabel.text = rarity.displayName.ToUpperInvariant();
                    rarityLabel.style.color = rarity.displayColor;
                    card.style.borderTopColor = rarity.displayColor;
                    card.style.borderBottomColor = rarity.displayColor;
                    card.style.borderLeftColor = rarity.displayColor;
                    card.style.borderRightColor = rarity.displayColor;
                    if (pet.Icon != null && pet.Icon.texture != null)
                        icon.style.backgroundImage = new StyleBackground(pet.Icon.texture);
                }
                else
                {
                    name.text = roll.PetId;
                    rarityLabel.text = roll.RarityId.ToUpperInvariant();
                }

                card.Add(badge);
                card.Add(icon);
                card.Add(rarityLabel);
                card.Add(name);
                _multiGrid.Add(card);
            }

            _status.text = newCount > 0
                ? $"Acquired {newCount} new pet(s)! All items stacked to inventory."
                : "All duplicate pets stacked to inventory.";
        }

        private void Continue()
        {
            if (_busy || _committed) return;
            _result.style.display = DisplayStyle.None;
            SetSemanticState();
            RenderPreview();
        }

        private void SetSemanticState(string state = null)
        {
            _modal.EnableInClassList("is-confirming", state == "is-confirming");
            _modal.EnableInClassList("is-busy", state == "is-busy");
            _modal.EnableInClassList("is-error", state == "is-error");
            _modal.EnableInClassList("is-success", state == "is-success");
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
