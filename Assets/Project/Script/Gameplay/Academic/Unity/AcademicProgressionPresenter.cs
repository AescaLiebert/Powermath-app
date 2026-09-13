using System;

namespace PowerMath.Gameplay.Academic.Unity
{
    public sealed class AcademicProgressionPresenter : IDisposable
    {
        private readonly AcademicProgressionView _view;
        private readonly Func<bool> _isAdminBypassActive;

        public AcademicProgressionPresenter(
            AcademicProgressionView view,
            Func<bool> isAdminBypassActive = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _isAdminBypassActive = isAdminBypassActive ?? (() => false);
        }

        public AcademicProgressionView View => _view;

        public void Initialize(AcademicProgressionProjection projection)
        {
            _view.Bind();
            _view.Render(projection);
            _view.ClearQuestion();
            _view.ClearCurrencyGain();
            _view.HideRankTransition();
        }

        public void Render(AcademicProgressionProjection projection)
        {
            _view.Render(projection);
        }

        public void ShowQuestion(QuestionPresentationDescriptor question)
        {
            _view.ShowQuestion(question, _isAdminBypassActive());
        }

        public void ShowAttemptOutcome(AcademicAttemptResult result)
        {
            _view.ShowCurrencyGain(result);
        }

        public void ClearAttemptPresentation()
        {
            _view.ClearQuestion();
            _view.ClearCurrencyGain();
        }

        public void Dispose()
        {
            _view.Dispose();
        }
    }
}
