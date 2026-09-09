using System;
using UnityEngine;
using UnityEngine.Video;
using PowerMath.UI.Shared;

namespace PowerMath.PlayerLifecycle
{
    [CreateAssetMenu(menuName = "PowerMath/Player/Character Presentation Catalog")]
    public sealed class CharacterPresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Character
        {
            public string id;
            [Tooltip("Full-screen art used when the selection video is unavailable.")]
            public Sprite overviewArt;
            public Sprite selectionArt;
            public Sprite profileIcon;
            public Sprite hubSprite;
            public Sprite battleSprite;
            public Sprite battleAttackSprite;
            public Sprite battleHurtSprite;
            public Sprite battleIdleSprite => battleSprite;
            public VideoClip hubVideo;
            [Tooltip("Hosted or StreamingAssets-relative hub-loop URL used by WebGL.")]
            public string hubVideoUrl;
        }
        public Material chromaKeyMaterial;
        [Header("First Login Selection")]
        public VideoClip selectionVideo;
        [Tooltip("Hosted selection-loop URL for WebGL, which cannot use embedded VideoClip assets.")]
        public string selectionVideoUrl;
        public Sprite selectionBackground;
        public bool HasHostedSelectionVideo =>
            StreamingVideoPath.TryResolve(selectionVideoUrl, out _);
        public Character[] characters = { new Character { id = "ricko" }, new Character { id = "stellar" } };
        public Character Find(string id) => Array.Find(characters ?? Array.Empty<Character>(), value => value != null && value.id == id);
        private void OnValidate()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var entry in characters ?? Array.Empty<Character>())
                if (entry == null || !Session.PlayerLifecyclePolicy.IsCharacter(entry.id) || !ids.Add(entry.id))
                    PowerMath.Diagnostics.AppLog.Error("Character", "Character catalog requires unique ricko/stellar IDs.", this);
        }
    }
}

