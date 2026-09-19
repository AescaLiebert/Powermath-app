using System;
using PowerMath.Session;
using PowerMath.UI.Authentication.Announcements;
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
        private Label _loginButtonText;
        private VisualElement _root;
        private Button _worldwideButton;
        private VisualElement _languageFlyout;
        private Button _thButton;
        private Button _enButton;
        private bool _isLanguageFlyoutOpen;
        private bool _eventsBound;
        private bool _bindingErrorLogged;
        private AuthenticationAnnouncementController _announcements;

        private bool _isInteractive = true;

        /// <summary>
        /// Shows the login form in its ready/interactive state.
        /// Optionally pre-fills the username field (e.g. when a non-remembered
        /// credential exists so the player only has to enter their password).
        /// </summary>
        public void RenderReady(string prefillUsername = null)
        {
            if (!TryBindElements())
            {
                return;
            }

            SetInteractive(true);
            _root.SetSemanticState(UiSemanticState.Ready);
            RefreshLocale();

            if (!string.IsNullOrEmpty(prefillUsername))
            {
                _usernameField.value = prefillUsername;
                _passwordField.Focus();
            }
            else
            {
                _usernameField.Focus();
            }

            _announcements?.RenderReady();
        }

        public void RenderBusy()
        {
            if (!TryBindElements())
            {
                return;
            }

            SetInteractive(false);
            _root.SetSemanticState(UiSemanticState.Busy);
            RefreshLocale();
            _announcements?.RenderUnavailable();
        }

        public void RenderFailure(string playerMessage, bool clearPassword)
        {
            if (!TryBindElements())
            {
                return;
            }

            SetInteractive(true);
            _root.SetSemanticState(UiSemanticState.Error);

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
            RefreshLocale();
            _announcements?.RenderUnavailable();

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
                _loginButtonText = _loginButton?.Q<Label>(className: "login-button-text");
                _worldwideButton = _root.Q<Button>("worldwide-button") ?? _root.Q<Button>("Utility / Language");
                _languageFlyout = _root.Q<VisualElement>("auth-language-flyout");
                _thButton = _root.Q<Button>("language-button-th");
                _enButton = _root.Q<Button>("language-button-en");
            }

            if (_root != null)
            {
                _announcements = AuthenticationAnnouncementController.Ensure(
                    gameObject,
                    _root);
            }

            bool isBound = _root != null && _usernameField != null &&
                _passwordField != null && _rememberToggle != null &&
                _loginButton != null;

            if (isBound && !_eventsBound)
            {
                PowerMath.UI.Core.StatusToastOverlay.Attach(_root);
                PowerMath.Audio.UiSfxAudioBinder.Bind(_root);
                _loginButton.clicked += Submit;
                _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
                PowerMath.Localization.LocalizationService.Changed += RefreshLocale;
                if (_worldwideButton != null) _worldwideButton.clicked += ToggleLanguageFlyout;
                if (_thButton != null) _thButton.clicked += OnThClicked;
                if (_enButton != null) _enButton.clicked += OnEnClicked;
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
            PowerMath.Audio.SfxController.Instance.PlayAuthentication(PowerMath.Audio.AuthenticationSfxState.LanguageSwitch);
            PowerMath.Diagnostics.AppLog.Info("Localization", $"Language changed to: {locale}");
            PowerMath.Localization.LocalizationService.SetLocale(locale);
            PowerMath.UI.Core.StatusMessageService.ShowInfo(locale == "th" ? "ภาษาไทย" : "English", 1800);
            CloseLanguageFlyout();
        }

        public void ToggleLanguageFlyout()
        {
            PowerMath.Audio.SfxController.Instance.PlayAuthentication(PowerMath.Audio.AuthenticationSfxState.LanguageSwitch);
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
            string currentLocale = PowerMath.Localization.LocalizationService.Locale;
            _thButton?.EnableInClassList("is-active", currentLocale == "th");
            _enButton?.EnableInClassList("is-active", currentLocale == "en");
            ApplyLoginButtonText();
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

            PowerMath.Audio.SfxController.Instance.PlayAuthentication(PowerMath.Audio.AuthenticationSfxState.LoginClick);

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
            _isInteractive = interactive;
            _usernameField.SetEnabled(interactive);
            _passwordField.SetEnabled(interactive);
            _rememberToggle.SetEnabled(interactive);
            _loginButton.SetEnabled(interactive);
            ApplyLoginButtonText();
        }

        private void ApplyLoginButtonText()
        {
            string text = _isInteractive
                ? PowerMath.Localization.LocalizationService.Get("auth.enter")
                : PowerMath.Localization.LocalizationService.Get("auth.busy");
            if (_loginButtonText != null)
                _loginButtonText.text = text;
            else if (_loginButton != null)
                _loginButton.text = text;
        }
    }
}
