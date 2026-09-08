using System;
using System.Collections;
using PowerMath.Bootstrap;
using PowerMath.PlayerData;
using UnityEngine;

namespace PowerMath.Session
{
    public sealed class ReleaseCheckedLifecycleCommands : IPlayerLifecycleCommands
    {
        private readonly GameApiSettings _settings;
        private readonly IPlayerLifecycleCommands _inner;
        public ReleaseCheckedLifecycleCommands(GameApiSettings settings, IPlayerLifecycleCommands inner)
        { _settings = settings; _inner = inner; }
        public bool IsServerAuthoritative => _inner.IsServerAuthoritative;
        public IEnumerator Execute(PlayerLifecycleCommand command, Action<PlayerSnapshot> succeeded, Action<FirestoreRestClient.Failure> failed)
        {
            if (_settings.EnableVersionCheck)
            {
                GameVersionManifest manifest = null;
                yield return GameVersionChecker.FetchManifest(_settings.VersionManifestUrl, _settings.RequestTimeoutSeconds, value => manifest = value, _ => { });
                var result = GameVersionChecker.EvaluateCompatibility(manifest, Application.version, PlayerSessionStore.SupportedSchemaVersion, out string message);
                if (result != VersionCompatibilityResult.Compatible && result != VersionCompatibilityResult.UpdateRecommended)
                {
                    failed?.Invoke(new FirestoreRestClient.Failure(
                        result == VersionCompatibilityResult.NetworkError ? FirestoreRestClient.FailureKind.Network : FirestoreRestClient.FailureKind.IncompatibleClient,
                        message));
                    yield break;
                }
            }
            yield return _inner.Execute(command, succeeded, failed);
        }
    }
}
