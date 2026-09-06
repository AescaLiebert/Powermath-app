using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    /// <summary>USS-configurable rounded shadow layer for Figma drop-shadow effects.</summary>
    [UxmlElement]
    public partial class FigmaShadowElement : VisualElement
    {
        private static readonly CustomStyleProperty<Color> ColorProperty =
            new("--figma-shadow-color");
        private static readonly CustomStyleProperty<float> OffsetXProperty =
            new("--figma-shadow-offset-x");
        private static readonly CustomStyleProperty<float> OffsetYProperty =
            new("--figma-shadow-offset-y");
        private static readonly CustomStyleProperty<float> BlurProperty =
            new("--figma-shadow-blur");
        private static readonly CustomStyleProperty<float> SpreadProperty =
            new("--figma-shadow-spread");
        private static readonly CustomStyleProperty<float> RadiusProperty =
            new("--figma-shadow-radius");
        private static readonly CustomStyleProperty<float> InsetProperty =
            new("--figma-shadow-inset");

        private Color _color = new(0f, 0f, 0f, 0.25f);
        private Vector2 _offset = Vector2.zero;
        private float _blur;
        private float _spread;
        private float _radius;
        private bool _inset;

        public FigmaShadowElement()
        {
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(ResolveCustomStyles);
            generateVisualContent += DrawShadow;
        }

        internal Color ShadowColor => _color;
        internal Vector2 ShadowOffset => _offset;
        internal float BlurRadius => _blur;
        internal float Spread => _spread;
        internal float Radius => _radius;
        internal bool IsInset => _inset;

        private void ResolveCustomStyles(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(ColorProperty, out Color color))
                _color = color;
            if (evt.customStyle.TryGetValue(OffsetXProperty, out float offsetX))
                _offset.x = offsetX;
            if (evt.customStyle.TryGetValue(OffsetYProperty, out float offsetY))
                _offset.y = offsetY;
            if (evt.customStyle.TryGetValue(BlurProperty, out float blur))
                _blur = Mathf.Max(0f, blur);
            if (evt.customStyle.TryGetValue(SpreadProperty, out float spread))
                _spread = spread;
            if (evt.customStyle.TryGetValue(RadiusProperty, out float radius))
                _radius = Mathf.Max(0f, radius);
            if (evt.customStyle.TryGetValue(InsetProperty, out float inset))
                _inset = inset >= 0.5f;

            MarkDirtyRepaint();
        }

        private void DrawShadow(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 0f || bounds.height <= 0f || _color.a <= 0f)
                return;

            if (_inset)
            {
                DrawInsetShadow(context.painter2D, bounds);
                return;
            }

            Painter2D painter = context.painter2D;
            int passes = _blur <= 0.01f
                ? 1
                : Mathf.Clamp(Mathf.CeilToInt(_blur), 2, 16);

            // Paint outside-in. Multiple translucent shells approximate a soft
            // shadow while blur-zero Figma effects remain pixel-exact.
            for (int pass = passes - 1; pass >= 0; pass--)
            {
                float t = passes == 1 ? 0f : pass / (passes - 1f);
                float expansion = _spread + _blur * t;
                Rect shadowBounds = new(
                    bounds.xMin + _offset.x - expansion,
                    bounds.yMin + _offset.y - expansion,
                    bounds.width + expansion * 2f,
                    bounds.height + expansion * 2f);
                Color passColor = _color;
                passColor.a *= passes == 1 ? 1f : (1f - t) / passes * 2f;
                painter.fillColor = passColor;
                DrawRoundedRect(painter, shadowBounds, _radius + expansion);
                painter.Fill();
            }
        }

        private void DrawInsetShadow(Painter2D painter, Rect bounds)
        {
            int passes = _blur <= 0.01f
                ? 1
                : Mathf.Clamp(Mathf.CeilToInt(_blur), 2, 16);

            for (int pass = 0; pass < passes; pass++)
            {
                float t = passes == 1 ? 0f : pass / (passes - 1f);
                float inset = Mathf.Max(0f, _spread + _blur * t);
                Rect ringBounds = new(
                    bounds.xMin + inset + _offset.x,
                    bounds.yMin + inset + _offset.y,
                    Mathf.Max(0f, bounds.width - inset * 2f),
                    Mathf.Max(0f, bounds.height - inset * 2f));
                if (ringBounds.width <= 0f || ringBounds.height <= 0f)
                    continue;

                Color passColor = _color;
                passColor.a *= passes == 1
                    ? 1f
                    : (1f - t) / passes * 2f;
                painter.strokeColor = passColor;
                painter.lineWidth = Mathf.Max(1f, _blur / passes * 2f);
                DrawRoundedRect(
                    painter,
                    ringBounds,
                    Mathf.Max(0f, _radius - inset));
                painter.Stroke();
            }
        }

        internal static void DrawRoundedRect(
            Painter2D painter,
            Rect bounds,
            float radius)
        {
            float r = Mathf.Clamp(
                radius,
                0f,
                Mathf.Min(bounds.width, bounds.height) * 0.5f);

            painter.BeginPath();
            if (r <= 0.01f)
            {
                painter.MoveTo(new Vector2(bounds.xMin, bounds.yMin));
                painter.LineTo(new Vector2(bounds.xMax, bounds.yMin));
                painter.LineTo(new Vector2(bounds.xMax, bounds.yMax));
                painter.LineTo(new Vector2(bounds.xMin, bounds.yMax));
                painter.ClosePath();
                return;
            }

            painter.MoveTo(new Vector2(bounds.xMin + r, bounds.yMin));
            painter.LineTo(new Vector2(bounds.xMax - r, bounds.yMin));
            painter.ArcTo(
                new Vector2(bounds.xMax, bounds.yMin),
                new Vector2(bounds.xMax, bounds.yMin + r),
                r);
            painter.LineTo(new Vector2(bounds.xMax, bounds.yMax - r));
            painter.ArcTo(
                new Vector2(bounds.xMax, bounds.yMax),
                new Vector2(bounds.xMax - r, bounds.yMax),
                r);
            painter.LineTo(new Vector2(bounds.xMin + r, bounds.yMax));
            painter.ArcTo(
                new Vector2(bounds.xMin, bounds.yMax),
                new Vector2(bounds.xMin, bounds.yMax - r),
                r);
            painter.LineTo(new Vector2(bounds.xMin, bounds.yMin + r));
            painter.ArcTo(
                new Vector2(bounds.xMin, bounds.yMin),
                new Vector2(bounds.xMin + r, bounds.yMin),
                r);
            painter.ClosePath();
        }
    }
}
