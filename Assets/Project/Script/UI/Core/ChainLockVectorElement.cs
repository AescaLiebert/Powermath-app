using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    /// <summary>
    /// Scale-independent vector element drawing heavy crossed chains and a central padlock.
    /// Used for Version 1.0 feature locks on Biome Map, PlayerHub, and Pet Gacha.
    /// </summary>
    [UxmlElement]
    public sealed partial class ChainLockVectorElement : FigmaVectorPathElement
    {
        private static readonly Color ChainDark = new Color32(25, 32, 44, 240);
        private static readonly Color ChainSteel = new Color32(150, 165, 185, 255);
        private static readonly Color ChainLight = new Color32(215, 228, 242, 255);

        private static readonly Color ShackleDark = new Color32(30, 38, 50, 255);
        private static readonly Color ShackleChrome = new Color32(205, 220, 238, 255);
        private static readonly Color ShackleHighlight = new Color32(245, 250, 255, 255);

        private static readonly Color LockBodyBorder = new Color32(40, 28, 10, 255);
        private static readonly Color LockBodyGold = new Color32(218, 160, 32, 255);
        private static readonly Color LockBodyLight = new Color32(250, 205, 75, 255);
        private static readonly Color LockBodyShadow = new Color32(160, 108, 16, 255);
        private static readonly Color KeyholeColor = new Color32(18, 24, 34, 255);

        public ChainLockVectorElement()
        {
            pickingMode = PickingMode.Ignore;
        }

        protected override void DrawVectorPath(Painter2D painter, Rect bounds)
        {
            float minDim = Mathf.Min(bounds.width, bounds.height);
            if (minDim <= 4f) return;

            // 1. Draw diagonal chains crossing behind the lock
            DrawChainDiagonal(painter, bounds, true, minDim);
            DrawChainDiagonal(painter, bounds, false, minDim);

            // 2. Draw central padlock
            DrawCentralPadlock(painter, bounds, minDim);
        }

        private static void DrawChainDiagonal(Painter2D painter, Rect bounds, bool mainDiagonal, float minDim)
        {
            Vector2 start = mainDiagonal
                ? new Vector2(bounds.xMin, bounds.yMin)
                : new Vector2(bounds.xMax, bounds.yMin);
            Vector2 end = mainDiagonal
                ? new Vector2(bounds.xMax, bounds.yMax)
                : new Vector2(bounds.xMin, bounds.yMax);

            int linkCount = 7;
            float linkLength = minDim * 0.18f;
            float linkThickness = minDim * 0.08f;
            float strokeWidth = minDim * 0.024f;

            for (int i = 0; i < linkCount; i++)
            {
                float t = (i + 0.5f) / linkCount;
                Vector2 center = Vector2.Lerp(start, end, t);

                // Alternating flat and side-profile chain links
                bool isFlat = (i % 2 == 0);
                float w = isFlat ? linkLength : linkLength * 0.45f;
                float h = isFlat ? linkThickness : linkThickness * 1.3f;
                float radius = Mathf.Min(w, h) * 0.45f;

                Rect linkRect = new Rect(center.x - w * 0.5f, center.y - h * 0.5f, w, h);

                // Dark outer outline
                painter.strokeColor = ChainDark;
                painter.lineWidth = strokeWidth * 1.8f;
                FigmaShadowElement.DrawRoundedRect(painter, linkRect, radius);
                painter.Stroke();

                // Steel inner ring
                painter.strokeColor = ChainSteel;
                painter.lineWidth = strokeWidth;
                FigmaShadowElement.DrawRoundedRect(painter, linkRect, radius);
                painter.Stroke();

                // Highlight accent
                if (isFlat)
                {
                    Rect innerHighlight = new Rect(
                        linkRect.xMin + strokeWidth,
                        linkRect.yMin + strokeWidth * 0.5f,
                        linkRect.width - strokeWidth * 2f,
                        linkRect.height * 0.35f);
                    painter.fillColor = ChainLight;
                    FigmaShadowElement.DrawRoundedRect(painter, innerHighlight, radius * 0.5f);
                    painter.Fill();
                }
            }
        }

        private static void DrawCentralPadlock(Painter2D painter, Rect bounds, float minDim)
        {
            Vector2 center = bounds.center;
            float lockSize = minDim * 0.48f;
            float bodyWidth = lockSize * 0.88f;
            float bodyHeight = lockSize * 0.68f;
            float bodyRadius = lockSize * 0.16f;

            float bodyTop = center.y - bodyHeight * 0.25f;
            Rect bodyRect = new Rect(
                center.x - bodyWidth * 0.5f,
                bodyTop,
                bodyWidth,
                bodyHeight);

            // Shackle (inverted U)
            float shackleOuterRadius = bodyWidth * 0.30f;
            float shackleWidth = lockSize * 0.14f;
            float shackleHeight = lockSize * 0.42f;
            float shackleTop = bodyTop - shackleHeight * 0.82f;

            // Outer shackle stroke (dark base)
            painter.lineCap = LineCap.Round;
            painter.strokeColor = ShackleDark;
            painter.lineWidth = shackleWidth + minDim * 0.024f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - shackleOuterRadius, bodyTop + lockSize * 0.05f));
            painter.LineTo(new Vector2(center.x - shackleOuterRadius, shackleTop + shackleOuterRadius));
            painter.Arc(
                new Vector2(center.x, shackleTop + shackleOuterRadius),
                shackleOuterRadius,
                180f,
                0f);
            painter.LineTo(new Vector2(center.x + shackleOuterRadius, bodyTop + lockSize * 0.05f));
            painter.Stroke();

            // Inner shackle stroke (chrome)
            painter.strokeColor = ShackleChrome;
            painter.lineWidth = shackleWidth;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - shackleOuterRadius, bodyTop + lockSize * 0.05f));
            painter.LineTo(new Vector2(center.x - shackleOuterRadius, shackleTop + shackleOuterRadius));
            painter.Arc(
                new Vector2(center.x, shackleTop + shackleOuterRadius),
                shackleOuterRadius,
                180f,
                0f);
            painter.LineTo(new Vector2(center.x + shackleOuterRadius, bodyTop + lockSize * 0.05f));
            painter.Stroke();

            // Shackle highlight gleam
            painter.strokeColor = ShackleHighlight;
            painter.lineWidth = shackleWidth * 0.35f;
            painter.BeginPath();
            painter.Arc(
                new Vector2(center.x, shackleTop + shackleOuterRadius),
                shackleOuterRadius - shackleWidth * 0.15f,
                160f,
                70f);
            painter.Stroke();

            // Padlock Body - Outer Dark Border
            painter.fillColor = LockBodyBorder;
            Rect borderRect = new Rect(
                bodyRect.xMin - minDim * 0.016f,
                bodyRect.yMin - minDim * 0.016f,
                bodyRect.width + minDim * 0.032f,
                bodyRect.height + minDim * 0.032f);
            FigmaShadowElement.DrawRoundedRect(painter, borderRect, bodyRadius + minDim * 0.016f);
            painter.Fill();

            // Padlock Body - Base Gold
            painter.fillColor = LockBodyGold;
            FigmaShadowElement.DrawRoundedRect(painter, bodyRect, bodyRadius);
            painter.Fill();

            // Upper metallic highlight
            Rect topHighlight = new Rect(
                bodyRect.xMin + bodyWidth * 0.08f,
                bodyRect.yMin + bodyHeight * 0.08f,
                bodyWidth * 0.84f,
                bodyHeight * 0.38f);
            painter.fillColor = LockBodyLight;
            FigmaShadowElement.DrawRoundedRect(painter, topHighlight, bodyRadius * 0.6f);
            painter.Fill();

            // Lower bevel shadow
            Rect bottomShadow = new Rect(
                bodyRect.xMin + bodyWidth * 0.08f,
                bodyRect.yMax - bodyHeight * 0.28f,
                bodyWidth * 0.84f,
                bodyHeight * 0.20f);
            painter.fillColor = LockBodyShadow;
            FigmaShadowElement.DrawRoundedRect(painter, bottomShadow, bodyRadius * 0.4f);
            painter.Fill();

            // Keyhole
            float keyholeCenterY = bodyRect.center.y;
            float circleRadius = lockSize * 0.055f;

            // Keyhole round head
            painter.fillColor = KeyholeColor;
            painter.BeginPath();
            painter.Arc(new Vector2(center.x, keyholeCenterY - circleRadius * 0.4f), circleRadius, 0f, 360f);
            painter.Fill();

            // Keyhole bottom slot
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - circleRadius * 0.7f, keyholeCenterY - circleRadius * 0.2f));
            painter.LineTo(new Vector2(center.x + circleRadius * 0.7f, keyholeCenterY - circleRadius * 0.2f));
            painter.LineTo(new Vector2(center.x + circleRadius * 0.4f, keyholeCenterY + circleRadius * 1.8f));
            painter.LineTo(new Vector2(center.x - circleRadius * 0.4f, keyholeCenterY + circleRadius * 1.8f));
            painter.ClosePath();
            painter.Fill();

            // Small corner rivets on lock face
            float rivetOffset = bodyRadius * 0.7f;
            float rivetRadius = minDim * 0.014f;
            painter.fillColor = LockBodyBorder;
            DrawCircle(painter, bodyRect.xMin + rivetOffset, bodyRect.yMin + rivetOffset, rivetRadius);
            DrawCircle(painter, bodyRect.xMax - rivetOffset, bodyRect.yMin + rivetOffset, rivetRadius);
            DrawCircle(painter, bodyRect.xMin + rivetOffset, bodyRect.yMax - rivetOffset, rivetRadius);
            DrawCircle(painter, bodyRect.xMax - rivetOffset, bodyRect.yMax - rivetOffset, rivetRadius);
        }

        private static void DrawCircle(Painter2D painter, float cx, float cy, float radius)
        {
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), radius, 0f, 360f);
            painter.Fill();
        }
    }
}
