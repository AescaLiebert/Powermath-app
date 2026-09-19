using System;
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
        private readonly CombatImpactBurstPlayer _impactBurst;
        private readonly CombatJuiceProfileDefinition _juiceProfile;
        private readonly ActorPresentationController _playerActor;
        private readonly ActorPresentationController _enemyActor;
        private readonly ActorPresentationController _petActor;
        private readonly RewardMagnetFeedbackPlayer _rewardMagnet;
        private readonly MonoBehaviour _routineHost;
        private readonly FloatingRewardTextService _floatingRewardText;

        // Per-monster accumulated rank currency — reset each time a new encounter begins.
        // Allows the death-burst FRT to show the true total earned across all correct hits.
        private long _accumulatedSilver;
        private long _accumulatedGold;
        private long _accumulatedDiamond;
        private string _lastEncounterId = string.Empty;

        public bool AreActorsStable =>
            (_playerActor == null || _playerActor.IsIdle ||
             _playerActor.State == ActorVisualState.Hidden) &&
            (_enemyActor == null || _enemyActor.IsIdle ||
             _enemyActor.State == ActorVisualState.Hidden) &&
            (_petActor == null || _petActor.IsIdle ||
             _petActor.State == ActorVisualState.Hidden);

        public CombatFeedbackPlayer(
            CombatLobbyView view,
            CombatAudioPlayer audio,
            bool reducedMotion,
            FloatingCombatTextService floatingText = null,
            ICombatAnchor enemyDamageAnchor = null,
            CombatWorldImpulsePlayer impactImpulse = null,
            ActorPresentationController playerActor = null,
            ActorPresentationController enemyActor = null,
            ActorPresentationController petActor = null,
            RewardMagnetFeedbackPlayer rewardMagnet = null,
            MonoBehaviour routineHost = null,
            FloatingRewardTextService floatingRewardText = null,
            CombatImpactBurstPlayer impactBurst = null,
            CombatJuiceProfileDefinition juiceProfile = null)
        {
            _view = view;
            _audio = audio;
            _reducedMotion = reducedMotion;
            _floatingText = floatingText;
            _enemyDamageAnchor = enemyDamageAnchor;
            _impactImpulse = impactImpulse;
            _playerActor = playerActor;
            _enemyActor = enemyActor;
            _petActor = petActor;
            _rewardMagnet = rewardMagnet;
            _routineHost = routineHost;
            _floatingRewardText = floatingRewardText;
            _impactBurst = impactBurst;
            _juiceProfile = juiceProfile;
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
                        breakdown.EffectiveAttack.ToString(CultureInfo.InvariantCulture),
                        false);
                    _audio?.PlayDamageMultiplying();
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        "RANK",
                        FormatMultiplier(breakdown.RankMultiplier),
                        false);
                    _audio?.PlayDamageMultiplying();
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        "BUFF",
                        FormatMultiplier(breakdown.BuffMultiplier),
                        false);
                    _audio?.PlayDamageMultiplying();
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        resolution.IsCritical ? "CRITICAL" : "NO CRITICAL",
                        FormatMultiplier(breakdown.CriticalMultiplier),
                        false);
                    _audio?.PlayDamageMultiplying();
                    yield return new WaitForSecondsRealtime(rowDelay);

                    _view.AddFeedbackStep(
                        $"RESPONSE SCORE {resolution.ResponseScore}",
                        FormatMultiplier(breakdown.ResponseMultiplier),
                        false);
                    _audio?.PlayDamageMultiplying();
                    yield return new WaitForSecondsRealtime(rowDelay);
                }
                else
                {
                    _view.AddFeedbackStep(
                        "RESPONSE SCORE",
                        resolution.ResponseScore.ToString(CultureInfo.InvariantCulture),
                        false);
                    _audio?.PlayDamageMultiplying();
                    yield return new WaitForSecondsRealtime(rowDelay);
                }

                _view.AddFeedbackStep(
                    "FINAL DAMAGE",
                    resolution.FinalDamage.ToString(CultureInfo.InvariantCulture),
                    true);
                _audio?.PlayDamageMultiplying();
                yield return new WaitForSecondsRealtime(
                    _reducedMotion ? 0.18f : 0.38f);
            }

            // Preserve the complete result after its existing buildup. This is
            // informational timing only and never delays authoritative resolution.
            yield return new WaitForSecondsRealtime(0.5f);
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
                yield return PlayPlayerAttackAndEnemyReaction(
                    receipt?.PresentationId,
                    resolution.PlayerDamage,
                    resolution.IsCritical,
                    resolution.EnemyHpBefore,
                    resolution.PlayerEnemyHpAfter);
            }
            else if (_playerActor != null)
            {
                yield return _playerActor.Play(
                    PresentationActionKind.PlayerFailedAttack);
            }

            IEnumerator rewardMagnetRoutine = CreateRewardMagnetRoutine(attempt);
            bool playerDefeatedSource = resolution.IsCorrect &&
                resolution.PlayerEnemyHpAfter == 0;
            bool petDefeatedTarget = resolution.PetFollowUp?.EnemyDefeated == true;

            if (playerDefeatedSource)
            {
                yield return PlayEnemyDefeat(receipt, rewardMagnetRoutine);

                if (resolution.PetFollowUp != null && resolution.PetFollowUp.Carried)
                {
                    CombatSnapshot petTarget = resolution.PetFollowUp.Target;
                    yield return PlayEncounterEntrance(
                        petTarget,
                        !string.Equals(
                            petTarget.BiomeId,
                            receipt?.Source?.BiomeId,
                            StringComparison.Ordinal) ||
                        _view.RequiresBackgroundTransition(petTarget));
                    yield return PlayPetFollowUpAndEnemyReaction(
                        receipt?.PresentationId,
                        resolution.PetFollowUp);
                    if (petDefeatedTarget)
                    {
                        yield return PlayEnemyDefeat(null, null);
                        if (resolution.PetFollowUp.StageAdvanced)
                            yield return PlayEncounterEntrance(
                                resolution.Snapshot,
                                !string.Equals(
                                    resolution.Snapshot.BiomeId,
                                    petTarget.BiomeId,
                                    StringComparison.Ordinal) ||
                                _view.RequiresBackgroundTransition(resolution.Snapshot));
                    }
                    else
                    {
                        _view.InitiateEnemyActions(resolution.Snapshot);
                    }
                }
                else if (resolution.StageAdvanced)
                {
                    yield return PlayEncounterEntrance(
                        resolution.Snapshot,
                        resolution.BiomeChanged ||
                        _view.RequiresBackgroundTransition(resolution.Snapshot));
                }
            }
            else if (resolution.PetFollowUp != null)
            {
                yield return PlayPetFollowUpAndEnemyReaction(
                    receipt?.PresentationId,
                    resolution.PetFollowUp);
                if (petDefeatedTarget)
                {
                    yield return PlayEnemyDefeat(receipt, rewardMagnetRoutine);
                    if (resolution.PetFollowUp.StageAdvanced)
                        yield return PlayEncounterEntrance(
                            resolution.Snapshot,
                            resolution.BiomeChanged ||
                            _view.RequiresBackgroundTransition(resolution.Snapshot));
                }
                else if (resolution.EnemyAttacked)
                {
                    if (receipt != null)
                        yield return _view.ConsumeEnemyAction(
                            receipt.PresentationId,
                            ResolveToken(receipt),
                            false);
                    yield return PlayEnemyAttackAndPlayerReaction(
                        resolution.Snapshot.PlayerCurrentHearts,
                        resolution.Snapshot.PlayerMaximumHearts);
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
            }
            else if (resolution.EnemyFled)
            {
                yield return RunConcurrent(
                    receipt == null
                        ? null
                        : _view.ConsumeEnemyAction(
                            receipt.PresentationId,
                            EnemyActionTokenKind.Flee,
                            false),
                    _enemyActor?.Play(PresentationActionKind.EnemyFlee),
                    rewardMagnetRoutine);
                if (resolution.StageAdvanced)
                {
                    yield return PlayEncounterEntrance(
                        resolution.Snapshot,
                        resolution.BiomeChanged ||
                        _view.RequiresBackgroundTransition(resolution.Snapshot));
                }
            }
            else if (resolution.EnemyAttacked)
            {
                if (receipt != null)
                    yield return _view.ConsumeEnemyAction(
                        receipt.PresentationId,
                        ResolveToken(receipt),
                        false);
                yield return PlayEnemyAttackAndPlayerReaction(
                    resolution.Snapshot.PlayerCurrentHearts,
                    resolution.Snapshot.PlayerMaximumHearts);
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
                _view.HideBattleBanner();
            }
        }

        private IEnumerator PlayEnemyDefeat(
            AttemptPresentationReceipt receipt,
            IEnumerator rewardRoutine)
        {
            bool major = (_enemyActor?.IsMajorDeath ?? false) ||
                (PowerMath.Audio.MusicController.Instance != null &&
                 (PowerMath.Audio.MusicController.Instance.IsBossActive ||
                  PowerMath.Audio.MusicController.Instance.IsEncounterOverrideActive));
            if (major) _audio?.FadeOutBossMusic(1.2f);
            _audio?.PlayDeath(major);
            yield return RunConcurrent(
                receipt == null
                    ? null
                    : _view.ConsumeEnemyAction(
                        receipt.PresentationId,
                        ResolveToken(receipt),
                        true),
                _enemyActor?.Play(PresentationActionKind.EnemyDie),
                rewardRoutine);
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
            if (IsBossEncounter(destination))
            {
                _audio?.PlayBossWarning();
                yield return _view.PlayBossWarning();
            }
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
                yield return PlayPlayerAttackAndEnemyReaction(
                    receipt.PresentationId,
                    receipt.PlayerDamage,
                    receipt.IsCritical,
                    receipt.Source.EnemyCurrentHp,
                    receipt.PlayerEnemyHpAfter);
            }
            else if (_playerActor != null)
            {
                yield return _playerActor.Play(PresentationActionKind.PlayerFailedAttack);
            }

            EnemyActionTokenKind token = ResolveToken(receipt);
            bool playerDefeatedSource = correct && receipt.PlayerEnemyHpAfter == 0;
            if (receipt.PetFollowUp != null && !receipt.PetFollowUp.Carried)
            {
                yield return PlayPetFollowUpAndEnemyReaction(
                    receipt.PresentationId,
                    ToPetResolution(receipt.PetFollowUp));
            }
            if (receipt.EnemyDefeated || receipt.EnemyFled)
            {
                if (receipt.EnemyDefeated)
                    _audio?.PlayDeath(_enemyActor?.IsMajorDeath ?? false);
                yield return RunConcurrent(
                    _view.ConsumeEnemyAction(
                        receipt.PresentationId, token, receipt.EnemyDefeated),
                    _enemyActor?.Play(receipt.EnemyFled
                        ? PresentationActionKind.EnemyFlee
                        : PresentationActionKind.EnemyDie));
                if (receipt.StageAdvanced)
                {
                    if (playerDefeatedSource && receipt.PetFollowUp?.Carried == true)
                    {
                        CombatSnapshot petTarget = ToCombatSnapshot(
                            receipt.PetFollowUp.Target);
                        _view.SkipBossWarning();
                        _view.PrepareEncounterPresentation(petTarget);
                        _view.InitiateEnemyActions(petTarget);
                        _enemyActor?.CancelAndApply(ActorVisualState.Idle);
                        yield return PlayPetFollowUpAndEnemyReaction(
                            receipt.PresentationId,
                            ToPetResolution(receipt.PetFollowUp));
                        if (receipt.PetFollowUp.EnemyDefeated)
                        {
                            _audio?.PlayDeath(_enemyActor?.IsMajorDeath ?? false);
                            if (_enemyActor != null)
                                yield return _enemyActor.Play(
                                    PresentationActionKind.EnemyDie);
                        }
                    }
                    CombatSnapshot destination = ToCombatSnapshot(receipt.Destination);
                    // Recovery is authoritative state restoration, not a replay.
                    // Bind directly to the saved encounter and leave the actor idle.
                    _view.SkipBossWarning();
                    _view.PrepareEncounterPresentation(destination);
                    _view.InitiateEnemyActions(destination);
                    _enemyActor?.CancelAndApply(ActorVisualState.Idle);
                }
            }
            else if (receipt.EnemyAttacked)
            {
                yield return _view.ConsumeEnemyAction(
                    receipt.PresentationId, token, false);
                yield return PlayEnemyAttackAndPlayerReaction(
                    receipt.Destination.PlayerCurrentHearts,
                    receipt.Destination.PlayerMaximumHearts);
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

        private static PetFollowUpResolution ToPetResolution(
            PetFollowUpPresentationReceipt receipt)
        {
            return receipt == null
                ? null
                : new PetFollowUpResolution(
                    receipt.Damage,
                    receipt.IsCritical,
                    ToCombatSnapshot(receipt.Target),
                    receipt.EnemyHpAfter,
                    receipt.EnemyDefeated,
                    receipt.StageAdvanced,
                    receipt.Carried);
        }

        private static bool IsBossEncounter(CombatSnapshot snapshot)
        {
            return snapshot != null &&
                (snapshot.EncounterKind == StageEncounterKind.BigBoss ||
                 snapshot.EncounterKind == StageEncounterKind.FinalBoss);
        }

        private IEnumerator PlayPlayerAttackAndEnemyReaction(
            string presentationId,
            int damage,
            bool isCritical,
            int enemyHpBefore,
            int enemyHpAfter)
        {
            bool componentFct = _floatingText != null &&
                _floatingText.IsReady && _enemyDamageAnchor != null &&
                !string.IsNullOrWhiteSpace(presentationId);
            float hitStop = ResolveHitStop(isCritical, false);

            yield return PlayPairedAction(
                _playerActor,
                PresentationActionKind.PlayerPrimaryAttack,
                _enemyActor,
                hitStop,
                () => _audio?.PlaySwing(),
                () =>
                {
                    if (componentFct)
                    {
                        _floatingText.Spawn(new FloatingCombatTextRequest(
                            presentationId,
                            FloatingCombatTextSemantic.Damage,
                            damage,
                            _enemyDamageAnchor,
                            isCritical,
                            0));
                    }
                    else
                    {
                        _view.ShowDamage(damage, isCritical);
                    }

                    _view.SetEnemyHit(true, isCritical);
                    if (isCritical)
                        _impactImpulse?.PlayCritical();
                    else
                        _impactImpulse?.PlayNormal();
                    _impactBurst?.Play(
                        _enemyDamageAnchor,
                        isCritical
                            ? CombatImpactBurstKind.Critical
                            : CombatImpactBurstKind.Normal);
                    _audio?.PlayHit(isCritical);
                },
                _enemyActor?.Play(
                    PresentationActionKind.EnemyTakeDamage,
                    null,
                    isCritical),
                InterpolateEnemyHp(enemyHpBefore, enemyHpAfter, hitStop));

            _view.SetEnemyHp(enemyHpAfter);
            yield return new WaitForSecondsRealtime(
                ResolvePostHitHold(isCritical));
            _view.SetEnemyHit(false, false);
            if (!componentFct) _view.HideDamage();
        }

        private IEnumerator PlayEnemyAttackAndPlayerReaction(
            int playerHeartsAfter,
            int playerMaximumHearts)
        {
            float hitStop = ResolveHitStop(false, true);
            yield return PlayPairedAction(
                _enemyActor,
                PresentationActionKind.EnemyAttack,
                _playerActor,
                hitStop,
                () => _audio?.PlayEnemyAttack(),
                () =>
                {
                    _view.PlayPlayerDamageHearts(
                        playerHeartsAfter,
                        playerMaximumHearts);
                    _impactImpulse?.PlayPlayerDamage();
                    _impactBurst?.Play(
                        _playerActor?.DamageTextAnchor,
                        CombatImpactBurstKind.PlayerDamage);
                },
                _playerActor?.Play(PresentationActionKind.PlayerTakeDamage));
        }

        private IEnumerator PlayPetFollowUpAndEnemyReaction(
            string presentationId,
            PetFollowUpResolution followUp)
        {
            if (followUp == null) yield break;
            bool componentFct = _floatingText != null &&
                _floatingText.IsReady &&
                _enemyDamageAnchor != null &&
                !string.IsNullOrWhiteSpace(presentationId);
            float hitStop = ResolveHitStop(followUp.IsCritical, false);

            yield return PlayPairedAction(
                _petActor,
                PresentationActionKind.PetFollowUpAttack,
                _enemyActor,
                hitStop,
                () => _audio?.PlaySwing(),
                () =>
                {
                    if (componentFct)
                    {
                        _floatingText.Spawn(new FloatingCombatTextRequest(
                            presentationId,
                            FloatingCombatTextSemantic.Damage,
                            followUp.Damage,
                            _enemyDamageAnchor,
                            followUp.IsCritical,
                            1));
                    }
                    else
                    {
                        _view.ShowDamage(followUp.Damage, followUp.IsCritical);
                    }
                    _view.SetEnemyHit(true, followUp.IsCritical);
                    if (followUp.IsCritical)
                        _impactImpulse?.PlayCritical();
                    else
                        _impactImpulse?.PlayNormal();
                    _impactBurst?.Play(
                        _enemyDamageAnchor,
                        followUp.IsCritical
                            ? CombatImpactBurstKind.Critical
                            : CombatImpactBurstKind.Normal);
                    _audio?.PlayHit(followUp.IsCritical);
                },
                _enemyActor?.Play(
                    PresentationActionKind.EnemyTakeDamage,
                    null,
                    followUp.IsCritical),
                InterpolateEnemyHp(
                    followUp.Target.EnemyCurrentHp,
                    followUp.EnemyHpAfter,
                    hitStop));

            _view.SetEnemyHp(followUp.EnemyHpAfter);
            yield return new WaitForSecondsRealtime(
                ResolvePostHitHold(followUp.IsCritical));
            _view.SetEnemyHit(false, false);
            if (!componentFct) _view.HideDamage();
        }

        private IEnumerator InterpolateEnemyHp(
            int before,
            int after,
            float initialDelay)
        {
            float delayElapsed = 0f;
            while (delayElapsed < initialDelay)
            {
                delayElapsed += ResolveFrameDelta();
                yield return null;
            }

            float duration = _reducedMotion ? 0.12f : 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += ResolveFrameDelta();
                float progress = Mathf.Clamp01(elapsed / duration);
                _view.SetEnemyHp(Mathf.RoundToInt(Mathf.Lerp(
                    before,
                    after,
                    progress)));
                yield return null;
            }

            _view.SetEnemyHp(after);
        }

        private IEnumerator PlayPairedAction(
            ActorPresentationController attacker,
            PresentationActionKind attackAction,
            ActorPresentationController reactor,
            float hitStopSeconds,
            Action playFallbackAttack,
            Action playImpactFeedback,
            params IEnumerator[] reactionRoutines)
        {
            bool impactTriggered = false;
            bool reactionsComplete = false;
            IEnumerator reactionGroup = null;

            void BeginImpact()
            {
                if (impactTriggered) return;
                impactTriggered = true;
                attacker?.HoldCurrentPose(hitStopSeconds);
                reactor?.HoldCurrentPose(hitStopSeconds);
                playImpactFeedback?.Invoke();
                reactionGroup = RunConcurrent(reactionRoutines);
                if (_routineHost != null)
                {
                    _routineHost.StartCoroutine(RunTracked(
                        reactionGroup,
                        () => reactionsComplete = true));
                }
            }

            if (attacker != null)
            {
                yield return attacker.Play(attackAction, BeginImpact);
            }
            else
            {
                playFallbackAttack?.Invoke();
                BeginImpact();
            }

            if (!impactTriggered) BeginImpact();
            if (_routineHost == null)
            {
                if (reactionGroup != null) yield return reactionGroup;
            }
            else
            {
                float timeoutSeconds = 6.0f;
                float elapsed = 0f;
                while (!reactionsComplete && elapsed < timeoutSeconds)
                {
                    elapsed += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                    yield return null;
                }
            }
        }

        private float ResolveHitStop(bool critical, bool playerDamaged)
        {
            if (_reducedMotion)
                return _juiceProfile == null
                    ? 0.025f
                    : _juiceProfile.ReducedMotionHitStopSeconds;
            if (playerDamaged)
                return _juiceProfile == null
                    ? 0.060f
                    : _juiceProfile.PlayerDamageHitStopSeconds;
            if (critical)
                return _juiceProfile == null
                    ? 0.075f
                    : _juiceProfile.CriticalHitStopSeconds;
            return _juiceProfile == null
                ? 0.060f
                : _juiceProfile.NormalHitStopSeconds;
        }

        private float ResolvePostHitHold(bool critical)
        {
            if (_reducedMotion) return 0.12f;
            if (critical)
                return _juiceProfile == null
                    ? 0.35f
                    : _juiceProfile.CriticalPostHitHoldSeconds;
            return _juiceProfile == null
                ? 0.20f
                : _juiceProfile.NormalPostHitHoldSeconds;
        }

        private static float ResolveFrameDelta()
        {
            return Time.unscaledDeltaTime > 0f
                ? Time.unscaledDeltaTime
                : 0.05f;
        }

        private IEnumerator CreateRewardMagnetRoutine(AttemptResolution attempt)
        {
            if (attempt == null) return null;

            if (attempt.IsAcademic)
            {
                // Accumulate rank currency on every academic answer (data awarded regardless).
                // Only display the visual on the killing blow so the burst feels earned.
                AccumulateRankCurrency(attempt);
                if (!attempt.Combat.EnemyDefeated)
                    return null;

                return PlayAcademicDeathRewards(attempt);
            }

            // Event (Power Coin) path — unchanged.
            long totalGrant = attempt.Event.PowerCoinsGranted;
            if (totalGrant <= 0) return null;

            Vector2 enemyPosition = Vector2.zero;
            if (_enemyDamageAnchor != null &&
                _enemyDamageAnchor.TryGetLocalPoint(null, null, out Vector2 anchorPoint))
                enemyPosition = anchorPoint;

            if (_floatingRewardText != null)
            {
                if (_enemyDamageAnchor != null)
                    _floatingRewardText.Spawn(RewardCurrencyKind.PowerCoin, totalGrant, _enemyDamageAnchor);
                else
                    _floatingRewardText.Spawn(RewardCurrencyKind.PowerCoin, totalGrant, enemyPosition);
            }

            if (_rewardMagnet == null) return null;

            return _rewardMagnet.PlayRewardDropAndMagnet(
                RewardCurrencyKind.PowerCoin,
                Math.Max(0, attempt.Snapshot.PowerCoins - totalGrant),
                totalGrant,
                enemyPosition);
        }

        /// <summary>
        /// Adds this answer's rank currency delta to the per-monster accumulator.
        /// Resets when the encounter ID changes (new monster spawned).
        /// NOTE: Uses Presentation.Source.EncounterId (pre-resolution) rather than
        /// Combat.Snapshot.EnemyId because on a killing blow the engine calls
        /// LoadSelection(next) before CreateSnapshot(), so Snapshot already carries
        /// the next monster's ID and would incorrectly reset the accumulator.
        /// </summary>
        private void AccumulateRankCurrency(AttemptResolution attempt)
        {
            // Source = state BEFORE this resolution — always the monster we just hit.
            string encounterId = attempt.Presentation?.Source.EncounterId
                ?? attempt.Combat.Snapshot?.EnemyId
                ?? string.Empty;

            if (encounterId != _lastEncounterId)
            {
                _accumulatedSilver = 0;
                _accumulatedGold = 0;
                _accumulatedDiamond = 0;
                _lastEncounterId = encounterId;
            }

            long delta = attempt.Academic.CurrencyDelta;
            if (delta <= 0) return;

            switch (attempt.Academic.RankAtCommit.Tier)
            {
                case AcademicRankTier.Silver:  _accumulatedSilver  += delta; break;
                case AcademicRankTier.Gold:    _accumulatedGold    += delta; break;
                case AcademicRankTier.Diamond: _accumulatedDiamond += delta; break;
            }
        }

        /// <summary>
        /// Fires one FRT + one magnet-icon drop for every rank tier earned during
        /// this monster encounter. Concurrent drops, FRT pops staggered by 0.12 s.
        /// </summary>
        private IEnumerator PlayAcademicDeathRewards(AttemptResolution attempt)
        {
            RankCurrencyBalances balances = attempt.Snapshot.Academic.Balances;

            // Collect every tier with a positive accumulated total (order: Silver→Gold→Diamond).
            var grants = new System.Collections.Generic.List<(RewardCurrencyKind kind, long total, long resulting)>();
            if (_accumulatedSilver  > 0) grants.Add((RewardCurrencyKind.RankSilver,  _accumulatedSilver,  balances.Silver));
            if (_accumulatedGold    > 0) grants.Add((RewardCurrencyKind.RankGold,    _accumulatedGold,    balances.Gold));
            if (_accumulatedDiamond > 0) grants.Add((RewardCurrencyKind.RankDiamond, _accumulatedDiamond, balances.Diamond));

            // Reset — they have been consumed by this visual burst.
            _accumulatedSilver = 0;
            _accumulatedGold   = 0;
            _accumulatedDiamond = 0;

            if (grants.Count == 0) yield break;

            Vector2 enemyPosition = Vector2.zero;
            if (_enemyDamageAnchor != null &&
                _enemyDamageAnchor.TryGetLocalPoint(null, null, out Vector2 anchorPoint))
                enemyPosition = anchorPoint;

            for (int i = 0; i < grants.Count; i++)
            {
                var (kind, total, resulting) = grants[i];

                // FRT pop — one per tier, spawnOrdinal staggers horizontal separation.
                if (_floatingRewardText != null)
                {
                    if (_enemyDamageAnchor != null)
                        _floatingRewardText.Spawn(kind, total, _enemyDamageAnchor, i);
                    else
                        _floatingRewardText.Spawn(kind, total, enemyPosition, i);
                }

                // Magnet icon drop — fire concurrently via routineHost so drops overlap.
                if (_rewardMagnet != null)
                {
                    long startAmount = Math.Max(0, resulting - total);
                    IEnumerator drop = _rewardMagnet.PlayRewardDropAndMagnet(
                        kind, startAmount, total, enemyPosition);
                    if (_routineHost != null)
                        _routineHost.StartCoroutine(drop);
                    else
                        yield return drop;
                }

                // Stagger FRT pops slightly so both labels are readable when multi-tier.
                if (i < grants.Count - 1)
                    yield return new WaitForSecondsRealtime(0.12f);
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

            float timeoutSeconds = 6.0f;
            float elapsed = 0f;
            while (pending > 0 && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime > 0f ? Time.unscaledDeltaTime : 0.016f;
                yield return null;
            }
        }

        private static IEnumerator RunTracked(
            IEnumerator routine,
            System.Action completed)
        {
            try
            {
                yield return routine;
            }
            finally
            {
                completed?.Invoke();
            }
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
                return EnemyActionTokenKind.Flee;
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
