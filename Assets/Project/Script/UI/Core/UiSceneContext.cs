using System;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    public sealed class UiSceneContext
    {
        public UiSceneContext(
            VisualElement root,
            IUiMotionDriver motionDriver,
            UiMotionProfileDefinition motionProfile)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            MotionDriver = motionDriver ??
                throw new ArgumentNullException(nameof(motionDriver));
            MotionProfile = motionProfile ??
                throw new ArgumentNullException(nameof(motionProfile));
        }

        public VisualElement Root { get; }
        public IUiMotionDriver MotionDriver { get; }
        public UiMotionProfileDefinition MotionProfile { get; }
    }
}
