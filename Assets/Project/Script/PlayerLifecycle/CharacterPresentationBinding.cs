using PowerMath.Localization;
using PowerMath.PlayerData;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.PlayerLifecycle
{
    public sealed class CharacterPresentationBinding : MonoBehaviour
    {
        private VisualElement _root;
        private PlayerSessionStore _store;
        private CharacterPresentationCatalog _catalog;
        public void Bind(VisualElement root)
        {
            _root = root;
            _catalog = Resources.Load<CharacterPresentationCatalog>("CharacterPresentationCatalog");
            _store = PlayerSessionStore.Instance;
            if (_store != null) _store.Changed += Render;
            LocalizationService.Changed += Refresh;
            Refresh();
        }
        private void Refresh() => Render(_store?.Snapshot);
        private void Render(PlayerSnapshot player)
        {
            string id = player?.profile?.characterId;
            if (!Session.PlayerLifecyclePolicy.IsCharacter(id)) return;
            var definition = _catalog?.Find(id);
            string title = LocalizationService.Get("onboarding." + id);
            var name = _root.Q<Label>("CharacterName");
            if (name != null) { name.text = player.profile.displayName; name.tooltip = title; }
            Apply(_root.Q("player-hub-avatar"), CharacterPlaceholderSprites.Resolve(definition?.hubSprite, id), title);
            _root.Query(className: "hud-profile-picture").ForEach(element => Apply(element, CharacterPlaceholderSprites.Resolve(definition?.profileIcon, id), title));
            foreach (var actor in FindObjectsByType<PowerMath.Gameplay.Combat.Unity.ActorPresentationController>())
            {
                if (!actor.IsPlayer || actor.gameObject.name == "bg") continue;
                ApplyToPlayerActor(actor, id);
            }
        }
        public static void ApplyToPlayerActor(PowerMath.Gameplay.Combat.Unity.ActorPresentationController actor, string characterId = null)
        {
            if (actor == null || !actor.IsPlayer || actor.gameObject.name == "bg") return;
            string id = characterId ?? PlayerSessionStore.Instance?.Snapshot?.profile?.characterId;
            if (!Session.PlayerLifecyclePolicy.IsCharacter(id)) id = "ricko";
            var catalog = Resources.Load<CharacterPresentationCatalog>("CharacterPresentationCatalog");
            var definition = catalog?.Find(id);
            actor.ConfigureSprites(
                CharacterPlaceholderSprites.Resolve(definition?.battleSprite, id),
                definition?.battleAttackSprite,
                definition?.battleHurtSprite);
        }
        private static void Apply(VisualElement element, Sprite sprite, string title)
        {
            if (element == null) return;
            element.tooltip = title;
            if (sprite != null)
            {
                element.style.backgroundImage = new StyleBackground(sprite);
                element.Q<Label>("character-placeholder")?.RemoveFromHierarchy();
                element.Query<Label>(className: "player-hub-avatar-placeholder").ForEach(l => l.style.display = DisplayStyle.None);
            }
            else
            {
                // An explicit named placeholder avoids implying the opposite protagonist.
                element.style.backgroundImage = StyleKeyword.None;
                var placeholder = element.Q<Label>("character-placeholder");
                if (placeholder == null)
                {
                    placeholder = new Label { name = "character-placeholder" };
                    placeholder.style.whiteSpace = WhiteSpace.Normal;
                    element.Add(placeholder);
                }
                placeholder.text = title;
                element.Query<Label>(className: "player-hub-avatar-placeholder").ForEach(l => l.style.display = DisplayStyle.Flex);
            }
        }
        private void OnDestroy()
        {
            if (_store != null) _store.Changed -= Render;
            LocalizationService.Changed -= Refresh;
        }
    }
}
