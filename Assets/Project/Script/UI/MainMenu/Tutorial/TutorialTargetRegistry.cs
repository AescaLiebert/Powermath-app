using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.Tutorial
{
    public readonly struct TutorialFocusTarget
    {
        private readonly Func<bool> _activate;

        public TutorialFocusTarget(RectTransform sceneTarget, Func<bool> activate = null)
            : this(sceneTarget, new Rect(0f, 0f, 1f, 1f), activate)
        {
        }

        public TutorialFocusTarget(
            RectTransform sceneTarget,
            Rect normalizedFocusRect,
            Func<bool> activate = null)
        {
            SceneTarget = sceneTarget;
            UiTarget = null;
            NormalizedFocusRect = normalizedFocusRect;
            _activate = activate;
        }

        public TutorialFocusTarget(VisualElement uiTarget, Func<bool> activate = null)
        {
            SceneTarget = null;
            UiTarget = uiTarget;
            NormalizedFocusRect = new Rect(0f, 0f, 1f, 1f);
            _activate = activate;
        }

        public RectTransform SceneTarget { get; }
        public VisualElement UiTarget { get; }
        public Rect NormalizedFocusRect { get; }
        public bool IsAvailable => SceneTarget != null || UiTarget?.panel != null;
        public bool TryActivate() => _activate?.Invoke() == true;
    }

    public sealed class TutorialTargetRegistry
    {
        private readonly Dictionary<string, TutorialFocusTarget> _targets =
            new Dictionary<string, TutorialFocusTarget>(StringComparer.Ordinal);

        public TutorialTargetRegistry Register(string id, RectTransform target)
        {
            if (!string.IsNullOrWhiteSpace(id) && target != null)
                _targets[id] = new TutorialFocusTarget(target);
            return this;
        }

        public TutorialTargetRegistry Register(
            string id,
            RectTransform target,
            Rect normalizedFocusRect,
            Func<bool> activate = null)
        {
            if (!string.IsNullOrWhiteSpace(id) && target != null)
            {
                var safeRect = new Rect(
                    Mathf.Clamp01(normalizedFocusRect.x),
                    Mathf.Clamp01(normalizedFocusRect.y),
                    Mathf.Clamp01(normalizedFocusRect.width),
                    Mathf.Clamp01(normalizedFocusRect.height));
                safeRect.width = Mathf.Min(safeRect.width, 1f - safeRect.x);
                safeRect.height = Mathf.Min(safeRect.height, 1f - safeRect.y);
                _targets[id] = new TutorialFocusTarget(target, safeRect, activate);
            }
            return this;
        }

        public TutorialTargetRegistry Register(
            string id,
            VisualElement target,
            Func<bool> activate)
        {
            if (!string.IsNullOrWhiteSpace(id) && target != null)
                _targets[id] = new TutorialFocusTarget(target, activate);
            return this;
        }

        public TutorialTargetRegistry Register(string id, VisualElement target)
        {
            if (!string.IsNullOrWhiteSpace(id) && target != null)
                _targets[id] = new TutorialFocusTarget(target);
            return this;
        }

        public bool TryResolve(
            string targetId,
            string fallbackTargetId,
            out TutorialFocusTarget target)
        {
            if (TryResolve(targetId, out target)) return true;
            return TryResolve(fallbackTargetId, out target);
        }

        private bool TryResolve(string id, out TutorialFocusTarget target)
        {
            if (!string.IsNullOrWhiteSpace(id) &&
                _targets.TryGetValue(id, out target) && target.IsAvailable)
                return true;
            target = default;
            return false;
        }
    }
}
