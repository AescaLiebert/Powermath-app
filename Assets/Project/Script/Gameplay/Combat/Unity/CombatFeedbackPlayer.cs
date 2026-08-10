using System.Collections;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class CombatFeedbackPlayer
    {
        private readonly CombatLobbyView _view;
        private readonly CombatAudioPlayer _audio;
        private readonly bool _reducedMotion;

        public CombatFeedbackPlayer(
            CombatLobbyView view,
            CombatAudioPlayer audio,
            bool reducedMotion)
        {
            _view = view;
            _audio = audio;
            _reducedMotion = reducedMotion;
        }

        public IEnumerator Play(CombatResolution resolution)
        {
            _view.SetAnswerInputEnabled(false);

            if (resolution.TimedOut)
            {
                _view.SetResult("TIME EXPIRED - 0 DAMAGE", false);
                _audio.PlayTimeout();
                yield return new WaitForSecondsRealtime(0.6f);
            }
            else if (!resolution.IsCorrect)
            {
                _view.SetResult("INCORRECT - 0 DAMAGE", false);
                _audio.PlayTimeout();
                yield return new WaitForSecondsRealtime(0.6f);
            }
            else
            {
                _view.SetResult($"CORRECT - SCORE {resolution.ResponseScore}", true);
                _audio.PlaySuccess();
                yield return new WaitForSecondsRealtime(0.35f);

                _view.ShowDamage(resolution.FinalDamage, resolution.IsCritical);
                _view.SetEnemyHit(true, resolution.IsCritical);
                _audio.PlayHit(resolution.IsCritical);

                float hpDuration = _reducedMotion ? 0.12f : 0.25f;
                float elapsed = 0f;
                while (elapsed < hpDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / hpDuration);
                    int displayedHp = Mathf.RoundToInt(
                        Mathf.Lerp(
                            resolution.EnemyHpBefore,
                            resolution.EnemyHpAfter,
                            progress
                        )
                    );
                    _view.SetEnemyHp(displayedHp);
                    yield return null;
                }

                _view.SetEnemyHp(resolution.EnemyHpAfter);
                yield return new WaitForSecondsRealtime(
                    resolution.IsCritical ? 0.70f : 0.45f
                );
                _view.SetEnemyHit(false, false);
                _view.HideDamage();
            }

            if (resolution.EnemyDefeated)
            {
                _view.SetResult(
                    resolution.ResolvedStage.IsFinal
                        ? "FINAL ENEMY DEFEATED - RUN COMPLETE"
                        : $"STAGE {resolution.ResolvedStage.Value} CLEARED",
                    true
                );
                _audio.PlayDefeat();
                yield return new WaitForSecondsRealtime(0.75f);
            }
            else if (resolution.EnemyAttacked)
            {
                _view.SetResult("ENEMY COUNTERATTACK - LOST 1 HEART", false);
                yield return new WaitForSecondsRealtime(0.55f);
                _audio.PlayEnemyAttack();
                yield return new WaitForSecondsRealtime(0.35f);
            }

            if (resolution.PlayerDefeated)
            {
                _view.SetResult("RUN DEFEAT - RESET FLOW NOT IN THIS SLICE", false);
            }
        }
    }
}
