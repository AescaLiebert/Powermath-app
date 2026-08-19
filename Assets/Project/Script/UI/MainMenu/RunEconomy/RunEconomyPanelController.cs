using System;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using PowerMath.Session;
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
            bool reducedMotion)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (player == null) throw new ArgumentNullException(nameof(player));

            var store = new FirestoreProgressionCommandStore(
                settings,
                player,
                questions,
                baseWeaponAttack);
            var publisher = new FirestoreLeaderboardProjectionPublisher(settings);
            _settlement = new RunSettlementPanelController(
                host,
                root,
                player,
                store,
                publisher,
                baseAttack,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent);
            _playerHub = new PlayerHubPanelController(
                host,
                root,
                player,
                store,
                publisher,
                catalog,
                baseAttack,
                baseWeaponAttack,
                baseCriticalRate,
                baseCriticalDamagePercent);

            PetGachaCatalog petCatalog = null;
            IPetGachaCommandStore petStore = null;
            string unavailableReason;
            if (petGachaDefinition == null)
            {
                unavailableReason = "Pet Gacha content is not configured.";
            }
            else if (!petGachaDefinition.TryBuildCatalog(out petCatalog, out unavailableReason))
            {
                unavailableReason = "Pet Gacha content is invalid: " + unavailableReason;
            }
            else
            {
                _petGachaRandom = new CryptoPetGachaRandomSource();
                petStore = new FirestorePetGachaCommandStore(
                    settings,
                    player,
                    petCatalog,
                    _petGachaRandom);
                unavailableReason = string.Empty;
            }
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
                unavailableReason);
        }

        public void Dispose()
        {
            _settlement.Dispose();
            _playerHub.Dispose();
            _petGacha.Dispose();
            _petGachaRandom?.Dispose();
        }
    }
}
