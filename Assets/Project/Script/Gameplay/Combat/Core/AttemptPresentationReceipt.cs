using System;
using PowerMath.Gameplay.Academic;

namespace PowerMath.Gameplay.Combat
{
    public enum AttemptOutcomeKind
    {
        Correct,
        Incorrect,
        Timeout
    }

    public readonly struct RankTransitionReceipt
    {
        public RankTransitionReceipt(AcademicRank previous, AcademicRank current)
        {
            Previous = previous;
            Current = current;
        }

        public AcademicRank Previous { get; }
        public AcademicRank Current { get; }
        public bool Changed => Previous != Current;
        public bool IsPromotion => Changed && Current.Tier > Previous.Tier;
        public bool IsDemotion => Changed && Current.Tier < Previous.Tier;
    }

    public sealed class CombatPresentationSnapshot
    {
        public CombatPresentationSnapshot(
            StageId stage,
            string biomeId,
            string encounterId,
            StageEncounterKind encounterKind,
            int enemyCurrentHp,
            int enemyMaximumHp,
            int enemyRemainingCooldown,
            int enemyMaximumCooldown,
            int playerCurrentHearts,
            int playerMaximumHearts,
            CombatPhase phase)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
                throw new ArgumentException("Encounter ID is required.", nameof(encounterId));
            if (enemyMaximumHp <= 0 || enemyCurrentHp < 0 || enemyCurrentHp > enemyMaximumHp)
                throw new ArgumentOutOfRangeException(nameof(enemyCurrentHp));
            if (enemyMaximumCooldown < 0 || enemyRemainingCooldown < 0 ||
                enemyRemainingCooldown > enemyMaximumCooldown)
                throw new ArgumentOutOfRangeException(nameof(enemyRemainingCooldown));
            if (playerMaximumHearts <= 0 || playerCurrentHearts < 0 ||
                playerCurrentHearts > playerMaximumHearts)
                throw new ArgumentOutOfRangeException(nameof(playerCurrentHearts));

            Stage = stage;
            BiomeId = biomeId ?? string.Empty;
            EncounterId = encounterId;
            EncounterKind = encounterKind;
            EnemyCurrentHp = enemyCurrentHp;
            EnemyMaximumHp = enemyMaximumHp;
            EnemyRemainingCooldown = enemyRemainingCooldown;
            EnemyMaximumCooldown = enemyMaximumCooldown;
            PlayerCurrentHearts = playerCurrentHearts;
            PlayerMaximumHearts = playerMaximumHearts;
            Phase = phase;
        }

        public StageId Stage { get; }
        public string BiomeId { get; }
        public string EncounterId { get; }
        public StageEncounterKind EncounterKind { get; }
        public int EnemyCurrentHp { get; }
        public int EnemyMaximumHp { get; }
        public int EnemyRemainingCooldown { get; }
        public int EnemyMaximumCooldown { get; }
        public int PlayerCurrentHearts { get; }
        public int PlayerMaximumHearts { get; }
        public CombatPhase Phase { get; }

        public static CombatPresentationSnapshot From(CombatSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return new CombatPresentationSnapshot(
                snapshot.Stage,
                snapshot.BiomeId,
                snapshot.EnemyId,
                snapshot.EncounterKind,
                snapshot.EnemyCurrentHp,
                snapshot.EnemyMaximumHp,
                snapshot.EnemyRemainingCooldown,
                snapshot.EnemyMaximumCooldown,
                snapshot.PlayerCurrentHearts,
                snapshot.PlayerMaximumHearts,
                snapshot.Phase);
        }
    }

    public sealed class AttemptPresentationReceipt
    {
        public const int CurrentVersion = 1;

        public AttemptPresentationReceipt(
            string presentationId,
            string attemptId,
            AttemptOutcomeKind outcome,
            int responseScore,
            int finalDamage,
            bool isCritical,
            CombatPresentationSnapshot source,
            CombatPresentationSnapshot destination,
            int resolvedEnemyHpAfter,
            bool enemyDefeated,
            bool enemyAttacked,
            bool playerDefeated,
            bool stageAdvanced,
            bool biomeChanged,
            RankTransitionReceipt rankTransition,
            int version = CurrentVersion)
        {
            PresentationId = presentationId ?? string.Empty;
            AttemptId = attemptId ?? string.Empty;
            Version = version;
            Outcome = outcome;
            ResponseScore = responseScore;
            FinalDamage = finalDamage;
            IsCritical = isCritical;
            Source = source;
            Destination = destination;
            ResolvedEnemyHpAfter = resolvedEnemyHpAfter;
            EnemyDefeated = enemyDefeated;
            EnemyAttacked = enemyAttacked;
            PlayerDefeated = playerDefeated;
            StageAdvanced = stageAdvanced;
            BiomeChanged = biomeChanged;
            RankTransition = rankTransition;

            if (!TryValidate(out string error))
                throw new ArgumentException(error, nameof(presentationId));
        }

        public string PresentationId { get; }
        public string AttemptId { get; }
        public int Version { get; }
        public AttemptOutcomeKind Outcome { get; }
        public int ResponseScore { get; }
        public int FinalDamage { get; }
        public bool IsCritical { get; }
        public CombatPresentationSnapshot Source { get; }
        public CombatPresentationSnapshot Destination { get; }
        public int ResolvedEnemyHpAfter { get; }
        public bool EnemyDefeated { get; }
        public bool EnemyAttacked { get; }
        public bool PlayerDefeated { get; }
        public bool StageAdvanced { get; }
        public bool BiomeChanged { get; }
        public RankTransitionReceipt RankTransition { get; }

        public bool TryValidate(out string error)
        {
            if (Version != CurrentVersion)
                return Fail("Unsupported attempt presentation receipt version.", out error);
            if (string.IsNullOrWhiteSpace(PresentationId) || string.IsNullOrWhiteSpace(AttemptId))
                return Fail("Presentation and attempt IDs are required.", out error);
            if (Source == null || Destination == null)
                return Fail("Source and destination snapshots are required.", out error);
            if (ResponseScore < 0 || ResponseScore > 10 || FinalDamage < 0)
                return Fail("Attempt values are outside their accepted ranges.", out error);
            if (Outcome != AttemptOutcomeKind.Correct && FinalDamage != 0)
                return Fail("Failed attempts cannot carry damage.", out error);
            if (IsCritical && Outcome != AttemptOutcomeKind.Correct)
                return Fail("Only a correct attempt can be critical.", out error);
            if (ResolvedEnemyHpAfter < 0 || ResolvedEnemyHpAfter > Source.EnemyMaximumHp)
                return Fail("Resolved enemy HP is outside the source encounter range.", out error);
            int expectedEnemyHp = Math.Max(0, Source.EnemyCurrentHp - FinalDamage);
            if (ResolvedEnemyHpAfter != expectedEnemyHp)
                return Fail("Resolved enemy HP does not agree with accepted damage.", out error);
            if (EnemyDefeated != (ResolvedEnemyHpAfter == 0))
                return Fail("Enemy defeat does not agree with resolved enemy HP.", out error);
            if (EnemyDefeated && EnemyAttacked)
                return Fail("A defeated enemy cannot also attack.", out error);
            if (PlayerDefeated && (Destination.PlayerCurrentHearts != 0 ||
                Destination.Phase != CombatPhase.RunDefeat))
                return Fail("Player defeat requires zero hearts and RunDefeat.", out error);
            if (EnemyAttacked && Source.PlayerCurrentHearts - Destination.PlayerCurrentHearts != 1)
                return Fail("Enemy attack must agree with the accepted heart delta.", out error);
            if (!EnemyAttacked && Source.PlayerCurrentHearts != Destination.PlayerCurrentHearts)
                return Fail("Hearts changed without an accepted enemy attack.", out error);
            if (Destination.Phase != CombatPhase.PresentingResult &&
                Destination.Phase != CombatPhase.RunDefeat &&
                Destination.Phase != CombatPhase.RunComplete)
                return Fail("A pending receipt requires a presentation or terminal phase.", out error);

            error = string.Empty;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
