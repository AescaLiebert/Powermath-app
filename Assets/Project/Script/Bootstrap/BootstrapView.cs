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
                Debug.LogError("BootstrapView could not find its required UI Toolkit elements.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_retryButton != null)
            {
                _retryButton.clicked += OnRetryClicked;
            }
        }

        private void OnDisable()
        {
            if (_retryButton != null)
            {
                _retryButton.clicked -= OnRetryClicked;
            }
        }

        public void Render(BootstrapState state, string detail = null)
        {
            if (!enabled)
            {
                return;
            }

            _retryButton.AddToClassList("is-hidden");
            _retryButton.SetEnabled(false);

            switch (state)
            {
                case BootstrapState.CheckingSession:
                    SetProgress(
                        "CHECKING SESSION",
                        "Checking this device...",
                        detail ?? "Looking for a saved school session.",
                        25f
                    );
                    break;
                case BootstrapState.LoadingPlayer:
                    SetProgress(
                        "RESTORING PROGRESS",
                        "Loading your progress...",
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
                        "ENTERING MATH:WORLD",
                        "Opening your game...",
                        detail ?? "Preparing the Main Menu and current encounter.",
                        100f
                    );
                    break;
                case BootstrapState.Recovering:
                    ShowRetry(
                        "LOAD INTERRUPTED",
                        "We could not finish loading.",
                        detail ?? "Check your connection, then try again."
                    );
                    break;
                case BootstrapState.AuthenticationRequired:
                    SetProgress(
                        "SIGN IN REQUIRED",
                        "Opening sign in...",
                        detail ?? "A school account is needed to restore your progress.",
                        50f
                    );
                    break;
                case BootstrapState.IncompatibleClient:
                    ShowBlocked(
                        "UPDATE REQUIRED",
                        "Game update required",
                        detail ?? "Install the latest version before continuing."
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
            _progressBar.title = "READY TO RETRY";
            _retryButton.RemoveFromClassList("is-hidden");
            _retryButton.SetEnabled(true);
        }

        private void ShowBlocked(string phase, string status, string detail)
        {
            _screen.SetSemanticState(UiSemanticState.Blocked);
            _statusLabel.text = status;
            _detailLabel.text = detail;
            _progressBar.value = 0f;
            _progressBar.title = "UPDATE NEEDED";
        }

        private void OnRetryClicked()
        {
            RetryRequested?.Invoke();
        }
    }
}
