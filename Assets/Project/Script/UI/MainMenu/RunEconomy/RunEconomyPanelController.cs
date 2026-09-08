using System;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Combat.Unity;
using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class RunEconomyPanelController : IDisposable
    {
        private readonly RunSettlementPanelController _settlement;
        private readonly PlayerHubPanelController _playerHub;
        private readonly PetGachaPanelController _petGacha;
        private readonly CryptoPetGachaRandomSource _petGachaRandom;

        public RunEconomyPanelController(
            MonoBehaviour host,
            VisualElement root,
            GameApiSettings settings,
            PlayerSnapshot player,
            QuestionCatalog questions,
            WeaponAscensionCatalogDefinition catalog,
            PetGachaCatalogDefinition petGachaDefinition,
            int baseAttack,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent,
            AudioSource audioSource,
            bool reducedMotion,
            IUiMotionDriver motionDriver,
            IMainMenuPanelHost panelHost,
            IMainMenuInteractionGate interactionGate = null,
            ActorPresentationController playerActor = null)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (panelHost == null) throw new ArgumentNullException(nameof(panelHost));

            PetGachaCatalog petCatalog = null;
            IPetGachaCommandStore petStore = null;
            IPetEquipCommandStore petEquipStore = null;
            string unavailableReason;
            if (petGachaDefinition == null)
            {
                unavailableReason = "Pet Gacha content is not configured.";
            }
            else if (!petGachaDefinition.TryBuildCatalog(
                         out petCatalog,
                         out unavailableReason))
            {
                unavailableReason = "Pet Gacha content is invalid: " + unavailableReason;
            }
            else if (settings == null)
            {
                unavailableReason = "Pet Gacha is unavailable in offline mode.";
            }
            else
            {
                _petGachaRandom = new CryptoPetGachaRandomSource();
                try
                {
                    petStore = new FirestorePetGachaCommandStore(
                        settings,
                        player,
                        petCatalog,
                        _petGachaRandom);
                    petEquipStore = new FirestorePetEquipCommandStore(
                        settings,
                        player,
                        petGachaDefinition);
                    unavailableReason = string.Empty;
                }
                catch (Exception exception)
                {
                    unavailableReason = "Pet Gacha offline: " + exception.Message;
                }
            }

            FirestoreProgressionCommandStore store = null;
            if (settings != null)
            {
                try
                {
                    store = new FirestoreProgressionCommandStore(
                        settings,
                        player,
                        questions,
                        baseWeaponAttack);
                }
                catch (Exception exception)
                {
                    PowerMath.Diagnostics.AppLog.Warning("Progression", $"Progression store offline: {exception.Message}");
                }
            }

            var publisher = settings != null
                ? new FirestoreLeaderboardProjectionPublisher(settings)
                : null;
            MainMenuSharedOverlayController sharedOverlay =
                MainMenuSharedOverlayController.GetOrCreate(
                    host.gameObject,
                    root,
                    panelHost);
            _settlement = new RunSettlementPanelController(
                host,
                root,
                player,
                store,
                publisher,
                baseAttack,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent,
                petCatalog,
                panelHost,
                interactionGate,
                playerActor,
                reducedMotion);
            _playerHub = new PlayerHubPanelController(
                host,
                root,
                player,
                store,
                publisher,
                catalog,
                petGachaDefinition,
                petCatalog,
                petEquipStore,
                baseAttack,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent,
                audioSource,
                motionDriver,
                panelHost,
                sharedOverlay);

            _petGacha = new PetGachaPanelController(
                host,
                root,
                player,
                petGachaDefinition,
                petCatalog,
                petStore,
                publisher,
                audioSource,
                reducedMotion,
                unavailableReason,
                panelHost);
        }

        public void Dispose()
        {
            _settlement.Dispose();
            _playerHub.Dispose();
            _petGacha.Dispose();
            _petGachaRandom?.Dispose();
        }

        public void NotifyTerminalPresentationCompleted(CombatPhase phase)
        {
            if (phase == CombatPhase.RunDefeat)
                _settlement.NotifyDeathPresentationCompleted();
        }
    }
}
