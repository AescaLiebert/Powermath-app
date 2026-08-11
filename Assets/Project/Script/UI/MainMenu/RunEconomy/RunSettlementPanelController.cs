using System;
using System.Collections;
using PowerMath.Gameplay.Progression;
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
        private readonly VisualElement _modal;
        private readonly Label _title;
        private readonly Label _stage;
        private readonly Label _coins;
        private readonly Label _coinsGain;
        private readonly Label _legacy;
        private readonly Label _legacyGain;
        private readonly Label _attack;
        private readonly Label _prestige;
        private readonly Label _status;
        private readonly Button _rebirth;
        private readonly Button _confirm;
        private readonly Button _close;
        private readonly Button _continue;
        private RunSettlementType? _pendingSettlement;
        private SettlementPreview _pendingPreview;
        private bool _busy;

        public RunSettlementPanelController(
            MonoBehaviour host,
            VisualElement root,
            PlayerSnapshot player,
            FirestoreProgressionCommandStore store,
            FirestoreLeaderboardProjectionPublisher publisher,
            int baseAttack,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _baseAttack = baseAttack;
            _baseWeaponAttack = baseWeaponAttack;
            _baseCriticalRate = baseCriticalRate;
            _baseCriticalDamagePercent = baseCriticalDamagePercent;
            _modal = Require<VisualElement>(root, "run-settlement-modal");
            _title = Require<Label>(root, "run-settlement-title");
            _stage = Require<Label>(root, "run-settlement-stage");
            _coins = Require<Label>(root, "run-settlement-coins");
            _coinsGain = Require<Label>(root, "run-settlement-coins-gain");
            _legacy = Require<Label>(root, "run-settlement-legacy");
            _legacyGain = Require<Label>(root, "run-settlement-legacy-gain");
            _attack = Require<Label>(root, "run-settlement-attack");
            _prestige = Require<Label>(root, "run-settlement-prestige");
            _status = Require<Label>(root, "run-settlement-status");
            _rebirth = Require<Button>(root, "rebirth-button");
            _confirm = Require<Button>(root, "run-settlement-confirm");
            _close = Require<Button>(root, "run-settlement-close");
            _continue = Require<Button>(root, "run-settlement-continue");

            _rebirth.clicked += OpenRebirth;
            _confirm.clicked += Confirm;
            _close.clicked += Close;
            _continue.clicked += ReloadScene;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;
            Close();
            RefreshButton();
            OnPlayerChanged(_player);
        }

        public void Dispose()
        {
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _rebirth.clicked -= OpenRebirth;
            _confirm.clicked -= Confirm;
            _close.clicked -= Close;
            _continue.clicked -= ReloadScene;
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
                Show(true, false);
                return;
            }

            RenderPreview(_pendingPreview, "RUN ENDED");
            _status.text = "Saving this run and preparing Stage 1...";
            Show(false, false);
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
            RenderPreview(_pendingPreview, "REBIRTH PREVIEW");
            _status.text =
                "Your Rank and lifetime records stay. Questions and the current audit restart.";
            _confirm.text = "REBIRTH";
            Show(true, false);
        }

        private bool TryBuildPreview(
            RunSettlementType type,
            out SettlementPreview preview,
            out string error)
        {
            preview = default;
            error = string.Empty;
            if (!RunSettlementPolicy.CanSettle(_player, type, out error))
                return false;

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
                additionalLegacy);
        }

        private void RenderPreview(SettlementPreview preview, string title)
        {
            _title.text = title;
            _stage.text = $"STAGE {preview.Award.StageReached}";
            _coins.text =
                $"{preview.CurrentCoins:N0}  ->  {preview.ResultingCoins:N0}";
            _coinsGain.text = $"+{preview.Award.PowerCoins:N0} POWER COINS";
            _legacy.text =
                $"{FormatPercent(preview.CurrentLegacy)}  ->  {FormatPercent(preview.ResultingLegacy)}";
            _legacyGain.text =
                $"+{FormatPercent(preview.Award.LegacyBasisPoints)} PERMANENT ATK";
            _attack.text =
                $"EFFECTIVE ATK  {preview.CurrentAttack:N0}  ->  {preview.ResultingAttack:N0}";
            _prestige.text = preview.Award.Prestige > 0
                ? $"PRESTIGE  {preview.CurrentPrestige}  ->  {preview.ResultingPrestige}   (+1)"
                : $"PRESTIGE  {preview.CurrentPrestige}  (unchanged)";
        }

        private void Confirm()
        {
            if (_busy || !_pendingSettlement.HasValue) return;
            _host.StartCoroutine(Settle(_pendingSettlement.Value));
        }

        private IEnumerator Settle(RunSettlementType type)
        {
            _busy = true;
            _status.text = "Saving to Firebase...";
            SetControls(false);
            RunSettlementAward award = default;
            bool success = false;
            string failure = string.Empty;
            yield return _store.Settle(
                type,
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
                _confirm.text = "RETRY SAVE";
                Show(true, false);
                yield break;
            }

            yield return Publish();
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            string acceptedTitle = type == RunSettlementType.Rebirth
                ? "REBIRTH COMPLETE"
                : "NEW RUN READY";
            if (_pendingPreview.Award.StageReached == award.StageReached)
                RenderPreview(_pendingPreview, acceptedTitle);
            else
                RenderAcceptedAward(award, acceptedTitle);
            _status.text =
                "Saved. Your Rank and lifetime leaderboard values were preserved.";
            Show(false, true);
            RefreshButton();
        }

        private void RenderAcceptedAward(
            RunSettlementAward award,
            string title)
        {
            long resultingCoins = _player.wallet?.powerCoins ?? 0;
            long resultingLegacy = _player.progression?.legacyAtkBonusBasisPoints ?? 0;
            int resultingPrestige = _player.progression?.prestige ?? 0;
            _title.text = title;
            _stage.text = $"STAGE {award.StageReached}";
            _coins.text =
                $"{Math.Max(0, resultingCoins - award.PowerCoins):N0}  ->  {resultingCoins:N0}";
            _coinsGain.text = $"+{award.PowerCoins:N0} POWER COINS";
            _legacy.text =
                $"{FormatPercent(Math.Max(0, resultingLegacy - award.LegacyBasisPoints))}  ->  " +
                FormatPercent(resultingLegacy);
            _legacyGain.text =
                $"+{FormatPercent(award.LegacyBasisPoints)} PERMANENT ATK";
            _attack.text = "EFFECTIVE ATK: RELOAD TO REFRESH";
            _prestige.text = award.Prestige > 0
                ? $"PRESTIGE  {Math.Max(0, resultingPrestige - award.Prestige)}  ->  " +
                    $"{resultingPrestige}   (+1)"
                : $"PRESTIGE  {resultingPrestige}  (unchanged)";
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

        private void RefreshButton()
        {
            bool eligible = !_busy && RunSettlementPolicy.CanSettle(
                _player,
                RunSettlementType.Rebirth,
                out _);
            _rebirth.SetEnabled(eligible);
        }

        private void Show(bool confirm, bool continueButton)
        {
            _modal.style.display = DisplayStyle.Flex;
            _confirm.style.display = confirm ? DisplayStyle.Flex : DisplayStyle.None;
            _continue.style.display = continueButton
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _close.style.display = continueButton ||
                _pendingSettlement == RunSettlementType.Death
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            SetControls(!_busy);
        }

        private void SetControls(bool enabled)
        {
            _confirm.SetEnabled(enabled);
            _close.SetEnabled(enabled);
            _continue.SetEnabled(enabled);
        }

        private void Close()
        {
            if (_busy) return;
            _modal.style.display = DisplayStyle.None;
            _pendingSettlement = null;
            _pendingPreview = default;
            _confirm.text = "CONFIRM";
        }

        private static string FormatPercent(long basisPoints)
        {
            return $"{basisPoints / 100d:0.0}%";
        }

        private static void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"Main Menu UI is missing '{name}'.");
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
