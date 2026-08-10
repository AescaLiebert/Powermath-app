using System.Collections;

namespace PowerMath.Gameplay.Academic.Unity
{
    public sealed class RankTransitionFeedbackPlayer
    {
        private readonly AcademicProgressionView _view;
        private readonly AcademicAudioPlayer _audio;
        private bool _acknowledged;

        public RankTransitionFeedbackPlayer(
            AcademicProgressionView view,
            AcademicAudioPlayer audio)
        {
            _view = view;
            _audio = audio;
        }

        public IEnumerator Play(RankTransition transition)
        {
            if (!transition.Changed)
            {
                yield break;
            }

            _acknowledged = false;
            _view.ContinueRequested += OnContinue;
            _view.ShowRankTransition(transition);
            _audio.PlayTransition(transition);
            while (!_acknowledged)
            {
                yield return null;
            }

            _view.ContinueRequested -= OnContinue;
            _view.HideRankTransition();
        }

        public void Cancel()
        {
            _view.ContinueRequested -= OnContinue;
            _view.HideRankTransition();
            _acknowledged = true;
        }

        private void OnContinue()
        {
            _acknowledged = true;
        }
    }
}
