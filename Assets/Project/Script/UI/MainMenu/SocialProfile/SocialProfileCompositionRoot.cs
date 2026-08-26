using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.SocialProfile
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SocialProfileCompositionRoot : MonoBehaviour
    {
        private LeaderboardPanelController _leaderboard;
        private ProfileAnalyticsPanelController _profile;

        private void Start()
        {
            UIDocument document = GetComponent<UIDocument>();
            PlayerSessionStore session = PlayerSessionStore.Instance;
            MainMenuPresenter presenter = GetComponent<MainMenuPresenter>();
            MainMenuPanelHostProvider provider =
                GetComponent<MainMenuPanelHostProvider>();
            if (document == null || document.rootVisualElement == null ||
                session == null || !session.IsReady || session.Snapshot == null ||
                presenter == null || presenter.ApiSettings == null || provider == null)
            {
                Debug.LogError("Leaderboard and Profile Analytics require a loaded player session and API settings.");
                return;
            }

            _leaderboard = new LeaderboardPanelController(
                document.rootVisualElement, this, presenter.ApiSettings,
                session.Snapshot, provider.Host);
            _profile = new ProfileAnalyticsPanelController(
                document.rootVisualElement, this, presenter.ApiSettings,
                session.Snapshot, provider.Host);
            if (!_leaderboard.IsValid || !_profile.IsValid)
            {
                Debug.LogError("Main Menu social/profile UI elements are missing.");
                return;
            }
            _leaderboard.Bind();
            _profile.Bind();
        }

        private void OnDisable()
        {
            _leaderboard?.Dispose();
            _profile?.Dispose();
        }
    }
}
