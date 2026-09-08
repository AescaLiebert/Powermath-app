using System.Runtime.InteropServices;
using UnityEngine;

namespace PowerMath.Bootstrap
{
    public static class WebCacheBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PowerMathPurgeCacheAndReload(string targetVersion);

        [DllImport("__Internal")]
        private static extern void PowerMathHardReload();
#endif

        public static void PurgeCacheAndReload(string targetVersion = "update")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathPurgeCacheAndReload(targetVersion);
#else
            PowerMath.Diagnostics.AppLog.Info("WebCache", "PurgeCacheAndReload requested (Editor/Standalone mock).");
#endif
        }

        public static void HardReload()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PowerMathHardReload();
#else
            PowerMath.Diagnostics.AppLog.Info("WebCache", "HardReload requested (Editor/Standalone mock).");
#endif
        }
    }
}
