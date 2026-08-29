namespace PowerMath.Gameplay.Combat.Presentation
{
    public enum ActorVisualState
    {
        Hidden,
        Appearing,
        Idle,
        Attacking,
        FailedAttack,
        TakingDamage,
        Walking,
        Dying,
        Rebirthing
    }

    public static class ActorPresentationStatePolicy
    {
        public static bool CanTransition(
            PresentationActor actor,
            ActorVisualState current,
            ActorVisualState next)
        {
            if (actor != PresentationActor.Player && actor != PresentationActor.Enemy)
                return false;
            if (current == next) return current == ActorVisualState.Idle;
            if (next == ActorVisualState.Hidden) return true;
            if (current == ActorVisualState.Hidden)
                return next == ActorVisualState.Appearing || next == ActorVisualState.Idle;
            if (current == ActorVisualState.Appearing)
                return next == ActorVisualState.Idle;
            if (current == ActorVisualState.Dying)
                return next == ActorVisualState.Hidden;
            if (current == ActorVisualState.Rebirthing)
                return next == ActorVisualState.Idle;
            if (current != ActorVisualState.Idle)
                return next == ActorVisualState.Idle || next == ActorVisualState.Dying;

            if (actor == PresentationActor.Player)
            {
                return next == ActorVisualState.Attacking ||
                    next == ActorVisualState.FailedAttack ||
                    next == ActorVisualState.TakingDamage ||
                    next == ActorVisualState.Dying ||
                    next == ActorVisualState.Rebirthing;
            }

            return next == ActorVisualState.Attacking ||
                next == ActorVisualState.TakingDamage ||
                next == ActorVisualState.Walking ||
                next == ActorVisualState.Dying;
        }
    }
}
