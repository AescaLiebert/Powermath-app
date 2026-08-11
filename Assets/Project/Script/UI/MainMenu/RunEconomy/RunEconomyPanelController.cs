using System;
using PowerMath.Gameplay.Academic;
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

        public RunEconomyPanelController(
            MonoBehaviour host,
            VisualElement root,
            GameApiSettings settings,
            PlayerSnapshot player,
            QuestionCatalog questions,
            WeaponAscensionCatalogDefinition catalog,
            int baseAttack,
            int baseWeaponAttack,
            double baseCriticalRate,
            double baseCriticalDamagePercent)
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
        }

        public void Dispose()
        {
            _settlement.Dispose();
            _playerHub.Dispose();
        }
    }
}
