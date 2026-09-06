using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core.Tests
{
    public sealed class FigmaEffectElementTests
    {
        [Test]
        public void GradientElement_HasSafeNativeDefaults()
        {
            var element = new FigmaGradientElement();

            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(element.StopCount, Is.EqualTo(2));
            Assert.That(element.GradientStart, Is.EqualTo(new Vector2(0.5f, 1f)));
            Assert.That(element.GradientEnd, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(element.Evaluate(0f), Is.EqualTo(Color.black));
            Assert.That(element.Evaluate(1f), Is.EqualTo(Color.white));
        }

        [Test]
        public void ShadowElement_DoesNotCaptureInput()
        {
            var element = new FigmaShadowElement();

            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(element.ShadowOffset, Is.EqualTo(Vector2.zero));
            Assert.That(element.BlurRadius, Is.Zero);
            Assert.That(element.Spread, Is.Zero);
            Assert.That(element.IsInset, Is.False);
        }

        [Test]
        public void StageVector_UsesReusableVectorPathElement()
        {
            var element = new StageInfoVectorPath();

            Assert.That(element, Is.InstanceOf<FigmaVectorPathElement>());
            Assert.That(element.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }
    }
}
