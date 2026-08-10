using System;
using System.Collections;

namespace PowerMath.Session
{
    public interface IAuthenticationService
    {
        IEnumerator Login(
            string username,
            string password,
            bool rememberDevice,
            Action onSuccess,
            Action<FirestoreRestClient.Failure> onFailure);

        IEnumerator Logout(
            Action onSuccess,
            Action<FirestoreRestClient.Failure> onFailure);
    }
}
