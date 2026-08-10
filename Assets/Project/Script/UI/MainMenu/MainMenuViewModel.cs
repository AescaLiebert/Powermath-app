using PowerMath.PlayerData;
using UnityEngine;

namespace PowerMath.UI.MainMenu
{
    public readonly struct MainMenuViewModel
    {
        private const int FinalStage = 200;

        public MainMenuViewModel(
            string displayName,
            string stageText,
            float stageProgress,
            string walletText,
            string loadoutText)
        {
            DisplayName = displayName;
            StageText = stageText;
            StageProgress = stageProgress;
            WalletText = walletText;
            LoadoutText = loadoutText;
        }

        public string DisplayName { get; }

        public string StageText { get; }

        public float StageProgress { get; }

        public string WalletText { get; }

        public string LoadoutText { get; }

        public static MainMenuViewModel From(PlayerSnapshot snapshot)
        {
            int currentStage = ResolveCurrentStage(snapshot);
            float stageProgress = Mathf.Clamp01(currentStage / (float)FinalStage) * 100f;

            string displayName = snapshot.profile.displayName;
            string stageText = $"Stage {currentStage} / {FinalStage}";
            string walletText = snapshot.wallet == null
                ? "Power Coins: --"
                : $"Power Coins: {snapshot.wallet.powerCoins}";

            string weapon = ResolveItemName(
                snapshot.loadout == null ? null : snapshot.loadout.weaponId
            );
            string pet = ResolveItemName(
                snapshot.loadout == null ? null : snapshot.loadout.petId
            );
            string loadoutText = $"Weapon: {weapon}  |  Pet: {pet}";

            return new MainMenuViewModel(
                displayName,
                stageText,
                stageProgress,
                walletText,
                loadoutText
            );
        }

        private static int ResolveCurrentStage(PlayerSnapshot snapshot)
        {
            if (snapshot.activeRun != null && snapshot.activeRun.currentStage > 0)
            {
                return Mathf.Clamp(snapshot.activeRun.currentStage, 1, FinalStage);
            }

            if (snapshot.progression != null && snapshot.progression.currentStage > 0)
            {
                return Mathf.Clamp(snapshot.progression.currentStage, 1, FinalStage);
            }

            return 1;
        }

        private static string ResolveItemName(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId) ? "None" : itemId;
        }
    }
}
