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
        private const int TransitionDurationMilliseconds = 2200;
        private const int RevealDropMilliseconds = 60;
        private const int RevealPetPrimeLeadMilliseconds = 32;
        private const int RevealPetMilliseconds = 620;
        private const int RevealCopyMilliseconds = 760;
        private const int RevealCopyPrimeLeadMilliseconds = 32;
        private const int RevealFlashClearMilliseconds = 820;
        private const int RevealStarsMilliseconds = 900;
        private const int RevealStarStaggerMilliseconds = 110;
        private const int RevealReadyPaddingMilliseconds = 260;
        private const float MainMenuExitSeconds = 0.26f;
        private const float BlackFadeSeconds = 0.20f;
        private const float PanelElementEnterSeconds = 0.42f;

        private enum PresentationState
        {
            None,
            Transition,
            Reveal,
            Results
        }

        private enum RarityTier
        {
            R,
            Sr,
            Ssr
        }

        private readonly MonoBehaviour _host;
        private PlayerSnapshot _player;
        private readonly PetGachaCatalogDefinition _definition;
        private readonly PetGachaCatalog _catalog;
        private readonly IPetGachaCommandStore _store;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly AudioSource _audio;
        private readonly bool _reducedMotion;
        private readonly IUiMotionDriver _motionDriver;
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
        private readonly VisualElement _openCurtain;
        private readonly VisualElement _mainMenuShell;
        private readonly VisualElement _combatLayer;
        private readonly VisualElement _transition;
        private readonly VisualElement _transitionTokens;
        private readonly Label _transitionHeadline;
        private readonly Button _transitionSkip;
        private readonly VisualElement _reveal;
        private readonly Button _revealSkip;
        private readonly Label _revealProgress;
        private readonly Label _revealTapHint;
        private readonly VisualElement _revealCopy;
        private readonly VisualElement _results;
        private readonly Label _resultsBalance;
        private readonly VisualElement _resultsGrid;
        private readonly Button _resultsContinue;
        private readonly Image _resultIcon;
        private readonly Image _resultSilhouette;
        private readonly List<VisualElement> _resultStars;
        private readonly Label _resultRarity;
        private readonly Label _resultName;
        private readonly Label _resultState;
        private readonly Label _resultBalance;
        private readonly VisualElement _bannerTopbar;
        private readonly VisualElement _bannerCopy;
        private readonly VisualElement _featuredStage;
        private readonly VisualElement _bannerFooter;

        private string _pendingTransactionId;
        private long _previewRevision;
        private int _selectedPullCount = 1;
        private bool _busy;
        private bool _committed;
        private PetGachaReceipt _presentationReceipt;
        private bool _hasPresentationReceipt;
        private PresentationState _presentationState;
        private int _revealIndex;
        private int _animationSequenceId;
        private int _visibleRevealStarCount;
        private int _openSequenceId;
        private bool _opening;

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
            IUiMotionDriver motionDriver,
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
            _motionDriver = motionDriver;
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
            _status.style.display = DisplayStyle.None;
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
            _openCurtain = Require<VisualElement>(root, "pet-gacha-open-curtain");
            _mainMenuShell = root.Q<VisualElement>("content");
            _combatLayer = root.Q<VisualElement>("combat-layer");
            _transition = Require<VisualElement>(root, "pet-gacha-transition");
            _transitionTokens = Require<VisualElement>(root, "pet-gacha-transition-tokens");
            _transitionHeadline = Require<Label>(root, "pet-gacha-transition-headline");
            _transitionSkip = Require<Button>(root, "pet-gacha-transition-skip");
            _reveal = Require<VisualElement>(root, "pet-gacha-reveal");
            _revealSkip = Require<Button>(root, "pet-gacha-reveal-skip");
            _revealProgress = Require<Label>(root, "pet-gacha-reveal-progress");
            _revealTapHint = Require<Label>(root, "pet-gacha-reveal-tap-hint");
            _revealCopy = root.Q<VisualElement>(className: "gacha-showcase-copy");
            _results = Require<VisualElement>(root, "pet-gacha-results");
            _resultsBalance = Require<Label>(root, "pet-gacha-results-balance");
            _resultsGrid = Require<VisualElement>(root, "pet-gacha-results-grid");
            _resultsContinue = Require<Button>(root, "pet-gacha-results-continue");
            _resultIcon = root.Q<Image>("pet-gacha-result-icon");
            if (_resultIcon != null)
                _resultIcon.scaleMode = ScaleMode.ScaleToFit;
            _resultSilhouette = root.Q<Image>("pet-gacha-result-silhouette");
            if (_resultSilhouette != null)
            {
                _resultSilhouette.scaleMode = ScaleMode.ScaleToFit;
                _resultSilhouette.tintColor = Color.black;
            }
            _resultStars = root.Query<VisualElement>(
                className: "pet-gacha-result-star").ToList();
            _resultRarity = root.Q<Label>("pet-gacha-result-rarity");
            _resultName = root.Q<Label>("pet-gacha-result-name");
            _resultState = root.Q<Label>("pet-gacha-result-state");
            _resultBalance = root.Q<Label>("pet-gacha-result-balance");
            _bannerTopbar = root.Q<VisualElement>(className: "gacha-topbar");
            _bannerCopy = root.Q<VisualElement>(className: "gacha-banner-copy");
            _featuredStage = root.Q<VisualElement>(className: "gacha-featured-stage");
            _bannerFooter = root.Q<VisualElement>(className: "gacha-banner-footer");

            _open.clicked += BeginOpenSequence;
            _close.clicked += Close;
            if (_detailsButton != null) _detailsButton.clicked += ShowDetails;
            if (_detailsClose != null) _detailsClose.clicked += HideDetails;
            if (_historyButton != null) _historyButton.clicked += ShowHistoryUnavailable;
            if (_pull1 != null) _pull1.clicked += OnPull1Clicked;
            if (_multiPullAvailable) _pull10.clicked += OnPull10Clicked;
            _cancel.clicked += CancelConfirmation;
            _confirm.clicked += Confirm;
            _transitionSkip.clicked += SkipTransition;
            _revealSkip.clicked += SkipRevealQueue;
            _reveal.RegisterCallback<ClickEvent>(OnRevealClicked);
            _resultsContinue.clicked += Continue;
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
            _open.clicked -= BeginOpenSequence;
            _close.clicked -= Close;
            if (_detailsButton != null) _detailsButton.clicked -= ShowDetails;
            if (_detailsClose != null) _detailsClose.clicked -= HideDetails;
            if (_historyButton != null) _historyButton.clicked -= ShowHistoryUnavailable;
            if (_pull1 != null) _pull1.clicked -= OnPull1Clicked;
            if (_multiPullAvailable) _pull10.clicked -= OnPull10Clicked;
            _cancel.clicked -= CancelConfirmation;
            _confirm.clicked -= Confirm;
            _transitionSkip.clicked -= SkipTransition;
            _revealSkip.clicked -= SkipRevealQueue;
            _reveal.UnregisterCallback<ClickEvent>(OnRevealClicked);
            _resultsContinue.clicked -= Continue;
            CancelOpenSequence(true);
            InvalidateAnimationSequence();
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
            InvalidateAnimationSequence();
            _modal.style.display = DisplayStyle.None;
            _modal.style.visibility = Visibility.Hidden;
            _modal.EnableInClassList("is-hidden", true);
            HideDetails();
            ResetPresentationVisuals();
            ResetPanelEnterAnimation();
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
            _open.SetEnabled(IsConfigured && safe && !_busy && !_opening);
            _open.tooltip = IsConfigured
                ? safe
                    ? "Spend Power Coins on transparent 1x or 10x pet pulls."
                    : "Finish the current question or run settlement first."
                : string.IsNullOrEmpty(_unavailableReason)
                    ? "Pet Gacha content is not configured."
                    : _unavailableReason;
        }

        private void BeginOpenSequence()
        {
            if (!GameVersionChecker.IsFeatureAvailable(GameFeature.PetGacha))
            {
                StatusMessageService.ShowWarning(
                    LocalizationService.Get("menu.lockedFeatureUpdate"));
                return;
            }
            if (!IsConfigured || _busy || _opening ||
                _panelHost.OpenPanel != MainMenuPanelId.None) return;

            _opening = true;
            _open.SetEnabled(false);
            int sequence = ++_openSequenceId;
            _openCurtain.style.display = DisplayStyle.Flex;
            _openCurtain.style.opacity = 0f;
            _openCurtain.pickingMode = PickingMode.Position;

            if (_motionDriver == null || _reducedMotion)
            {
                SetMainMenuExitProgress(1f);
                FadeCurtainIn(sequence);
                return;
            }

            int pending = 0;
            Action completed = () =>
            {
                pending--;
                if (pending == 0 && sequence == _openSequenceId)
                    FadeCurtainIn(sequence);
            };
            foreach (VisualElement target in new[] { _mainMenuShell, _combatLayer })
            {
                if (target == null) continue;
                pending++;
                _motionDriver.Tween(
                    target,
                    UiMotionChannel.Lifecycle,
                    MainMenuExitSeconds,
                    UiMotionEasing.OutCubic,
                    value => ApplyMainMenuExitProgress(target, value),
                    completed);
            }
            if (pending == 0) FadeCurtainIn(sequence);
        }

        private void FadeCurtainIn(int sequence)
        {
            if (sequence != _openSequenceId) return;
            if (_motionDriver == null)
            {
                _openCurtain.style.opacity = 1f;
                CompleteOpenBehindCurtain(sequence);
                return;
            }

            _motionDriver.Tween(
                _openCurtain,
                UiMotionChannel.Lifecycle,
                _reducedMotion ? 0.08f : BlackFadeSeconds,
                UiMotionEasing.OutCubic,
                value => _openCurtain.style.opacity = value,
                () => CompleteOpenBehindCurtain(sequence));
        }

        private void CompleteOpenBehindCurtain(int sequence)
        {
            if (sequence != _openSequenceId) return;
            OpenPanelNow();
            SetMainMenuExitProgress(0f);

            if (_motionDriver == null)
            {
                CompleteCurtainFade(sequence);
                return;
            }

            _motionDriver.Tween(
                _openCurtain,
                UiMotionChannel.Lifecycle,
                _reducedMotion ? 0.08f : BlackFadeSeconds,
                UiMotionEasing.OutCubic,
                value => _openCurtain.style.opacity = 1f - value,
                () => CompleteCurtainFade(sequence));
        }

        private void CompleteCurtainFade(int sequence)
        {
            if (sequence != _openSequenceId) return;
            _openCurtain.style.opacity = 0f;
            _openCurtain.style.display = DisplayStyle.None;
            _openCurtain.pickingMode = PickingMode.Ignore;
            _opening = false;
            RefreshAvailability();
        }

        private bool OpenPanelNow()
        {
            if (!_panelHost.TryOpen(
                    MainMenuPanelId.PetGacha,
                    _modal, _open)) return false;
            InvalidateAnimationSequence();
            _confirmation.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
            _transition.style.display = DisplayStyle.None;
            HideDetails();
            _status.text = string.Empty;
            _committed = false;
            _pendingTransactionId = string.Empty;
            SetSemanticState();
            RenderPreview();
            PlayPanelEnterAnimation();
            return true;
        }

        private void SetMainMenuExitProgress(float progress)
        {
            ApplyMainMenuExitProgress(_mainMenuShell, progress);
            ApplyMainMenuExitProgress(_combatLayer, progress);
        }

        private static void ApplyMainMenuExitProgress(
            VisualElement target,
            float progress)
        {
            if (target == null) return;
            float normalized = Mathf.Clamp01(progress);
            target.style.opacity = 1f - normalized;
            target.style.translate = new Translate(
                0f,
                new Length(-28f * normalized, LengthUnit.Pixel),
                0f);
        }

        private void CancelOpenSequence(bool restoreMainMenu)
        {
            _openSequenceId++;
            _opening = false;
            if (_motionDriver != null)
            {
                if (_mainMenuShell != null)
                    _motionDriver.Cancel(_mainMenuShell, UiMotionChannel.Lifecycle);
                if (_combatLayer != null)
                    _motionDriver.Cancel(_combatLayer, UiMotionChannel.Lifecycle);
                _motionDriver.Cancel(_openCurtain, UiMotionChannel.Lifecycle);
            }
            if (restoreMainMenu) SetMainMenuExitProgress(0f);
            _openCurtain.style.opacity = 0f;
            _openCurtain.style.display = DisplayStyle.None;
            _openCurtain.pickingMode = PickingMode.Ignore;
        }

        private void Close()
        {
            if (_busy || _committed) return;
            CloseImmediate();
        }

        private void CloseImmediate()
        {
            InvalidateAnimationSequence();
            if (_panelHost.OpenPanel == MainMenuPanelId.PetGacha)
                _panelHost.TryClose(MainMenuPanelId.PetGacha, _open);
            else
            {
                _modal.EnableInClassList("is-hidden", true);
                _modal.style.display = DisplayStyle.None;
            }
            _confirmation.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
            _transition.style.display = DisplayStyle.None;
            HideDetails();
            _pendingTransactionId = string.Empty;
            _committed = false;
            ResetPresentationVisuals();
            ResetPanelEnterAnimation();
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

        private void OnPull1Clicked() => BeginConfirmation(1);
        private void OnPull10Clicked()
        {
            if (!_multiPullAvailable)
            {
                StatusMessageService.ShowWarning("10x summon is locked in this hub release.");
                _status.text = string.Empty;
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
            StatusMessageService.ShowInfo("Summon history is coming soon.");
            _status.text = string.Empty;
        }

        private void BeginConfirmation(int pullCount = 1)
        {
            _selectedPullCount = pullCount;
            if (!CanPull(out string reason))
            {
                StatusMessageService.ShowWarning(reason);
                _status.text = string.Empty;
                Play(_definition?.ErrorClip);
                return;
            }
            long totalCost = PetGachaTransactionPolicy.GetCost(_selectedPullCount);
            long coins = Math.Max(0, _player.wallet?.powerCoins ?? 0);
            if (coins < totalCost)
            {
                StatusMessageService.ShowWarning($"You need {totalCost - coins:N0} more Power Coins.");
                _status.text = string.Empty;
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
                StatusMessageService.ShowWarning(reason);
                _status.text = string.Empty;
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
            _status.text = string.Empty;
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
            StartPresentation(receipt);
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
            if (failure.Code == PetGachaFailureCode.InsufficientFunds)
            {
                StatusMessageService.ShowWarning(failure.Message);
            }
            else
            {
                StatusMessageService.ShowError(failure.Message);
            }
            _status.text = string.Empty;
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
            }
            RefreshAvailability();
        }

        private void StartPresentation(PetGachaReceipt receipt)
        {
            InvalidateAnimationSequence();
            _presentationReceipt = receipt;
            _hasPresentationReceipt = true;
            _presentationState = PresentationState.Transition;
            _revealIndex = 0;
            _close.SetEnabled(false);
            _confirmation.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
            _transition.style.display = DisplayStyle.Flex;
            _transition.RemoveFromClassList("is-hidden");
            _transition.Focus();
            SetSemanticState("is-success");

            RarityTier highest = GetHighestRarity(receipt);
            ApplyRarityClass(_transition, highest);
            _transitionHeadline.text = highest == RarityTier.Ssr
                ? "A GOLDEN CALL ANSWERS"
                : highest == RarityTier.Sr
                    ? "A RARE CALL RESONATES"
                    : "THE CALL RESONATES";
            BuildTransitionTokens(receipt);
            ResetTransitionPhases();

            int sequence = _animationSequenceId;
            Schedule(_transition, sequence, 40, () =>
                _transition.AddToClassList("is-vortex-active"));
            Schedule(_transition, sequence, 430, () =>
                _transition.AddToClassList("is-comet-active"));
            Schedule(_transition, sequence, 1540, () =>
                _transition.AddToClassList("is-burst-active"));
            Schedule(_transition, sequence,
                _reducedMotion ? 180 : TransitionDurationMilliseconds,
                BeginRevealQueue);
        }

        private void BuildTransitionTokens(PetGachaReceipt receipt)
        {
            _transitionTokens.Clear();
            for (int i = 0; i < receipt.Results.Count; i++)
            {
                PetGachaResult roll = receipt.Results[i];
                var token = new VisualElement();
                token.AddToClassList("pet-gacha-transition-token");
                ApplyRarityClass(token, ResolveRarityTier(roll.RarityId, roll.PetId));
                var core = new VisualElement();
                core.AddToClassList("pet-gacha-transition-token-core");
                token.Add(core);
                _transitionTokens.Add(token);

                int sequence = _animationSequenceId;
                int delay = _reducedMotion ? 0 : 1680 + (i * 55);
                Schedule(token, sequence, delay, () => token.AddToClassList("is-entered"));
            }
        }

        private void SkipTransition()
        {
            if (_presentationState != PresentationState.Transition) return;
            BeginRevealQueue();
        }

        private void BeginRevealQueue()
        {
            if (!_hasPresentationReceipt ||
                _presentationState != PresentationState.Transition) return;
            InvalidateAnimationSequence();
            _transition.style.display = DisplayStyle.None;
            _transition.AddToClassList("is-hidden");
            _result.style.display = DisplayStyle.Flex;
            _result.RemoveFromClassList("is-hidden");
            _reveal.style.display = DisplayStyle.Flex;
            _results.style.display = DisplayStyle.None;
            _results.AddToClassList("is-hidden");
            _presentationState = PresentationState.Reveal;
            ShowRevealAt(0);
        }

        private void ShowRevealAt(int index)
        {
            if (!_hasPresentationReceipt ||
                index < 0 || index >= _presentationReceipt.Results.Count)
            {
                ShowFinalResults();
                return;
            }

            InvalidateAnimationSequence();
            _presentationState = PresentationState.Reveal;
            _revealIndex = index;
            ResetRevealPhases();
            PetGachaResult roll = _presentationReceipt.Results[index];
            RarityTier tier = ResolveRarityTier(roll.RarityId, roll.PetId);
            ApplyRarityClass(_reveal, tier);
            _revealProgress.text = $"REVEAL {index + 1} / {_presentationReceipt.Results.Count}";
            RenderReveal(roll, _presentationReceipt.ResultingPowerCoins);
            _reveal.Focus();

            int sequence = _animationSequenceId;
            int starStart = _reducedMotion ? 20 : RevealStarsMilliseconds;
            Schedule(_reveal, sequence,
                _reducedMotion
                    ? 0
                    : RevealDropMilliseconds - RevealPetPrimeLeadMilliseconds,
                PrimeRevealPet);
            Schedule(_reveal, sequence, _reducedMotion ? 0 : RevealDropMilliseconds,
                () =>
                {
                    _reveal.AddToClassList("is-reveal-drop");
                    if (_resultSilhouette != null)
                    {
                        _resultSilhouette.style.opacity = 1f;
                        _resultSilhouette.style.translate =
                            new Translate(0f, 0f, 0f);
                        _resultSilhouette.style.scale =
                            new Scale(Vector3.one);
                    }
                });
            Schedule(_reveal, sequence, _reducedMotion ? 0 : RevealPetMilliseconds,
                () =>
                {
                    _reveal.AddToClassList("is-reveal-pet");
                    if (_resultSilhouette != null)
                        _resultSilhouette.style.opacity = 0f;
                    if (_resultIcon != null)
                    {
                        _resultIcon.style.visibility = Visibility.Visible;
                        _resultIcon.style.opacity = 1f;
                        _resultIcon.style.scale = new Scale(Vector3.one);
                    }
                });
            Schedule(_reveal, sequence,
                _reducedMotion
                    ? 0
                    : RevealCopyMilliseconds - RevealCopyPrimeLeadMilliseconds,
                PrimeRevealCopy);
            Schedule(_reveal, sequence, _reducedMotion ? 0 : RevealCopyMilliseconds,
                () =>
                {
                    _reveal.AddToClassList("is-reveal-copy");
                    if (_revealCopy != null)
                    {
                        _revealCopy.style.opacity = 1f;
                        _revealCopy.style.translate = new Translate(0f, 0f, 0f);
                    }
                });
            Schedule(_reveal, sequence, _reducedMotion ? 0 : RevealFlashClearMilliseconds,
                () => _reveal.AddToClassList("is-reveal-flash-cleared"));

            for (int i = 0; i < _visibleRevealStarCount; i++)
            {
                int starIndex = i;
                Schedule(_resultStars[starIndex], sequence,
                    starStart + (_reducedMotion ? 0 : starIndex * RevealStarStaggerMilliseconds),
                    () => _resultStars[starIndex].AddToClassList("is-visible"));
            }

            int readyAt = starStart +
                Math.Max(0, _visibleRevealStarCount - 1) *
                (_reducedMotion ? 0 : RevealStarStaggerMilliseconds) +
                (_reducedMotion ? 40 : RevealReadyPaddingMilliseconds);
            Schedule(_reveal, sequence, readyAt, () =>
            {
                _reveal.AddToClassList("is-reveal-ready");
                _revealTapHint.AddToClassList("is-visible");
            });

            Play(roll.WasNew ? _definition?.NewPetClip : _definition?.DuplicateClip);
        }

        private void RenderReveal(PetGachaResult roll, long resultingPowerCoins)
        {
            if (_resultBalance != null)
                _resultBalance.text = $"POWER COINS: {resultingPowerCoins:N0}";
            if (_resultState != null)
                _resultState.text = roll.WasNew ? "NEW ✦" : "DUPLICATE";

            if (_definition != null && _definition.TryResolvePet(
                    roll.PetId,
                    out PetDefinition pet,
                    out PetGachaCatalogDefinition.RarityContent rarity))
            {
                if (_resultName != null) _resultName.text = pet.DisplayName.ToUpperInvariant();
                if (_resultRarity != null)
                {
                    _resultRarity.text = GetRarityLabel(ResolveRarityTier(roll.RarityId, roll.PetId));
                    _resultRarity.style.color = GetRarityColor(ResolveRarityTier(roll.RarityId, roll.PetId));
                }
                SetRevealStars(rarity.showcaseStarCount);
                if (_resultIcon != null) _resultIcon.sprite = pet.PreviewSprite;
                if (_resultSilhouette != null)
                {
                    _resultSilhouette.sprite = pet.PreviewSprite;
                    _resultSilhouette.tintColor = Color.black;
                }
            }
            else
            {
                if (_resultName != null) _resultName.text = roll.PetId.ToUpperInvariant();
                if (_resultRarity != null)
                {
                    RarityTier tier = ResolveRarityTier(roll.RarityId, roll.PetId);
                    _resultRarity.text = GetRarityLabel(tier);
                    _resultRarity.style.color = GetRarityColor(tier);
                }
                SetRevealStars(GetDefaultStarCount(ResolveRarityTier(roll.RarityId, roll.PetId)));
                if (_resultIcon != null) _resultIcon.sprite = null;
                if (_resultSilhouette != null) _resultSilhouette.sprite = null;
            }
        }

        private void SetRevealStars(int requestedCount)
        {
            _visibleRevealStarCount = Mathf.Clamp(requestedCount, 0, _resultStars.Count);
            for (int i = 0; i < _resultStars.Count; i++)
            {
                _resultStars[i].style.display = i < _visibleRevealStarCount
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _resultStars[i].RemoveFromClassList("is-visible");
            }
        }

        private void OnRevealClicked(ClickEvent evt)
        {
            if (_presentationState != PresentationState.Reveal ||
                !_reveal.ClassListContains("is-reveal-ready")) return;
            AdvanceReveal();
        }

        private void AdvanceReveal()
        {
            int next = _revealIndex + 1;
            if (next < _presentationReceipt.Results.Count)
                ShowRevealAt(next);
            else
                ShowFinalResults();
        }

        private void SkipRevealQueue()
        {
            if (_presentationState != PresentationState.Reveal) return;
            ShowFinalResults();
        }

        private void ShowFinalResults()
        {
            if (!_hasPresentationReceipt) return;
            InvalidateAnimationSequence();
            _presentationState = PresentationState.Results;
            _reveal.style.display = DisplayStyle.None;
            _results.style.display = DisplayStyle.Flex;
            _results.RemoveFromClassList("is-hidden");
            _resultsBalance.text = $"POWER COINS: {_presentationReceipt.ResultingPowerCoins:N0}";
            _resultsContinue.SetEnabled(false);
            _resultsGrid.Clear();

            int sequence = _animationSequenceId;
            for (int i = 0; i < _presentationReceipt.Results.Count; i++)
            {
                VisualElement card = CreateResultCard(_presentationReceipt.Results[i]);
                _resultsGrid.Add(card);
                int delay = _reducedMotion ? 0 : 90 + (i * 70);
                Schedule(card, sequence, delay, () => card.AddToClassList("is-entered"));
            }

            int unlockAt = _reducedMotion ? 0 :
                180 + (_presentationReceipt.Results.Count * 70);
            Schedule(_results, sequence, unlockAt, () =>
            {
                _resultsContinue.SetEnabled(true);
                _close.SetEnabled(true);
            });
            _results.Focus();
        }

        private VisualElement CreateResultCard(PetGachaResult roll)
        {
            RarityTier tier = ResolveRarityTier(roll.RarityId, roll.PetId);
            var card = new VisualElement();
            card.AddToClassList("pet-gacha-multi-card");
            card.AddToClassList("pet-gacha-multi-card--" + GetRarityClassSuffix(tier));

            var badge = new Label(roll.WasNew ? "NEW!" : "DUPE");
            badge.AddToClassList("pet-gacha-multi-card-badge");
            badge.AddToClassList(roll.WasNew
                ? "pet-gacha-badge--new"
                : "pet-gacha-badge--duplicate");
            var icon = new VisualElement();
            icon.AddToClassList("pet-gacha-multi-card-icon");
            var rarityLabel = new Label(GetRarityLabel(tier));
            rarityLabel.AddToClassList("pet-gacha-multi-card-rarity");
            rarityLabel.style.color = GetRarityColor(tier);
            var name = new Label(roll.PetId);
            name.AddToClassList("pet-gacha-multi-card-name");

            if (_definition != null && _definition.TryResolvePet(
                    roll.PetId,
                    out PetDefinition pet,
                    out PetGachaCatalogDefinition.RarityContent rarity))
            {
                name.text = pet.DisplayName;
                if (pet.Icon != null && pet.Icon.texture != null)
                    icon.style.backgroundImage = new StyleBackground(pet.Icon.texture);
            }

            card.Add(badge);
            card.Add(icon);
            card.Add(rarityLabel);
            card.Add(name);
            return card;
        }

        private void Continue()
        {
            if (_busy || _committed || _presentationState != PresentationState.Results) return;
            InvalidateAnimationSequence();
            _result.style.display = DisplayStyle.None;
            ResetPresentationVisuals();
            SetSemanticState();
            RenderPreview();
        }

        private void PlayPanelEnterAnimation()
        {
            _modal.AddToClassList("is-panel-entering");
            AnimatePanelElement(_bannerTopbar, new Vector2(0f, -28f), 0f);
            AnimatePanelElement(_bannerCopy, new Vector2(0f, 34f), 0.06f);
            AnimatePanelElement(_featuredStage, new Vector2(96f, 0f), 0.12f);
            AnimatePanelElement(_bannerFooter, new Vector2(0f, 34f), 0.18f);
        }

        private void AnimatePanelElement(
            VisualElement element,
            Vector2 startOffset,
            float delaySeconds)
        {
            if (element == null) return;
            Vector2 offset = _reducedMotion ? Vector2.zero : startOffset;
            SetEntered(element, false);
            element.style.translate = new Translate(offset.x, offset.y, 0f);

            if (_motionDriver == null)
            {
                int sequence = _animationSequenceId;
                Schedule(_modal, sequence,
                    Mathf.RoundToInt(delaySeconds * 1000f),
                    () =>
                    {
                        element.style.translate = new Translate(0f, 0f, 0f);
                        SetEntered(element, true);
                    });
                return;
            }

            _motionDriver.Tween(
                element,
                UiMotionChannel.Lifecycle,
                _reducedMotion ? 0.10f : PanelElementEnterSeconds,
                UiMotionEasing.OutCubic,
                value =>
                {
                    element.style.opacity = value;
                    element.style.translate = new Translate(
                        Mathf.Lerp(offset.x, 0f, value),
                        Mathf.Lerp(offset.y, 0f, value),
                        0f);
                },
                () => SetEntered(element, true),
                _reducedMotion ? 0f : delaySeconds);
        }

        private void ResetPanelEnterAnimation()
        {
            _modal.RemoveFromClassList("is-panel-entering");
            CancelPanelEnterTweens();
            SetEntered(_bannerTopbar, false);
            SetEntered(_bannerCopy, false);
            SetEntered(_featuredStage, false);
            SetEntered(_bannerFooter, false);
        }

        private void CancelPanelEnterTweens()
        {
            if (_motionDriver == null) return;
            foreach (VisualElement target in new[]
                     { _bannerTopbar, _bannerCopy, _featuredStage, _bannerFooter })
            {
                if (target != null)
                    _motionDriver.Cancel(target, UiMotionChannel.Lifecycle);
            }
        }

        private static void SetEntered(VisualElement element, bool entered)
        {
            if (element == null) return;
            element.EnableInClassList("is-entered", entered);
            element.style.opacity = entered ? 1f : 0f;
            element.pickingMode = entered ? PickingMode.Position : PickingMode.Ignore;
        }

        private void ResetPresentationVisuals()
        {
            _hasPresentationReceipt = false;
            _presentationState = PresentationState.None;
            _revealIndex = 0;
            _visibleRevealStarCount = 0;
            _transitionTokens.Clear();
            ResetTransitionPhases();
            ResetRevealPhases();
            _transition.style.display = DisplayStyle.None;
            _transition.AddToClassList("is-hidden");
            _reveal.style.display = DisplayStyle.Flex;
            _results.style.display = DisplayStyle.None;
            _results.AddToClassList("is-hidden");
            _resultsGrid.Clear();
            _revealTapHint.RemoveFromClassList("is-visible");
        }

        private void ResetTransitionPhases()
        {
            _transition.RemoveFromClassList("is-vortex-active");
            _transition.RemoveFromClassList("is-comet-active");
            _transition.RemoveFromClassList("is-burst-active");
        }

        private void ResetRevealPhases()
        {
            _reveal.AddToClassList("is-reveal-sequencing");
            _reveal.RemoveFromClassList("is-reveal-drop");
            _reveal.RemoveFromClassList("is-reveal-pet");
            _reveal.RemoveFromClassList("is-reveal-copy");
            _reveal.RemoveFromClassList("is-reveal-flash-cleared");
            _reveal.RemoveFromClassList("is-reveal-ready");
            _revealTapHint.RemoveFromClassList("is-visible");
            if (_revealCopy != null)
            {
                _revealCopy.style.display = DisplayStyle.Flex;
                _revealCopy.style.visibility = Visibility.Hidden;
                _revealCopy.style.opacity = 0f;
                _revealCopy.style.translate = new Translate(0f, 44f, 0f);
            }
            if (_resultSilhouette != null)
            {
                _resultSilhouette.tintColor = Color.black;
                _resultSilhouette.style.visibility = Visibility.Hidden;
                _resultSilhouette.style.opacity = 0f;
                _resultSilhouette.style.translate =
                    new Translate(0f, -118f, 0f);
                _resultSilhouette.style.scale =
                    new Scale(new Vector3(1.76f, 1.76f, 1f));
            }
            if (_resultIcon != null)
            {
                _resultIcon.style.visibility = Visibility.Hidden;
                _resultIcon.style.opacity = 0f;
                _resultIcon.style.scale =
                    new Scale(new Vector3(0.78f, 0.78f, 1f));
            }
            foreach (VisualElement star in _resultStars)
                star.RemoveFromClassList("is-visible");
        }

        private void PrimeRevealCopy()
        {
            if (_revealCopy == null) return;
            _revealCopy.style.display = DisplayStyle.Flex;
            _revealCopy.style.visibility = Visibility.Visible;
            _revealCopy.style.opacity = 0f;
            _revealCopy.style.translate = new Translate(0f, 44f, 0f);
        }

        private void PrimeRevealPet()
        {
            if (_resultSilhouette == null) return;
            _resultSilhouette.tintColor = Color.black;
            _resultSilhouette.style.visibility = Visibility.Visible;
            _resultSilhouette.style.opacity = 0f;
            _resultSilhouette.style.translate =
                new Translate(0f, -118f, 0f);
            _resultSilhouette.style.scale =
                new Scale(new Vector3(1.76f, 1.76f, 1f));
        }

        private void InvalidateAnimationSequence()
        {
            unchecked { _animationSequenceId++; }
        }

        private void Schedule(
            VisualElement owner,
            int sequence,
            int delayMilliseconds,
            Action action)
        {
            owner.schedule.Execute(() =>
            {
                if (sequence != _animationSequenceId) return;
                action?.Invoke();
            }).StartingIn(Math.Max(0, delayMilliseconds));
        }

        private RarityTier GetHighestRarity(PetGachaReceipt receipt)
        {
            RarityTier highest = RarityTier.R;
            foreach (PetGachaResult roll in receipt.Results)
            {
                RarityTier tier = ResolveRarityTier(roll.RarityId, roll.PetId);
                if (tier > highest) highest = tier;
            }
            return highest;
        }

        private RarityTier ResolveRarityTier(string rarityId, string petId)
        {
            string normalized = (rarityId ?? string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
            if (normalized.Contains("ssr") || normalized.Contains("legendary"))
                return RarityTier.Ssr;
            if (normalized.Contains("sr") || normalized.Contains("superrare") ||
                normalized.Contains("epic"))
                return RarityTier.Sr;

            if (_definition != null && _definition.TryResolvePet(
                    petId,
                    out PetDefinition ignoredPet,
                    out PetGachaCatalogDefinition.RarityContent rarity))
            {
                string display = (rarity.displayName ?? string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace(" ", string.Empty)
                    .ToLowerInvariant();
                if (display.Contains("ssr") || display.Contains("legendary"))
                    return RarityTier.Ssr;
                if (display.Contains("sr") || display.Contains("superrare") ||
                    display.Contains("epic"))
                    return RarityTier.Sr;
            }
            return RarityTier.R;
        }

        private static void ApplyRarityClass(VisualElement element, RarityTier tier)
        {
            element.EnableInClassList("rarity-r", tier == RarityTier.R);
            element.EnableInClassList("rarity-sr", tier == RarityTier.Sr);
            element.EnableInClassList("rarity-ssr", tier == RarityTier.Ssr);
        }

        private static string GetRarityClassSuffix(RarityTier tier)
        {
            return tier == RarityTier.Ssr ? "ssr" : tier == RarityTier.Sr ? "sr" : "r";
        }

        private static string GetRarityLabel(RarityTier tier)
        {
            return tier == RarityTier.Ssr ? "SSR" : tier == RarityTier.Sr ? "SR" : "R";
        }

        private static Color GetRarityColor(RarityTier tier)
        {
            if (tier == RarityTier.Ssr) return new Color(1f, 0.79f, 0.2f, 1f);
            if (tier == RarityTier.Sr) return new Color(0.69f, 0.41f, 1f, 1f);
            return Color.white;
        }

        private static int GetDefaultStarCount(RarityTier tier)
        {
            return tier == RarityTier.Ssr ? 5 : tier == RarityTier.Sr ? 4 : 3;
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
