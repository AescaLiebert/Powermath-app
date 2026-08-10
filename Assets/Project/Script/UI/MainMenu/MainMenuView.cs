using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour
    {
        public event Action LogoutRequested;

        private Label _displayNameLabel;
        private Label _stageLabel;
        private ProgressBar _stageProgress;
        private Label _walletLabel;
        private Label _loadoutLabel;
        private VisualElement _content;
        private Button _logoutButton;
        private Label _sessionStatusLabel;
        private bool _eventsBound;
        private bool _bindingErrorLogged;

        public void Render(MainMenuViewModel model)
        {
            if (!TryBindElements())
            {
                return;
            }

            _displayNameLabel.text = model.DisplayName;
            _stageLabel.text = model.StageText;
            _stageProgress.value = model.StageProgress;
            _walletLabel.text = model.WalletText;
            _loadoutLabel.text = model.LoadoutText;
            _sessionStatusLabel.text = string.Empty;
            _logoutButton.SetEnabled(true);
            _content.SetEnabled(true);
        }

        public void RenderLogoutBusy()
        {
            if (!TryBindElements())
            {
                return;
            }

            _logoutButton.SetEnabled(false);
            _sessionStatusLabel.text = "Signing out...";
        }

        public void RenderLogoutFailure(string playerMessage)
        {
            if (!TryBindElements())
            {
                return;
            }

            _logoutButton.SetEnabled(true);
            _sessionStatusLabel.text = playerMessage;
        }

        public void RenderUnavailable()
        {
            if (!TryBindElements())
            {
                return;
            }

            _displayNameLabel.text = "Player data unavailable";
            _stageLabel.text = "Return to sign in";
            _stageProgress.value = 0f;
            _walletLabel.text = string.Empty;
            _loadoutLabel.text = string.Empty;
            _content.SetEnabled(false);
        }

        private bool TryBindElements()
        {
            if (_displayNameLabel != null && _stageLabel != null &&
                _stageProgress != null && _walletLabel != null &&
                _loadoutLabel != null && _content != null &&
                _logoutButton != null && _sessionStatusLabel != null &&
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
                _displayNameLabel = root.Q<Label>("player-display-name");
                _stageLabel = root.Q<Label>("current-stage-label");
                _stageProgress = root.Q<ProgressBar>("stage-progress");
                _walletLabel = root.Q<Label>("wallet-summary");
                _loadoutLabel = root.Q<Label>("loadout-summary");
                _content = root.Q<VisualElement>("content");
                _logoutButton = root.Q<Button>("logout-button");
                _sessionStatusLabel = root.Q<Label>("session-status");
            }

            bool isBound = _displayNameLabel != null && _stageLabel != null &&
                _stageProgress != null && _walletLabel != null &&
                _loadoutLabel != null && _content != null &&
                _logoutButton != null && _sessionStatusLabel != null;

            if (isBound && !_eventsBound)
            {
                _logoutButton.clicked += OnLogoutClicked;
                _eventsBound = true;
            }

            if (!isBound && !_bindingErrorLogged)
            {
                _bindingErrorLogged = true;
                Debug.LogError(
                    "MainMenuView could not find its required UI Toolkit elements."
                );
            }

            return isBound;
        }

        private void OnDisable()
        {
            if (_eventsBound)
            {
                _logoutButton.clicked -= OnLogoutClicked;
                _eventsBound = false;
            }
        }

        private void OnLogoutClicked()
        {
            LogoutRequested?.Invoke();
        }
    }
}
