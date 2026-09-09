using System;
using System.Collections;
using System.Globalization;
using System.Text;
using PowerMath.PlayerData;

namespace PowerMath.Session
{
    public enum PlayerLifecycleCommandKind { SetLocale, CompleteOpening, SelectCharacter, CompletePreparation, AdvanceTutorial }

    public sealed class PlayerLifecycleCommand
    {
        public PlayerLifecycleCommandKind kind;
        public string operationId;
        public string playerId;
        public long expectedRevision;
        public string value;
        public string displayName;
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

        public static bool TryNormalizeName(string input, out string name)
        {
            name = (input ?? string.Empty).Normalize(NormalizationForm.FormC).Trim();
            if (string.IsNullOrWhiteSpace(name)) return false;
            // Shared with the 20-character profile layouts; text elements keep Thai input intact.
            if (new StringInfo(name).LengthInTextElements >
                MaximumDisplayNameLength) return false;
            foreach (char c in name)
                if (char.IsControl(c) || c == '<' || c == '>') return false;
            return true;
        }

        public static FirestorePatchPlan Plan(PlayerSnapshot player, string username, PlayerLifecycleCommand command)
        {
            if (player == null || command == null || player.playerId != command.playerId ||
                !Guid.TryParseExact(command.operationId, "N", out _))
                throw new ArgumentException("Invalid player operation.");
            var builder = new FirestorePatchDocumentBuilder();
            string[] Path(params string[] parts)
            {
                var result = new string[parts.Length + 2];
                result[0] = username; result[1] = "gamedata";
                Array.Copy(parts, 0, result, 2, parts.Length);
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
                PlayerLifecycleCommandKind.AdvanceTutorial => player.tutorial?.checkpointId == command.value,
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
                    // Authored steps must be supplied by a catalog before this command is enabled.
                    throw new NotSupportedException("Tutorial steps have not been configured.");
                default: throw new ArgumentException("Unsupported operation.");
            }
            builder.AddInteger(Path("revision"), checked(player.revision + 1));
            return builder.Build();
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
