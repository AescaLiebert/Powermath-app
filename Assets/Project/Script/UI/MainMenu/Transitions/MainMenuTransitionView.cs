using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class MainMenuTransitionView
    {
        private const string ReducedMotionClass = "is-reduced-motion";
        private const float OffscreenMarginPixels = 96f;
        private const float FallbackViewportWidth = 2560f;
        private const float FallbackViewportHeight = 1440f;

        private readonly VisualElement _screen;
        private readonly VisualElement _safeArea;
        private readonly VisualElement _transitionLayer;
        private readonly VisualElement _cover;
        private readonly Label _title;
        private readonly VisualElement _accentLeft;
        private readonly VisualElement _accentRight;
        private readonly VisualElement[] _topTargets;
        private readonly VisualElement _playerMenu;
        private readonly VisualElement _dashboard;
        private Vector2 _topStartOffset;
        private Vector2 _playerMenuStartOffset;
        private Vector2 _dashboardStartOffset;
        private bool _reducedMotion;

        public MainMenuTransitionView(VisualElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            _screen = Require<VisualElement>(root, "main-menu-screen");
            _screen.pickingMode = PickingMode.Ignore;
            _safeArea = Require<VisualElement>(root, "safe-area");
            _safeArea.pickingMode = PickingMode.Ignore;
            _transitionLayer = Require<VisualElement>(root, "main-menu-transition-layer");
            _cover = Require<VisualElement>(root, "main-menu-transition-cover");
            _title = Require<Label>(root, "battle-start-title");
            _accentLeft = Require<VisualElement>(root, "battle-start-accent-left");
            _accentRight = Require<VisualElement>(root, "battle-start-accent-right");
            _topTargets = new[]
            {
                Require<VisualElement>(root, "profile-panel"),
                Require<VisualElement>(root, "combat-enemy-card"),
                Require<VisualElement>(root, "main-navigator")
            };
            _playerMenu = Require<VisualElement>(root, "player-menu");
            _dashboard = Require<VisualElement>(root, "player-dashboard");
        }

        public VisualElement Screen => _screen;

        public void PrepareBootstrap(bool reducedMotion)
        {
            PrepareTargets(reducedMotion);
            SetInputLocked(true);
            _transitionLayer.AddToClassList("is-bootstrap-active");
            _cover.RemoveFromClassList("is-scene-revealed");
            SetTitleVisible(false);
        }

        public void ShowBattleTitle()
        {
            SetTitleVisible(true);
        }

        public void HideBattleTitle()
        {
            SetTitleVisible(false);
        }

        public void RevealScene()
        {
            _cover.AddToClassList("is-scene-revealed");
        }

        public void RevealTop()
        {
            ApplyTopProgress(1f);
        }

        public void RevealPlayerMenu()
        {
            ApplyPlayerMenuProgress(1f);
        }

        public void RevealDashboard()
        {
            ApplyDashboardProgress(1f);
        }

        public void ApplyTopProgress(float progress)
        {
            ApplyProgress(_topTargets, _topStartOffset, progress);
        }

        public void ApplyPlayerMenuProgress(float progress)
        {
            ApplyProgress(_playerMenu, _playerMenuStartOffset, progress);
        }

        public void ApplyDashboardProgress(float progress)
        {
            ApplyProgress(_dashboard, _dashboardStartOffset, progress);
        }

        public void ApplySessionUiProgress(float progress)
        {
            ApplyTopProgress(progress);
            ApplyPlayerMenuProgress(progress);
            ApplyDashboardProgress(progress);
        }

        public void ApplyFinalState()
        {
            _screen.RemoveFromClassList(ReducedMotionClass);
            _transitionLayer.RemoveFromClassList("is-bootstrap-active");
            _cover.AddToClassList("is-scene-revealed");
            SetTitleVisible(false);
            RevealTop();
            RevealPlayerMenu();
            RevealDashboard();
            SetInputLocked(false);
        }

        private void PrepareTargets(bool reducedMotion)
        {
            _reducedMotion = reducedMotion;
            _screen.EnableInClassList(ReducedMotionClass, reducedMotion);
            RefreshOffscreenOffsets();
            ApplyTopProgress(0f);
            ApplyPlayerMenuProgress(0f);
            ApplyDashboardProgress(0f);
        }

        private void RefreshOffscreenOffsets()
        {
            float width = ResolveDimension(
                _screen.resolvedStyle.width,
                global::UnityEngine.Screen.width,
                FallbackViewportWidth);
            float height = ResolveDimension(
                _screen.resolvedStyle.height,
                global::UnityEngine.Screen.height,
                FallbackViewportHeight);

            _topStartOffset = Vector2.down * (height + OffscreenMarginPixels);
            _playerMenuStartOffset = Vector2.left *
                (width + OffscreenMarginPixels);
            _dashboardStartOffset = Vector2.up *
                (height + OffscreenMarginPixels);
        }

        private void ApplyProgress(
            VisualElement[] targets,
            Vector2 startOffset,
            float progress)
        {
            for (int index = 0; index < targets.Length; index++)
                ApplyProgress(targets[index], startOffset, progress);
        }

        private void ApplyProgress(
            VisualElement target,
            Vector2 startOffset,
            float progress)
        {
            float normalized = Mathf.Clamp01(progress);
            Vector2 offset = _reducedMotion
                ? Vector2.zero
                : Vector2.LerpUnclamped(startOffset, Vector2.zero, progress);
            target.style.translate = new Translate(
                new Length(offset.x, LengthUnit.Pixel),
                new Length(offset.y, LengthUnit.Pixel),
                0f);
            target.style.opacity = normalized;
        }

        private static float ResolveDimension(
            float resolved,
            float screen,
            float fallback)
        {
            if (!float.IsNaN(resolved) && !float.IsInfinity(resolved) &&
                resolved > 1f)
                return resolved;
            return screen > 1f ? screen : fallback;
        }

        private void SetInputLocked(bool locked)
        {
            _safeArea.SetEnabled(!locked);
            _transitionLayer.EnableInClassList("is-transition-blocking", locked);
            _transitionLayer.pickingMode = locked
                ? PickingMode.Position
                : PickingMode.Ignore;
        }

        private void SetTitleVisible(bool visible)
        {
            _title.EnableInClassList("is-visible", visible);
            _accentLeft.EnableInClassList("is-visible", visible);
            _accentRight.EnableInClassList("is-visible", visible);
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException(
                    $"Main Menu transition requires {typeof(T).Name} '{name}'.");
            return element;
        }
    }
}
