using System;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class SimulationQuestionPresentation : IQuestionPresentation
    {
        private bool _cancelled;

        public void Begin(
            QuestionPresentationDescriptor question,
            Action<QuestionPresentationResult> completed)
        {
            if (completed == null)
            {
                throw new ArgumentNullException(nameof(completed));
            }

            _cancelled = false;
            if (!_cancelled)
            {
                completed(new QuestionPresentationResult(
                    QuestionPresentationStatus.Ready,
                    "DEVELOPMENT QUESTION - ENTER THE QA TARGET"
                ));
            }
        }

        public void Cancel()
        {
            _cancelled = true;
        }

        public void Dismiss()
        {
        }
    }
}
