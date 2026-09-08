using System;
using PowerMath.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Bootstrap
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BootstrapView : MonoBehaviour
    {
        public event Action RetryRequested;

        private BootstrapState _lastState;
        private string _lastDetail;
        private Label _statusLabel;
        private Label _detailLabel;
        private ProgressBar _progressBar;
        private Button _retryButton;
        private VisualElement _screen;

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _screen = root.Q<VisualElement>("bootstrap-screen");
            _statusLabel = root.Q<Label>("bootstrap-status");
            _detailLabel = root.Q<Label>("bootstrap-detail");
            _progressBar = root.Q<ProgressBar>("bootstrap-progress");
            _retryButton = root.Q<Button>("retry-button");

            if (_screen == null ||
                _statusLabel == null || _detailLabel == null ||
                _progressBar == null || _retryButton == null)
            {
                PowerMath.Diagnostics.AppLog.Error("Bootstrap", "BootstrapView could not find its required UI Toolkit elements.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            PowerMath.Localization.LocalizationService.Changed += RefreshLocale;
            if (_retryButton != null)
            {
                _retryButton.clicked += OnRetryClicked;
            }
        }

        private void OnDisable()
        {
            PowerMath.Localization.LocalizationService.Changed -= RefreshLocale;
            if (_retryButton != null)
            {
                _retryButton.clicked -= OnRetryClicked;
            }
        }

        private void RefreshLocale() => Render(_lastState, _lastDetail);

        public void Render(BootstrapState state, string detail = null)
        {
            _lastState = state; _lastDetail = detail;
            if (!enabled)
            {
                return;
            }

            _retryButton.text = PowerMath.Localization.LocalizationService.Get("common.retry");
            _retryButton.AddToClassList("is-hidden");
            _retryButton.SetEnabled(false);

            switch (state)
            {
                case BootstrapState.CheckingVersion:
                    SetProgress(
                        PowerMath.Localization.LocalizationService.Get("bootstrap.versionPhase"),
                        PowerMath.Localization.LocalizationService.Get("bootstrap.checkingVersion"),
                        detail ?? PowerMath.Localization.LocalizationService.Get("bootstrap.versionDetail"),
                        10f
                    );
                    break;
                case BootstrapState.CheckingSession:
                    SetProgress(
                        PowerMath.Localization.LocalizationService.Get("bootstrap.sessionPhase"),
                        PowerMath.Localization.LocalizationService.Get("bootstrap.checkingSession"),
                        detail ?? PowerMath.Localization.LocalizationService.Get("bootstrap.sessionDetail"),
                        25f
                    );
                    break;
                case BootstrapState.LoadingPlayer:
                    SetProgress(
                        "RESTORING PROGRESS",
                        PowerMath.Localization.LocalizationService.Get("bootstrap.loading"),
                        detail ?? "Restoring your Stage, loadout, and learning progress.",
                        70f
                    );
                    break;
                case BootstrapState.Ready:
                    SetProgress(
                        "PROGRESS READY",
                        "Your progress is ready.",
                        detail ?? "Everything is in place for your next challenge.",
                        90f
                    );
                    break;
                case BootstrapState.LoadingScene:
                    SetProgress(
                        PowerMath.Localization.LocalizationService.Get("bootstrap.enterPhase"),
                        PowerMath.Localization.LocalizationService.Get("bootstrap.entering"),
                        detail ?? PowerMath.Localization.LocalizationService.Get("bootstrap.enterDetail"),
                        100f
                    );
                    break;
                case BootstrapState.Recovering:
                    ShowRetry(
                        PowerMath.Localization.LocalizationService.Get("bootstrap.interruptPhase"),
                        PowerMath.Localization.LocalizationService.Get("bootstrap.interrupted"),
                        detail ?? PowerMath.Localization.LocalizationService.Get("bootstrap.retryDetail")
                    );
                    break;
                case BootstrapState.AuthenticationRequired:
                    SetProgress(
                        PowerMath.Localization.LocalizationService.Get("bootstrap.authPhase"),
                        PowerMath.Localization.LocalizationService.Get("bootstrap.signIn"),
                        detail ?? PowerMath.Localization.LocalizationService.Get("bootstrap.accountDetail"),
                        50f
                    );
                    break;
                case BootstrapState.IncompatibleClient:
                    ShowBlocked(
                        PowerMath.Localization.LocalizationService.Get("bootstrap.updatePhase"),
                        PowerMath.Localization.LocalizationService.Get("bootstrap.update"),
                        detail ?? PowerMath.Localization.LocalizationService.Get("bootstrap.updateDetail")
                    );
                    break;
                case BootstrapState.MaintenanceMode:
                    ShowBlocked(
                        PowerMath.Localization.LocalizationService.Get("bootstrap.maintenancePhase"),
                        PowerMath.Localization.LocalizationService.Get("bootstrap.maintenance"),
                        detail ?? PowerMath.Localization.LocalizationService.Get("bootstrap.maintenanceDetail")
                    );
                    break;
            }
        }

        private void SetProgress(
            string phase,
            string status,
            string detail,
            float value)
        {
            _screen.SetSemanticState(
                value >= 90f ? UiSemanticState.Success : UiSemanticState.Busy
            );
            _statusLabel.text = status;
            _detailLabel.text = detail;
            _progressBar.value = value;
            _progressBar.title = phase;
        }

        private void ShowRetry(string phase, string status, string detail)
        {
            _screen.SetSemanticState(UiSemanticState.Error);
            _statusLabel.text = status;
            _detailLabel.text = detail;
            _progressBar.value = 0f;
            _progressBar.title = PowerMath.Localization.LocalizationService.Get("bootstrap.retry");
            _retryButton.RemoveFromClassList("is-hidden");
            _retryButton.SetEnabled(true);
        }

        private void ShowBlocked(string phase, string status, string detail)
        {
            _screen.SetSemanticState(UiSemanticState.Blocked);
            _statusLabel.text = status;
            _detailLabel.text = detail;
            _progressBar.value = 0f;
            _progressBar.title = phase;
            if (_lastState == BootstrapState.IncompatibleClient)
            {
                _retryButton.text = PowerMath.Localization.LocalizationService.Get("common.reload");
                _retryButton.RemoveFromClassList("is-hidden");
                _retryButton.SetEnabled(true);
            }
        }

        private void OnRetryClicked()
        {
            if (_lastState == BootstrapState.IncompatibleClient) WebCacheBridge.HardReload();
            else RetryRequested?.Invoke();
        }
    }
}
