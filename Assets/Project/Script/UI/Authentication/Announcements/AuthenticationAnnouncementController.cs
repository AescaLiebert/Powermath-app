using System;
using System.Collections.Generic;
using PowerMath.Localization;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Authentication.Announcements
{
    [DisallowMultipleComponent]
    public sealed class AuthenticationAnnouncementController : MonoBehaviour
    {
        private VisualElement _documentRoot;
        private VisualElement _overlay;
        private VisualElement _contentRoot;
        private VisualElement _confirmation;
        private ScrollView _patchScroll;
        private ScrollView _contentScroll;
        private Label _boardTitle;
        private Label _patchTitle;
        private Label _patchMeta;
        private Label _confirmText;
        private Button _openButton;
        private Button _closeButton;
        private Button _suppressButton;
        private Button _confirmCancelButton;
        private Button _confirmButton;
        private AnnouncementCatalog _catalog;
        private AnnouncementPatch _selectedPatch;
        private readonly Dictionary<string, Button> _patchButtons =
            new Dictionary<string, Button>(StringComparer.Ordinal);
        private AnnouncementMediaPresenter _media;
        private AnnouncementMarkdownRenderer _renderer;
        private UiPanelLifecycle _lifecycle;
        private bool _bound;
        private bool _sessionDismissed;
        private bool _catalogErrorLogged;
        private bool _contentActive;

        public bool IsVisible => _lifecycle != null &&
            _lifecycle.State != UiMotionState.Hidden;

        public static AuthenticationAnnouncementController Ensure(
            GameObject owner,
            VisualElement root)
        {
            if (owner == null)
            {
                return null;
            }

            AuthenticationAnnouncementController controller =
                owner.GetComponent<AuthenticationAnnouncementController>();
            if (controller == null)
            {
                controller = owner.AddComponent<
                    AuthenticationAnnouncementController>();
            }
            controller.Bind(root);
            return controller;
        }

        public void Bind(VisualElement root)
        {
            if (_bound || root == null)
            {
                return;
            }

            _documentRoot = root;
            _overlay = root.Q<VisualElement>("announcement-overlay");
            _contentRoot = root.Q<VisualElement>("announcement-content-root");
            _confirmation = root.Q<VisualElement>("announcement-confirmation");
            _patchScroll = root.Q<ScrollView>("announcement-patch-scroll");
            _contentScroll = root.Q<ScrollView>("announcement-content-scroll");
            _boardTitle = root.Q<Label>("announcement-board-title");
            _patchTitle = root.Q<Label>("announcement-patch-title");
            _patchMeta = root.Q<Label>("announcement-patch-meta");
            _confirmText = root.Q<Label>("announcement-confirm-text");
            _openButton = root.Q<Button>("announcement-open-button");
            _closeButton = root.Q<Button>("announcement-close-button");
            _suppressButton = root.Q<Button>("announcement-suppress-button");
            _confirmCancelButton = root.Q<Button>("announcement-confirm-cancel");
            _confirmButton = root.Q<Button>("announcement-confirm-button");

            if (_overlay == null || _contentRoot == null ||
                _patchScroll == null || _contentScroll == null ||
                _confirmation == null || _boardTitle == null ||
                _patchTitle == null || _patchMeta == null ||
                _confirmText == null ||
                _openButton == null || _closeButton == null ||
                _suppressButton == null || _confirmCancelButton == null ||
                _confirmButton == null)
            {
                PowerMath.Diagnostics.AppLog.Error(
                    "Announcements",
                    "Authentication announcement UI is missing required elements.",
                    this);
                return;
            }

            // The template wrapper fills the screen; it must never become the
            // hit target when the modal is hidden. Children remain pickable.
            if (_overlay.parent != null)
            {
                _overlay.parent.pickingMode = PickingMode.Ignore;
            }

            UiMotionDriverProvider provider =
                GetComponent<UiMotionDriverProvider>();
            if (provider == null)
            {
                provider = gameObject.AddComponent<UiMotionDriverProvider>();
            }

            _lifecycle?.Dispose();
            _media?.Dispose();
            _media = new AnnouncementMediaPresenter(this);
            _renderer = new AnnouncementMarkdownRenderer(_media);
            _lifecycle = new UiPanelLifecycle(
                _overlay,
                provider.Driver,
                provider.Profile);

            _openButton.clicked += OpenManual;
            _closeButton.clicked += CloseForSession;
            _suppressButton.clicked += ShowSuppressionConfirmation;
            _confirmCancelButton.clicked += HideSuppressionConfirmation;
            _confirmButton.clicked += ConfirmSuppression;
            _documentRoot.RegisterCallback<KeyDownEvent>(OnKeyDown);
            LocalizationService.Changed += RefreshLocale;
            _bound = true;

            LoadCatalog();
            RefreshLocale();
        }

        public void RenderReady()
        {
            if (!_bound || _catalog == null || _sessionDismissed ||
                AnnouncementPreferenceStore.IsSuppressedToday(
                    _catalog.catalogRevision,
                    DateTime.Now))
            {
                return;
            }

            Open();
        }

        public void RenderUnavailable()
        {
            if (!_bound || _lifecycle == null)
            {
                return;
            }

            _media?.Clear();
            _contentActive = false;
            _lifecycle.CancelAndApply(UiMotionEndState.ApplyHidden);
            HideSuppressionConfirmation();
        }

        public void OpenManual()
        {
            if (_catalog == null)
            {
                LoadCatalog();
            }

            if (_catalog != null)
            {
                Open();
            }
        }

        private void OnDisable()
        {
            if (!_bound)
            {
                return;
            }

            _openButton.clicked -= OpenManual;
            _closeButton.clicked -= CloseForSession;
            _suppressButton.clicked -= ShowSuppressionConfirmation;
            _confirmCancelButton.clicked -= HideSuppressionConfirmation;
            _confirmButton.clicked -= ConfirmSuppression;
            _documentRoot.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            LocalizationService.Changed -= RefreshLocale;
            _lifecycle?.CancelAndApply(UiMotionEndState.ApplyHidden);
            _contentActive = false;
            _media?.Clear();
            if (_overlay != null && _overlay.parent != null)
            {
                _overlay.parent.pickingMode = PickingMode.Ignore;
            }
            _bound = false;
        }

        private void OnDestroy()
        {
            _lifecycle?.Dispose();
            _lifecycle = null;
            _media?.Dispose();
            _media = null;
        }

        private void LoadCatalog()
        {
            if (!AnnouncementCatalogLoader.TryLoad(
                    out _catalog,
                    out string error))
            {
                _openButton.style.display = DisplayStyle.None;
                if (!_catalogErrorLogged)
                {
                    _catalogErrorLogged = true;
                    PowerMath.Diagnostics.AppLog.Warning(
                        "Announcements",
                        error,
                        this);
                }
                return;
            }

            _openButton.style.display = DisplayStyle.Flex;
            BuildPatchButtons();
            SelectInitialPatch();
        }

        private void BuildPatchButtons()
        {
            _patchButtons.Clear();
            _patchScroll.Clear();
            foreach (AnnouncementPatch patch in _catalog.patches)
            {
                AnnouncementPatch captured = patch;
                var button = new Button(() => SelectPatch(captured))
                {
                    name = "announcement-patch-" + patch.id
                };
                button.AddToClassList("announcement-patch-tab");
                button.AddToClassList("mw-button");
                _patchButtons.Add(patch.id, button);
                _patchScroll.Add(button);
            }

            PowerMath.Audio.UiSfxAudioBinder.Bind(_patchScroll);
        }

        private void SelectInitialPatch()
        {
            AnnouncementPatch initial = null;
            foreach (AnnouncementPatch patch in _catalog.patches)
            {
                if (patch.featured)
                {
                    initial = patch;
                    break;
                }
            }
            SelectPatch(initial ?? _catalog.patches[0]);
        }

        private void SelectPatch(AnnouncementPatch patch)
        {
            if (patch == null)
            {
                return;
            }

            _selectedPatch = patch;
            foreach (KeyValuePair<string, Button> item in _patchButtons)
            {
                item.Value.EnableInClassList(
                    "is-selected",
                    string.Equals(
                        item.Key,
                        patch.id,
                        StringComparison.Ordinal));
            }

            if (_contentActive)
            {
                RenderSelectedPatch();
            }
            else
            {
                UpdatePatchHeader();
            }
            _contentScroll.scrollOffset = Vector2.zero;
        }

        private void RenderSelectedPatch()
        {
            if (_selectedPatch == null)
            {
                return;
            }

            string locale = LocalizationService.Locale;
            UpdatePatchHeader();
            _renderer.Render(_contentRoot, _selectedPatch.GetBody(locale));
        }

        private void UpdatePatchHeader()
        {
            if (_selectedPatch == null)
            {
                return;
            }

            string locale = LocalizationService.Locale;
            _patchTitle.text = _selectedPatch.GetTitle(locale);
            _patchMeta.text = FormatPatchMeta(_selectedPatch, locale);
        }

        private void RefreshLocale()
        {
            if (!_bound)
            {
                return;
            }

            bool thai = LocalizationService.Locale == "th";
            _boardTitle.text = thai ? "ประกาศและแพตช์" : "ANNOUNCEMENTS";
            _closeButton.text = thai ? "ปิด" : "CLOSE";
            _suppressButton.text = thai
                ? "ไม่ต้องแสดงอีกในวันนี้"
                : "DON'T SHOW AGAIN TODAY";
            _confirmText.text = thai
                ? "ซ่อนประกาศอัตโนมัติจนถึงวันพรุ่งนี้?"
                : "Hide automatic announcements until tomorrow?";
            _confirmCancelButton.text = thai ? "ยกเลิก" : "CANCEL";
            _confirmButton.text = thai ? "ยืนยัน" : "CONFIRM";
            _openButton.tooltip = thai ? "เปิดประกาศ" : "Open announcements";

            foreach (AnnouncementPatch patch in _catalog?.patches ??
                Array.Empty<AnnouncementPatch>())
            {
                if (_patchButtons.TryGetValue(patch.id, out Button button))
                {
                    button.text = patch.GetTitle(LocalizationService.Locale) +
                        "\n" + patch.version;
                }
            }
            if (_contentActive)
            {
                RenderSelectedPatch();
            }
            else
            {
                UpdatePatchHeader();
            }
        }

        private void Open()
        {
            HideSuppressionConfirmation();
            _contentActive = true;
            RenderSelectedPatch();
            _lifecycle.Enter(() => _closeButton.Focus());
        }

        private void CloseForSession()
        {
            _sessionDismissed = true;
            HideSuppressionConfirmation();
            _contentActive = false;
            _media?.Clear();
            _lifecycle.Exit(RestoreLoginFocus);
        }

        private void ShowSuppressionConfirmation()
        {
            _confirmation.style.display = DisplayStyle.Flex;
            _confirmation.AddToClassList("is-open");
            _confirmButton.Focus();
        }

        private void HideSuppressionConfirmation()
        {
            if (_confirmation == null)
            {
                return;
            }
            _confirmation.RemoveFromClassList("is-open");
            _confirmation.style.display = DisplayStyle.None;
        }

        private void ConfirmSuppression()
        {
            AnnouncementPreferenceStore.SuppressToday(
                _catalog.catalogRevision,
                DateTime.Now);
            CloseForSession();
        }

        private void OnKeyDown(KeyDownEvent keyEvent)
        {
            if (!IsVisible || keyEvent.keyCode != KeyCode.Escape)
            {
                return;
            }

            if (_confirmation.style.display.value == DisplayStyle.Flex)
            {
                HideSuppressionConfirmation();
                _suppressButton.Focus();
            }
            else
            {
                CloseForSession();
            }
            keyEvent.StopPropagation();
        }

        private void RestoreLoginFocus()
        {
            _documentRoot.Q<TextField>("username-field")?.Focus();
        }

        private static string FormatPatchMeta(
            AnnouncementPatch patch,
            string locale)
        {
            if (!DateTime.TryParse(
                    patch.publishedAtUtc,
                    null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime published))
            {
                return patch.version;
            }

            string date = locale == "th"
                ? published.ToString("dd/MM/yyyy")
                : published.ToString("MMM d, yyyy");
            return "v" + patch.version + "  •  " + date;
        }
    }
}
