using System;
using System.Collections;
using PowerMath.Gameplay.Progression;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Combat.Presentation;
using PowerMath.Gameplay.Combat.Unity;
using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class RunSettlementPanelController : IDisposable
    {
        private readonly MonoBehaviour _host;
        private readonly PlayerSnapshot _player;
        private readonly FirestoreProgressionCommandStore _store;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly int _baseAttack;
        private readonly int _baseWeaponAttack;
        private readonly double _baseCriticalRate;
        private readonly double _baseCriticalDamagePercent;
        private readonly PetGachaCatalog _petCatalog;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly VisualElement _modal;
        private readonly Label _title;
        private readonly Label _attackBefore;
        private readonly Label _attackAfter;
        private readonly Label _stageBefore;
        private readonly Label _stageAfter;
        private readonly Label _coinsBefore;
        private readonly Label _coinsAfter;
        private readonly Label _status;
        private readonly VisualElement _progressFill;
        private readonly Label _progressValue;
        private readonly Button _rebirth;
        private readonly Button _confirm;
        private readonly Label _confirmLabel;
        private readonly Button _close;
        private readonly Button _continue;
        private RunSettlementType? _pendingSettlement;
        private SettlementPreview _pendingPreview;
        private bool _busy;
        private readonly IMainMenuInteractionGate _interactionGate;
        private readonly ActorPresentationController _playerActor;
        private readonly bool _reducedMotion;
        private IInteractionLock _terminalLock;
        private readonly UiToolkitLifecycleController _lifecycle;

        public RunSettlementPanelController(
            MonoBehaviour host,
            VisualElement root,
            PlayerSnapshot player,
            FirestoreProgressionCommandStore store,
            FirestoreLeaderboardProjectionPublisher publisher,
            int baseAttack,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            PetGachaCatalog petCatalog,
            IMainMenuPanelHost panelHost,
            IMainMenuInteractionGate interactionGate = null,
            ActorPresentationController playerActor = null,
            bool reducedMotion = false)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _store = store;
            _publisher = publisher;
            _baseAttack = baseAttack;
            _baseWeaponAttack = baseWeaponAttack;
            _baseCriticalRate = baseCriticalRate;
            _baseCriticalDamagePercent = baseCriticalDamagePercent;
            _petCatalog = petCatalog;
            _panelHost = panelHost ?? throw new ArgumentNullException(nameof(panelHost));
            _interactionGate = interactionGate;
            _playerActor = playerActor;
            _reducedMotion = reducedMotion;
            _modal = Require<VisualElement>(root, "run-settlement-modal");
            _title = Require<Label>(root, "Title");
            _attackBefore = RequireClass<Label>(root, "rebirth-atk-before");
            _attackAfter = RequireClass<Label>(root, "rebirth-atk-after");
            _stageBefore = RequireClass<Label>(root, "rebirth-stage-before");
            _stageAfter = RequireClass<Label>(root, "rebirth-stage-after");
            _coinsBefore = RequireClass<Label>(root, "rebirth-coins-before");
            _coinsAfter = RequireClass<Label>(root, "rebirth-coins-after");
            _status = Require<Label>(root, "run-settlement-status");
            _progressFill = RequireClass<VisualElement>(root, "rebirth-progress-fill");
            _progressValue = Require<Label>(root, "Progress Value");
            _rebirth = Require<Button>(root, "rebirth-button");
            _confirm = Require<Button>(root, "Button / Rebirth");
            _confirmLabel = Require<Label>(root, "Text Component");
            _close = Require<Button>(root, "Button / Close");
            _continue = Require<Button>(root, "run-settlement-continue");
            _attackBefore.enableRichText = true;
            _attackAfter.enableRichText = true;
            _lifecycle = new UiToolkitLifecycleController(_modal);

            _rebirth.clicked += OpenRebirth;
            _confirm.clicked += Confirm;
            _close.clicked += Close;
            _continue.clicked += AcknowledgeAndReload;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;
            HideInitially();
            RefreshButton();
            OnPlayerChanged(_player);
            if (HasPendingSettlementPresentation(_player))
                _host.StartCoroutine(RecoverPendingSettlement());
            else if (string.Equals(_player.activeRun?.phase, "RunDefeat",
                         StringComparison.Ordinal) &&
                     _player.activeRun?.pendingPresentation == null)
                _host.StartCoroutine(RecoverUnsettledDeath());
        }

        public void Dispose()
        {
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _rebirth.clicked -= OpenRebirth;
            _confirm.clicked -= Confirm;
            _close.clicked -= Close;
            _continue.clicked -= AcknowledgeAndReload;
            _terminalLock?.Dispose();
            _terminalLock = null;
            _lifecycle.CancelAndApply(UiLifecycleState.Hidden);
            if (_panelHost.OpenPanel == MainMenuPanelId.Rebirth)
                _panelHost.TryClose(MainMenuPanelId.Rebirth, _rebirth);
        }

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            if (player == null || _busy) return;
            RefreshButton();
            if (!string.Equals(
                    player.activeRun?.phase,
                    "RunDefeat",
                    StringComparison.Ordinal)) return;

            _pendingSettlement = RunSettlementType.Death;
            if (!TryBuildPreview(RunSettlementType.Death, out _pendingPreview,
                    out string error))
            {
                _title.text = "RUN ENDED";
                _status.text = error;
                SetSemanticState("is-error");
                return;
            }

            RenderPreview(_pendingPreview, "RUN ENDED");
            _status.text = "Waiting for the defeat presentation to finish...";
        }

        public void NotifyDeathPresentationCompleted()
        {
            if (_busy) return;
            _pendingSettlement = RunSettlementType.Death;
            if (!TryBuildPreview(RunSettlementType.Death, out _pendingPreview,
                    out string error))
            {
                AcquireTerminalLock();
                _title.text = "RUN ENDED";
                _status.text = error;
                Show(true, false, true);
                SetSemanticState("is-error");
                return;
            }
            AcquireTerminalLock();
            _host.StartCoroutine(Settle(RunSettlementType.Death));
        }

        private void OpenRebirth()
        {
            if (!TryBuildPreview(RunSettlementType.Rebirth, out _pendingPreview,
                    out string error))
            {
                _status.text = error;
                return;
            }

            _pendingSettlement = RunSettlementType.Rebirth;
            if (!Show(true, false))
            {
                _pendingSettlement = null;
                _pendingPreview = default;
                return;
            }
            SetSemanticState();
            RenderPreview(_pendingPreview, "REBIRTH");
            bool canSettle = RunSettlementPolicy.CanSettle(_player, RunSettlementType.Rebirth, out string reason);
            if (canSettle)
            {
                _status.text =
                    "Your Rank and lifetime records stay. Questions and the current audit restart.";
                SetConfirmText("Rebirth");
            }
            else
            {
                _status.text = reason;
                SetConfirmText("Locked");
            }
            _confirm.SetEnabled(canSettle);
        }

        private bool TryBuildPreview(
            RunSettlementType type,
            out SettlementPreview preview,
            out string error)
        {
            preview = default;
            error = string.Empty;
            if (_player?.progression == null || _player.activeRun == null)
            {
                error = "Player run data is unavailable.";
                return false;
            }

            try
            {
                RunSettlementAward award = RunSettlementPolicy.Calculate(_player, type);
                PlayerStatProjection current = ProjectStats();
                PlayerStatProjection resulting = ProjectStats(award.LegacyBasisPoints);
                long currentCoins = _player.wallet?.powerCoins ?? 0;
                long currentLegacy = _player.progression?.legacyAtkBonusBasisPoints ?? 0;
                int currentPrestige = _player.progression?.prestige ?? 0;
                preview = new SettlementPreview(
                    award,
                    currentCoins,
                    checked(currentCoins + award.PowerCoins),
                    currentLegacy,
                    checked(currentLegacy + award.LegacyBasisPoints),
                    current.EffectiveAttack,
                    resulting.EffectiveAttack,
                    currentPrestige,
                    checked(currentPrestige + award.Prestige));
                return true;
            }
            catch (Exception exception) when (
                exception is InvalidOperationException ||
                exception is ArgumentOutOfRangeException ||
                exception is OverflowException)
            {
                error = $"Rebirth preview is unavailable: {exception.Message}";
                return false;
            }
        }

        private PlayerStatProjection ProjectStats(long additionalLegacy = 0)
        {
            return PlayerStatProjectionFactory.Create(
                _player,
                _baseAttack,
                _baseWeaponAttack,
                _baseCriticalRate,
                _baseCriticalDamagePercent,
                _petCatalog,
                additionalLegacy);
        }

        private void RenderPreview(SettlementPreview preview, string title)
        {
            _title.text = title;
            _attackBefore.text = FormatAttack(preview.CurrentAttack, preview.CurrentLegacy);
            _attackAfter.text = FormatAttack(preview.ResultingAttack, preview.ResultingLegacy);
            _stageBefore.text = preview.Award.StageReached.ToString("N0");
            _stageAfter.text = "1";
            _coinsBefore.text = preview.CurrentCoins.ToString("N0");
            _coinsAfter.text = preview.ResultingCoins.ToString("N0");
            UpdateProgressBar(preview.Award.StageReached);
        }

        private void Confirm()
        {
            if (_busy || !_pendingSettlement.HasValue) return;
            if (_pendingSettlement == RunSettlementType.Rebirth &&
                !RunSettlementPolicy.CanSettle(_player, RunSettlementType.Rebirth, out string reason))
            {
                _status.text = reason;
                _confirm.SetEnabled(false);
                return;
            }
            AcquireTerminalLock();
            _host.StartCoroutine(Settle(_pendingSettlement.Value));
        }

        private IEnumerator Settle(RunSettlementType type)
        {
            if (_store == null)
            {
                _status.text = "Rebirth save is not configured in offline mode.";
                SetConfirmText("Offline");
                Show(true, false);
                SetSemanticState("is-error");
                yield break;
            }

            _busy = true;
            SetSemanticState("is-busy");
            _status.text = "Saving to Firebase...";
            SetControls(false);
            RunSettlementAward award = default;
            bool success = false;
            string failure = string.Empty;
            var presentation = new RunSettlementPresentationValues(
                _pendingPreview.CurrentAttack,
                _pendingPreview.ResultingAttack);
            yield return _store.Settle(
                type,
                presentation,
                value =>
                {
                    award = value;
                    success = true;
                },
                message => failure = message);
            _busy = false;
            if (!success)
            {
                _status.text = failure;
                SetConfirmText("Retry");
                Show(true, false);
                SetSemanticState("is-error");
                yield break;
            }

            yield return Publish();
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            if (type == RunSettlementType.Rebirth && _playerActor != null)
            {
                if (_playerActor.State == ActorVisualState.Hidden)
                    _playerActor.CancelAndApply(ActorVisualState.Idle);
                yield return _playerActor.Play(PresentationActionKind.PlayerRebirth);
            }
            string acceptedTitle = type == RunSettlementType.Rebirth
                ? "REBIRTH COMPLETE"
                : "RUN ENDED";
            if (_pendingPreview.Award.StageReached == award.StageReached)
                RenderPreview(_pendingPreview, acceptedTitle);
            else
                RenderAcceptedAward(award, acceptedTitle);
            _status.text =
                "Saved. Your Rank and lifetime leaderboard values were preserved.";
            Show(false, true);
            SetSemanticState("is-success");
            RefreshButton();
        }

        private void RenderAcceptedAward(
            RunSettlementAward award,
            string title)
        {
            long resultingCoins = _player.wallet?.powerCoins ?? 0;
            long resultingLegacy = _player.progression?.legacyAtkBonusBasisPoints ?? 0;
            _title.text = title;
            long sourceLegacy = Math.Max(0, resultingLegacy - award.LegacyBasisPoints);
            PlayerStatProjection sourceStats = ProjectStats();
            PlayerStatProjection resultStats = ProjectStats(award.LegacyBasisPoints);
            _attackBefore.text = FormatAttack(sourceStats.EffectiveAttack, sourceLegacy);
            _attackAfter.text = FormatAttack(resultStats.EffectiveAttack, resultingLegacy);
            _stageBefore.text = award.StageReached.ToString("N0");
            _stageAfter.text = "1";
            _coinsBefore.text = Math.Max(0, resultingCoins - award.PowerCoins).ToString("N0");
            _coinsAfter.text = resultingCoins.ToString("N0");
            UpdateProgressBar(award.StageReached);
        }

        private IEnumerator Publish()
        {
            if (_publisher == null) yield break;
            string warning = string.Empty;
            yield return _publisher.Publish(
                _player,
                () => { },
                message => warning = message);
            if (!string.IsNullOrEmpty(warning)) PowerMath.Diagnostics.AppLog.Warning("Progression", warning);
        }

        private void RefreshButton()
        {
            bool canAccess = !_busy && !string.Equals(
                _player.activeRun?.phase,
                "RunDefeat",
                StringComparison.Ordinal);
            _rebirth.SetEnabled(canAccess);

            int stage = Math.Min(200, Math.Max(1,
                Math.Max(_player.progression?.currentStage ?? 1, _player.activeRun?.currentStage ?? 1)));
            bool isReady = canAccess && stage >= RunSettlementPolicy.MinimumRebirthStage;
            _rebirth.EnableInClassList("is-ready", isReady);
        }

        private bool Show(
            bool confirm,
            bool continueButton,
            bool forceOpen = false)
        {
            if (forceOpen && _panelHost.OpenPanel != MainMenuPanelId.Rebirth)
                _panelHost.ForceCloseAll();
            if (!_panelHost.TryOpen(
                    MainMenuPanelId.Rebirth,
                    _modal,
                    _rebirth)) return false;
            _confirm.style.display = confirm ? DisplayStyle.Flex : DisplayStyle.None;
            _continue.style.display = continueButton
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _close.style.display = continueButton ||
                _pendingSettlement == RunSettlementType.Death
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            SetControls(false);
            _lifecycle.Enter(() => SetControls(!_busy));
            return true;
        }

        private void SetControls(bool enabled)
        {
            bool canConfirm = enabled && (_pendingSettlement != RunSettlementType.Rebirth ||
                RunSettlementPolicy.CanSettle(_player, RunSettlementType.Rebirth, out _));
            _confirm.SetEnabled(canConfirm);
            _close.SetEnabled(enabled);
            _continue.SetEnabled(enabled);
        }

        private void Close()
        {
            if (_busy) return;
            if (_panelHost.OpenPanel == MainMenuPanelId.Rebirth)
            {
                _lifecycle.Exit(() =>
                    _panelHost.TryClose(MainMenuPanelId.Rebirth, _rebirth));
            }
            else if (_panelHost.OpenPanel == MainMenuPanelId.None)
                _lifecycle.CancelAndApply(UiLifecycleState.Hidden);
            _pendingSettlement = null;
            _pendingPreview = default;
            SetConfirmText("Rebirth");
            SetSemanticState();
        }

        private IEnumerator RecoverPendingSettlement()
        {
            PlayerSnapshot.RunSettlementData receipt = _player.lastRunSettlement;
            if (receipt == null || receipt.presentationVersion != 1 ||
                !Enum.TryParse(receipt.presentationCause, true,
                    out RunSettlementType type))
            {
                AcquireTerminalLock();
                _status.text = "This saved run result requires a newer PowerMath version.";
                Show(false, false, true);
                SetSemanticState("is-error");
                yield break;
            }

            _pendingSettlement = type;
            AcquireTerminalLock();
            _panelHost.ForceCloseAll();
            if (_playerActor != null)
            {
                if (_playerActor.State == ActorVisualState.Hidden)
                    _playerActor.CancelAndApply(ActorVisualState.Idle);
                if (type == RunSettlementType.Death)
                {
                    yield return _playerActor.Play(
                        PresentationActionKind.PlayerTakeDamage);
                    yield return _playerActor.Play(PresentationActionKind.PlayerDie);
                }
                else
                {
                    yield return _playerActor.Play(
                        PresentationActionKind.PlayerRebirth);
                }
            }
            else if (!_reducedMotion)
            {
                yield return new WaitForSecondsRealtime(
                    type == RunSettlementType.Death ? 0.90f : 0.85f);
            }

            RenderSavedSettlement(receipt, type);
            _status.text = "Saved result recovered. Continue to acknowledge it.";
            Show(false, true, true);
            SetSemanticState("is-success");
        }

        private IEnumerator RecoverUnsettledDeath()
        {
            AcquireTerminalLock();
            if (_playerActor != null)
            {
                if (_playerActor.State == ActorVisualState.Hidden)
                    _playerActor.CancelAndApply(ActorVisualState.Idle);
                yield return _playerActor.Play(
                    PresentationActionKind.PlayerTakeDamage);
                yield return _playerActor.Play(PresentationActionKind.PlayerDie);
            }
            else if (!_reducedMotion)
            {
                yield return new WaitForSecondsRealtime(0.90f);
            }
            NotifyDeathPresentationCompleted();
        }

        private void RenderSavedSettlement(
            PlayerSnapshot.RunSettlementData value,
            RunSettlementType type)
        {
            _title.text = type == RunSettlementType.Death
                ? "RUN ENDED"
                : "REBIRTH COMPLETE";
            long resultingLegacy = checked(
                value.sourceLegacyAtkBasisPoints + value.legacyAtkBasisPointsGranted);
            _attackBefore.text = FormatAttack(
                value.sourceEffectiveAttack,
                value.sourceLegacyAtkBasisPoints);
            _attackAfter.text = FormatAttack(
                value.resultingEffectiveAttack,
                resultingLegacy);
            _stageBefore.text = value.stageReached.ToString("N0");
            _stageAfter.text = "1";
            _coinsBefore.text = value.sourcePowerCoins.ToString("N0");
            _coinsAfter.text = value.resultingPowerCoins.ToString("N0");
            UpdateProgressBar(value.stageReached);
        }

        private void UpdateProgressBar(int stageReached)
        {
            int target = RunSettlementPolicy.MinimumRebirthStage;
            int clampedValue = Math.Min(target, Math.Max(0, stageReached));
            float width = clampedValue <= 0
                ? 0f
                : Math.Max(97f, 862f * clampedValue / target);
            _progressFill.style.width = width;
            _progressValue.text = $"{stageReached:N0} / {target:N0}";
        }

        private void AcknowledgeAndReload()
        {
            if (_busy) return;
            string sourceRunId = _player.lastRunSettlement?.runId ?? string.Empty;
            _host.StartCoroutine(AcknowledgeAndReloadRoutine(sourceRunId));
        }

        private IEnumerator AcknowledgeAndReloadRoutine(string sourceRunId)
        {
            _busy = true;
            _status.text = "Acknowledging result...";
            SetControls(false);
            bool success = false;
            string failure = string.Empty;
            yield return _store.AcknowledgeSettlementPresentation(
                sourceRunId,
                () => success = true,
                message => failure = message);
            _busy = false;
            if (!success)
            {
                _status.text = string.IsNullOrWhiteSpace(failure)
                    ? "Result acknowledgement failed. Try again."
                    : failure;
                SetControls(true);
                SetSemanticState("is-error");
                yield break;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void AcquireTerminalLock()
        {
            if (_terminalLock != null || _interactionGate == null) return;
            _terminalLock = _interactionGate.Acquire(
                "run-settlement:" + (_player.activeRun?.runId ??
                    _player.lastRunSettlement?.runId ?? "unknown"),
                InteractionScope.All & ~InteractionScope.TerminalAction);
        }

        private static bool HasPendingSettlementPresentation(PlayerSnapshot player)
        {
            return player?.lastRunSettlement != null &&
                string.Equals(player.lastRunSettlement.presentationStatus,
                    "Pending", StringComparison.Ordinal);
        }

        private static string FormatAttack(long attack, long basisPoints)
        {
            return $"{attack:N0}<size=24>(+{basisPoints / 100d:0.##}%)</size>";
        }

        private void SetConfirmText(string value)
        {
            _confirmLabel.text = value;
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"Main Menu UI is missing '{name}'.");
        }

        private static T RequireClass<T>(VisualElement root, string className)
            where T : VisualElement
        {
            return root.Q<T>(className: className) ?? throw new InvalidOperationException(
                $"Main Menu UI is missing class '{className}'.");
        }

        private void HideInitially()
        {
            _lifecycle.CancelAndApply(UiLifecycleState.Hidden);
        }

        private void SetSemanticState(string state = null)
        {
            _modal.EnableInClassList("is-busy", state == "is-busy");
            _modal.EnableInClassList("is-success", state == "is-success");
            _modal.EnableInClassList("is-error", state == "is-error");
        }

        private readonly struct SettlementPreview
        {
            public SettlementPreview(
                RunSettlementAward award,
                long currentCoins,
                long resultingCoins,
                long currentLegacy,
                long resultingLegacy,
                int currentAttack,
                int resultingAttack,
                int currentPrestige,
                int resultingPrestige)
            {
                Award = award;
                CurrentCoins = currentCoins;
                ResultingCoins = resultingCoins;
                CurrentLegacy = currentLegacy;
                ResultingLegacy = resultingLegacy;
                CurrentAttack = currentAttack;
                ResultingAttack = resultingAttack;
                CurrentPrestige = currentPrestige;
                ResultingPrestige = resultingPrestige;
            }

            public RunSettlementAward Award { get; }
            public long CurrentCoins { get; }
            public long ResultingCoins { get; }
            public long CurrentLegacy { get; }
            public long ResultingLegacy { get; }
            public int CurrentAttack { get; }
            public int ResultingAttack { get; }
            public int CurrentPrestige { get; }
            public int ResultingPrestige { get; }
        }
    }
}
