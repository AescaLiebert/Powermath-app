using System;
using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Combat;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class AdminBypassQuestionPresentationTests
    {
        private sealed class MockPresentation : IQuestionPresentation
        {
            public int BeginCount { get; private set; }
            public int CancelCount { get; private set; }
            public int DismissCount { get; private set; }

            public void Begin(QuestionPresentationDescriptor question, Action<QuestionPresentationResult> completed)
            {
                BeginCount++;
                completed?.Invoke(new QuestionPresentationResult(QuestionPresentationStatus.Ready, "MOCK READY"));
            }

            public void Cancel() => CancelCount++;
            public void Dismiss() => DismissCount++;
        }

        private static QuestionPresentationDescriptor CreateDescriptor()
        {
            return new QuestionPresentationDescriptor(
                new QuestionId(1),
                AcademicRank.Silver,
                new Uri("https://example.com/video"),
                "Prompt",
                "video-123");
        }

        [Test]
        public void Begin_WhenBypassInactive_DelegatesToLivePresentation()
        {
            var live = new MockPresentation();
            var bypass = new MockPresentation();
            var adaptive = new AdminBypassQuestionPresentation(() => false, live, bypass);

            QuestionPresentationResult? result = null;
            adaptive.Begin(CreateDescriptor(), r => result = r);

            Assert.That(live.BeginCount, Is.EqualTo(1));
            Assert.That(bypass.BeginCount, Is.Zero);
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.IsReady, Is.True);
        }

        [Test]
        public void Begin_WhenBypassActive_DelegatesToBypassPresentation()
        {
            var live = new MockPresentation();
            var bypass = new MockPresentation();
            var adaptive = new AdminBypassQuestionPresentation(() => true, live, bypass);

            QuestionPresentationResult? result = null;
            adaptive.Begin(CreateDescriptor(), r => result = r);

            Assert.That(live.BeginCount, Is.Zero);
            Assert.That(bypass.BeginCount, Is.EqualTo(1));
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value.IsReady, Is.True);
        }

        [Test]
        public void Cancel_CancelsBothPresentations()
        {
            var live = new MockPresentation();
            var bypass = new MockPresentation();
            var adaptive = new AdminBypassQuestionPresentation(() => true, live, bypass);

            adaptive.Cancel();

            Assert.That(live.CancelCount, Is.EqualTo(1));
            Assert.That(bypass.CancelCount, Is.EqualTo(1));
        }
    }
}
