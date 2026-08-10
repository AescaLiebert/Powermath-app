using System;
using PowerMath.Session;
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
        private Label _editorHintLabel;
        private VisualElement _root;
        private bool _eventsBound;
        private bool _bindingErrorLogged;

        public void RenderReady()
        {
            if (!TryBindElements())
            {
                return;
            }

            SetInteractive(true);
            _statusLabel.text = "Enter the account details provided by your school.";
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
            _statusLabel.text = "Signing in...";
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
            _statusLabel.text = playerMessage;
            _statusLabel.AddToClassList("auth-status--error");
            _statusLabel.RemoveFromClassList("auth-status--success");

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
            _statusLabel.text = "Signed in. Loading your progress...";
            _statusLabel.RemoveFromClassList("auth-status--error");
            _statusLabel.AddToClassList("auth-status--success");
        }

        public void SetEditorHint(string hint)
        {
            if (!TryBindElements())
            {
                return;
            }

            _editorHintLabel.text = hint ?? string.Empty;
            _editorHintLabel.style.display = string.IsNullOrWhiteSpace(hint)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
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
                _editorHintLabel = _root.Q<Label>("editor-sample-hint");
            }

            bool isBound = _root != null && _usernameField != null &&
                _passwordField != null && _rememberToggle != null &&
                _loginButton != null && _statusLabel != null &&
                _editorHintLabel != null;

            if (isBound && !_eventsBound)
            {
                _loginButton.clicked += Submit;
                _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
                _eventsBound = true;
            }

            if (!isBound && !_bindingErrorLogged)
            {
                _bindingErrorLogged = true;
                Debug.LogError(
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
            _eventsBound = false;
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
            _loginButton.text = interactive ? "Sign In" : "Please Wait...";
        }
    }
}
