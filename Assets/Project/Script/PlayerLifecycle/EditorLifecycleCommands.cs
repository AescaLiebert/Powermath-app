#if UNITY_EDITOR
using System;
using System.Collections;
using PowerMath.PlayerData;
using PowerMath.Session;

namespace PowerMath.PlayerLifecycle
{
    public sealed class EditorLifecycleCommands : IPlayerLifecycleCommands
    {
        public bool IsServerAuthoritative => false;
        public IEnumerator Execute(PlayerLifecycleCommand command, Action<PlayerSnapshot> succeeded, Action<FirestoreRestClient.Failure> failed)
        {
            var player = PlayerSessionStore.Instance.Snapshot;
            try
            {
                var plan = PlayerLifecyclePolicy.Plan(player, "editor", command);
                if (!plan.IsEmpty)
                {
                    switch (command.kind)
                    {
                        case PlayerLifecycleCommandKind.SetLocale: player.preferences.locale = command.value; break;
                        case PlayerLifecycleCommandKind.CompleteOpening:
                            player.onboarding.phase = "character"; player.onboarding.openingCheckpointId = "complete"; break;
                        case PlayerLifecycleCommandKind.SelectCharacter:
                            player.onboarding.phase = "name"; player.onboarding.selectedCharacterId = command.value; break;
                        case PlayerLifecycleCommandKind.CompletePreparation:
                            player.profile.characterId = command.value; player.profile.displayName = command.displayName;
                            player.onboarding.phase = "complete"; player.onboarding.completionOperationId = command.operationId; break;
                    }
                    player.schemaVersion = PlayerSchemaMigrator.CurrentSchemaVersion;
                    player.revision++;
                }
                succeeded?.Invoke(player);
            }
            catch (Exception ex) { failed?.Invoke(new FirestoreRestClient.Failure(FirestoreRestClient.FailureKind.InvalidResponse, ex.Message)); }
            yield break;
        }
    }
}
#endif
