using System;
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

        private void Awake()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _statusLabel = root.Q<Label>("bootstrap-status");
            _detailLabel = root.Q<Label>("bootstrap-detail");
            _progressBar = root.Q<ProgressBar>("bootstrap-progress");
            _retryButton = root.Q<Button>("retry-button");

            if (_statusLabel == null || _detailLabel == null ||
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

            _retryButton.style.display = DisplayStyle.None;
            _detailLabel.text = detail ?? string.Empty;

            switch (state)
            {
                case BootstrapState.CheckingSession:
                    SetProgress("Checking this device...", 25f);
                    break;
                case BootstrapState.LoadingPlayer:
                    SetProgress("Loading your progress...", 70f);
                    break;
                case BootstrapState.Ready:
                    SetProgress("Your progress is ready.", 90f);
                    break;
                case BootstrapState.LoadingScene:
                    SetProgress("Opening your game...", 100f);
                    break;
                case BootstrapState.Recovering:
                    ShowRetry("We could not finish loading.");
                    break;
                case BootstrapState.AuthenticationRequired:
                    SetProgress("Opening sign in...", 50f);
                    break;
                case BootstrapState.IncompatibleClient:
                    _statusLabel.text = "Game update required";
                    _progressBar.value = 0f;
                    break;
            }
        }

        private void SetProgress(string status, float value)
        {
            _statusLabel.text = status;
            _progressBar.value = value;
        }

        private void ShowRetry(string status)
        {
            _statusLabel.text = status;
            _progressBar.value = 0f;
            _retryButton.style.display = DisplayStyle.Flex;
        }

        private void OnRetryClicked()
        {
            RetryRequested?.Invoke();
        }
    }
}
