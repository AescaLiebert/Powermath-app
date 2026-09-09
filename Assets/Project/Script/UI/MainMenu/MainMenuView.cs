using System;
using PowerMath.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour
    {
        public event Action LogoutRequested;

        private Label _displayNameLabel;
        private Label _loadoutCharacterNameLabel;
        private Label _stageLabel;
        private ProgressBar _stageProgress;
        private Label _walletLabel;
        private Label _loadoutLabel;
        private VisualElement _content;
        private VisualElement _screen;
        private Button _logoutButton;
        private Label _sessionStatusLabel;
        private Label _silverLabel;
        private Label _goldLabel;
        private Label _diamondLabel;
        private Label _powerCoinLabel;
        private Label _weaponLabel;
        private Label _petLabel;
        private Button _playerMenuToggle;
        private Label _playerMenuChevron;
        private VisualElement _playerMenuContent;
        private VisualElement _playerMenuShadow;
        private IVisualElementScheduledItem _playerMenuSettle;
        private bool _eventsBound;
        private bool _bindingErrorLogged;

        public void Render(MainMenuViewModel model)
        {
            if (!TryBindElements())
            {
                return;
            }

            _displayNameLabel.text = model.DisplayName;
            _loadoutCharacterNameLabel.text = model.CharacterName;
            _loadoutCharacterNameLabel.tooltip = model.CharacterName;
            if (_stageLabel != null)
            {
                _stageLabel.text = model.StageText;
            }
            _stageProgress.value = model.StageProgress;
            _walletLabel.text = model.WalletText;
            _loadoutLabel.text = model.LoadoutText;
            _silverLabel.text = model.SilverText;
            _goldLabel.text = model.GoldText;
            _diamondLabel.text = model.DiamondText;
            _powerCoinLabel.text = model.PowerCoinText;
            _weaponLabel.text = model.WeaponText.ToUpperInvariant();
            _petLabel.text = model.PetText.ToUpperInvariant();
            _screen.SetSemanticState(UiSemanticState.Ready);
            if (_sessionStatusLabel != null)
            {
                _sessionStatusLabel.text = string.Empty;
                _sessionStatusLabel.AddToClassList("is-hidden");
            }
            _logoutButton.SetEnabled(true);
            _content.SetEnabled(true);
            _playerMenuToggle.SetEnabled(true);
        }

        public void RenderLogoutBusy()
        {
            if (!TryBindElements())
            {
                return;
            }

            _logoutButton.SetEnabled(false);
            _screen.SetSemanticState(UiSemanticState.Busy);
            if (_sessionStatusLabel != null)
            {
                _sessionStatusLabel.text = "Signing out...";
                _sessionStatusLabel.RemoveFromClassList("is-hidden");
            }
        }

        public void RenderLogoutFailure(string playerMessage)
        {
            if (!TryBindElements())
            {
                return;
            }

            _logoutButton.SetEnabled(true);
            _screen.SetSemanticState(UiSemanticState.Error);
            if (_sessionStatusLabel != null)
            {
                _sessionStatusLabel.text = playerMessage;
                _sessionStatusLabel.RemoveFromClassList("is-hidden");
            }
        }

        public void RenderUnavailable()
        {
            if (!TryBindElements())
            {
                return;
            }

            _displayNameLabel.text = "Player data unavailable";
            _loadoutCharacterNameLabel.text = "—";
            _loadoutCharacterNameLabel.tooltip = string.Empty;
            _screen.SetSemanticState(UiSemanticState.Blocked);
            if (_stageLabel != null)
            {
                _stageLabel.text = "Return to sign in";
            }
            _stageProgress.value = 0f;
            _walletLabel.text = string.Empty;
            _loadoutLabel.text = string.Empty;
            _silverLabel.text = "—";
            _goldLabel.text = "—";
            _diamondLabel.text = "—";
            _powerCoinLabel.text = "—";
            _weaponLabel.text = "NONE";
            _petLabel.text = "NONE";
            // Keep the shell and menu toggle interactive in offline/fallback
            // states. Feature controllers remain responsible for disabling
            // actions that require hydrated player data.
            _content.SetEnabled(true);
            _playerMenuToggle.SetEnabled(true);
        }

        private bool TryBindElements()
        {
            if (_displayNameLabel != null && _loadoutCharacterNameLabel != null &&
                _stageProgress != null && _walletLabel != null &&
                _loadoutLabel != null && _content != null &&
                _screen != null && _logoutButton != null &&
                _silverLabel != null && _goldLabel != null &&
                _diamondLabel != null && _powerCoinLabel != null &&
                _weaponLabel != null && _petLabel != null &&
                _playerMenuToggle != null && _playerMenuChevron != null &&
                _playerMenuContent != null &&
                _eventsBound)
            {
                return true;
            }

            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document == null
                ? null
                : document.rootVisualElement;

            if (root != null)
            {
                root.pickingMode = PickingMode.Ignore;
                _screen = root.Q<VisualElement>("main-menu-screen");
                if (_screen != null) _screen.pickingMode = PickingMode.Ignore;
                VisualElement safeArea = root.Q<VisualElement>("safe-area");
                if (safeArea != null) safeArea.pickingMode = PickingMode.Ignore;
                _displayNameLabel = root.Q<Label>("player-display-name");
                _loadoutCharacterNameLabel = root.Q<Label>("CharacterName");
                _stageLabel = root.Q<Label>("current-stage-label");
                _stageProgress = root.Q<ProgressBar>("stage-progress");
                _walletLabel = root.Q<Label>("wallet-summary");
                _loadoutLabel = root.Q<Label>("loadout-summary");
                _content = root.Q<VisualElement>("content");
                _logoutButton = root.Q<Button>("logout-button");
                _sessionStatusLabel = root.Q<Label>("session-status");
                _silverLabel = root.Q<Label>("profile-silver-value");
                _goldLabel = root.Q<Label>("profile-gold-value");
                _diamondLabel = root.Q<Label>("profile-diamond-value");
                _powerCoinLabel = root.Q<Label>("Currency_Value") ??
                    root.Q<Label>("player-menu-power-coins");
                _weaponLabel = root.Q<Label>("dashboard-weapon");
                _petLabel = root.Q<Label>("dashboard-pet");
                _playerMenuToggle = root.Q<Button>(className: "hud-player-menu-toggle");
                _playerMenuChevron = root.Q<Label>("Chevron");
                _playerMenuContent = root.Q<VisualElement>(
                    className: "hud-player-menu-content");
                _playerMenuShadow = root.Q<VisualElement>("Player Menu Shadow");
            }

            bool isBound = _displayNameLabel != null &&
                _loadoutCharacterNameLabel != null &&
                _stageProgress != null && _walletLabel != null &&
                _loadoutLabel != null && _content != null &&
                _screen != null && _logoutButton != null &&
                _silverLabel != null &&
                _goldLabel != null && _diamondLabel != null &&
                _powerCoinLabel != null && _weaponLabel != null &&
                _petLabel != null && _playerMenuToggle != null &&
                _playerMenuChevron != null && _playerMenuContent != null;

            if (isBound && !_eventsBound)
            {
                _logoutButton.clicked += OnLogoutClicked;
                _playerMenuToggle.clicked += TogglePlayerMenu;
                _eventsBound = true;
            }

            if (!isBound && !_bindingErrorLogged)
            {
                _bindingErrorLogged = true;
                PowerMath.Diagnostics.AppLog.Error(
                    "UI",
                    "MainMenuView could not find its required UI Toolkit elements."
                );
            }

            return isBound;
        }

        private void OnDisable()
        {
            _playerMenuSettle?.Pause();
            _playerMenuSettle = null;
            if (_eventsBound)
            {
                _logoutButton.clicked -= OnLogoutClicked;
                _playerMenuToggle.clicked -= TogglePlayerMenu;
                _eventsBound = false;
            }
        }

        private void OnLogoutClicked()
        {
            LogoutRequested?.Invoke();
        }

        private void TogglePlayerMenu()
        {
            bool collapsed = !_playerMenuContent.ClassListContains("is-collapsed");
            _playerMenuSettle?.Pause();
            _playerMenuSettle = null;
            SetPlayerMenuClass("is-follow-through", false);

            if (!collapsed && !_screen.ClassListContains("is-reduced-motion"))
                SetPlayerMenuClass("is-follow-through", true);

            SetPlayerMenuClass("is-collapsed", collapsed);
            _playerMenuChevron.text = collapsed ? ">" : "<";
            _playerMenuToggle.tooltip = collapsed
                ? "Open Player Menu"
                : "Close Player Menu";

            if (!collapsed &&
                _playerMenuContent.ClassListContains("is-follow-through"))
            {
                _playerMenuSettle = _playerMenuContent.schedule
                    .Execute(() =>
                    {
                        SetPlayerMenuClass("is-follow-through", false);
                        _playerMenuSettle = null;
                    })
                    .StartingIn(260);
            }
        }

        private void SetPlayerMenuClass(string className, bool enabled)
        {
            _playerMenuContent.EnableInClassList(className, enabled);
            _playerMenuShadow?.EnableInClassList(className, enabled);
            _playerMenuToggle.EnableInClassList(className, enabled);
        }
    }
}
