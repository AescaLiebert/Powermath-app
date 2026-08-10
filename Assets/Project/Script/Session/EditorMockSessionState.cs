#if UNITY_EDITOR
using UnityEngine;

namespace PowerMath.Session
{
    internal static class EditorMockSessionState
    {
        public static bool IsAuthenticated { get; set; }

        public static void Clear()
        {
            IsAuthenticated = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            Clear();
        }
    }
}
#endif
