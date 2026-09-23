using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PowerMath.Gameplay.Tutorial;
using PowerMath.PlayerData;

namespace PowerMath.Session
{
    public enum PlayerLifecycleCommandKind
    {
        SetLocale,
        CompleteOpening,
        SelectCharacter,
        CompletePreparation,
        AdvanceTutorial,
        ClaimTutorialPowerCoinReward
    }

    public sealed class PlayerLifecycleCommand
    {
        public PlayerLifecycleCommandKind kind;
        public string operationId;
        public string playerId;
        public long expectedRevision;
        public string value;
        public string displayName;
        public int tutorialVersion;
        public string tutorialStatus;
        public string tutorialStepId;
        public long tutorialTriggerRecordedAtUnixSeconds;
        public long tutorialCompletedAtUnixSeconds;
        public bool tutorialRewardClaimed;
        public string tutorialLastTransactionId;
        public bool tutorialLegacyPlayer;
        public string tutorialGuidedEncounterId;
        public string tutorialFirstAttemptOutcome;
        public string tutorialVariant;
        public long tutorialPowerCoinReward;
    }

    public interface IPlayerLifecycleCommands
    {
        bool IsServerAuthoritative { get; }
        IEnumerator Execute(PlayerLifecycleCommand command, Action<PlayerSnapshot> succeeded, Action<FirestoreRestClient.Failure> failed);
    }

    public static class PlayerLifecyclePolicy
    {
        public const int MaximumDisplayNameLength = 20;

        public static bool IsCharacter(string id) => id == "ricko" || id == "stellar";
        public static bool IsLocale(string locale) => locale == "th" || locale == "en";
        public static bool IsComplete(PlayerSnapshot player) =>
            player?.onboarding?.phase == "complete" && IsCharacter(player.profile?.characterId);

        public static bool TryNormalizeName(string input, out string name) =>
            TryNormalizeName(input, out name, out _);

        public static bool TryNormalizeName(string input, out string name, out DisplayNameValidationResult result)
        {
            return DisplayNamePolicy.TryValidate(input, out name, out result);
        }

        public static FirestorePatchPlan Plan(PlayerSnapshot player, string username, PlayerLifecycleCommand command)
        {
            if (player == null || command == null || player.playerId != command.playerId ||
                !Guid.TryParseExact(command.operationId, "N", out _))
                throw new ArgumentException("Invalid player operation.");
            var builder = new FirestorePatchDocumentBuilder();
            string[] Path(params string[] parts)
            {
                var result = new string[parts.Length + 1];
                result[0] = "gamedata";
                Array.Copy(parts, 0, result, 1, parts.Length);
                return result;
            }
            var onboarding = player.onboarding ?? throw new FormatException("Missing onboarding state.");
            bool alreadyApplied = command.kind switch
            {
                PlayerLifecycleCommandKind.SetLocale => player.preferences?.locale == command.value,
                PlayerLifecycleCommandKind.CompleteOpening => onboarding.openingCheckpointId == "complete",
                PlayerLifecycleCommandKind.SelectCharacter => onboarding.phase == "name" && onboarding.selectedCharacterId == command.value,
                PlayerLifecycleCommandKind.CompletePreparation => onboarding.phase == "complete" &&
                    onboarding.completionOperationId == command.operationId &&
                    player.profile.characterId == command.value &&
                    player.profile.displayName == command.displayName,
                PlayerLifecycleCommandKind.AdvanceTutorial =>
                    TutorialProgressPolicy.IsAlreadyApplied(player, command),
                PlayerLifecycleCommandKind.ClaimTutorialPowerCoinReward =>
                    TutorialProgressPolicy.IsRewardClaimed(player, command),
                _ => false
            };
            if (alreadyApplied) return builder.Build();
            if (player.revision != command.expectedRevision) throw new InvalidOperationException("Player data changed. Please reload and retry.");
            switch (command.kind)
            {
                case PlayerLifecycleCommandKind.SetLocale:
                    if (!IsLocale(command.value)) throw new ArgumentException("Unsupported language.");
                    builder.AddString(Path("preferences", "locale"), command.value);
                    break;
                case PlayerLifecycleCommandKind.CompleteOpening:
                    if (onboarding.phase != "opening") throw new InvalidOperationException("Opening is no longer active.");
                    builder.AddString(Path("onboarding", "openingCheckpointId"), "complete");
                    builder.AddString(Path("onboarding", "phase"), "character");
                    break;
                case PlayerLifecycleCommandKind.SelectCharacter:
                    if (!IsCharacter(command.value) || (onboarding.phase != "character" && onboarding.phase != "name"))
                        throw new ArgumentException("Character selection is unavailable.");
                    builder.AddString(Path("onboarding", "selectedCharacterId"), command.value);
                    builder.AddString(Path("onboarding", "phase"), "name");
                    break;
                case PlayerLifecycleCommandKind.CompletePreparation:
                    if (onboarding.phase != "name" || onboarding.selectedCharacterId != command.value ||
                        !IsCharacter(command.value) || !TryNormalizeName(command.displayName, out string name))
                        throw new ArgumentException("Choose a character and enter a valid name.");
                    builder.AddString(Path("profile", "characterId"), command.value);
                    builder.AddString(Path("profile", "displayName"), name);
                    builder.AddString(Path("onboarding", "phase"), "complete");
                    builder.AddString(Path("onboarding", "completionOperationId"), command.operationId);
                    break;
                case PlayerLifecycleCommandKind.AdvanceTutorial:
                    TutorialProgressPolicy.AddPatch(builder, root: new[] { "gamedata" }, player, command);
                    break;
                case PlayerLifecycleCommandKind.ClaimTutorialPowerCoinReward:
                    TutorialProgressPolicy.AddRewardClaimPatch(
                        builder, root: new[] { "gamedata" }, player, command);
                    break;
                default: throw new ArgumentException("Unsupported operation.");
            }
            builder.AddInteger(Path("revision"), checked(player.revision + 1));
            return builder.Build();
        }
    }

    public static class TutorialProgressPolicy
    {
        public static bool IsAlreadyApplied(
            PlayerSnapshot player,
            PlayerLifecycleCommand command)
        {
            PlayerSnapshot.TutorialEntryData entry = Find(player, command?.value);
            return entry != null && command != null &&
                string.Equals(entry.lastOperationId, command.operationId,
                    StringComparison.Ordinal);
        }

        public static bool IsRewardClaimed(
            PlayerSnapshot player,
            PlayerLifecycleCommand command)
        {
            PlayerSnapshot.TutorialEntryData entry = Find(player, command?.value);
            return entry != null && entry.rewardClaimed;
        }

        public static void AddRewardClaimPatch(
            FirestorePatchDocumentBuilder builder,
            IReadOnlyList<string> root,
            PlayerSnapshot player,
            PlayerLifecycleCommand command)
        {
            if (builder == null || root == null || player == null || command == null)
                throw new ArgumentNullException();
            if (!string.Equals(command.value, "OnFirstRebirth", StringComparison.Ordinal) ||
                command.tutorialPowerCoinReward != 180)
                throw new ArgumentException("Invalid tutorial reward.");

            PlayerSnapshot.TutorialEntryData entry = Find(player, command.value);
            if (entry == null || entry.rewardClaimed ||
                (entry.status != TutorialStatus.Active.ToString() &&
                 entry.status != TutorialStatus.Queued.ToString()))
                throw new InvalidOperationException("Tutorial reward is unavailable.");
            if (player.wallet == null)
                throw new FormatException("Missing wallet state.");

            string[] EntryPath(params string[] fields)
            {
                var result = new string[root.Count + fields.Length + 2];
                for (int index = 0; index < root.Count; index++) result[index] = root[index];
                result[root.Count] = "tutorialMap";
                result[root.Count + 1] = command.value;
                Array.Copy(fields, 0, result, root.Count + 2, fields.Length);
                return result;
            }

            var walletPath = new string[root.Count + 2];
            for (int index = 0; index < root.Count; index++) walletPath[index] = root[index];
            walletPath[root.Count] = "wallet";
            walletPath[root.Count + 1] = "powerCoins";
            builder.AddInteger(walletPath, checked(player.wallet.powerCoins + command.tutorialPowerCoinReward));
            builder.AddBoolean(EntryPath("rewardClaimed"), true);
            builder.AddString(EntryPath("lastOperationId"), command.operationId);
        }

        public static void ApplyRewardClaimToSnapshot(
            PlayerSnapshot player,
            PlayerLifecycleCommand command)
        {
            PlayerSnapshot.TutorialEntryData entry = Find(player, command?.value);
            if (entry == null || player?.wallet == null) return;
            entry.rewardClaimed = true;
            entry.lastOperationId = command.operationId;
            player.wallet.powerCoins = checked(
                player.wallet.powerCoins + command.tutorialPowerCoinReward);
        }

        public static void AddPatch(
            FirestorePatchDocumentBuilder builder,
            IReadOnlyList<string> root,
            PlayerSnapshot player,
            PlayerLifecycleCommand command)
        {
            if (builder == null || root == null || player == null || command == null)
                throw new ArgumentNullException();
            if (!IsStableId(command.value) || command.tutorialVersion <= 0 ||
                !Enum.TryParse(command.tutorialStatus, false, out TutorialStatus status) ||
                !Enum.TryParse(command.tutorialFirstAttemptOutcome, false,
                    out TutorialAttemptOutcome _))
                throw new ArgumentException("Invalid tutorial transition.");
            if (status != TutorialStatus.Completed &&
                status != TutorialStatus.Queued &&
                string.IsNullOrWhiteSpace(command.tutorialStepId))
                throw new ArgumentException("Active tutorial transition requires a step ID.");
            if (!string.IsNullOrWhiteSpace(command.tutorialStepId) &&
                !IsStableId(command.tutorialStepId))
                throw new ArgumentException("Invalid tutorial step ID.");

            PlayerSnapshot.TutorialEntryData current = Find(player, command.value);
            if (current != null &&
                string.Equals(current.status, TutorialStatus.Completed.ToString(),
                    StringComparison.Ordinal) && status != TutorialStatus.Completed)
                throw new InvalidOperationException("Completed tutorials cannot be reopened.");
            if (current != null && current.version > command.tutorialVersion)
                throw new NotSupportedException("This tutorial save requires newer content.");

            string[] EntryPath(params string[] fields)
            {
                var result = new string[root.Count + fields.Length + 2];
                for (int index = 0; index < root.Count; index++) result[index] = root[index];
                result[root.Count] = "tutorialMap";
                result[root.Count + 1] = command.value;
                Array.Copy(fields, 0, result, root.Count + 2, fields.Length);
                return result;
            }

            builder.AddInteger(EntryPath("version"), command.tutorialVersion);
            builder.AddString(EntryPath("status"), status.ToString());
            builder.AddString(EntryPath("currentStepId"), command.tutorialStepId);
            builder.AddInteger(EntryPath("triggerRecordedAt"),
                Math.Max(0, command.tutorialTriggerRecordedAtUnixSeconds));
            builder.AddInteger(EntryPath("completedAt"),
                Math.Max(0, command.tutorialCompletedAtUnixSeconds));
            builder.AddBoolean(EntryPath("rewardClaimed"), command.tutorialRewardClaimed);
            builder.AddString(EntryPath("lastTransactionId"),
                command.tutorialLastTransactionId);
            builder.AddString(EntryPath("lastOperationId"), command.operationId);
            builder.AddBoolean(EntryPath("legacyPlayer"), command.tutorialLegacyPlayer);
            builder.AddString(EntryPath("guidedEncounterId"),
                command.tutorialGuidedEncounterId);
            builder.AddString(EntryPath("firstAttemptOutcome"),
                command.tutorialFirstAttemptOutcome);
            builder.AddString(EntryPath("variant"), command.tutorialVariant);
        }

        public static void ApplyToSnapshot(
            PlayerSnapshot player,
            PlayerLifecycleCommand command)
        {
            PlayerSnapshot.TutorialEntryData value = Find(player, command.value);
            if (value == null)
            {
                int length = player.tutorialEntries?.Length ?? 0;
                var entries = new PlayerSnapshot.TutorialEntryData[length + 1];
                if (length > 0) Array.Copy(player.tutorialEntries, entries, length);
                value = new PlayerSnapshot.TutorialEntryData { tutorialId = command.value };
                entries[length] = value;
                player.tutorialEntries = entries;
            }
            value.version = command.tutorialVersion;
            value.status = command.tutorialStatus;
            value.currentStepId = command.tutorialStepId;
            value.triggerRecordedAtUnixSeconds = command.tutorialTriggerRecordedAtUnixSeconds;
            value.completedAtUnixSeconds = command.tutorialCompletedAtUnixSeconds;
            value.rewardClaimed = command.tutorialRewardClaimed;
            value.lastTransactionId = command.tutorialLastTransactionId;
            value.lastOperationId = command.operationId;
            value.legacyPlayer = command.tutorialLegacyPlayer;
            value.guidedEncounterId = command.tutorialGuidedEncounterId;
            value.firstAttemptOutcome = command.tutorialFirstAttemptOutcome;
            value.variant = command.tutorialVariant;
        }

        public static PlayerSnapshot.TutorialEntryData Find(
            PlayerSnapshot player,
            string tutorialId)
        {
            if (player?.tutorialEntries == null || string.IsNullOrWhiteSpace(tutorialId))
                return null;
            for (int index = 0; index < player.tutorialEntries.Length; index++)
            {
                PlayerSnapshot.TutorialEntryData entry = player.tutorialEntries[index];
                if (entry != null && string.Equals(entry.tutorialId, tutorialId,
                    StringComparison.Ordinal))
                    return entry;
            }
            return null;
        }

        private static bool IsStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 80) return false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!char.IsLetterOrDigit(character) && character != '-' &&
                    character != '_' && character != '.') return false;
            }
            return true;
        }
    }

    // Explicit prototype adapter. Replaced by a trusted transport without changing presenters.
    public sealed class DirectFirestoreLifecycleCommands : IPlayerLifecycleCommands
    {
        private readonly FirestoreRestClient _client;
        public DirectFirestoreLifecycleCommands(GameApiSettings settings) => _client = new FirestoreRestClient(settings);
        public bool IsServerAuthoritative => false;
        public IEnumerator Execute(PlayerLifecycleCommand command, Action<PlayerSnapshot> succeeded, Action<FirestoreRestClient.Failure> failed) =>
            _client.ExecuteLifecycle(command, succeeded, failed);
    }
}
