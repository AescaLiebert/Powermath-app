using System;
using System.Collections;
using PowerMath.Bootstrap;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.Admin
{
    public sealed class AdminPanelController
    {
        private readonly VisualElement _open;
        private readonly VisualElement _modal;
        private readonly Button _close;
        private readonly Label _accountSummary;
        private readonly Button _resetButton;
        private readonly Label _statusLabel;
        private readonly VisualElement _attemptPanel;
        private readonly MonoBehaviour _host;
        private readonly GameApiSettings _settings;
        private readonly PlayerSnapshot _player;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly SceneFlowController _sceneFlow;
        private readonly FirestorePlayerResetService _resetService;

        private bool _bound;
        private bool _busy;
        private bool _confirmingReset;
        private Coroutine _confirmTimeoutCoroutine;

        public AdminPanelController(
            VisualElement root,
            MonoBehaviour host,
            GameApiSettings settings,
            PlayerSnapshot player,
            IMainMenuPanelHost panelHost,
            SceneFlowController sceneFlow = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _settings = settings;
            _player = player;
            _panelHost = panelHost ?? throw new ArgumentNullException(nameof(panelHost));
            _sceneFlow = sceneFlow ?? host.GetComponent<SceneFlowController>();

            _open = root.Q<Button>("setting");
            _modal = root.Q<VisualElement>("admin-panel-modal");
            _close = root.Q<Button>("admin-panel-close");
            _accountSummary = root.Q<Label>("admin-account-summary");
            _resetButton = root.Q<Button>("admin-reset-button");
            _statusLabel = root.Q<Label>("admin-reset-status");
            _attemptPanel = root.Q<VisualElement>("combat-attempt-panel");

            _resetService = new FirestorePlayerResetService(_settings, _player);
        }

        public bool IsValid => _open != null && _modal != null && _close != null &&
                               _resetButton != null && _statusLabel != null;

        public void Bind()
        {
            if (_bound || !IsValid) return;

            if (_open is Button openButton)
            {
                openButton.clicked += Open;
            }
            else
            {
                _open.RegisterCallback<ClickEvent>(_ => Open());
            }

            _close.clicked += Close;
            _resetButton.clicked += OnResetClicked;
            _modal.style.display = DisplayStyle.None;
            _bound = true;
        }

        public void Dispose()
        {
            if (!_bound) return;

            if (_open is Button openButton)
            {
                openButton.clicked -= Open;
            }
            _close.clicked -= Close;
            _resetButton.clicked -= OnResetClicked;

            CancelConfirmTimeout();

            if (_panelHost.OpenPanel == MainMenuPanelId.Settings)
            {
                _panelHost.TryClose(MainMenuPanelId.Settings, _open as Focusable);
            }
            _bound = false;
        }

        public void Open()
        {
            if (_attemptPanel != null && _attemptPanel.resolvedStyle.display != DisplayStyle.None)
            {
                return;
            }

            if (!_panelHost.TryOpen(MainMenuPanelId.Settings, _modal, _open as Focusable))
            {
                return;
            }

            RenderAccountSummary();
            ResetConfirmState();
            _statusLabel.text = "Ready";
            SetSemanticState();
        }

        public void Close()
        {
            if (_busy) return;

            CancelConfirmTimeout();
            ResetConfirmState();
            _panelHost.TryClose(MainMenuPanelId.Settings, _open as Focusable);
            SetSemanticState();
        }

        private void OnResetClicked()
        {
            if (_busy) return;

            if (!_confirmingReset)
            {
                _confirmingReset = true;
                _resetButton.text = "CONFIRM RESET USER DATA";
                _resetButton.AddToClassList("is-confirming");
                _statusLabel.text = "Warning: Click again to permanently reset all user data.";
                SetSemanticState();

                CancelConfirmTimeout();
                if (_host != null && _host.isActiveAndEnabled)
                {
                    _confirmTimeoutCoroutine = _host.StartCoroutine(ConfirmTimeoutRoutine());
                }
            }
            else
            {
                CancelConfirmTimeout();
                ExecuteReset();
            }
        }

        private void ExecuteReset()
        {
            _busy = true;
            _confirmingReset = false;
            _resetButton.RemoveFromClassList("is-confirming");
            _resetButton.text = "RESETTING...";
            _resetButton.SetEnabled(false);
            _close.SetEnabled(false);
            _statusLabel.text = "Resetting player data in Firebase...";
            SetSemanticState("is-busy");

            _host.StartCoroutine(_resetService.ResetUserData(OnResetSuccess, OnResetFailure));
        }

        private void OnResetSuccess()
        {
            _statusLabel.text = "User data reset successfully! Reloading session...";
            SetSemanticState("is-success");

            if (_host != null && _host.isActiveAndEnabled)
            {
                _host.StartCoroutine(ReloadSessionRoutine());
            }
        }

        private void OnResetFailure(string errorMessage)
        {
            _busy = false;
            _resetButton.SetEnabled(true);
            _close.SetEnabled(true);
            _resetButton.text = "RESET USER DATA";
            _statusLabel.text = "Reset failed: " + errorMessage;
            SetSemanticState("is-error");
        }

        private IEnumerator ReloadSessionRoutine()
        {
            yield return new WaitForSeconds(1.0f);

            _busy = false;
            PlayerSessionStore.Instance?.Clear();

            string targetScene = _settings != null && !string.IsNullOrWhiteSpace(_settings.BootstrapSceneName)
                ? _settings.BootstrapSceneName
                : "BootstrapScene";

            if (_sceneFlow != null)
            {
                _sceneFlow.TryLoadScene(targetScene, error =>
                {
                    _statusLabel.text = "Reload failed: " + error;
                    SetSemanticState("is-error");
                });
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
            }
        }

        private IEnumerator ConfirmTimeoutRoutine()
        {
            yield return new WaitForSeconds(5.0f);
            if (_confirmingReset && !_busy)
            {
                ResetConfirmState();
                _statusLabel.text = "Reset cancelled (timed out).";
            }
        }

        private void CancelConfirmTimeout()
        {
            if (_confirmTimeoutCoroutine != null)
            {
                if (_host != null) _host.StopCoroutine(_confirmTimeoutCoroutine);
                _confirmTimeoutCoroutine = null;
            }
        }

        private void ResetConfirmState()
        {
            _confirmingReset = false;
            _resetButton.RemoveFromClassList("is-confirming");
            _resetButton.text = "RESET USER DATA";
            _resetButton.SetEnabled(true);
            _close.SetEnabled(true);
        }

        private void RenderAccountSummary()
        {
            if (_accountSummary == null) return;

            string name = _player?.profile?.displayName ?? "Unknown Student";
            string grade = _player?.profile?.gradeBand ?? "Grade";
            string playerId = _player?.playerId ?? "N/A";

            _accountSummary.text = name + " (" + grade + ") • " + playerId;
        }

        private void SetSemanticState(string state = null)
        {
            _modal.EnableInClassList("is-busy", state == "is-busy");
            _modal.EnableInClassList("is-success", state == "is-success");
            _modal.EnableInClassList("is-error", state == "is-error");
        }
    }
}
