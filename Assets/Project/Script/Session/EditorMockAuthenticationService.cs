#if UNITY_EDITOR
using System;
using System.Collections;

namespace PowerMath.Session
{
    public sealed class EditorMockAuthenticationService : IAuthenticationService
    {
        public const string SampleUsername = "sample-student";
        public const string SamplePassword = "123456";

        public IEnumerator Login(
            string username,
            string password,
            bool rememberDevice,
            Action onSuccess,
            Action<FirestoreRestClient.Failure> onFailure)
        {
            yield return null;

            bool validUsername = string.Equals(
                username?.Trim(),
                SampleUsername,
                StringComparison.OrdinalIgnoreCase
            );
            bool validPassword = string.Equals(
                password,
                SamplePassword,
                StringComparison.Ordinal
            );

            if (!validUsername || !validPassword)
            {
                onFailure?.Invoke(new FirestoreRestClient.Failure(
                    FirestoreRestClient.FailureKind.InvalidCredentials,
                    "The username or six-digit password is incorrect."
                ));
                yield break;
            }

            EditorMockSessionState.IsAuthenticated = true;
            onSuccess?.Invoke();
        }

        public IEnumerator Logout(
            Action onSuccess,
            Action<FirestoreRestClient.Failure> onFailure)
        {
            yield return null;
            EditorMockSessionState.Clear();
            onSuccess?.Invoke();
        }
    }
}
#endif
