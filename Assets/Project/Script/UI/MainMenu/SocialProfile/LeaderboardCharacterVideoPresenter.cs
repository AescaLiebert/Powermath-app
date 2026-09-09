using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;
using PowerMath.UI.Shared;

namespace PowerMath.UI.MainMenu.SocialProfile
{
    /// <summary>
    /// Presents the rank-one hub video inside the UI Toolkit hierarchy. The
    /// existing chroma-key material is applied into a transparent RenderTexture,
    /// avoiding a second raycasting UI surface above the menu.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class LeaderboardCharacterVideoPresenter : MonoBehaviour
    {
        private Image _target;
        private Label _placeholder;
        private CharacterPresentationCatalog _catalog;
        private VideoPlayer _player;
        private Material _chromaMaterial;
        private RenderTexture _source;
        private RenderTexture _output;
        private string _characterId;
        private bool _visible;

        public void Bind(VisualElement root)
        {
            _target = root?.Q<Image>(".character-sprite-placeholder");
            _placeholder = _target?.Q<Label>("Slot Label");
            _catalog = Resources.Load<CharacterPresentationCatalog>("CharacterPresentationCatalog");
            if (_target != null)
                _target.pickingMode = PickingMode.Ignore;
        }

        public void Show(string characterId)
        {
            _visible = true;
            if (_target == null)
                return;

            CharacterPresentationCatalog.Character definition = _catalog?.Find(characterId);
            if (!PlayerLifecyclePolicy.IsCharacter(characterId))
            {
                StopVideo();
                _target.image = null;
                _target.sprite = null;
                SetPlaceholder(true);
                return;
            }
            bool hasVideo = definition != null &&
#if UNITY_WEBGL && !UNITY_EDITOR
                StreamingVideoPath.TryResolve(definition.hubVideoUrl, out _);
#else
                definition.hubVideo != null;
#endif
            if (!hasVideo || _catalog.chromaKeyMaterial == null)
            {
                StopVideo();
                _target.image = null;
                _target.sprite = CharacterPlaceholderSprites.Resolve(definition?.hubSprite, characterId);
                SetPlaceholder(false);
                return;
            }

            if (_player != null && _characterId == characterId)
            {
                _player.Play();
                _target.image = _output;
                SetPlaceholder(false);
                return;
            }

            StopVideo();
            _characterId = characterId;
#if UNITY_WEBGL && !UNITY_EDITOR
            int width = 1024;
            int height = 1536;
#else
            int width = definition.hubVideo.width > 0 ? (int)definition.hubVideo.width : 1024;
            int height = definition.hubVideo.height > 0 ? (int)definition.hubVideo.height : 1524;
#endif
            _source = CreateTexture(width, height, "Leaderboard Character Video Source");
            _output = CreateTexture(width, height, "Leaderboard Character Video Chroma");
            _chromaMaterial = new Material(_catalog.chromaKeyMaterial)
            {
                name = "Leaderboard Character ChromaKey (Runtime)"
            };

            var videoObject = new GameObject("CharacterVideoOwnCanvas");
            videoObject.transform.SetParent(transform, false);
            _player = videoObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = true;
            _player.audioOutputMode = VideoAudioOutputMode.None;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _source;
#if UNITY_WEBGL && !UNITY_EDITOR
            _player.source = VideoSource.Url;
            StreamingVideoPath.TryResolve(definition.hubVideoUrl, out string hubVideoUrl);
            _player.url = hubVideoUrl;
#else
            _player.source = VideoSource.VideoClip;
            _player.clip = definition.hubVideo;
#endif
            _player.errorReceived += OnVideoError;
            _player.skipOnDrop = true;
            _target.sprite = null;
            _target.image = _output;
            SetPlaceholder(false);
            _player.Play();
        }

        public void Hide()
        {
            _visible = false;
            if (_player != null)
                _player.Pause();
        }

        private void LateUpdate()
        {
            if (!_visible || _player == null || !_player.isPrepared ||
                _source == null || _output == null || _chromaMaterial == null)
                return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _output;
            GL.Clear(true, true, Color.clear);
            Graphics.Blit(_source, _output, _chromaMaterial);
            RenderTexture.active = previous;
            _target?.MarkDirtyRepaint();
        }

        private void StopVideo()
        {
            if (_target != null && ReferenceEquals(_target.image, _output))
                _target.image = null;
            if (_player != null)
            {
                _player.errorReceived -= OnVideoError;
                _player.Stop();
                Destroy(_player.gameObject);
                _player = null;
            }
            if (_source != null) { _source.Release(); Destroy(_source); _source = null; }
            if (_output != null) { _output.Release(); Destroy(_output); _output = null; }
            if (_chromaMaterial != null) { Destroy(_chromaMaterial); _chromaMaterial = null; }
            _characterId = null;
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            string characterId = _characterId;
            CharacterPresentationCatalog.Character definition = _catalog?.Find(characterId);
            StopVideo();
            if (_target == null) return;
            _target.image = null;
            _target.sprite = CharacterPlaceholderSprites.Resolve(
                definition?.hubSprite,
                characterId);
            SetPlaceholder(false);
            PowerMath.Diagnostics.AppLog.Warning(
                "Leaderboard",
                "Character video could not be played: " + message,
                this);
        }

        private static RenderTexture CreateTexture(int width, int height, string name)
        {
            var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();
            return texture;
        }

        private void SetPlaceholder(bool visible)
        {
            if (_placeholder != null)
                _placeholder.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDestroy() => StopVideo();
    }
}
