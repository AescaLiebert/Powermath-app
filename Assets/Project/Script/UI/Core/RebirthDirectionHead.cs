using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    /// <summary>Scale-independent vector path for the Figma comparison arrow.</summary>
    [UxmlElement]
    public partial class RebirthDirectionHead : FigmaVectorPathElement
    {
        private static readonly CustomStyleProperty<Color> ColorProperty =
            new("--rebirth-arrow-color");

        private Color _color = new(33f / 255f, 94f / 255f, 194f / 255f);

        public RebirthDirectionHead()
        {
            RegisterCallback<CustomStyleResolvedEvent>(ResolveCustomStyles);
        }

        private void ResolveCustomStyles(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(ColorProperty, out Color color))
                _color = color;
            MarkDirtyRepaint();
        }

        protected override void DrawVectorPath(Painter2D painter, Rect bounds)
        {
            painter.fillColor = _color;
            painter.BeginPath();
            painter.MoveTo(Point(bounds, 0f, 0f));
            painter.LineTo(Point(bounds, 1f, 0.5f));
            painter.LineTo(Point(bounds, 0f, 1f));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
