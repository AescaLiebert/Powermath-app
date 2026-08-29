using System.Collections;
using PowerMath.Gameplay.Combat.Presentation;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class EnemyActionQueuePresenter
    {
        private readonly EnemyActionQueueView _view;
        private readonly bool _reducedMotion;
        private string _armedPresentationId = string.Empty;

        public EnemyActionQueuePresenter(EnemyActionQueueView view, bool reducedMotion)
        {
            _view = view;
            _reducedMotion = reducedMotion;
        }

        public bool IsStable { get; private set; } = true;

        public void Synchronize(CombatSnapshot snapshot, bool animateInitiate)
        {
            IsStable = !animateInitiate;
            _armedPresentationId = string.Empty;
            _view.Rebuild(snapshot, animateInitiate);
            if (!animateInitiate) return;
            _view.PlayInitiate(_reducedMotion, () => IsStable = true);
        }

        public void ArmNext(string presentationId)
        {
            if (string.IsNullOrWhiteSpace(presentationId) || !IsStable) return;
            _armedPresentationId = presentationId;
            _view.ArmFirst();
        }

        public IEnumerator ConsumeArmed(
            string presentationId,
            EnemyActionTokenKind kind,
            bool cancelled)
        {
            if (!string.Equals(_armedPresentationId, presentationId,
                System.StringComparison.Ordinal) || !_view.HasArmedToken)
                yield break;

            IsStable = false;
            Vector2[] oldPositions = _view.CaptureSurvivorPositions();
            _view.BeginFirstExit(cancelled);
            yield return new WaitForSecondsRealtime(_reducedMotion ? 0.04f : 0.16f);
            _view.RemoveFirst();
            yield return null;
            _view.ApplyInverseReflow(oldPositions);
            yield return null;
            _view.CompleteReflow();
            if (!_reducedMotion)
                yield return new WaitForSecondsRealtime(0.14f);
            _armedPresentationId = string.Empty;
            IsStable = true;
        }

        public void CancelAndRebuild(CombatSnapshot snapshot)
        {
            Synchronize(snapshot, false);
        }
    }
}
