using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    /// <summary>
    /// USS-configurable native UI Toolkit gradient surface.
    /// Coordinates use Figma's normalized, top-left-relative gradient handles.
    /// </summary>
    [UxmlElement]
    public partial class FigmaGradientElement : VisualElement
    {
        private const int MaximumStops = 4;

        private static readonly CustomStyleProperty<Color>[] ColorProperties =
        {
            new("--figma-gradient-color-0"),
            new("--figma-gradient-color-1"),
            new("--figma-gradient-color-2"),
            new("--figma-gradient-color-3")
        };

        private static readonly CustomStyleProperty<float>[] StopProperties =
        {
            new("--figma-gradient-stop-0"),
            new("--figma-gradient-stop-1"),
            new("--figma-gradient-stop-2"),
            new("--figma-gradient-stop-3")
        };

        private static readonly CustomStyleProperty<float> StopCountProperty =
            new("--figma-gradient-stop-count");
        private static readonly CustomStyleProperty<float> StartXProperty =
            new("--figma-gradient-start-x");
        private static readonly CustomStyleProperty<float> StartYProperty =
            new("--figma-gradient-start-y");
        private static readonly CustomStyleProperty<float> EndXProperty =
            new("--figma-gradient-end-x");
        private static readonly CustomStyleProperty<float> EndYProperty =
            new("--figma-gradient-end-y");
        private static readonly CustomStyleProperty<float> RadiusProperty =
            new("--figma-gradient-radius");

        private readonly Color[] _colors =
        {
            Color.black,
            Color.white,
            Color.white,
            Color.white
        };

        private readonly float[] _stops = { 0f, 1f, 1f, 1f };
        private readonly Gradient _gradient = new();
        private int _stopCount = 2;
        private Vector2 _start = new(0.5f, 1f);
        private Vector2 _end = new(0.5f, 0f);
        private float _radius;

        public FigmaGradientElement()
        {
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(ResolveCustomStyles);
            generateVisualContent += DrawGradient;
            RebuildGradient();
        }

        internal int StopCount => _stopCount;
        internal Vector2 GradientStart => _start;
        internal Vector2 GradientEnd => _end;

        internal Color Evaluate(float position)
        {
            return _gradient.Evaluate(Mathf.Clamp01(position));
        }

        private void ResolveCustomStyles(CustomStyleResolvedEvent evt)
        {
            for (int i = 0; i < MaximumStops; i++)
            {
                if (evt.customStyle.TryGetValue(ColorProperties[i], out Color color))
                    _colors[i] = color;
                if (evt.customStyle.TryGetValue(StopProperties[i], out float stop))
                    _stops[i] = Mathf.Clamp01(stop);
            }

            if (evt.customStyle.TryGetValue(StopCountProperty, out float count))
                _stopCount = Mathf.Clamp(Mathf.RoundToInt(count), 2, MaximumStops);
            if (evt.customStyle.TryGetValue(StartXProperty, out float startX))
                _start.x = startX;
            if (evt.customStyle.TryGetValue(StartYProperty, out float startY))
                _start.y = startY;
            if (evt.customStyle.TryGetValue(EndXProperty, out float endX))
                _end.x = endX;
            if (evt.customStyle.TryGetValue(EndYProperty, out float endY))
                _end.y = endY;
            if (evt.customStyle.TryGetValue(RadiusProperty, out float radius))
                _radius = Mathf.Max(0f, radius);

            RebuildGradient();
            MarkDirtyRepaint();
        }

        private void RebuildGradient()
        {
            var keys = new GradientColorKey[_stopCount];
            var alphaKeys = new GradientAlphaKey[_stopCount];
            var sorted = new (float stop, Color color)[_stopCount];

            for (int i = 0; i < _stopCount; i++)
                sorted[i] = (_stops[i], _colors[i]);
            Array.Sort(sorted, (left, right) => left.stop.CompareTo(right.stop));

            for (int i = 0; i < _stopCount; i++)
            {
                keys[i] = new GradientColorKey(sorted[i].color, sorted[i].stop);
                alphaKeys[i] = new GradientAlphaKey(sorted[i].color.a, sorted[i].stop);
            }

            _gradient.SetKeys(keys, alphaKeys);
        }

        private void DrawGradient(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 0f || bounds.height <= 0f)
                return;

            Vector2 start = new(
                bounds.xMin + _start.x * bounds.width,
                bounds.yMin + _start.y * bounds.height);
            Vector2 end = new(
                bounds.xMin + _end.x * bounds.width,
                bounds.yMin + _end.y * bounds.height);

            Painter2D painter = context.painter2D;
            painter.fillGradient = FillGradient.MakeLinearGradient(
                _gradient,
                start,
                end,
                AddressMode.Clamp);
            FigmaShadowElement.DrawRoundedRect(painter, bounds, _radius);
            painter.Fill();
        }
    }
}
