using System;
using System.Globalization;
using PowerMath.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.PlayerLifecycle
{
    public sealed class PlayerPreparationView : IDisposable
    {
        public const int MaximumDisplayNameLength =
            PowerMath.Session.PlayerLifecyclePolicy.MaximumDisplayNameLength;

        private const int DotColumns = 64;
        private const int DotRows = 36;
        private const int TransitionSquareCount = 15;

        private readonly VisualElement _overlay;
        private readonly VisualElement _narrativeStage;
        private readonly Label _narrativeLine;
        private readonly Button _openingContinueButton;
        private readonly VisualElement _openingVideoStage;
        private readonly Image _openingVideoImage;
        private readonly Label _videoStatus;
        private readonly Button _openingSkipButton;
        private readonly VisualElement _selectionStage;
        private readonly Image _selectionVideoImage;
        private readonly VisualElement _selectionFallback;
        private readonly Image _rickoOverview;
        private readonly Image _stellarOverview;
        private readonly Label _selectionTitle;
        private readonly Label _selectionHint;
        private readonly Button _rickoButton;
        private readonly Button _stellarButton;
        private readonly VisualElement _detailStage;
        private readonly VisualElement _detailGradient;
        private readonly VisualElement _dotGrid;
        private readonly VisualElement _detailAccent;
        private readonly Image _detailShadow;
        private readonly Image _detailArt;
        private readonly VisualElement _detailInfo;
        private readonly Label _detailKicker;
        private readonly Label _detailName;
        private readonly Label _detailDescription;
        private readonly Button _selectButton;
        private readonly Button _detailBackButton;
        private readonly VisualElement _nameStage;
        private readonly Image _nameArt;
        private readonly VisualElement _nameCard;
        private readonly Label _nameHeading;
        private readonly Label _namePrompt;
        private readonly TextField _nameField;
        private readonly Label _nameNote;
        private readonly Label _nameCount;
        private readonly Button _confirmButton;
        private readonly Button _nameBackButton;
        private readonly VisualElement _transitionLayer;
        private readonly VisualElement[] _transitionSquares;
        private readonly VisualElement _flash;
        private readonly Label _error;
        private readonly VisualElement _recovery;
        private readonly VisualElement[] _stages;

        private bool _isRicko;
        private bool _disposed;
        private Texture2D _detailGradientTexture;

        public PlayerPreparationView(VisualElement root, bool reducedMotion)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _overlay = Require<VisualElement>(root, "player-preparation");
            _narrativeStage = Require<VisualElement>(root, "prep-narrative");
            _narrativeLine = Require<Label>(root, "prep-narrative-line");
            _openingContinueButton = Require<Button>(root, "common.continue");
            _openingVideoStage = Require<VisualElement>(root, "prep-opening-video");
            _openingVideoImage = Require<Image>(root, "prep-opening-video-image");
            _videoStatus = Require<Label>(root, "prep-video-status");
            _openingSkipButton = Require<Button>(root, "onboarding.skip");
            _selectionStage = Require<VisualElement>(root, "prep-character-selection");
            _selectionVideoImage = Require<Image>(root, "prep-selection-video-image");
            _selectionFallback = Require<VisualElement>(root, "prep-selection-fallback");
            _rickoOverview = Require<Image>(root, "portrait-ricko");
            _stellarOverview = Require<Image>(root, "portrait-stellar");
            _selectionTitle = Require<Label>(root, "prep-selection-title");
            _selectionHint = Require<Label>(root, "prep-selection-hint");
            _rickoButton = Require<Button>(root, "ricko");
            _stellarButton = Require<Button>(root, "stellar");
            _detailStage = Require<VisualElement>(root, "prep-character-detail");
            _detailGradient = Require<VisualElement>(root, "prep-detail-gradient");
            _dotGrid = Require<VisualElement>(root, "prep-dot-grid");
            _detailAccent = Require<VisualElement>(root, "prep-detail-accent");
            _detailShadow = Require<Image>(root, "prep-character-shadow");
            _detailArt = Require<Image>(root, "prep-character-art");
            _detailInfo = Require<VisualElement>(root, "prep-character-info");
            _detailKicker = Require<Label>(root, "prep-character-kicker");
            _detailName = Require<Label>(root, "prep-character-name");
            _detailDescription = Require<Label>(root, "prep-character-description");
            _selectButton = Require<Button>(root, "onboarding.choose");
            _detailBackButton = Require<Button>(root, "detail-back-button");
            _nameStage = Require<VisualElement>(root, "prep-name-entry");
            _nameArt = Require<Image>(root, "prep-name-character-art");
            _nameCard = Require<VisualElement>(root, "prep-name-card");
            _nameHeading = Require<Label>(root, "prep-name-heading");
            _namePrompt = Require<Label>(root, "prep-name-prompt");
            _nameField = Require<TextField>(root, "preparation-display-name");
            _nameNote = Require<Label>(root, "prep-name-note");
            _nameCount = Require<Label>(root, "prep-name-count");
            _confirmButton = Require<Button>(root, "common.confirm");
            _nameBackButton = Require<Button>(root, "name-back-button");
            _transitionLayer = Require<VisualElement>(root, "prep-transition-layer");
            _flash = Require<VisualElement>(root, "prep-flash");
            _error = Require<Label>(root, "prep-error");
            _recovery = Require<VisualElement>(root, "prep-recovery");
            _stages = new[]
            {
                _narrativeStage,
                _openingVideoStage,
                _selectionStage,
                _detailStage,
                _nameStage
            };

            _transitionSquares = BuildTransitionSquares();
            BuildDetailGradient();
            BuildDotPattern();
            BindInputs();
            _nameField.maxLength = MaximumDisplayNameLength;
            _openingVideoImage.scaleMode = ScaleMode.ScaleAndCrop;
            _selectionVideoImage.scaleMode = ScaleMode.ScaleAndCrop;
            _rickoOverview.scaleMode = ScaleMode.ScaleAndCrop;
            _stellarOverview.scaleMode = ScaleMode.ScaleAndCrop;
            _detailArt.scaleMode = ScaleMode.ScaleAndCrop;
            _detailShadow.scaleMode = ScaleMode.ScaleAndCrop;
            _nameArt.scaleMode = ScaleMode.ScaleToFit;
            _overlay.EnableInClassList("is-reduced-motion", reducedMotion);
            SetState(PlayerPreparationSequenceState.Hidden);
        }

        public event Action OpeningSkipRequested;
        public event Action<string> CharacterRequested;
        public event Action CharacterAccepted;
        public event Action DetailBackRequested;
        public event Action NameBackRequested;
        public event Action<string> NameChanged;
        public event Action NameConfirmed;

        public PlayerPreparationSequenceState State { get; private set; }
        public Label NarrativeLine => _narrativeLine;
        public Image DetailArt => _detailArt;
        public VisualElement DetailAccent => _detailAccent;
        public VisualElement TransitionLayer => _transitionLayer;
        public string DisplayName => _nameField.value;

        public void SetState(PlayerPreparationSequenceState state)
        {
            State = state;
            _overlay.EnableInClassList("is-hidden", state == PlayerPreparationSequenceState.Hidden);
            _overlay.pickingMode = state == PlayerPreparationSequenceState.Hidden
                ? PickingMode.Ignore
                : PickingMode.Position;
        }

        public void ShowNarrative(string text)
        {
            ShowOnly(_narrativeStage);
            SetState(PlayerPreparationSequenceState.Narrative);
            _narrativeLine.text = text ?? string.Empty;
            _narrativeLine.style.opacity = 1f;
            ClearError();
        }

        public void SetNarrativeText(string text)
        {
            _narrativeLine.text = text ?? string.Empty;
        }

        public void SetNarrativeOpacity(float value)
        {
            _narrativeLine.style.opacity = Mathf.Clamp01(value);
        }

        public void ShowOpeningVideo(RenderTexture texture, string status)
        {
            ShowOnly(_openingVideoStage);
            SetState(PlayerPreparationSequenceState.OpeningVideo);
            _openingVideoImage.image = texture;
            _videoStatus.text = status ?? string.Empty;
            _videoStatus.EnableInClassList("is-hidden", string.IsNullOrWhiteSpace(status));
            ClearError();
        }

        public void SetVideoStatus(string status)
        {
            _videoStatus.text = status ?? string.Empty;
            _videoStatus.EnableInClassList("is-hidden", string.IsNullOrWhiteSpace(status));
        }

        public void ShowSelection(
            RenderTexture texture,
            Sprite rickoOverview,
            Sprite stellarOverview)
        {
            ShowOnly(_selectionStage);
            SetState(PlayerPreparationSequenceState.CharacterSelection);
            _selectionVideoImage.image = texture;
            bool hasVideo = texture != null;
            _selectionVideoImage.style.display = hasVideo
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _selectionFallback.style.display = hasVideo
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            _rickoOverview.sprite = rickoOverview;
            _stellarOverview.sprite = stellarOverview;
            ClearError();
        }

        public void ShowCharacterDetail(
            string characterId,
            Sprite art,
            string displayName,
            string description,
            PlayerPreparationSequenceState state)
        {
            ApplyCharacterTheme(characterId);
            ShowOnly(_detailStage);
            SetState(state);
            _detailArt.sprite = art;
            _detailShadow.sprite = art;
            _detailShadow.tintColor = new Color(1f, 1f, 1f, 0.5f);
            _detailName.text = (displayName ?? characterId).ToUpperInvariant();
            _detailDescription.text = description ?? string.Empty;
            ClearError();
        }

        public void SetCharacterCopy(string displayName, string description)
        {
            _detailName.text = (displayName ?? string.Empty).ToUpperInvariant();
            _detailDescription.text = description ?? string.Empty;
        }

        public void ShowNameEntry(
            string characterId,
            Sprite art,
            string currentName)
        {
            ApplyCharacterTheme(characterId);
            ShowOnly(_nameStage);
            SetState(PlayerPreparationSequenceState.NameEntry);
            _nameArt.sprite = art;
            _nameField.SetValueWithoutNotify(currentName ?? string.Empty);
            UpdateNameCount(currentName);
            ClearError();
            _nameField.schedule.Execute(_nameField.Focus).StartingIn(80);
        }

        public void RefreshLocale()
        {
            _openingContinueButton.text = LocalizationService.Get("onboarding.skip");
            _openingSkipButton.text = LocalizationService.Get("onboarding.skip");
            _selectionTitle.text = LocalizationService.Get("onboarding.select").ToUpperInvariant();
            _selectionHint.text = LocalizationService.Get("onboarding.select").ToUpperInvariant();
            _detailKicker.text = LocalizationService.Get("onboarding.yourCharacter").ToUpperInvariant();
            _selectButton.text = LocalizationService.Get("onboarding.choose").ToUpperInvariant();
            _detailBackButton.tooltip = LocalizationService.Get("common.back");
            _nameBackButton.text = LocalizationService.Get("common.back").ToUpperInvariant();
            _nameHeading.text = LocalizationService.Get("onboarding.almostReady");
            _namePrompt.text = LocalizationService.Get("onboarding.name");
            _nameNote.text = LocalizationService.Get("onboarding.permanent");
            _confirmButton.text = LocalizationService.Get("common.confirm").ToUpperInvariant();
            _rickoButton.tooltip = LocalizationService.Get("onboarding.ricko");
            _stellarButton.tooltip = LocalizationService.Get("onboarding.stellar");
        }

        public void SetInteractive(bool interactive)
        {
            _openingContinueButton.SetEnabled(interactive);
            _openingSkipButton.SetEnabled(interactive);
            _rickoButton.SetEnabled(interactive);
            _stellarButton.SetEnabled(interactive);
            _selectButton.SetEnabled(interactive);
            _detailBackButton.SetEnabled(interactive);
            _nameBackButton.SetEnabled(interactive);
            _nameField.SetEnabled(interactive);
            _confirmButton.SetEnabled(interactive);
        }

        public void SetNameValue(string value)
        {
            _nameField.SetValueWithoutNotify(value ?? string.Empty);
            UpdateNameCount(value);
        }

        public void ShowError(string message)
        {
            _error.text = message ?? string.Empty;
            _error.EnableInClassList("is-hidden", string.IsNullOrWhiteSpace(message));
        }

        public void ClearError()
        {
            ShowError(string.Empty);
            _recovery.Clear();
        }

        public void ShowRecovery(string label, Action action)
        {
            _recovery.Clear();
            var button = new Button(action) { text = label ?? string.Empty };
            _recovery.Add(button);
        }

        public void ApplyDetailProgress(float progress)
        {
            float t = progress;
            float side = _isRicko ? -1f : 1f;
            float artX = Mathf.LerpUnclamped(side * 280f, 0f, t);
            float infoX = Mathf.LerpUnclamped(-side * 240f, 0f, t);
            float scale = Mathf.LerpUnclamped(0.76f, 1f, t);
            _detailArt.style.translate = TranslatePixels(artX, 0f);
            _detailArt.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _detailArt.style.opacity = Mathf.Clamp01(t);
            _detailShadow.style.translate = TranslatePixels(
                artX + 27f,
                _isRicko ? 0f : 9f);
            _detailShadow.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _detailShadow.style.opacity = Mathf.Clamp01(t) * 0.5f;
            _detailInfo.style.translate = TranslatePixels(infoX, 0f);
            _detailInfo.style.opacity = Mathf.Clamp01(t);
            _dotGrid.style.opacity = Mathf.Clamp01(t) * 0.84f;
        }

        public void ApplyDetailExitProgress(float progress)
        {
            float t = Mathf.Clamp01(progress);
            float side = _isRicko ? -1f : 1f;
            float artX = Mathf.Lerp(0f, side * 220f, t);
            float infoX = Mathf.Lerp(0f, -side * 220f, t);
            float scale = Mathf.Lerp(1f, 1.08f, t);
            _detailArt.style.translate = TranslatePixels(artX, -18f * t);
            _detailArt.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _detailArt.style.opacity = 1f - t;
            _detailShadow.style.translate = TranslatePixels(
                artX + 27f,
                (_isRicko ? 0f : 9f) - 18f * t);
            _detailShadow.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _detailShadow.style.opacity = (1f - t) * 0.5f;
            _detailInfo.style.translate = TranslatePixels(infoX, 0f);
            _detailInfo.style.opacity = 1f - t;
            _dotGrid.style.opacity = (1f - t) * 0.84f;
        }

        public void ApplyHoldProgress(float progress)
        {
            float t = Mathf.Clamp01(progress);
            float y = Mathf.Lerp(-5f, 5f, t);
            float scale = Mathf.Lerp(0.995f, 1.012f, t);
            _detailArt.style.translate = TranslatePixels(0f, y);
            _detailArt.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _detailShadow.style.translate = TranslatePixels(
                27f,
                (_isRicko ? 0f : 9f) - y);
        }

        public void ApplyDotHoldProgress(float progress)
        {
            float t = Mathf.Clamp01(progress);
            _dotGrid.style.translate = TranslatePixels(
                Mathf.Lerp(-18f, 18f, t),
                Mathf.Lerp(10f, -10f, t));
            _dotGrid.style.opacity = Mathf.Lerp(0.72f, 0.88f, t);
        }

        public void ApplyTransitionProgress(float progress, bool covering)
        {
            float global = Mathf.Clamp01(progress);
            _transitionLayer.style.opacity = 1f;
            for (int index = 0; index < _transitionSquares.Length; index++)
            {
                float stagger = (index % 5) * 0.055f + (index / 5) * 0.035f;
                float local = Mathf.Clamp01((global - stagger) / Mathf.Max(0.01f, 1f - stagger));
                float visible = covering ? local : 1f - local;
                float scale = Mathf.Lerp(0.12f, 1.08f, visible);
                _transitionSquares[index].style.opacity = visible;
                _transitionSquares[index].style.scale = new Scale(new Vector3(scale, scale, 1f));
                _transitionSquares[index].style.rotate = new Rotate(
                    new Angle((1f - visible) * ((index % 2 == 0) ? -22f : 22f)));
            }
            if (!covering && global >= 1f)
            {
                _transitionLayer.style.opacity = 0f;
            }
        }

        public void HideTransition()
        {
            _transitionLayer.style.opacity = 0f;
            for (int index = 0; index < _transitionSquares.Length; index++)
            {
                _transitionSquares[index].style.opacity = 0f;
            }
        }

        public void ApplyFlash(float progress)
        {
            _flash.style.opacity = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
        }

        public void SetCurtainOpacity(float opacity)
        {
            _flash.style.opacity = Mathf.Clamp01(opacity);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _openingContinueButton.clicked -= OnOpeningSkip;
            _openingSkipButton.clicked -= OnOpeningSkip;
            _rickoButton.clicked -= OnRicko;
            _stellarButton.clicked -= OnStellar;
            _selectButton.clicked -= OnCharacterAccepted;
            _detailBackButton.clicked -= OnDetailBack;
            _nameBackButton.clicked -= OnNameBack;
            _confirmButton.clicked -= OnNameConfirmed;
            _nameField.UnregisterValueChangedCallback(OnNameChanged);
            _nameField.UnregisterCallback<KeyDownEvent>(OnNameKeyDown);
            if (_detailGradientTexture != null)
            {
                UnityEngine.Object.Destroy(_detailGradientTexture);
                _detailGradientTexture = null;
            }
        }

        private void BindInputs()
        {
            _openingContinueButton.clicked += OnOpeningSkip;
            _openingSkipButton.clicked += OnOpeningSkip;
            _rickoButton.clicked += OnRicko;
            _stellarButton.clicked += OnStellar;
            _selectButton.clicked += OnCharacterAccepted;
            _detailBackButton.clicked += OnDetailBack;
            _nameBackButton.clicked += OnNameBack;
            _confirmButton.clicked += OnNameConfirmed;
            _nameField.RegisterValueChangedCallback(OnNameChanged);
            _nameField.RegisterCallback<KeyDownEvent>(OnNameKeyDown);

            // Register hover sounds
            _rickoButton.RegisterCallback<MouseEnterEvent>(evt =>
                PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.CardHover));
            _stellarButton.RegisterCallback<MouseEnterEvent>(evt =>
                PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.CardHover));
            _selectButton.RegisterCallback<MouseEnterEvent>(evt =>
                PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.CardHover));
            _confirmButton.RegisterCallback<MouseEnterEvent>(evt =>
                PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.CardHover));
        }

        private void OnOpeningSkip()
        {
            PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.ButtonClick);
            OpeningSkipRequested?.Invoke();
        }

        private void OnRicko()
        {
            PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.CardClick);
            CharacterRequested?.Invoke("ricko");
        }

        private void OnStellar()
        {
            PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.CardClick);
            CharacterRequested?.Invoke("stellar");
        }

        private void OnCharacterAccepted()
        {
            PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.CharacterAccepted);
            CharacterAccepted?.Invoke();
        }

        private void OnDetailBack()
        {
            PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.NavigationBack);
            DetailBackRequested?.Invoke();
        }

        private void OnNameBack()
        {
            PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.NavigationBack);
            NameBackRequested?.Invoke();
        }

        private void OnNameConfirmed()
        {
            PowerMath.Audio.SfxController.Instance?.PlayCharacterSelection(PowerMath.Audio.CharacterSelectionSfxState.NameConfirmed);
            NameConfirmed?.Invoke();
        }

        private void OnNameKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                OnNameConfirmed();
            }
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            UpdateNameCount(evt.newValue);
            NameChanged?.Invoke(evt.newValue);
        }

        private void UpdateNameCount(string value)
        {
            int length = new StringInfo(value ?? string.Empty).LengthInTextElements;
            _nameCount.text = length.ToString(CultureInfo.InvariantCulture) +
                " / " + MaximumDisplayNameLength.ToString(CultureInfo.InvariantCulture);
        }

        private void ApplyCharacterTheme(string characterId)
        {
            _isRicko = string.Equals(characterId, "ricko", StringComparison.Ordinal);
            _overlay.EnableInClassList("is-ricko", _isRicko);
            _overlay.EnableInClassList("is-stellar", !_isRicko);
            _detailStage.EnableInClassList("is-ricko", _isRicko);
            _detailStage.EnableInClassList("is-stellar", !_isRicko);
            _nameStage.EnableInClassList("is-ricko", _isRicko);
            _nameStage.EnableInClassList("is-stellar", !_isRicko);
        }

        private void ShowOnly(VisualElement active)
        {
            for (int index = 0; index < _stages.Length; index++)
            {
                _stages[index].style.display = ReferenceEquals(_stages[index], active)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void BuildDotPattern()
        {
            if (_dotGrid.childCount > 0) return;
            for (int row = 0; row < DotRows; row++)
            {
                for (int column = 0; column < DotColumns; column++)
                {
                    var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                    dot.AddToClassList("prep-dot");
                    if ((row + column * 3) % 11 == 0)
                        dot.AddToClassList("prep-dot--soft");
                    else if ((row * 5 + column) % 13 == 0)
                        dot.AddToClassList("prep-dot--bright");
                    dot.style.left = Length.Percent(column * 100f / (DotColumns - 1));
                    dot.style.top = Length.Percent(row * 100f / (DotRows - 1));
                    float variedOpacity = 0.62f + ((row + column) % 4) * 0.11f;
                    dot.style.opacity = variedOpacity;
                    _dotGrid.Add(dot);
                }
            }
        }

        private void BuildDetailGradient()
        {
            const int height = 128;
            _detailGradientTexture = new Texture2D(
                2,
                height,
                TextureFormat.RGBA32,
                false,
                true)
            {
                name = "PlayerPreparationDetailGradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < height; y++)
            {
                float t = 1f - y / (height - 1f);
                Color color = new Color(0f, 0f, 0f, t * 0.75f);
                _detailGradientTexture.SetPixel(0, y, color);
                _detailGradientTexture.SetPixel(1, y, color);
            }
            _detailGradientTexture.Apply(false, true);
            _detailGradient.style.backgroundImage = new StyleBackground(
                Background.FromTexture2D(_detailGradientTexture));
        }

        private VisualElement[] BuildTransitionSquares()
        {
            _transitionLayer.Clear();
            var squares = new VisualElement[TransitionSquareCount];
            for (int index = 0; index < squares.Length; index++)
            {
                var square = new VisualElement { pickingMode = PickingMode.Ignore };
                square.AddToClassList("prep-transition-square");
                if (index == 2 || index == 12)
                {
                    square.AddToClassList("prep-transition-square--accent");
                }
                _transitionLayer.Add(square);
                squares[index] = square;
            }
            return squares;
        }

        private static Translate TranslatePixels(float x, float y)
        {
            return new Translate(
                new Length(x, LengthUnit.Pixel),
                new Length(y, LengthUnit.Pixel),
                0f);
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException(
                    $"Player preparation requires {typeof(T).Name} '{name}'.");
            }
            return element;
        }
    }
}
