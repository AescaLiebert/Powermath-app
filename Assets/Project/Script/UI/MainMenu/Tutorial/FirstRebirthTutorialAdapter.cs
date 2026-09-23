using System;
using System.Collections;
using PowerMath.Gameplay.Tutorial;
using PowerMath.PlayerData;
using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using PowerMath.UI.Core;
using UnityEngine;

namespace PowerMath.UI.MainMenu.Tutorial
{
    /// <summary>
    /// Translates the durable Rebirth/Gacha/Hub receipts into authored tutorial
    /// events. It does not mutate progression directly; the existing panels
    /// retain ownership of every gameplay operation.
    /// </summary>
    public sealed class FirstRebirthTutorialAdapter : IDisposable
    {
        private const long FirstPetGrant = 180;

        private readonly MonoBehaviour _host;
        private readonly RunEconomyPanelController _economy;
        private readonly TutorialDirector _unlock;
        private readonly TutorialDirector _rebirth;
        private bool _disposed;
        private bool _claimInFlight;
        private bool _gachaConfirmationInFlight;
        private bool _confirmDeathSettlementAfterOpenTutorial;

        public FirstRebirthTutorialAdapter(
            MonoBehaviour host,
            RunEconomyPanelController economy,
            TutorialDirector unlock,
            TutorialDirector rebirth)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _unlock = unlock ?? throw new ArgumentNullException(nameof(unlock));
            _rebirth = rebirth ?? throw new ArgumentNullException(nameof(rebirth));

            _economy.Settlement.TutorialPanelOpened += OnSettlementPanelOpened;
            _economy.Settlement.TutorialSettlementCommitted += OnSettlementCommitted;
            _economy.PetGacha.TutorialPanelOpened += OnGachaPanelOpened;
            _economy.PetGacha.TutorialRevealCompleted += OnGachaRevealCompleted;
            _economy.PetGacha.TutorialReturnedToMainMenu += OnReturnedToMainMenu;
            _economy.PlayerHub.TutorialPanelOpened += OnPlayerHubOpened;
            _economy.PlayerHub.TutorialWeaponAscendSucceeded += OnWeaponAscended;
            _economy.PlayerHub.TutorialPetsSectionShown += OnPetsSectionShown;
            _rebirth.StepPresented += OnRebirthStepPresented;
            _unlock.SequenceCompleted += OnOpenTutorialCompleted;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _economy.Settlement.TutorialPanelOpened -= OnSettlementPanelOpened;
            _economy.Settlement.TutorialSettlementCommitted -= OnSettlementCommitted;
            _economy.PetGacha.TutorialPanelOpened -= OnGachaPanelOpened;
            _economy.PetGacha.TutorialRevealCompleted -= OnGachaRevealCompleted;
            _economy.PetGacha.TutorialReturnedToMainMenu -= OnReturnedToMainMenu;
            _economy.PlayerHub.TutorialPanelOpened -= OnPlayerHubOpened;
            _economy.PlayerHub.TutorialWeaponAscendSucceeded -= OnWeaponAscended;
            _economy.PlayerHub.TutorialPetsSectionShown -= OnPetsSectionShown;
            _rebirth.StepPresented -= OnRebirthStepPresented;
            _unlock.SequenceCompleted -= OnOpenTutorialCompleted;
        }

        private void OnSettlementPanelOpened(bool isDeath)
        {
            _confirmDeathSettlementAfterOpenTutorial = isDeath;
            _unlock.TryReservePendingOwnership();
            _host.StartCoroutine(QueueUnlockRoutine(isDeath));
        }

        private IEnumerator QueueUnlockRoutine(bool isDeath)
        {
            yield return _unlock.QueueFromTrigger(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                isDeath ? "death" : "rebirth",
                useReturningStart: !isDeath);
            yield return _unlock.NotifyExternalEvent(
                isDeath ? "rebirth.death-panel" : "rebirth.panel-open");
        }

        private IEnumerator OnSettlementCommitted(string settlementType)
        {
            // Session 5 is a separate, generic post-settlement tutorial. It no
            // longer depends on a death/voluntary branch or a Session 4 wait.
            yield return _rebirth.QueueFromTrigger(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                settlementType);
        }

        private void OnOpenTutorialCompleted()
        {
            if (_disposed || !_confirmDeathSettlementAfterOpenTutorial) return;
            _confirmDeathSettlementAfterOpenTutorial = false;
            _host.StartCoroutine(ConfirmDeathSettlementNextFrame());
        }

        private IEnumerator ConfirmDeathSettlementNextFrame()
        {
            yield return null;
            if (!_disposed && !_economy.Settlement.TryConfirmSettlementForTutorial())
                StatusMessageService.ShowError(
                    "The restart confirmation is unavailable. Please try again.");
        }

        private void OnGachaPanelOpened() => Notify(_rebirth, "gacha.panel-open");
        private void OnGachaRevealCompleted() => Notify(_rebirth, "gacha.reveal-complete");
        private void OnReturnedToMainMenu() => Notify(_rebirth, "gacha.return-main-menu");
        private void OnPlayerHubOpened() => Notify(_rebirth, "hub.panel-open");
        private void OnWeaponAscended() => Notify(_rebirth, "hub.weapon-ascended");
        private void OnPetsSectionShown() => Notify(_rebirth, "hub.pets-shown");

        private void Notify(TutorialDirector director, string eventId)
        {
            if (!_disposed) _host.StartCoroutine(NotifyNextFrame(director, eventId));
        }

        private IEnumerator NotifyNextFrame(TutorialDirector director, string eventId)
        {
            // Panel and tab callbacks can fire inside the same target activation
            // that causes their preceding focus step to become a Wait state.
            yield return null;
            if (!_disposed) yield return director.NotifyExternalEvent(eventId);
        }

        private void OnRebirthStepPresented(TutorialStep step)
        {
            if (_disposed || step == null) return;
            if (!_claimInFlight && string.Equals(
                    step.Id, "wait-power-coin-grant", StringComparison.Ordinal))
            {
                _host.StartCoroutine(ClaimFirstPetGrantRoutine());
                return;
            }
            if (!_gachaConfirmationInFlight && string.Equals(
                    step.Id, "wait-pet-reveal", StringComparison.Ordinal))
                _host.StartCoroutine(ConfirmGachaAfterCheckpointRoutine());
        }

        private IEnumerator ConfirmGachaAfterCheckpointRoutine()
        {
            _gachaConfirmationInFlight = true;
            // The wait checkpoint now owns the latest player revision. Start
            // the pull only after that save, never concurrently with it.
            yield return null;
            if (!_disposed &&
                !_economy.PetGacha.TryConfirmOnePullForTutorial())
                yield return _rebirth.CompleteSafely(
                    "gacha-confirm-unavailable");
            _gachaConfirmationInFlight = false;
        }

        private IEnumerator ClaimFirstPetGrantRoutine()
        {
            _claimInFlight = true;
            PlayerSnapshot live = PlayerSessionStore.Instance?.Snapshot;
            if (live == null || PlayerLifecycleRuntime.Commands == null)
            {
                _claimInFlight = false;
                StatusMessageService.ShowError("The tutorial reward is unavailable. Please reload and try again.");
                yield break;
            }

            PlayerSnapshot saved = null;
            FirestoreRestClient.Failure? failure = null;
            var command = new PlayerLifecycleCommand
            {
                kind = PlayerLifecycleCommandKind.ClaimTutorialPowerCoinReward,
                operationId = Guid.NewGuid().ToString("N"),
                playerId = live.playerId,
                expectedRevision = live.revision,
                value = TutorialDirector.OnFirstRebirthId,
                tutorialPowerCoinReward = FirstPetGrant
            };
            yield return PlayerLifecycleRuntime.Commands.Execute(
                command,
                value => saved = value,
                value => failure = value);
            _claimInFlight = false;
            if (_disposed) yield break;
            if (saved == null)
            {
                StatusMessageService.ShowError(failure?.PlayerMessage ??
                    "The tutorial reward could not be saved. Please try again.");
                yield break;
            }

            MergeRewardIntoLiveSnapshot(live, saved);
            if (!_rebirth.RefreshProgressFromSession()) yield break;
            yield return _economy.Settlement.PlayTutorialPowerCoinGrant(
                FirstPetGrant);
            yield return _rebirth.NotifyExternalEvent("gacha.power-coin-granted");
        }

        private static void MergeRewardIntoLiveSnapshot(
            PlayerSnapshot live,
            PlayerSnapshot saved)
        {
            if (live == null || saved == null) return;
            live.schemaVersion = saved.schemaVersion;
            live.revision = saved.revision;
            if (live.wallet != null && saved.wallet != null)
                live.wallet.powerCoins = saved.wallet.powerCoins;
            live.tutorial = saved.tutorial;
            live.tutorialEntries = saved.tutorialEntries ??
                Array.Empty<PlayerSnapshot.TutorialEntryData>();
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
        }
    }
}
