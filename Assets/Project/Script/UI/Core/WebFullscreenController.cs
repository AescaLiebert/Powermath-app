using System.Runtime.InteropServices;
using UnityEngine;

namespace PowerMath.UI.Core
{
    public static class WebFullscreenController
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int PowerMathTryEnterLandscapeFullscreen();

        [DllImport("__Internal")]
        private static extern int PowerMathTryExitFullscreen();
#endif

        public static bool IsFullscreen => Screen.fullScreen;

        public static bool TrySetFullscreen(bool enabled)
        {
            if (Screen.fullScreen == enabled) return true;
#if UNITY_WEBGL && !UNITY_EDITOR
            return enabled
                ? PowerMathTryEnterLandscapeFullscreen() != 0
                : PowerMathTryExitFullscreen() != 0;
#else
            Screen.fullScreen = enabled;
            return true;
#endif
        }

        public static bool TryEnterLandscapeFullscreen()
        {
            if (Screen.fullScreen)
            {
                return false;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            return PowerMathTryEnterLandscapeFullscreen() != 0;
#else
            return false;
#endif
        }
    }
}
