using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    [UxmlElement]
    public sealed partial class LeaderboardLockVector : FigmaVectorPathElement
    {
        protected override void DrawVectorPath(Painter2D painter, Rect bounds)
        {
            painter.fillColor = new Color32(11, 37, 69, 255);
            FigmaShadowElement.DrawRoundedRect(painter, new Rect(
                bounds.xMin + bounds.width * .15f,
                bounds.yMin + bounds.height * .425f,
                bounds.width * .7f,
                bounds.height * .475f), bounds.width * .125f);
            painter.Fill();

            painter.strokeColor = new Color32(11, 37, 69, 255);
            painter.lineWidth = bounds.width * .125f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(Point(bounds, .3f, .425f));
            painter.LineTo(Point(bounds, .3f, .3f));
            painter.BezierCurveTo(Point(bounds, .3f, .1f), Point(bounds, .7f, .1f), Point(bounds, .7f, .3f));
            painter.LineTo(Point(bounds, .7f, .425f));
            painter.Stroke();

            painter.fillColor = Color.white;
            float keyRadius = bounds.width * .075f;
            FigmaShadowElement.DrawRoundedRect(painter, new Rect(
                bounds.center.x - keyRadius,
                bounds.yMin + bounds.height * .65f - keyRadius,
                keyRadius * 2f,
                keyRadius * 2f), keyRadius);
            painter.Fill();
        }
    }

    [UxmlElement]
    public sealed partial class LeaderboardCloseVector : FigmaVectorPathElement
    {
        protected override void DrawVectorPath(Painter2D painter, Rect bounds)
        {
            painter.strokeColor = new Color32(11, 37, 69, 255);
            painter.lineWidth = Mathf.Min(bounds.width, bounds.height) * (10f / 72f);
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(Point(bounds, 17f / 72f, 17f / 72f));
            painter.LineTo(Point(bounds, 55f / 72f, 55f / 72f));
            painter.MoveTo(Point(bounds, 55f / 72f, 17f / 72f));
            painter.LineTo(Point(bounds, 17f / 72f, 55f / 72f));
            painter.Stroke();
        }
    }

    [UxmlElement]
    public sealed partial class LeaderboardPodiumVector : FigmaVectorPathElement
    {
        protected override void DrawVectorPath(Painter2D painter, Rect bounds)
        {
            DrawPolygon(painter, bounds, new Color32(210,236,252,255),
                new Vector2(.07143f,.42857f), new Vector2(.5f,.02857f), new Vector2(.92857f,.42857f), new Vector2(.5f,.82857f));
            DrawPolygon(painter, bounds, new Color32(111,179,232,255),
                new Vector2(.07143f,.42857f), new Vector2(.5f,.82857f), new Vector2(.5f,1f), new Vector2(.07143f,.6f));
            DrawPolygon(painter, bounds, new Color32(75,149,209,255),
                new Vector2(.5f,.82857f), new Vector2(.92857f,.42857f), new Vector2(.92857f,.6f), new Vector2(.5f,1f));
            DrawPolygon(painter, bounds, new Color32(232,246,255,255),
                new Vector2(.19643f,.34286f), new Vector2(.5f,.05714f), new Vector2(.80357f,.34286f), new Vector2(.5f,.62857f));
        }

        private static void DrawPolygon(Painter2D painter, Rect bounds, Color color, params Vector2[] points)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(Point(bounds, points[0].x, points[0].y));
            for (int index = 1; index < points.Length; index++)
                painter.LineTo(Point(bounds, points[index].x, points[index].y));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
