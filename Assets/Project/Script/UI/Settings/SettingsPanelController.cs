using System;
using System.Collections;
using PowerMath.Audio;
using PowerMath.Gameplay.Combat;
using PowerMath.Localization;
using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.UI.Core;
using PowerMath.UI.MainMenu;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Settings
{
    public sealed class SettingsPanelController : IDisposable
    {
        private const string MusicKey = "powermath.settings.v1.music";
        private const string SfxKey = "powermath.settings.v1.sfx";
        private readonly VisualElement _root;
        private readonly MonoBehaviour _host;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly GameApiSettings _apiSettings;
        private readonly PlayerSnapshot _player;
        private readonly FirestoreAdminTuningService _tuning;
        private readonly IAuthenticationService _authenticationService;
        private readonly Button _open;
        private readonly VisualElement _modal;
        private readonly Button _close;
        private readonly Button _generalTab;
        private readonly Button _soundTab;
        private readonly Button _adminTab;
        private readonly VisualElement _generalPage;
        private readonly VisualElement _soundPage;
        private readonly VisualElement _adminPage;
        private readonly Toggle _fullscreen;
        private readonly Button _languageTh;
        private readonly Button _languageEn;
        private readonly Button _logoutButton;
        private readonly Label _accountStatus;
        private readonly Slider _music;
        private readonly Slider _sfx;
        private readonly Label _musicValue;
        private readonly Label _sfxValue;
        private readonly Label _adminStatus;
        private readonly TextField _attack;
        private readonly TextField _criticalRate;
        private readonly TextField _criticalDamage;
        private readonly Toggle _invincible;
        private readonly Toggle _bypassVideo;
        private readonly TextField _silver;
        private readonly TextField _gold;
        private readonly TextField _diamond;
        private readonly TextField _power;
        private readonly Button _applyCombat;
        private readonly Button _restoreHearts;
        private readonly TextField _stage;
        private readonly Button _applyStage;
        private readonly Button _rankSilver;
        private readonly Button _rankGold;
        private readonly Button _rankDiamond;
        private readonly Button _applyCurrency;
        private readonly Button _resetButton;
        private readonly Button _openAdminLeaderboard;
        private readonly PowerMath.UI.MainMenu.SocialProfile.AdminLeaderboardPanelController _adminLeaderboard;
        private bool _bound;
        private bool _busy;
        private bool _confirmingReset;
        private Coroutine _confirmRoutine;

        public SettingsPanelController(VisualElement root, MonoBehaviour host,
            IMainMenuPanelHost panelHost, GameApiSettings apiSettings, PlayerSnapshot player)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _panelHost = panelHost;
            _apiSettings = apiSettings;
            _player = player;
            _open = root.Q<Button>("Utility / Settings") ?? root.Q<Button>("setting") ?? root.Q<Button>("settings");
            _modal = root.Q<VisualElement>("settings-panel-modal");
            _close = root.Q<Button>("settings-panel-close");
            _generalTab = root.Q<Button>("settings-tab-general");
            _soundTab = root.Q<Button>("settings-tab-sound");
            _adminTab = root.Q<Button>("settings-tab-admin");
            _generalPage = root.Q<VisualElement>("settings-page-general");
            _soundPage = root.Q<VisualElement>("settings-page-sound");
            _adminPage = root.Q<VisualElement>("settings-page-admin");
            _fullscreen = root.Q<Toggle>("settings-fullscreen-toggle");
            _languageTh = root.Q<Button>("settings-language-th");
            _languageEn = root.Q<Button>("settings-language-en");
            _logoutButton = root.Q<Button>("settings-account-logout");
            _accountStatus = root.Q<Label>("settings-account-status");
            _music = root.Q<Slider>("settings-music-slider");
            _sfx = root.Q<Slider>("settings-sfx-slider");
            _musicValue = root.Q<Label>("settings-music-value");
            _sfxValue = root.Q<Label>("settings-sfx-value");
            _adminStatus = root.Q<Label>("settings-admin-status");
            _attack = root.Q<TextField>("settings-admin-atk");
            _criticalRate = root.Q<TextField>("settings-admin-cr");
            _criticalDamage = root.Q<TextField>("settings-admin-cd");
            _invincible = root.Q<Toggle>("settings-admin-invincible");
            _bypassVideo = root.Q<Toggle>("settings-admin-bypass-video");
            _silver = root.Q<TextField>("settings-admin-silver");
            _gold = root.Q<TextField>("settings-admin-gold");
            _diamond = root.Q<TextField>("settings-admin-diamond");
            _power = root.Q<TextField>("settings-admin-power");
            _applyCombat = root.Q<Button>("settings-admin-apply-combat");
            _restoreHearts = root.Q<Button>("settings-admin-restore-hp");
            _stage = root.Q<TextField>("settings-admin-stage");
            _applyStage = root.Q<Button>("settings-admin-apply-stage");
            _rankSilver = root.Q<Button>("settings-rank-silver");
            _rankGold = root.Q<Button>("settings-rank-gold");
            _rankDiamond = root.Q<Button>("settings-rank-diamond");
            _applyCurrency = root.Q<Button>("settings-admin-apply-currency");
            _resetButton = root.Q<Button>("settings-admin-reset");
            _openAdminLeaderboard = root.Q<Button>("settings-admin-open-leaderboard");
            _adminLeaderboard = new PowerMath.UI.MainMenu.SocialProfile.AdminLeaderboardPanelController(
                root, host, apiSettings, player);
            _tuning = player == null ? null : new FirestoreAdminTuningService(apiSettings, player);
#if UNITY_EDITOR
            _authenticationService = apiSettings == null
                ? null
                : apiSettings.UseEditorSampleStudent
                    ? (IAuthenticationService)new EditorMockAuthenticationService()
                    : new DirectFirestoreAuthenticationService(apiSettings);
#else
            _authenticationService = apiSettings == null
                ? null
                : new DirectFirestoreAuthenticationService(apiSettings);
#endif
        }

        public bool IsValid => _open != null && _modal != null && _close != null &&
            _generalTab != null && _soundTab != null && _generalPage != null &&
            _soundPage != null && _fullscreen != null && _languageTh != null &&
            _languageEn != null && _logoutButton != null && _music != null && _sfx != null;

        public void Bind()
        {
            if (_bound || !IsValid) return;
            _open.clicked += Open;
            _close.clicked += Close;
            _generalTab.clicked += ShowGeneral;
            _soundTab.clicked += ShowSound;
            if (_adminTab != null) _adminTab.clicked += ShowAdmin;
            _fullscreen.RegisterValueChangedCallback(OnFullscreenChanged);
            _languageTh.clicked += SelectThai;
            _languageEn.clicked += SelectEnglish;
            _logoutButton.clicked += Logout;
            LocalizationService.Changed += RefreshLanguageButtons;
            _music.RegisterValueChangedCallback(OnMusicChanged);
            _sfx.RegisterValueChangedCallback(OnSfxChanged);
            BindAdmin();
            ApplySavedAudio();
            SetAdminVisibility(AdminAccountAccessPolicy.IsAuthorized(_player));
            RefreshLanguageButtons();
            _modal.style.display = DisplayStyle.None;
            _bound = true;
        }

        public void Dispose()
        {
            if (!_bound) return;
            _open.clicked -= Open;
            _close.clicked -= Close;
            _generalTab.clicked -= ShowGeneral;
            _soundTab.clicked -= ShowSound;
            if (_adminTab != null) _adminTab.clicked -= ShowAdmin;
            _fullscreen.UnregisterValueChangedCallback(OnFullscreenChanged);
            _languageTh.clicked -= SelectThai;
            _languageEn.clicked -= SelectEnglish;
            _logoutButton.clicked -= Logout;
            LocalizationService.Changed -= RefreshLanguageButtons;
            _music.UnregisterValueChangedCallback(OnMusicChanged);
            _sfx.UnregisterValueChangedCallback(OnSfxChanged);
            UnbindAdmin();
            CancelConfirm();
            if (_panelHost?.OpenPanel == MainMenuPanelId.Settings)
                _panelHost.TryClose(MainMenuPanelId.Settings, _open);
            else SetVisible(false);
            _bound = false;
        }

        private void Open()
        {
            if (_panelHost != null)
            {
                if (!_panelHost.TryOpen(MainMenuPanelId.Settings, _modal, _open)) return;
            }
            else SetVisible(true);
            _fullscreen.SetValueWithoutNotify(Screen.fullScreen);
            ShowGeneral();
        }

        private void Close()
        {
            if (_busy) return;
            CancelConfirm();
            _adminLeaderboard?.Close();
            if (_panelHost != null) _panelHost.TryClose(MainMenuPanelId.Settings, _open);
            else
            {
                SetVisible(false);
                _open.Focus();
            }
            PlayerPrefs.Save();
        }

        private void SetVisible(bool visible)
        {
            _modal.EnableInClassList("is-hidden", !visible);
            _modal.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible) _modal.Focus();
        }

        private void ShowGeneral() => SelectPage(_generalPage, _generalTab);
        private void ShowSound() => SelectPage(_soundPage, _soundTab);
        private void ShowAdmin()
        {
            if (AdminAccountAccessPolicy.IsAuthorized(_player)) SelectPage(_adminPage, _adminTab);
        }

        private void SelectPage(VisualElement page, Button tab)
        {
            _generalPage.EnableInClassList("is-hidden", page != _generalPage);
            _soundPage.EnableInClassList("is-hidden", page != _soundPage);
            if (_adminPage != null) _adminPage.EnableInClassList("is-hidden", page != _adminPage);
            _generalTab.EnableInClassList("is-selected", tab == _generalTab);
            _soundTab.EnableInClassList("is-selected", tab == _soundTab);
            _adminTab?.EnableInClassList("is-selected", tab == _adminTab);
        }

        private void ApplySavedAudio()
        {
            float music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
            float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            _music.SetValueWithoutNotify(music);
            _sfx.SetValueWithoutNotify(sfx);
            MusicController.Instance.MusicVolume = music;
            SfxController.Instance.SfxVolume = sfx;
            RenderVolume(_musicValue, music);
            RenderVolume(_sfxValue, sfx);
        }

        private void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            bool accepted;
            if (evt.newValue)
            {
                accepted = Screen.fullScreen || WebFullscreenController.TryEnterLandscapeFullscreen();
            }
            else
            {
                Screen.fullScreen = false;
                accepted = true;
            }
            if (!accepted)
            {
                _fullscreen.SetValueWithoutNotify(Screen.fullScreen);
                StatusMessageService.ShowWarning(
                    "Fullscreen was blocked. Tap the switch again from the game window.");
            }
        }

        private void OnMusicChanged(ChangeEvent<float> evt)
        {
            float value = Mathf.Clamp01(evt.newValue);
            MusicController.Instance.MusicVolume = value;
            PlayerPrefs.SetFloat(MusicKey, value);
            RenderVolume(_musicValue, value);
        }

        private void SelectThai() => SelectLanguage("th");
        private void SelectEnglish() => SelectLanguage("en");

        private void SelectLanguage(string locale)
        {
            LocalizationService.SetLocale(locale);
            StatusMessageService.ShowInfo(locale == "th" ? "ภาษาไทย" : "English");
        }

        private void RefreshLanguageButtons()
        {
            string locale = LocalizationService.Locale;
            _languageTh?.EnableInClassList("is-selected", locale == "th");
            _languageEn?.EnableInClassList("is-selected", locale == "en");
        }

        private void Logout()
        {
            if (_busy) return;
            _busy = true;
            _logoutButton.SetEnabled(false);
            _close.SetEnabled(false);
            if (_accountStatus != null)
            {
                _accountStatus.text = LocalizationService.Get("settings.signingOut");
                _accountStatus.RemoveFromClassList("is-error");
            }

            if (_authenticationService == null)
            {
                DirectFirestoreCredentialStore.Clear();
                CompleteLogout();
                return;
            }

            _host.StartCoroutine(_authenticationService.Logout(
                CompleteLogout,
                failure =>
                {
                    _busy = false;
                    _logoutButton.SetEnabled(true);
                    _close.SetEnabled(true);
                    if (_accountStatus != null)
                    {
                        _accountStatus.text = failure.PlayerMessage;
                        _accountStatus.AddToClassList("is-error");
                    }
                    StatusMessageService.ShowError(failure.PlayerMessage);
                }));
        }

        private void CompleteLogout()
        {
            DirectFirestoreCredentialStore.Clear();
            PlayerSessionStore.Instance?.Clear();
            PlayerPrefs.Save();
            string scene = _apiSettings != null &&
                !string.IsNullOrWhiteSpace(_apiSettings.AuthenticationSceneName)
                    ? _apiSettings.AuthenticationSceneName
                    : "AuthenticationScene";
            _host.StartCoroutine(ReloadNextFrame(scene));
        }

        private void OnSfxChanged(ChangeEvent<float> evt)
        {
            float value = Mathf.Clamp01(evt.newValue);
            SfxController.Instance.SfxVolume = value;
            PlayerPrefs.SetFloat(SfxKey, value);
            RenderVolume(_sfxValue, value);
        }

        private static void RenderVolume(Label label, float value)
        {
            if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void SetAdminVisibility(bool visible)
        {
            _adminTab?.EnableInClassList("is-hidden", !visible);
            _adminPage?.EnableInClassList("is-hidden", true);
            if (!visible) return;
            Label account = _root.Q<Label>("settings-admin-account");
            if (account != null && AdminAccountAccessPolicy.TryGetUsername(CurrentPlayer, out string username))
                account.text = username + " · changes apply only to this account";
            if (CurrentPlayer?.wallet != null)
            {
                _silver.SetValueWithoutNotify(CurrentPlayer.wallet.silver.ToString());
                _gold.SetValueWithoutNotify(CurrentPlayer.wallet.gold.ToString());
                _diamond.SetValueWithoutNotify(CurrentPlayer.wallet.diamond.ToString());
                _power.SetValueWithoutNotify(CurrentPlayer.wallet.powerCoins.ToString());
            }
            if (CurrentPlayer?.progression != null)
            {
                _stage?.SetValueWithoutNotify(CurrentPlayer.progression.currentStage.ToString());
            }
            PlayerSnapshot.AdminTuningData tuning = CurrentPlayer?.adminTuning;
            bool bypassSaved = (tuning != null && tuning.bypassVideoQuestion) ||
                PlayerPrefs.GetInt("PowerMath.Admin.BypassVideoQuestion", 0) == 1;
            _bypassVideo?.SetValueWithoutNotify(bypassSaved);
            if (tuning != null && tuning.combatOverrideEnabled)
            {
                _attack.SetValueWithoutNotify(tuning.attack.ToString());
                _criticalRate.SetValueWithoutNotify((tuning.criticalRateBasisPoints / 100f).ToString("0.##"));
                _criticalDamage.SetValueWithoutNotify((tuning.criticalDamageBasisPoints / 100f).ToString("0.##"));
                _invincible.SetValueWithoutNotify(tuning.invincible);
            }
        }

        private void BindAdmin()
        {
            if (_openAdminLeaderboard != null) _openAdminLeaderboard.clicked += OpenAdminLeaderboard;
            _adminLeaderboard?.Bind();
            if (_applyCombat == null) return;
            _applyCombat.clicked += ApplyCombat;
            _restoreHearts.clicked += RestoreHearts;
            if (_applyStage != null) _applyStage.clicked += ApplyStage;
            _rankSilver.clicked += SetSilverRank;
            _rankGold.clicked += SetGoldRank;
            _rankDiamond.clicked += SetDiamondRank;
            _applyCurrency.clicked += ApplyCurrency;
            _resetButton.clicked += ResetClicked;
            _bypassVideo?.RegisterValueChangedCallback(OnBypassVideoChanged);
        }

        private void UnbindAdmin()
        {
            if (_openAdminLeaderboard != null) _openAdminLeaderboard.clicked -= OpenAdminLeaderboard;
            _adminLeaderboard?.Dispose();
            if (_applyCombat == null) return;
            _applyCombat.clicked -= ApplyCombat;
            _restoreHearts.clicked -= RestoreHearts;
            if (_applyStage != null) _applyStage.clicked -= ApplyStage;
            _rankSilver.clicked -= SetSilverRank;
            _rankGold.clicked -= SetGoldRank;
            _rankDiamond.clicked -= SetDiamondRank;
            _applyCurrency.clicked -= ApplyCurrency;
            _resetButton.clicked -= ResetClicked;
            _bypassVideo?.UnregisterValueChangedCallback(OnBypassVideoChanged);
        }

        private void OpenAdminLeaderboard()
        {
            if (_adminLeaderboard == null || !_adminLeaderboard.IsValid) return;
            _modal.style.display = DisplayStyle.None;
            _modal.AddToClassList("is-hidden");
            _adminLeaderboard.Open(() =>
            {
                if (_modal != null && (_panelHost?.OpenPanel == MainMenuPanelId.Settings || _panelHost == null))
                {
                    _modal.style.display = DisplayStyle.Flex;
                    _modal.RemoveFromClassList("is-hidden");
                    _modal.Focus();
                }
            });
        }

        private void OnBypassVideoChanged(ChangeEvent<bool> evt)
        {
            PlayerPrefs.SetInt("PowerMath.Admin.BypassVideoQuestion", evt.newValue ? 1 : 0);
            PlayerPrefs.Save();
            if (CurrentPlayer != null)
            {
                if (CurrentPlayer.adminTuning == null)
                    CurrentPlayer.adminTuning = new PlayerSnapshot.AdminTuningData();
                CurrentPlayer.adminTuning.bypassVideoQuestion = evt.newValue;
            }
        }

        private void ApplyCombat()
        {
            if (!TryAdmin() || !int.TryParse(_attack.value, out int attack) || attack < 1 || attack > 1000000 ||
                !float.TryParse(_criticalRate.value, out float criticalRate) || criticalRate < 0 || criticalRate > 100 ||
                !float.TryParse(_criticalDamage.value, out float criticalDamage) || criticalDamage < 0 || criticalDamage > 10000)
            {
                Status("Enter ATK 1–1,000,000, CR 0–100, and CD 0–10,000.", true);
                return;
            }
            int rateBasisPoints = Mathf.RoundToInt(criticalRate * 100f);
            int damageBasisPoints = Mathf.RoundToInt(criticalDamage * 100f);
            bool bypass = _bypassVideo != null && _bypassVideo.value;
            PlayerPrefs.SetInt("PowerMath.Admin.BypassVideoQuestion", bypass ? 1 : 0);
            PlayerPrefs.Save();
            if (CurrentPlayer != null)
            {
                if (CurrentPlayer.adminTuning == null)
                    CurrentPlayer.adminTuning = new PlayerSnapshot.AdminTuningData();
                CurrentPlayer.adminTuning.bypassVideoQuestion = bypass;
            }
            Run(
                _tuning == null
                    ? (AdminCommand)null
                    : (ok, fail) => _tuning.SetCombat(
                        attack,
                        rateBasisPoints,
                        damageBasisPoints,
                        _invincible.value,
                        bypass,
                        ok,
                        fail),
                "Saving combat settings to Firebase...",
                "Combat settings saved.",
                reload: true);
        }

        private void RestoreHearts() => Run(
            _tuning == null ? (AdminCommand)null : _tuning.RestoreHearts,
            "Restoring hearts...", "All hearts restored.", reload: true);

        private void ApplyStage()
        {
            if (!TryAdmin() || !int.TryParse(_stage.value, out int stage) ||
                stage < StageId.First || stage > StageId.Final)
            {
                Status($"Enter a valid stage between {StageId.First} and {StageId.Final}.", true);
                return;
            }
            Run(
                _tuning == null
                    ? (AdminCommand)null
                    : (ok, fail) => _tuning.SetStage(stage, ok, fail),
                "Teleporting to stage " + stage + "...",
                "Teleported to stage " + stage + ".",
                reload: true);
        }
        private void SetSilverRank() => SetRank("Silver");
        private void SetGoldRank() => SetRank("Gold");
        private void SetDiamondRank() => SetRank("Diamond");
        private void SetRank(string rank) => Run(
            _tuning == null ? (AdminCommand)null : (ok, fail) => _tuning.SetRank(rank, ok, fail),
            "Changing Rank and clearing audit...",
            "Rank changed to " + rank + ".",
            reload: true);

        private void ApplyCurrency()
        {
            if (!long.TryParse(_silver.value, out long silver) || !long.TryParse(_gold.value, out long gold) ||
                !long.TryParse(_diamond.value, out long diamond) || !long.TryParse(_power.value, out long power))
            {
                Status("Enter whole, non-negative currency values.", true);
                return;
            }
            Run(_tuning == null
                    ? (AdminCommand)null
                    : (ok, fail) => _tuning.SetCurrency(silver, gold, diamond, power, ok, fail),
                "Updating currency...", "Currency updated.");
        }

        private delegate IEnumerator AdminCommand(Action succeeded, Action<string> failed);
        private void Run(
            AdminCommand command,
            string pending,
            string success,
            bool reload = false)
        {
            if (!TryAdmin() || _busy || command == null) return;
            _busy = true;
            SetAdminEnabled(false);
            Status(pending);
            _host.StartCoroutine(command(() =>
            {
                _busy = false;
                SetAdminEnabled(true);
                Status(success);
                if (reload) ReloadMainMenu();
            }, error =>
            {
                _busy = false;
                SetAdminEnabled(true);
                Status(error, true);
            }));
        }

        private bool TryAdmin()
        {
            bool allowed = AdminAccountAccessPolicy.IsAuthorized(CurrentPlayer);
            if (!allowed) Status("This account is not authorized for admin tools.", true);
            return allowed;
        }

        private void ResetClicked()
        {
            if (!TryAdmin() || _busy) return;
            if (!_confirmingReset)
            {
                _confirmingReset = true;
                _resetButton.text = "CONFIRM RESET — CANNOT UNDO";
                _resetButton.AddToClassList("is-confirming");
                Status("Click again within 5 seconds to erase this account's game data.", true);
                _confirmRoutine = _host.StartCoroutine(ResetConfirmTimeout());
                return;
            }
            CancelConfirm();
            _busy = true;
            SetAdminEnabled(false);
            Status("Resetting user data...");
            var reset = new FirestorePlayerResetService(_apiSettings, CurrentPlayer);
            _host.StartCoroutine(reset.ResetUserData(OnResetSucceeded, error =>
            {
                _busy = false;
                SetAdminEnabled(true);
                Status(error, true);
            }));
        }

        private void OnResetSucceeded()
        {
            Status("User data reset. Loading the fresh start...");
            PlayerSessionStore.Instance?.Clear();
            string scene = _apiSettings != null && !string.IsNullOrWhiteSpace(_apiSettings.BootstrapSceneName)
                ? _apiSettings.BootstrapSceneName
                : "BootstrapScene";
            _host.StartCoroutine(ReloadNextFrame(scene));
        }

        private IEnumerator ResetConfirmTimeout()
        {
            yield return new WaitForSeconds(5f);
            _confirmRoutine = null;
            _confirmingReset = false;
            _resetButton.text = "RESET USER DATA";
            _resetButton.RemoveFromClassList("is-confirming");
            Status("Reset cancelled.");
        }

        private void CancelConfirm()
        {
            if (_confirmRoutine != null) _host.StopCoroutine(_confirmRoutine);
            _confirmRoutine = null;
            _confirmingReset = false;
            if (_resetButton != null)
            {
                _resetButton.text = "RESET USER DATA";
                _resetButton.RemoveFromClassList("is-confirming");
            }
        }

        private void SetAdminEnabled(bool enabled)
        {
            _applyCombat?.SetEnabled(enabled); _restoreHearts?.SetEnabled(enabled);
            _applyStage?.SetEnabled(enabled);
            _rankSilver?.SetEnabled(enabled); _rankGold?.SetEnabled(enabled); _rankDiamond?.SetEnabled(enabled);
            _applyCurrency?.SetEnabled(enabled); _resetButton?.SetEnabled(enabled); _close?.SetEnabled(enabled);
        }

        private void Status(string message, bool error = false)
        {
            if (_adminStatus == null) return;
            _adminStatus.text = message;
            _adminStatus.EnableInClassList("is-error", error);
        }

        private void ReloadMainMenu()
        {
            if (_panelHost == null) return;
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            _host.StartCoroutine(ReloadNextFrame(sceneName));
        }

        private PlayerSnapshot CurrentPlayer =>
            PlayerSessionStore.Instance?.Snapshot ?? _player;

        private static IEnumerator ReloadNextFrame(string sceneName)
        {
            yield return null;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

}
