using UnityEngine;

namespace PowerMath.PlayerData
{
    [DisallowMultipleComponent]
    public sealed class ProfileActivityTracker : MonoBehaviour
    {
        private double _pendingSeconds;
        private bool _focused = true;
        private bool _paused;

        public long PendingWholeSeconds => (long)_pendingSeconds;

        private void Update()
        {
            if (_focused && !_paused)
                _pendingSeconds += Time.unscaledDeltaTime;
        }

        private void OnApplicationFocus(bool focused) { _focused = focused; }
        private void OnApplicationPause(bool paused) { _paused = paused; }

        public void Commit(long seconds)
        {
            if (seconds <= 0) return;
            _pendingSeconds = System.Math.Max(0d, _pendingSeconds - seconds);
        }
    }
}
