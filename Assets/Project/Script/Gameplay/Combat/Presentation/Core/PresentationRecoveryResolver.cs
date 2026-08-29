namespace PowerMath.Gameplay.Combat.Presentation
{
    public enum PresentationRecoveryKind
    {
        Normal,
        PendingAttempt,
        RunDefeat,
        PendingDeathResult,
        PendingRebirthResult,
        UnsupportedReceipt
    }

    public static class PresentationRecoveryResolver
    {
        public static PresentationRecoveryKind Resolve(
            CombatPhase phase,
            AttemptPresentationReceipt pendingAttempt,
            RunPresentationReceipt pendingRun)
        {
            if (pendingAttempt != null)
            {
                if (!pendingAttempt.TryValidate(out _))
                    return PresentationRecoveryKind.UnsupportedReceipt;
                return PresentationRecoveryKind.PendingAttempt;
            }

            if (pendingRun != null && pendingRun.IsPending)
            {
                if (!pendingRun.IsSupported)
                    return PresentationRecoveryKind.UnsupportedReceipt;
                return pendingRun.Cause == RunPresentationCause.Death
                    ? PresentationRecoveryKind.PendingDeathResult
                    : PresentationRecoveryKind.PendingRebirthResult;
            }

            if (phase == CombatPhase.PresentingResult)
                return PresentationRecoveryKind.UnsupportedReceipt;
            if (phase == CombatPhase.RunDefeat)
                return PresentationRecoveryKind.RunDefeat;
            return PresentationRecoveryKind.Normal;
        }
    }
}
