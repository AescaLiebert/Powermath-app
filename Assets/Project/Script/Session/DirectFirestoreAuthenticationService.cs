using System;
using System.Collections;
using PowerMath.Bootstrap;
using PowerMath.PlayerData;
using UnityEngine;

namespace PowerMath.Session
{
    public sealed class DirectFirestoreAuthenticationService : IAuthenticationService
    {
        private readonly GameApiSettings _settings;
        private readonly FirestoreRestClient _client;

        public DirectFirestoreAuthenticationService(GameApiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _client = new FirestoreRestClient(settings);
        }

        public IEnumerator Login(
            string username,
            string password,
            bool rememberDevice,
            Action onSuccess,
            Action<FirestoreRestClient.Failure> onFailure)
        {
            if (_settings.EnableVersionCheck)
            {
                GameVersionManifest manifest = null;
                string manifestError = null;
                yield return GameVersionChecker.FetchManifest(
                    _settings.VersionManifestUrl,
                    _settings.RequestTimeoutSeconds,
                    value => manifest = value,
                    error => manifestError = error);

                VersionCompatibilityResult compatibility =
                    GameVersionChecker.EvaluateCompatibility(
                        manifest,
                        Application.version,
                        PlayerSessionStore.SupportedSchemaVersion,
                        out string statusMessage);
                if (compatibility != VersionCompatibilityResult.Compatible &&
                    compatibility != VersionCompatibilityResult.UpdateRecommended)
                {
                    onFailure?.Invoke(new FirestoreRestClient.Failure(
                        compatibility == VersionCompatibilityResult.NetworkError
                            ? FirestoreRestClient.FailureKind.Network
                            : FirestoreRestClient.FailureKind.IncompatibleClient,
                        string.IsNullOrWhiteSpace(statusMessage)
                            ? manifestError ?? "Release policy could not be verified."
                            : statusMessage));
                    yield break;
                }
            }

            FirestoreRestClient.AuthenticatedAccount? account = null;
            FirestoreRestClient.Failure? failure = null;

            yield return _client.Authenticate(
                username,
                password,
                result => account = result,
                error => failure = error
            );

            if (failure.HasValue || !account.HasValue)
            {
                onFailure?.Invoke(failure ?? new FirestoreRestClient.Failure(
                    FirestoreRestClient.FailureKind.InvalidResponse,
                    "The account response could not be read."
                ));
                yield break;
            }

            DirectFirestoreCredentialStore.Set(
                account.Value.Username,
                password,
                rememberDevice
            );
            onSuccess?.Invoke();
        }

        public IEnumerator Logout(
            Action onSuccess,
            Action<FirestoreRestClient.Failure> onFailure)
        {
            DirectFirestoreCredentialStore.Clear();
            yield return null;
            onSuccess?.Invoke();
        }
    }
}
