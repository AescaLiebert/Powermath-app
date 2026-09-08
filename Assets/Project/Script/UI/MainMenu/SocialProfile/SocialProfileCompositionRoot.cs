using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.UI.MainMenu.Admin;
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
        private AdminPanelController _admin;

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
                PowerMath.Diagnostics.AppLog.Error("SocialProfile", "Leaderboard and Profile Analytics require a loaded player session and API settings.");
                return;
            }

            _leaderboard = new LeaderboardPanelController(
                document.rootVisualElement, this, presenter.ApiSettings,
                session.Snapshot, provider.Host);
            _profile = new ProfileAnalyticsPanelController(
                document.rootVisualElement, this, presenter.ApiSettings,
                session.Snapshot, provider.Host);
            _admin = new AdminPanelController(
                document.rootVisualElement, this, presenter.ApiSettings,
                session.Snapshot, provider.Host);
            if (!_leaderboard.IsValid || !_profile.IsValid || !_admin.IsValid)
            {
                PowerMath.Diagnostics.AppLog.Error("SocialProfile", "Main Menu social/profile/admin UI elements are missing.");
                return;
            }
            _leaderboard.Bind();
            _profile.Bind();
            _admin.Bind();
            SynchronizeLeaderboardProjection(
                presenter.ApiSettings,
                session.Snapshot);
        }

        private void SynchronizeLeaderboardProjection(
            GameApiSettings settings,
            PlayerSnapshot player)
        {
#if UNITY_EDITOR
            if (settings != null && settings.UseEditorSampleStudent)
                return;
#endif
            var publisher = new FirestoreLeaderboardProjectionPublisher(settings);
            StartCoroutine(publisher.Publish(
                player,
                () => { },
                message => PowerMath.Diagnostics.AppLog.Warning("SocialProfile", message)));
        }

        private void OnDisable()
        {
            _leaderboard?.Dispose();
            _profile?.Dispose();
            _admin?.Dispose();
        }
    }
}
