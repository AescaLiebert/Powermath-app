using System;
using PowerMath.PlayerData;

namespace PowerMath.Session
{
    [Serializable]
    public sealed class BootstrapResponse
    {
        public int schemaVersion;
        public bool remembered;
        public PlayerSnapshot player;
        public string serverTimeUtc;
    }
}
