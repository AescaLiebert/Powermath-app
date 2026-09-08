using System;
using System.Collections;
using PowerMath.Localization;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.PlayerLifecycle
{
    public sealed class PlayerPreparationPresenter : MonoBehaviour
    {
        private VisualElement _overlay;
        private VisualElement _content;
        private Label _error;
        private VisualElement _recovery;
        private PlayerSessionStore _store;
        private IPlayerLifecycleCommands _commands;
        private Action _completed;
        private PlayerLifecycleCommand _pending;
        private bool _busy;
        private string _preview;
        private string _name;
        private CharacterPresentationCatalog _catalog;
        private OpeningSequenceDefinition _opening;
        private UnityEngine.Video.VideoPlayer _video;
        private RenderTexture _videoTexture;
        private Image _videoImage;

        public void Initialize(VisualElement root, PlayerSessionStore store, IPlayerLifecycleCommands commands, Action completed)
        {
            _store = store; _commands = commands; _completed = completed;
            _catalog = Resources.Load<CharacterPresentationCatalog>("CharacterPresentationCatalog");
            _opening = Resources.Load<OpeningSequenceDefinition>("OpeningSequence");
            _overlay = new VisualElement { name = "player-preparation" };
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = _overlay.style.right = _overlay.style.top = _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = new Color(0.035f, 0.08f, 0.16f);
            _overlay.style.color = Color.white;
            _overlay.style.alignItems = Align.Center;
            _overlay.style.justifyContent = Justify.Center;
            _content = new VisualElement();
            _content.style.width = Length.Percent(85);
            _content.style.maxWidth = 850;
            _overlay.Add(_content);
            _recovery = new VisualElement();
            _overlay.Add(_recovery);
            root.Add(_overlay);
            LocalizationService.Changed += Render;
            Render();
        }

        private Button Button(string key, Action action)
        {
            var button = new Button(action) { name = key, text = LocalizationService.Get(key) };
            button.style.minHeight = 44;
            button.style.marginTop = 10;
            _content.Add(button);
            return button;
        }
        private void Title(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 28;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 15;
            _content.Add(label);
        }
        private void Render()
        {
            if (_content == null || _store?.Snapshot == null) return;
            CleanupVideo();
            _content.Clear();
            _recovery.Clear();
            var player = _store.Snapshot;
            if (PlayerLifecyclePolicy.IsComplete(player))
            {
                _overlay.RemoveFromHierarchy();
                _completed?.Invoke();
                Destroy(this);
                return;
            }
            if (player.onboarding?.phase == "opening")
            {
                Title(LocalizationService.Get("onboarding.openingTitle"));
                string copy = LocalizationService.Locale == "th" ? _opening?.thaiText : _opening?.englishText;
                var text = new Label(string.IsNullOrWhiteSpace(copy) ? LocalizationService.Get("onboarding.openingPlaceholder") : copy);
                text.style.whiteSpace = WhiteSpace.Normal;
                _content.Add(text);
                if (_opening != null && _opening.HasVideo)
                {
                    _videoImage = new Image();
                    _videoImage.style.height = 220;
                    _content.Add(_videoImage);
                    Button("common.play", PlayOpening);
                }
                Button("common.continue", () => Submit(PlayerLifecycleCommandKind.CompleteOpening));
                Button("onboarding.skip", () => Submit(PlayerLifecycleCommandKind.CompleteOpening));
            }
            else if (player.onboarding?.phase == "character" || _preview != null)
            {
                Title(LocalizationService.Get("onboarding.select"));
                if (_preview == null)
                {
                    var choices = new VisualElement();
                    choices.style.flexDirection = FlexDirection.Row;
                    _content.Add(choices);
                    foreach (string id in new[] { "ricko", "stellar" })
                    {
                        var card = new Button(() => { _preview = id; Render(); }) { name = id };
                        card.style.width = Length.Percent(48);
                        card.style.height = 220;
                        card.style.marginRight = 10;
                        card.style.backgroundColor = id == "ricko" ? new Color(.38f, .17f, .12f) : new Color(.16f, .24f, .48f);
                        card.style.color = Color.white;
                        card.style.fontSize = 28;
                        card.Add(new Label(LocalizationService.Get("onboarding." + id)));
                        card.style.flexDirection = FlexDirection.Column;
                        var portrait = new Image { name = "portrait-" + id, sprite = CharacterPlaceholderSprites.Resolve(_catalog?.Find(id)?.selectionArt, id) };
                        portrait.style.width = 130;
                        portrait.style.height = 150;
                        portrait.style.flexShrink = 0;
                        portrait.style.alignSelf = Align.Center;
                        card.Add(portrait);
                        choices.Add(card);
                    }
                }
                else
                {
                    Title(LocalizationService.Get("onboarding." + _preview));
                    Sprite art = CharacterPlaceholderSprites.Resolve(_catalog?.Find(_preview)?.selectionArt, _preview);
                    var image = new Image { sprite = art };
                    image.style.height = 180;
                    if (art != null) _content.Add(image);
                    else _content.Add(new Label(LocalizationService.Get("onboarding.missingArt")));
                    Button("onboarding.choose", () => Submit(PlayerLifecycleCommandKind.SelectCharacter, _preview));
                    Button("common.back", () => { _preview = null; Render(); });
                }
            }
            else if (player.onboarding?.phase == "name")
            {
                Title(LocalizationService.Get("onboarding." + player.onboarding.selectedCharacterId));
                Title(LocalizationService.Get("onboarding.name"));
                var input = new TextField { name = "preparation-display-name", value = _name ?? (player.onboarding.legacyPlayer ? player.profile.displayName : string.Empty) };
                input.style.minHeight = 40;
                input.RegisterValueChangedCallback(evt => _name = evt.newValue);
                _name = input.value;
                _content.Add(input);
                _content.Add(new Label(LocalizationService.Get("onboarding.permanent")));
                Button("common.confirm", () =>
                {
                    if (!PlayerLifecyclePolicy.TryNormalizeName(_name, out var normalized))
                    { _error.text = LocalizationService.Get("onboarding.invalidName"); return; }
                    _name = normalized;
                    Submit(PlayerLifecycleCommandKind.CompletePreparation, player.onboarding.selectedCharacterId);
                });
                Button("common.back", () => { _preview = player.onboarding.selectedCharacterId; Render(); });
            }
            else
            {
                Title(LocalizationService.Get("errors.updateRequired"));
            }
            _error = new Label();
            _error.style.whiteSpace = WhiteSpace.Normal;
            _content.Add(_error);
            if (_pending != null && !_busy)
            {
                _content.SetEnabled(false);
                var recovery = new Button(() => StartCoroutine(SendPending())) { text = LocalizationService.Get("common.retry") };
                _recovery.Add(recovery);
                recovery.clicked += () => recovery.RemoveFromHierarchy();
            }
            else _content.SetEnabled(!_busy);
        }
        private void Submit(PlayerLifecycleCommandKind kind, string value = null)
        {
            if (_busy || _pending != null) return;
            _pending = new PlayerLifecycleCommand
            {
                kind = kind, value = value, displayName = _name,
                playerId = _store.Snapshot.playerId, expectedRevision = _store.Snapshot.revision,
                operationId = Guid.NewGuid().ToString("N")
            };
            StartCoroutine(SendPending());
        }
        private IEnumerator SendPending()
        {
            _busy = true; _content.SetEnabled(false); _recovery.Clear();
            _error.text = LocalizationService.Get("common.saving");
            var command = _pending;
            FirestoreRestClient.Failure? failure = null;
            PlayerSnapshot saved = null;
            yield return _commands.Execute(command, result => saved = result, error => failure = error);
            _busy = false;
            if (_store?.Snapshot?.playerId != command.playerId) yield break;
            if (saved != null)
            {
                _pending = null; _preview = null;
                _store.TryHydrate(new BootstrapResponse { player = saved, schemaVersion = saved.schemaVersion, remembered = _store.IsRemembered });
                Render();
            }
            else
            {
                Render();
                _error.text = LocalizationService.Get(failure?.Kind == FirestoreRestClient.FailureKind.Conflict ? "errors.conflict" : "errors.save");
                if (failure?.Kind == FirestoreRestClient.FailureKind.Conflict)
                {
                    _recovery.Clear();
                    _recovery.Add(new Button(() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
                    { text = LocalizationService.Get("common.reload") });
                }
            }
        }
        private void PlayOpening()
        {
            if (_video != null || _opening == null || !_opening.HasVideo) return;
            _videoTexture = new RenderTexture(1280, 720, 0);
            _video = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
            _video.playOnAwake = false;
            _video.isLooping = false;
            _video.source = UnityEngine.Video.VideoSource.Url;
            _video.url = _opening.videoUrl;
            _video.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            _video.targetTexture = _videoTexture;
            _videoImage.image = _videoTexture;
            _video.errorReceived += (player, message) =>
            {
                _error.text = LocalizationService.Get("onboarding.videoUnavailable");
                CleanupVideo();
            };
            _video.prepareCompleted += player => player.Play();
            _video.Prepare();
        }
        private void CleanupVideo()
        {
            if (_video != null) { _video.Stop(); Destroy(_video); _video = null; }
            if (_videoTexture != null) { _videoTexture.Release(); Destroy(_videoTexture); _videoTexture = null; }
        }
        private void OnDestroy()
        {
            CleanupVideo();
            LocalizationService.Changed -= Render;
            _overlay?.RemoveFromHierarchy();
        }
    }
}

