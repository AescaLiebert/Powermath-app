using System;
using System.Collections.Generic;

namespace PowerMath.Gameplay.Combat.Presentation
{
    public enum PresentationActor
    {
        System,
        Player,
        Enemy,
        Ui
    }

    public enum PresentationActionKind
    {
        AnswerFeedback,
        PlayerPrimaryAttack,
        PlayerFailedAttack,
        PlayerTakeDamage,
        PlayerDie,
        PlayerRebirth,
        PlayerPotionReaction,
        PlayerRankUp,
        PlayerRankDown,
        EnemyAppear,
        EnemyWalk,
        EnemyAttack,
        EnemyTakeDamage,
        EnemyDie,
        ArmEnemyAction,
        ConsumeEnemyAction,
        CancelEnemyAction,
        InitiateEnemyActions,
        SpawnFloatingText,
        InterpolateEnemyHp,
        InterpolatePlayerHearts,
        PlayImpactImpulse,
        ShowStageResult,
        PlayBiomeTransition
    }

    public enum PresentationBarrier
    {
        None,
        Blocking,
        Completion
    }

    public enum EnemyActionTokenKind
    {
        Walk,
        Attack,
        EventRisk
    }

    public sealed class PresentationPayload
    {
        public PresentationPayload(
            int value = 0,
            int previousValue = 0,
            int maximumValue = 0,
            bool isCritical = false,
            string semantic = "")
        {
            Value = value;
            PreviousValue = previousValue;
            MaximumValue = maximumValue;
            IsCritical = isCritical;
            Semantic = semantic ?? string.Empty;
        }

        public int Value { get; }
        public int PreviousValue { get; }
        public int MaximumValue { get; }
        public bool IsCritical { get; }
        public string Semantic { get; }
    }

    public sealed class CombatPresentationStep
    {
        public CombatPresentationStep(
            string stepId,
            PresentationActor actor,
            PresentationActionKind kind,
            string targetId,
            PresentationPayload payload,
            string profileKey,
            PresentationBarrier barrier)
        {
            if (string.IsNullOrWhiteSpace(stepId))
                throw new ArgumentException("Step ID is required.", nameof(stepId));

            StepId = stepId;
            Actor = actor;
            Kind = kind;
            TargetId = targetId ?? string.Empty;
            Payload = payload ?? new PresentationPayload();
            ProfileKey = profileKey ?? string.Empty;
            Barrier = barrier;
        }

        public string StepId { get; }
        public PresentationActor Actor { get; }
        public PresentationActionKind Kind { get; }
        public string TargetId { get; }
        public PresentationPayload Payload { get; }
        public string ProfileKey { get; }
        public PresentationBarrier Barrier { get; }
    }

    public sealed class CombatPresentationPlan
    {
        public CombatPresentationPlan(
            string presentationId,
            IReadOnlyList<CombatPresentationStep> steps,
            bool transfersToTerminalFlow)
        {
            if (string.IsNullOrWhiteSpace(presentationId))
                throw new ArgumentException("Presentation ID is required.", nameof(presentationId));
            PresentationId = presentationId;
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
            TransfersToTerminalFlow = transfersToTerminalFlow;
        }

        public string PresentationId { get; }
        public IReadOnlyList<CombatPresentationStep> Steps { get; }
        public bool TransfersToTerminalFlow { get; }
    }

    public enum RunPresentationCause
    {
        Death,
        Rebirth
    }

    public enum RunPresentationStatus
    {
        None,
        Pending,
        Acknowledged
    }

    public enum UiLifecycleState
    {
        Hidden,
        Entering,
        Idle,
        Exiting
    }

    public sealed class RunPresentationReceipt
    {
        public const int CurrentVersion = 1;

        public RunPresentationReceipt(
            string presentationId,
            string sourceRunId,
            RunPresentationCause cause,
            RunPresentationStatus status,
            string sourceEncounterId,
            int version = CurrentVersion)
        {
            PresentationId = presentationId ?? string.Empty;
            SourceRunId = sourceRunId ?? string.Empty;
            Cause = cause;
            Status = status;
            SourceEncounterId = sourceEncounterId ?? string.Empty;
            Version = version;
        }

        public string PresentationId { get; }
        public string SourceRunId { get; }
        public RunPresentationCause Cause { get; }
        public RunPresentationStatus Status { get; }
        public string SourceEncounterId { get; }
        public int Version { get; }
        public bool IsSupported => Version == CurrentVersion;
        public bool IsPending => Status == RunPresentationStatus.Pending;
    }
}
