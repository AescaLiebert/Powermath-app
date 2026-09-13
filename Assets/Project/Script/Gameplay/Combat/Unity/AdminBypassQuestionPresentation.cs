using System;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class AdminBypassQuestionPresentation : IQuestionPresentation
    {
        private readonly Func<bool> _isBypassActive;
        private readonly IQuestionPresentation _livePresentation;
        private readonly IQuestionPresentation _bypassPresentation;
        private IQuestionPresentation _activePresentation;

        public AdminBypassQuestionPresentation(
            Func<bool> isBypassActive,
            IQuestionPresentation livePresentation,
            IQuestionPresentation bypassPresentation = null)
        {
            _isBypassActive = isBypassActive ?? (() => false);
            _livePresentation = livePresentation ?? throw new ArgumentNullException(nameof(livePresentation));
            _bypassPresentation = bypassPresentation ?? new SimulationQuestionPresentation();
        }

        public void Begin(
            QuestionPresentationDescriptor question,
            Action<QuestionPresentationResult> completed)
        {
            if (completed == null)
            {
                throw new ArgumentNullException(nameof(completed));
            }

            if (_isBypassActive())
            {
                _activePresentation = _bypassPresentation;
                PowerMath.Diagnostics.AppLog.Info(
                    "Combat",
                    "Admin bypass active: Skipping YouTube video playback and presenting question directly.");
                _bypassPresentation.Begin(question, completed);
                return;
            }

            _activePresentation = _livePresentation;
            _livePresentation.Begin(question, completed);
        }

        public void Cancel()
        {
            _activePresentation?.Cancel();
            _livePresentation.Cancel();
            _bypassPresentation.Cancel();
            _activePresentation = null;
        }

        public void Dismiss()
        {
            _activePresentation?.Dismiss();
            _livePresentation.Dismiss();
            _bypassPresentation.Dismiss();
            _activePresentation = null;
        }
    }
}
