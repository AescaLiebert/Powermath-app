using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    /// <summary>
    /// Base for scale-independent Figma vector paths. Derived controls describe
    /// their geometry in normalized coordinates and preserve it at any layout size.
    /// </summary>
    public abstract class FigmaVectorPathElement : VisualElement
    {
        protected FigmaVectorPathElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += DrawVector;
        }

        protected abstract void DrawVectorPath(Painter2D painter, Rect bounds);

        protected static Vector2 Point(Rect bounds, float x, float y)
        {
            return new Vector2(
                bounds.xMin + bounds.width * x,
                bounds.yMin + bounds.height * y);
        }

        private void DrawVector(MeshGenerationContext context)
        {
            Rect bounds = contentRect;
            if (bounds.width <= 0f || bounds.height <= 0f)
                return;

            DrawVectorPath(context.painter2D, bounds);
        }
    }
}
