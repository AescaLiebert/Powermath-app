using System;
using System.Collections.Generic;
using PowerMath.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace PowerMath.UI.Authentication.Announcements
{
    public sealed class AnnouncementMediaPresenter : IDisposable
    {
        private readonly MonoBehaviour _owner;
        private readonly List<Action> _cleanup = new List<Action>();

        public AnnouncementMediaPresenter(MonoBehaviour owner)
        {
            _owner = owner;
        }

        public bool TryAdd(
            VisualElement container,
            string source,
            string alt)
        {
            if (container == null || string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            int separator = source.IndexOf(':');
            if (separator <= 0 || separator >= source.Length - 1)
            {
                return false;
            }

            string scheme = source.Substring(0, separator).Trim().ToLowerInvariant();
            string path = source.Substring(separator + 1).Trim();
            switch (scheme)
            {
                case "resource":
                    return AddStatic(container, path, alt);
                case "frames":
                    return AddFrames(container, path, alt);
                case "video":
                    return AddVideo(container, path, alt);
                default:
                    return false;
            }
        }

        public void Clear()
        {
            for (int index = _cleanup.Count - 1; index >= 0; index--)
            {
                _cleanup[index]?.Invoke();
            }
            _cleanup.Clear();
        }

        public void Dispose()
        {
            Clear();
        }

        private bool AddStatic(
            VisualElement container,
            string path,
            string alt)
        {
            Sprite sprite = Resources.Load<Sprite>(path);
            Texture2D texture = sprite == null
                ? Resources.Load<Texture2D>(path)
                : null;
            if (sprite == null && texture == null)
            {
                return false;
            }

            var image = CreateImage(alt);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
            else
            {
                image.image = texture;
            }
            container.Add(image);
            return true;
        }

        private bool AddFrames(
            VisualElement container,
            string path,
            string alt)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(path);
            if (frames == null || frames.Length == 0)
            {
                return false;
            }

            var image = CreateImage(alt);
            image.image = frames[0].texture;
            container.Add(image);
            int frameIndex = 0;
            IVisualElementScheduledItem scheduled = image.schedule.Execute(() =>
            {
                frameIndex = (frameIndex + 1) % frames.Length;
                image.image = frames[frameIndex].texture;
            }).Every(100);
            _cleanup.Add(() => scheduled.Pause());
            return true;
        }

        private bool AddVideo(
            VisualElement container,
            string path,
            string alt)
        {
            if (_owner == null ||
                !StreamingVideoPath.TryResolve(path, out string url))
            {
                return false;
            }

            var image = CreateImage(alt);
            var texture = new RenderTexture(
                960,
                540,
                0,
                RenderTextureFormat.ARGB32)
            {
                name = "Announcement Video"
            };
            texture.Create();
            image.image = texture;
            container.Add(image);

            VideoPlayer player = _owner.gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = texture;
            player.source = VideoSource.Url;
            player.url = url;
            player.errorReceived += OnVideoError;
            player.prepareCompleted += prepared => prepared.Play();
            player.Prepare();

            _cleanup.Add(() =>
            {
                if (player != null)
                {
                    player.Stop();
                    player.errorReceived -= OnVideoError;
                    UnityEngine.Object.Destroy(player);
                }
                if (texture != null)
                {
                    texture.Release();
                    UnityEngine.Object.Destroy(texture);
                }
            });
            return true;
        }

        private static Image CreateImage(string alt)
        {
            var image = new Image
            {
                name = string.IsNullOrWhiteSpace(alt)
                    ? "announcement-media"
                    : alt,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            image.AddToClassList("announcement-media");
            return image;
        }

        private static void OnVideoError(VideoPlayer player, string message)
        {
            PowerMath.Diagnostics.AppLog.Warning(
                "Announcements",
                "Announcement video failed: " + message);
        }
    }
}
