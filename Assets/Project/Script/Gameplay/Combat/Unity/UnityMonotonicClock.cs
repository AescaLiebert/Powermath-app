namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class UnityMonotonicClock : IMonotonicClock
    {
        public double NowSeconds => UnityEngine.Time.realtimeSinceStartupAsDouble;
    }
}
