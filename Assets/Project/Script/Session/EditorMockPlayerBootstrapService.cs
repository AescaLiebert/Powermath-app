#if UNITY_EDITOR
using System;
using System.Collections;
using PowerMath.Bootstrap;

namespace PowerMath.Session
{
    public sealed class EditorMockPlayerBootstrapService : IPlayerBootstrapService
    {
        public IEnumerator Fetch(
            Action<BootstrapResponse> onSuccess,
            Action<FirestoreRestClient.Failure> onFailure)
        {
            yield return null;

            if (!EditorMockSessionState.IsAuthenticated)
            {
                onFailure?.Invoke(new FirestoreRestClient.Failure(
                    FirestoreRestClient.FailureKind.AuthenticationRequired,
                    "Please sign in to continue."
                ));
                yield break;
            }

            onSuccess?.Invoke(EditorSampleStudentFactory.CreateResponse());
        }
    }
}
#endif
