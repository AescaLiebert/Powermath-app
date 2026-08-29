using System.Runtime.InteropServices;
using UnityEngine;

namespace PowerMath.Bootstrap
{
    public static class WebCacheBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PowerMathPurgeCacheAndReload();

        [DllImport("__Internal")]
        private static extern void PowerMathHardReload();
#endif

        public static void PurgeCacheAndReload()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathPurgeCacheAndReload();
#else
            Debug.Log("[WebCacheBridge] PurgeCacheAndReload requested (Editor/Standalone mock).");
#endif
        }

        public static void HardReload()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathHardReload();
#else
            Debug.Log("[WebCacheBridge] HardReload requested (Editor/Standalone mock).");
#endif
        }
    }
}
