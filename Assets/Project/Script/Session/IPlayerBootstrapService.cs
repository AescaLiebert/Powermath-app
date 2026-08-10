using System;
using System.Collections;

namespace PowerMath.Session
{
    public interface IPlayerBootstrapService
    {
        IEnumerator Fetch(
            Action<BootstrapResponse> onSuccess,
            Action<FirestoreRestClient.Failure> onFailure);
    }
}
