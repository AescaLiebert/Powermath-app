using System;
using System.Collections;

namespace PowerMath.Session
{
    public sealed class DirectFirestorePlayerBootstrapService : IPlayerBootstrapService
    {
        private readonly FirestoreRestClient _client;

        public DirectFirestorePlayerBootstrapService(GameApiSettings settings)
        {
            _client = new FirestoreRestClient(settings);
        }

        public IEnumerator Fetch(
            Action<BootstrapResponse> onSuccess,
            Action<FirestoreRestClient.Failure> onFailure)
        {
            if (!DirectFirestoreCredentialStore.TryGet(out var credentials))
            {
                onFailure?.Invoke(new FirestoreRestClient.Failure(
                    FirestoreRestClient.FailureKind.AuthenticationRequired,
                    "Please sign in to continue."
                ));
                yield break;
            }

            FirestoreRestClient.AuthenticatedAccount? account = null;
            FirestoreRestClient.Failure? authenticationFailure = null;
            yield return _client.Authenticate(
                credentials.Username,
                credentials.Password,
                result => account = result,
                error => authenticationFailure = error
            );

            if (authenticationFailure.HasValue || !account.HasValue)
            {
                if (authenticationFailure?.Kind ==
                    FirestoreRestClient.FailureKind.InvalidCredentials)
                {
                    DirectFirestoreCredentialStore.Clear();
                    onFailure?.Invoke(new FirestoreRestClient.Failure(
                        FirestoreRestClient.FailureKind.AuthenticationRequired,
                        "Please sign in to continue."
                    ));
                }
                else
                {
                    onFailure?.Invoke(authenticationFailure ??
                        new FirestoreRestClient.Failure(
                            FirestoreRestClient.FailureKind.InvalidResponse,
                            "The account response could not be read."
                        ));
                }
                yield break;
            }

            yield return _client.CreateBootstrap(
                account.Value,
                credentials.Remembered,
                onSuccess,
                onFailure
            );
        }
    }
}
