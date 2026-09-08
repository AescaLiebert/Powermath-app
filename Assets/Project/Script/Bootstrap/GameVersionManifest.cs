using System;

namespace PowerMath.Bootstrap
{
    public enum GameFeature
    {
        BiomeMap,
        PlayerHub,
        PetGacha
    }

    [Serializable]
    public sealed class GameVersionManifest
    {
        public string clientVersion;
        public string minSupportedVersion;
        public int schemaVersion = 1;
        public ContentVersions contentVersions;
        public FeatureFlags features;
        public MaintenanceInfo maintenance;
        public string forceReloadUrl;

        [Serializable]
        public sealed class ContentVersions
        {
            public string questionCatalog;
            public string stageMap;
            public string petGacha;
        }

        [Serializable]
        public sealed class FeatureFlags
        {
            public bool biomeMap;
            public bool playerHub;
            public bool petGacha;
        }

        [Serializable]
        public sealed class MaintenanceInfo
        {
            public bool isActive;
            public string message;
        }
    }

    public enum VersionCompatibilityResult
    {
        Compatible,
        UpdateRecommended,
        HardUpdateRequired,
        IncompatibleSchema,
        MaintenanceActive,
        NetworkError
    }
}
