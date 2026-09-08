using System;
using PowerMath.Session;
using PowerMath.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Authentication
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class AuthenticationView : MonoBehaviour
    {
        public readonly struct LoginIntent
        {
            public LoginIntent(
                string username,
                string password,
                bool rememberDevice)
            {
                Username = username;
                Password = password;
                RememberDevice = rememberDevice;
            }

            public string Username { get; }
            public string Password { get; }
            public bool RememberDevice { get; }
        }

        public event Action<LoginIntent> LoginRequested;

        private TextField _usernameField;
        private TextField _passwordField;
        private Toggle _rememberToggle;
        private Button _loginButton;
        private Label _statusLabel;
        private VisualElement _root;
        private Button _worldwideButton;
        private VisualElement _languageFlyout;
        private Button _thButton;
        private Button _enButton;
        private bool _isLanguageFlyoutOpen;
        private string _statusKey = "auth.help";
        private bool _eventsBound;
        private bool _bindingErrorLogged;

        public void RenderReady()
        {
            if (!TryBindElements())
            {
                return;
            }

            SetInteractive(true);
            _root.SetSemanticState(UiSemanticState.Ready);
            _statusKey = "auth.help";
            RefreshLocale();
            _statusLabel.RemoveFromClassList("auth-status--error");
            _statusLabel.RemoveFromClassList("auth-status--success");
            _usernameField.Focus();
        }

        public void RenderBusy()
        {
            if (!TryBindElements())
            {
                return;
            }

            SetInteractive(false);
            _root.SetSemanticState(UiSemanticState.Busy);
            _statusKey = "auth.busy";
            RefreshLocale();
            _statusLabel.RemoveFromClassList("auth-status--error");
            _statusLabel.RemoveFromClassList("auth-status--success");
        }

        public void RenderFailure(string playerMessage, bool clearPassword)
        {
            if (!TryBindElements())
            {
                return;
            }

            SetInteractive(true);
            _root.SetSemanticState(UiSemanticState.Error);
            _statusKey = null;
            _statusLabel.text = playerMessage;
            _statusLabel.AddToClassList("auth-status--error");
            _statusLabel.RemoveFromClassList("auth-status--success");

            PowerMath.UI.Core.StatusMessageService.ShowError(playerMessage);

            if (clearPassword)
            {
                _passwordField.value = string.Empty;
                _passwordField.Focus();
            }
        }

        public void RenderSuccess()
        {
            if (!TryBindElements())
            {
                return;
            }

            _passwordField.value = string.Empty;
            SetInteractive(false);
            _root.SetSemanticState(UiSemanticState.Success);
            _statusKey = "auth.success";
            RefreshLocale();
            _statusLabel.RemoveFromClassList("auth-status--error");
            _statusLabel.AddToClassList("auth-status--success");

            PowerMath.UI.Core.StatusMessageService.ShowSuccess(PowerMath.Localization.LocalizationService.Get("auth.success"));
        }

        private bool TryBindElements()
        {
            if (_root == null)
            {
                UIDocument document = GetComponent<UIDocument>();
                _root = document == null ? null : document.rootVisualElement;
            }

            if (_root != null && _usernameField == null)
            {
                _usernameField = _root.Q<TextField>("username-field");
                _passwordField = _root.Q<TextField>("password-field");
                _rememberToggle = _root.Q<Toggle>("remember-device-toggle");
                _loginButton = _root.Q<Button>("login-button");
                _statusLabel = _root.Q<Label>("auth-status");
                _worldwideButton = _root.Q<Button>("worldwide-button") ?? _root.Q<Button>("Utility / Language");
                _languageFlyout = _root.Q<VisualElement>("auth-language-flyout");
                _thButton = _root.Q<Button>("language-button-th");
                _enButton = _root.Q<Button>("language-button-en");
            }

            bool isBound = _root != null && _usernameField != null &&
                _passwordField != null && _rememberToggle != null &&
                _loginButton != null && _statusLabel != null;

            if (isBound && !_eventsBound)
            {
                PowerMath.UI.Core.StatusToastOverlay.Attach(_root);
                _loginButton.clicked += Submit;
                _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
                PowerMath.Localization.LocalizationService.Changed += RefreshLocale;
                if (_worldwideButton != null) _worldwideButton.clicked += ToggleLanguageFlyout;
                if (_thButton != null) _thButton.clicked += OnThClicked;
                if (_enButton != null) _enButton.clicked += OnEnClicked;
                _statusLabel.RemoveFromClassList("loc-auth.help");
                _eventsBound = true;
                RefreshLocale();
            }

            if (!isBound && !_bindingErrorLogged)
            {
                _bindingErrorLogged = true;
                PowerMath.Diagnostics.AppLog.Error(
                    "Auth",
                    "AuthenticationView could not find its required UI Toolkit elements."
                );
            }

            return isBound;
        }

        private void OnDisable()
        {
            if (!_eventsBound)
            {
                return;
            }

            _loginButton.clicked -= Submit;
            _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            PowerMath.Localization.LocalizationService.Changed -= RefreshLocale;
            if (_worldwideButton != null) _worldwideButton.clicked -= ToggleLanguageFlyout;
            if (_thButton != null) _thButton.clicked -= OnThClicked;
            if (_enButton != null) _enButton.clicked -= OnEnClicked;
            _eventsBound = false;
        }

        private void OnThClicked() => SelectLanguage("th");
        private void OnEnClicked() => SelectLanguage("en");

        private void SelectLanguage(string locale)
        {
            PowerMath.Diagnostics.AppLog.Info("Localization", $"Language changed to: {locale}");
            PowerMath.Localization.LocalizationService.SetLocale(locale);
            PowerMath.UI.Core.StatusMessageService.ShowInfo(locale == "th" ? "ภาษาไทย" : "English", 1800);
            CloseLanguageFlyout();
        }

        public void ToggleLanguageFlyout()
        {
            SetLanguageFlyoutOpen(!_isLanguageFlyoutOpen);
        }

        public void CloseLanguageFlyout()
        {
            if (_isLanguageFlyoutOpen)
            {
                SetLanguageFlyoutOpen(false);
            }
        }

        private void SetLanguageFlyoutOpen(bool open)
        {
            _isLanguageFlyoutOpen = open;
            if (_languageFlyout == null) return;

            if (open)
            {
                _languageFlyout.style.display = DisplayStyle.Flex;
                _languageFlyout.schedule.Execute(() =>
                {
                    if (_isLanguageFlyoutOpen)
                        _languageFlyout.EnableInClassList("is-open", true);
                });
            }
            else
            {
                _languageFlyout.EnableInClassList("is-open", false);
                _languageFlyout.schedule.Execute(() =>
                {
                    if (!_isLanguageFlyoutOpen)
                        _languageFlyout.style.display = DisplayStyle.None;
                }).StartingIn(220);
            }
        }

        private void RefreshLocale()
        {
            if (_statusLabel != null && _statusKey != null) _statusLabel.text = PowerMath.Localization.LocalizationService.Get(_statusKey);
            string currentLocale = PowerMath.Localization.LocalizationService.Locale;
            _thButton?.EnableInClassList("is-active", currentLocale == "th");
            _enButton?.EnableInClassList("is-active", currentLocale == "en");
        }

        private void OnKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode == KeyCode.Return ||
                keyEvent.keyCode == KeyCode.KeypadEnter)
            {
                Submit();
                keyEvent.StopPropagation();
            }
        }

        private void Submit()
        {
            if (!TryBindElements() || !_loginButton.enabledSelf)
            {
                return;
            }

            string username = _usernameField.value?.Trim();
            string password = _passwordField.value?.Trim();
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                RenderFailure("Enter both your username and password.", false);
                return;
            }

            if (!DirectFirestoreCredentialStore.IsSixDigitPassword(password))
            {
                RenderFailure("Password must contain exactly six digits.", true);
                return;
            }

            LoginRequested?.Invoke(new LoginIntent(
                username,
                password,
                _rememberToggle.value
            ));
        }

        private void SetInteractive(bool interactive)
        {
            _usernameField.SetEnabled(interactive);
            _passwordField.SetEnabled(interactive);
            _rememberToggle.SetEnabled(interactive);
            _loginButton.SetEnabled(interactive);
            _loginButton.text = interactive ? "ENTER  →" : "PLEASE WAIT...";
        }
    }
}
