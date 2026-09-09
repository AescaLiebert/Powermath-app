using System.Collections;
using System.Globalization;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Combat.Presentation;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class CombatFeedbackPlayer
    {
        private readonly CombatLobbyView _view;
        private readonly CombatAudioPlayer _audio;
        private readonly bool _reducedMotion;
        private readonly FloatingCombatTextService _floatingText;
        private readonly ICombatAnchor _enemyDamageAnchor;
        private readonly CombatWorldImpulsePlayer _impactImpulse;
        private readonly ActorPresentationController _playerActor;
        private readonly ActorPresentationController _enemyActor;
        private readonly RewardMagnetFeedbackPlayer _rewardMagnet;
        private readonly MonoBehaviour _routineHost;

        public bool AreActorsStable =>
            (_playerActor == null || _playerActor.IsIdle ||
             _playerActor.State == ActorVisualState.Hidden) &&
            (_enemyActor == null || _enemyActor.IsIdle ||
             _enemyActor.State == ActorVisualState.Hidden);

        public CombatFeedbackPlayer(
            CombatLobbyView view,
            CombatAudioPlayer audio,
            bool reducedMotion,
            FloatingCombatTextService floatingText = null,
            ICombatAnchor enemyDamageAnchor = null,
            CombatWorldImpulsePlayer impactImpulse = null,
            ActorPresentationController playerActor = null,
            ActorPresentationController enemyActor = null,
            RewardMagnetFeedbackPlayer rewardMagnet = null,
            MonoBehaviour routineHost = null)
        {
            _view = view;
            _audio = audio;
            _reducedMotion = reducedMotion;
            _floatingText = floatingText;
            _enemyDamageAnchor = enemyDamageAnchor;
            _impactImpulse = impactImpulse;
            _playerActor = playerActor;
            _enemyActor = enemyActor;
            _rewardMagnet = rewardMagnet;
            _routineHost = routineHost;
        }

        public IEnumerator PlayAnswerFeedback(CombatResolution resolution)
        {
            _view.SetAnswerInputEnabled(false);

            if (resolution.TimedOut)
            {
                _view.ShowAnswerFeedback(
                    "!",
                    "TIME EXPIRED",
                    "The answer window closed.",
                    false);
                _view.AddFeedbackStep("DAMAGE", "0", true);
                _audio?.PlayTimeout();
                yield return new WaitForSecondsRealtime(
                    _reducedMotion ? 0.30f : 0.75f);
            }
            else if (!resolution.IsCorrect)
            {
                _view.ShowAnswerFeedback(
                    "×",
                    "INCORRECT",
                    "Review the question and try again.",
                    false);
                _view.AddFeedbackStep("DAMAGE", "0", true);
                _audio?.PlayTimeout();
                yield return new WaitForSecondsRealtime(
                    _reducedMotion ? 0.30f : 0.75f);
            }
            else
            {
                _view.ShowAnswerFeedback(
                    "OK",
                    "CORRECT",
                    "Building your attack power",
                    true);
                _audio?.PlaySuccess();
                yield return new WaitForSecondsRealtime(
                    _reducedMotion ? 0.10f : 0.25f);

                float rowDelay = _reducedMotion ? 0.05f : 0.16f;
                DamageBreakdown breakdown = resolution.DamageBreakdown;
                if (breakdown.IsAvailable)
                {
                    _view.AddFeedbackStep(
                        "BASE ATK",
                        breakdown.BaseAttack.ToString(CultureInfo.InvariantCulture),
                        false);
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        "RANK",
                        FormatMultiplier(breakdown.RankMultiplier),
                        false);
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        "BUFF",
                        FormatMultiplier(breakdown.BuffMultiplier),
                        false);
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        resolution.IsCritical ? "CRITICAL" : "NO CRITICAL",
                        FormatMultiplier(breakdown.CriticalMultiplier),
                        false);
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        $"RESPONSE SCORE {resolution.ResponseScore}",
                        FormatMultiplier(breakdown.ResponseMultiplier),
                        false);
                    yield return new WaitForSecondsRealtime(rowDelay);
                }
                else
                {
                    _view.AddFeedbackStep(
                        "RESPONSE SCORE",
                        resolution.ResponseScore.ToString(CultureInfo.InvariantCulture),
                        false);
                    yield return new WaitForSecondsRealtime(rowDelay);
                }

                _view.AddFeedbackStep(
                    "FINAL DAMAGE",
                    resolution.FinalDamage.ToString(CultureInfo.InvariantCulture),
                    true);
                yield return new WaitForSecondsRealtime(
                    _reducedMotion ? 0.18f : 0.38f);
            }

            // Preserve the complete result after its existing buildup. This is
            // informational timing only and never delays authoritative resolution.
            yield return new WaitForSecondsRealtime(1f);
        }

        public IEnumerator PlayBattleResolution(AttemptResolution attempt)
        {
            if (attempt == null) yield break;
            CombatResolution resolution = attempt.Combat;
            AttemptPresentationReceipt receipt = attempt.Presentation;
            _view.HideAnswerFeedback();
            _view.HideBattleBanner();

            if (resolution.IsCorrect)
            {
                _audio?.PlaySwing();
                if (_playerActor != null)
                    yield return _playerActor.Play(
                        PresentationActionKind.PlayerPrimaryAttack);
                bool componentFct = _floatingText != null &&
                    _floatingText.IsReady && _enemyDamageAnchor != null &&
                    receipt != null;
                if (componentFct)
                {
                    _floatingText.Spawn(new FloatingCombatTextRequest(
                        receipt.PresentationId,
                        FloatingCombatTextSemantic.Damage,
                        resolution.FinalDamage,
                        _enemyDamageAnchor,
                        resolution.IsCritical,
                        0));
                }
                else
                {
                    _view.ShowDamage(resolution.FinalDamage, resolution.IsCritical);
                }
                _view.SetEnemyHit(true, resolution.IsCritical);
                if (resolution.IsCritical) _impactImpulse?.PlayCritical();
                _audio?.PlayHit(resolution.IsCritical);
                if (_enemyActor != null)
                    yield return _enemyActor.Play(
                        PresentationActionKind.EnemyTakeDamage);

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
                if (!componentFct) _view.HideDamage();
            }
            else if (_playerActor != null)
            {
                yield return _playerActor.Play(
                    PresentationActionKind.PlayerFailedAttack);
            }

            if (resolution.EnemyDefeated)
            {
                IEnumerator rewardMagnetRoutine = null;
                if (_rewardMagnet != null && attempt.IsAcademic && attempt.Academic.CurrencyDelta > 0)
                {
                    RewardCurrencyKind kind = attempt.Academic.RankAtCommit.Tier switch
                    {
                        AcademicRankTier.Gold => RewardCurrencyKind.RankGold,
                        AcademicRankTier.Diamond => RewardCurrencyKind.RankDiamond,
                        _ => RewardCurrencyKind.RankSilver
                    };
                    long totalGrant = attempt.Academic.CurrencyDelta;
                    long startAmount = attempt.Snapshot.Academic.Balances.Get(attempt.Academic.RankAtCommit) - totalGrant;
                    if (startAmount < 0) startAmount = 0;

                    Vector2 enemyPos = Vector2.zero;
                    if (_enemyDamageAnchor != null && _enemyDamageAnchor.TryGetLocalPoint(null, null, out Vector2 anchorPt))
                    {
                        enemyPos = anchorPt;
                    }

                    rewardMagnetRoutine = _rewardMagnet.PlayRewardDropAndMagnet(
                        kind,
                        startAmount,
                        totalGrant,
                        enemyPos);
                }

                _audio?.PlayDeath(_enemyActor?.IsMajorDeath ?? false);
                yield return RunConcurrent(
                    receipt == null
                        ? null
                        : _view.ConsumeEnemyAction(
                            receipt.PresentationId,
                            ResolveToken(receipt),
                            true),
                    _enemyActor?.Play(PresentationActionKind.EnemyDie),
                    rewardMagnetRoutine);
            }
            else if (resolution.EnemyAttacked)
            {
                if (receipt != null)
                    yield return _view.ConsumeEnemyAction(
                        receipt.PresentationId,
                        ResolveToken(receipt),
                        false);
                yield return new WaitForSecondsRealtime(0.55f);
                if (_enemyActor != null)
                    yield return _enemyActor.Play(PresentationActionKind.EnemyAttack);
                _audio?.PlayEnemyAttack();
                yield return new WaitForSecondsRealtime(0.35f);
                if (_playerActor != null)
                    yield return _playerActor.Play(
                        PresentationActionKind.PlayerTakeDamage);
                if (!resolution.PlayerDefeated)
                    _view.InitiateEnemyActions(resolution.Snapshot);
            }
            else if (receipt != null)
            {
                yield return _view.ConsumeEnemyAction(
                    receipt.PresentationId,
                    ResolveToken(receipt),
                    false);
            }

            if (resolution.PlayerDefeated)
            {
                _audio?.PlayDeath(true);
                if (_playerActor != null)
                    yield return _playerActor.Play(PresentationActionKind.PlayerDie);
                _view.ShowBattleBanner(
                    "RUN DEFEAT - RESET FLOW NOT IN THIS SLICE",
                    false);
            }
            else
            {
                if (resolution.StageAdvanced)
                {
                    yield return PlayEncounterEntrance(
                        resolution.Snapshot,
                        resolution.BiomeChanged ||
                        _view.RequiresBackgroundTransition(resolution.Snapshot));
                }
                _view.HideBattleBanner();
            }
        }

        public IEnumerator PlayEncounterEntrance(
            CombatSnapshot destination,
            bool biomeChanged = false)
        {
            // Preparing the destination first replaces the old background and
            // makes its transition a no-op. Recovery deliberately omits this
            // cosmetic transition and renders the saved destination directly.
            if (biomeChanged)
            {
                _audio?.PlayBiomeTransition();
                yield return _view.PlayBiomeTransition(destination);
            }
            _view.PrepareEncounterPresentation(destination);
            _view.InitiateEnemyActions(destination);
            if (_enemyActor != null)
                yield return _enemyActor.Play(PresentationActionKind.EnemyAppear);
        }

        public IEnumerator PlayRecoveredResolution(AttemptPresentationReceipt receipt)
        {
            if (receipt == null) yield break;
            _view.HideBattleBanner();
            _view.RebuildRecoveredEnemyActions(receipt);
            bool correct = receipt.Outcome == AttemptOutcomeKind.Correct;
            _view.ShowAnswerFeedback(
                correct ? "OK" : receipt.Outcome == AttemptOutcomeKind.Timeout ? "!" : "×",
                correct ? "CORRECT" : receipt.Outcome == AttemptOutcomeKind.Timeout
                    ? "TIME EXPIRED" : "INCORRECT",
                "Recovering the accepted combat result.",
                correct);
            yield return new WaitForSecondsRealtime(_reducedMotion ? 0.12f : 0.32f);
            _view.HideAnswerFeedback();

            if (correct)
            {
                if (_playerActor != null)
                    yield return _playerActor.Play(PresentationActionKind.PlayerPrimaryAttack);
                _floatingText?.Spawn(new FloatingCombatTextRequest(
                    receipt.PresentationId,
                    FloatingCombatTextSemantic.Damage,
                    receipt.FinalDamage,
                    _enemyDamageAnchor,
                    receipt.IsCritical,
                    0));
                if (receipt.IsCritical) _impactImpulse?.PlayCritical();
                _audio?.PlayHit(receipt.IsCritical);
                if (_enemyActor != null)
                    yield return _enemyActor.Play(PresentationActionKind.EnemyTakeDamage);
                _view.SetEnemyHp(receipt.ResolvedEnemyHpAfter);
            }
            else if (_playerActor != null)
            {
                yield return _playerActor.Play(PresentationActionKind.PlayerFailedAttack);
            }

            EnemyActionTokenKind token = ResolveToken(receipt);
            if (receipt.EnemyDefeated)
            {
                _audio?.PlayDeath(_enemyActor?.IsMajorDeath ?? false);
                yield return RunConcurrent(
                    _view.ConsumeEnemyAction(
                        receipt.PresentationId, token, true),
                    _enemyActor?.Play(PresentationActionKind.EnemyDie));
                if (receipt.StageAdvanced)
                {
                    CombatSnapshot destination = ToCombatSnapshot(receipt.Destination);
                    yield return PlayEncounterEntrance(destination);
                }
            }
            else if (receipt.EnemyAttacked)
            {
                yield return _view.ConsumeEnemyAction(
                    receipt.PresentationId, token, false);
                if (_enemyActor != null)
                    yield return _enemyActor.Play(PresentationActionKind.EnemyAttack);
                _audio?.PlayEnemyAttack();
                if (_playerActor != null)
                    yield return _playerActor.Play(PresentationActionKind.PlayerTakeDamage);
                if (receipt.PlayerDefeated)
                {
                    _audio?.PlayDeath(true);
                    if (_playerActor != null)
                        yield return _playerActor.Play(PresentationActionKind.PlayerDie);
                }
                else
                {
                    _view.InitiateEnemyActions(ToCombatSnapshot(receipt.Destination));
                }
            }
            else
            {
                yield return _view.ConsumeEnemyAction(
                    receipt.PresentationId, token, false);
            }
        }

        private IEnumerator RunConcurrent(params IEnumerator[] routines)
        {
            if (_routineHost == null)
            {
                foreach (IEnumerator routine in routines)
                {
                    if (routine != null) yield return routine;
                }
                yield break;
            }

            int pending = 0;
            foreach (IEnumerator routine in routines)
            {
                if (routine == null) continue;
                pending++;
                _routineHost.StartCoroutine(RunTracked(
                    routine,
                    () => pending--));
            }

            while (pending > 0) yield return null;
        }

        private static IEnumerator RunTracked(
            IEnumerator routine,
            System.Action completed)
        {
            yield return routine;
            completed?.Invoke();
        }

        private static CombatSnapshot ToCombatSnapshot(
            CombatPresentationSnapshot value)
        {
            return new CombatSnapshot(
                value.Stage,
                value.EncounterId,
                value.EncounterId,
                value.EnemyCurrentHp,
                value.EnemyMaximumHp,
                value.EnemyRemainingCooldown,
                value.EnemyMaximumCooldown,
                value.PlayerCurrentHearts,
                value.PlayerMaximumHearts,
                value.Phase,
                false,
                value.BiomeId,
                value.BiomeId,
                value.EncounterKind,
                string.Empty,
                0);
        }

        private static EnemyActionTokenKind ResolveToken(
            AttemptPresentationReceipt receipt)
        {
            if (receipt.Source.EncounterKind == StageEncounterKind.ChallengeEvent)
                return EnemyActionTokenKind.EventRisk;
            return receipt.EnemyAttacked
                ? EnemyActionTokenKind.Attack
                : EnemyActionTokenKind.Walk;
        }

        private static string FormatMultiplier(double value)
        {
            return "×" + value.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
