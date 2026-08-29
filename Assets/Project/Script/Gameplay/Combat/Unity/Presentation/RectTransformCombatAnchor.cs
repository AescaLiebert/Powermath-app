using System;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public interface ICombatAnchor
    {
        bool TryGetLocalPoint(RectTransform overlayRoot, Camera camera, out Vector2 point);
    }

    public sealed class RectTransformCombatAnchor : ICombatAnchor
    {
        private readonly RectTransform _target;
        private readonly Vector2 _normalizedAnchor;
        private readonly Vector2 _offset;

        public RectTransformCombatAnchor(
            RectTransform target,
            Vector2 normalizedAnchor,
            Vector2 offset)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _normalizedAnchor = normalizedAnchor;
            _offset = offset;
        }

        public bool TryGetLocalPoint(
            RectTransform overlayRoot,
            Camera camera,
            out Vector2 point)
        {
            point = default;
            if (_target == null || overlayRoot == null || !_target.gameObject.activeInHierarchy)
                return false;

            Vector3[] corners = new Vector3[4];
            _target.GetWorldCorners(corners);
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];
            Vector3 world = new Vector3(
                Mathf.Lerp(bottomLeft.x, topRight.x, _normalizedAnchor.x),
                Mathf.Lerp(bottomLeft.y, topRight.y, _normalizedAnchor.y),
                Mathf.Lerp(bottomLeft.z, topRight.z, _normalizedAnchor.y));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRoot, screen, camera, out point))
                return false;
            point += _offset;
            return true;
        }
    }
}
