using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PowerMath.Bootstrap
{
    public sealed class SceneFlowController : MonoBehaviour
    {
        private bool _isLoading;

        public bool TryLoadScene(
            string sceneName,
            Action<string> onFailure,
            Action onLoaded = null)
        {
            if (_isLoading)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                onFailure?.Invoke("The next scene is not configured.");
                return false;
            }

            _isLoading = true;
            StartCoroutine(LoadScene(sceneName, onFailure, onLoaded));
            return true;
        }

        private IEnumerator LoadScene(
            string sceneName,
            Action<string> onFailure,
            Action onLoaded)
        {
            AsyncOperation operation;

            try
            {
                operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception)
            {
                _isLoading = false;
                onFailure?.Invoke("The next scene could not be opened.");
                yield break;
            }

            if (operation == null)
            {
                _isLoading = false;
                onFailure?.Invoke("The next scene could not be opened.");
                yield break;
            }

            yield return operation;
            _isLoading = false;
            onLoaded?.Invoke();
        }
    }
}
