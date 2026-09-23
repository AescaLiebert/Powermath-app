using System;
using System.Collections;
using PowerMath.Gameplay.Tutorial;
using PowerMath.PlayerData;
using PowerMath.Session;

namespace PowerMath.UI.MainMenu.Tutorial
{
    public interface ITutorialProgressStore
    {
        bool IsServerAuthoritative { get; }
        IEnumerator Execute(
            TutorialProgress progress,
            PlayerSnapshot currentPlayer,
            string operationId,
            Action<PlayerSnapshot> succeeded,
            Action<FirestoreRestClient.Failure> failed);
    }

    public sealed class LifecycleTutorialProgressStore : ITutorialProgressStore
    {
        // All tutorial directors write the same player revision. Serialize
        // their writes so simultaneous tutorial triggers cannot lose a step.
        private static bool _commandInFlight;
        private readonly IPlayerLifecycleCommands _commands;

        public LifecycleTutorialProgressStore(IPlayerLifecycleCommands commands)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        public bool IsServerAuthoritative => _commands.IsServerAuthoritative;

        public IEnumerator Execute(
            TutorialProgress progress,
            PlayerSnapshot currentPlayer,
            string operationId,
            Action<PlayerSnapshot> succeeded,
            Action<FirestoreRestClient.Failure> failed)
        {
            if (progress == null || currentPlayer == null)
                throw new ArgumentNullException();
            while (_commandInFlight) yield return null;
            _commandInFlight = true;
            try
            {
                // Capture the revision only after the previous tutorial write
                // has merged its authoritative snapshot into this live object.
                var command = new PlayerLifecycleCommand
                {
                    kind = PlayerLifecycleCommandKind.AdvanceTutorial,
                    operationId = operationId,
                    playerId = currentPlayer.playerId,
                    expectedRevision = currentPlayer.revision,
                    value = progress.TutorialId,
                    tutorialVersion = progress.Version,
                    tutorialStatus = progress.Status.ToString(),
                    tutorialStepId = progress.CurrentStepId,
                    tutorialTriggerRecordedAtUnixSeconds = progress.TriggerRecordedAt,
                    tutorialCompletedAtUnixSeconds = progress.CompletedAt,
                    tutorialRewardClaimed = progress.RewardClaimed,
                    tutorialLastTransactionId = progress.LastTransactionId,
                    tutorialLegacyPlayer = progress.LegacyPlayer,
                    tutorialGuidedEncounterId = progress.GuidedEncounterId,
                    tutorialFirstAttemptOutcome = progress.FirstAttemptOutcome.ToString(),
                    tutorialVariant = progress.Variant
                };
                yield return _commands.Execute(command, succeeded, failed);
            }
            finally
            {
                _commandInFlight = false;
            }
        }
    }

    public static class TutorialProgressMapper
    {
        public static bool TryFind(
            PlayerSnapshot player,
            string tutorialId,
            out TutorialProgress progress,
            out string error)
        {
            progress = null;
            error = string.Empty;
            PlayerSnapshot.TutorialEntryData data =
                TutorialProgressPolicy.Find(player, tutorialId);
            if (data == null) return true;
            string savedOutcome = string.IsNullOrWhiteSpace(data.firstAttemptOutcome)
                ? TutorialAttemptOutcome.None.ToString()
                : data.firstAttemptOutcome;
            if (!Enum.TryParse(data.status, false, out TutorialStatus status) ||
                !Enum.TryParse(savedOutcome, false,
                    out TutorialAttemptOutcome outcome) || data.version <= 0)
            {
                error = "Saved tutorial progress requires a newer game version.";
                return false;
            }
            progress = new TutorialProgress(
                data.tutorialId,
                data.version,
                status,
                data.currentStepId,
                data.triggerRecordedAtUnixSeconds,
                data.completedAtUnixSeconds,
                data.rewardClaimed,
                data.lastTransactionId,
                data.lastOperationId,
                data.legacyPlayer,
                data.guidedEncounterId,
                outcome,
                data.variant);
            return true;
        }
    }
}
