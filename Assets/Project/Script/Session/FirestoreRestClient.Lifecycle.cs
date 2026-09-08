using System;
using System.Collections;
using PowerMath.PlayerData;
using UnityEngine.Networking;

namespace PowerMath.Session
{
    public sealed partial class FirestoreRestClient
    {
        public IEnumerator ExecuteLifecycle(PlayerLifecycleCommand command, Action<PlayerSnapshot> succeeded, Action<Failure> failed)
        {
            if (!DirectFirestoreCredentialStore.TryGet(out var credentials))
            {
                failed?.Invoke(new Failure(FailureKind.AuthenticationRequired, "Please sign in to continue."));
                yield break;
            }
            AuthenticatedAccount? account = null;
            yield return Authenticate(credentials.Username, credentials.Password, value => account = value, failed);
            if (!account.HasValue) yield break;
            if (account.Value.Player.playerId != command.playerId)
            {
                failed?.Invoke(new Failure(FailureKind.AuthenticationRequired, "The signed-in account changed."));
                yield break;
            }
            for (int index = 0; index < _settings.LevelDocumentCount; index++)
            {
                if (!_settings.TryGetLevelDocument(index, out string level, out string grade, out string url) ||
                    level != account.Value.LevelDocumentId) continue;
                JsonValue document = null;
                using (var get = CreateGetRequest(url))
                {
                    yield return get.SendWebRequest();
                    if (!TryHandleTransport(get, failed)) yield break;
                    if (!TryParseDocument(get.downloadHandler.text, out document))
                    { failed?.Invoke(InvalidResponseFailure()); yield break; }
                }
                if (!TryGetStudent(document, credentials.Username, out var student) ||
                    !CredentialsMatch(student, credentials.Username, credentials.Password))
                { failed?.Invoke(InvalidCredentialsFailure()); yield break; }

                FirestorePatchPlan plan = null;
                PlayerSnapshot current = null;
                Failure? validationFailure = null;
                try
                {
                    PlayerSaveContract.Inspect(student, out bool isNew);
                    if (isNew || !TryMapPlayer(credentials.Username, level, grade, student, out current))
                        throw new FormatException();
                    plan = PlayerLifecyclePolicy.Plan(current, credentials.Username, command);
                }
                catch (NotSupportedException) { validationFailure = new Failure(FailureKind.IncompatibleClient, "A game update or content configuration is required."); }
                catch (InvalidOperationException ex) { validationFailure = new Failure(FailureKind.Conflict, ex.Message); }
                catch (ArgumentException ex) { validationFailure = new Failure(FailureKind.InvalidResponse, ex.Message); }
                catch (FormatException) { validationFailure = InvalidResponseFailure(); }
                if (validationFailure.HasValue) { failed?.Invoke(validationFailure.Value); yield break; }
                if (!DirectFirestoreCredentialStore.TryGet(out var active) ||
                    active.Username != credentials.Username || active.Password != credentials.Password)
                { failed?.Invoke(new Failure(FailureKind.AuthenticationRequired, "The signed-in account changed.")); yield break; }
                if (plan.IsEmpty) { succeeded?.Invoke(current); yield break; }
                string updateTime = ReadRawStringProperty(document, "updateTime");
                if (string.IsNullOrWhiteSpace(updateTime)) { failed?.Invoke(InvalidResponseFailure()); yield break; }
                using (var patch = CreatePatchRequest(url, plan, updateTime))
                {
                    yield return patch.SendWebRequest();
                    if (patch.responseCode == 409 || patch.responseCode == 412)
                    { failed?.Invoke(new Failure(FailureKind.Conflict, "Player data changed. Please retry after reloading.")); yield break; }
                    if (!TryHandleTransport(patch, failed)) yield break;
                    if (!TryParseDocument(patch.downloadHandler.text, out var saved) ||
                        !TryGetStudent(saved, credentials.Username, out var savedStudent) ||
                        !TryMapPlayer(credentials.Username, level, grade, savedStudent, out var player))
                    { failed?.Invoke(InvalidResponseFailure()); yield break; }
                    succeeded?.Invoke(player);
                    yield break;
                }
            }
            failed?.Invoke(ConfigurationFailure());
        }
    }
}

