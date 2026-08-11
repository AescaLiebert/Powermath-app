using System.Collections;
using PowerMath.Gameplay.Academic.Unity;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class AttemptFeedbackSequence
    {
        private readonly CombatFeedbackPlayer _combat;
        private readonly AcademicProgressionPresenter _academic;
        private readonly AcademicAudioPlayer _academicAudio;
        private readonly RankTransitionFeedbackPlayer _rankTransition;

        public AttemptFeedbackSequence(
            CombatFeedbackPlayer combat,
            AcademicProgressionPresenter academic,
            AcademicAudioPlayer academicAudio,
            RankTransitionFeedbackPlayer rankTransition)
        {
            _combat = combat;
            _academic = academic;
            _academicAudio = academicAudio;
            _rankTransition = rankTransition;
        }

        public IEnumerator Play(AttemptResolution resolution)
        {
            if (resolution.IsAcademic)
            {
                _academic.ShowAttemptOutcome(resolution.Academic);
                if (resolution.Academic.CurrencyDelta > 0)
                    _academicAudio.PlayCurrency();
            }

            yield return _combat.Play(resolution.Combat);
            if (resolution.IsAcademic)
                yield return _rankTransition.Play(resolution.Academic.RankTransition);
            _academic.Render(resolution.Snapshot.Academic);
        }

        public void Cancel()
        {
            _rankTransition.Cancel();
        }
    }
}
