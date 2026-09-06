using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    /// <summary>Vector layer used by the Figma Stage Info heading.</summary>
    [UxmlElement]
    public sealed partial class StageInfoVectorPath : FigmaVectorPathElement
    {
        protected override void DrawVectorPath(Painter2D painter, Rect bounds)
        {
            painter.fillColor = resolvedStyle.color;
            painter.BeginPath();
            // Exact Figma fillGeometry for nodes 42:240 and 42:243.
            painter.MoveTo(Point(bounds, 0.5f, 0f));
            painter.LineTo(Point(bounds, 13.57143f / 19f, 5.42857f / 19f));
            painter.LineTo(Point(bounds, 1f, 0.5f));
            painter.LineTo(Point(bounds, 13.57143f / 19f, 13.57143f / 19f));
            painter.LineTo(Point(bounds, 0.5f, 1f));
            painter.LineTo(Point(bounds, 5.42857f / 19f, 13.57143f / 19f));
            painter.LineTo(Point(bounds, 0f, 0.5f));
            painter.LineTo(Point(bounds, 5.42857f / 19f, 5.42857f / 19f));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
