using System;
using PowerMath.Bootstrap;
using PowerMath.Session;
using UnityEngine;

namespace PowerMath.PlayerData
{
    public sealed class PlayerSessionStore : MonoBehaviour
    {
        public enum HydrationResult
        {
            Success,
            IncompatibleSchema,
            InvalidPlayer
        }

        public const int SupportedSchemaVersion = PlayerSchemaMigrator.CurrentSchemaVersion;

        public static PlayerSessionStore Instance { get; private set; }

        public event Action<PlayerSnapshot> Changed;

        public bool IsReady { get; private set; }
        public bool IsRemembered { get; private set; }
        public PlayerSnapshot Snapshot { get; private set; }
        public GameVersionManifest VersionManifest { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public HydrationResult TryHydrate(BootstrapResponse response)
        {
            if (response == null || response.schemaVersion < 0 || response.schemaVersion > SupportedSchemaVersion)
            {
                return HydrationResult.IncompatibleSchema;
            }

            if (response.player == null ||
                string.IsNullOrWhiteSpace(response.player.playerId) ||
                response.player.profile == null ||
                string.IsNullOrWhiteSpace(response.player.profile.displayName))
            {
                return HydrationResult.InvalidPlayer;
            }

            if (response.schemaVersion < SupportedSchemaVersion)
            {
                response.player = PlayerSchemaMigrator.Migrate(response.player, response.schemaVersion, SupportedSchemaVersion);
                response.schemaVersion = SupportedSchemaVersion;
            }

            response.player.schemaVersion = response.schemaVersion;
            PlayerSchemaMigrator.EnsureBaselineDefaults(response.player);
            PowerMath.PlayerLifecycle.PlayerLifecycleRuntime.ApplyAccountLocale(response.player);
            IsRemembered = response.remembered;
            Snapshot = response.player;
            IsReady = true;
            Changed?.Invoke(Snapshot);

            return HydrationResult.Success;
        }

        public void Clear()
        {
            IsRemembered = false;
            Snapshot = null;
            IsReady = false;
            Changed?.Invoke(null);
        }

        public void SetVersionManifest(GameVersionManifest manifest)
        {
            VersionManifest = manifest;
        }

        public void NotifyAuthoritativeUpdate()
        {
            if (IsReady && Snapshot != null) Changed?.Invoke(Snapshot);
        }
    }
}
