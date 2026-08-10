using System;
using System.Collections;

namespace PowerMath.Session
{
    public sealed class DirectFirestoreAuthenticationService : IAuthenticationService
    {
        private readonly FirestoreRestClient _client;

        public DirectFirestoreAuthenticationService(GameApiSettings settings)
        {
            _client = new FirestoreRestClient(settings);
        }

        public IEnumerator Login(
            string username,
            string password,
            bool rememberDevice,
            Action onSuccess,
            Action<FirestoreRestClient.Failure> onFailure)
        {
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
