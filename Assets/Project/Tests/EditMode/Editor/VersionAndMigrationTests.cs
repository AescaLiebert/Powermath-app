using NUnit.Framework;
using PowerMath.Bootstrap;
using PowerMath.PlayerData;

namespace PowerMath.Tests.EditMode
{
    public sealed class VersionAndMigrationTests
    {
        [Test]
        public void VersionChecker_EvaluatesMaintenanceMode()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "1.0.0.0",
                minSupportedVersion = "1.0.0.0",
                schemaVersion = 1,
                maintenance = new GameVersionManifest.MaintenanceInfo
                {
                    isActive = true,
                    message = "Server is under maintenance."
                }
            };

            var result = GameVersionChecker.EvaluateCompatibility(manifest, "1.0.0.0", 1, out string message);
            Assert.That(result, Is.EqualTo(VersionCompatibilityResult.MaintenanceActive));
            Assert.That(message, Is.EqualTo("Server is under maintenance."));
        }

        [Test]
        public void VersionChecker_EvaluatesHardUpdateRequired()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "2.0.0.0",
                minSupportedVersion = "2.0.0.0",
                schemaVersion = 1,
                maintenance = new GameVersionManifest.MaintenanceInfo { isActive = false }
            };

            var result = GameVersionChecker.EvaluateCompatibility(manifest, "1.0.0.0", 1, out string message);
            Assert.That(result, Is.EqualTo(VersionCompatibilityResult.HardUpdateRequired));
            Assert.That(message, Contains.Substring("no longer supported"));
        }

        [Test]
        public void VersionChecker_EvaluatesUpdateRecommended()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "1.1.0.0",
                minSupportedVersion = "1.0.0.0",
                schemaVersion = 1,
                maintenance = new GameVersionManifest.MaintenanceInfo { isActive = false }
            };

            var result = GameVersionChecker.EvaluateCompatibility(manifest, "1.0.0.0", 1, out string message);
            Assert.That(result, Is.EqualTo(VersionCompatibilityResult.UpdateRecommended));
            Assert.That(message, Contains.Substring("newer version"));
        }

        [Test]
        public void VersionChecker_EvaluatesIncompatibleSchema()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "1.0.0.0",
                minSupportedVersion = "1.0.0.0",
                schemaVersion = 3,
                maintenance = new GameVersionManifest.MaintenanceInfo { isActive = false }
            };

            var result = GameVersionChecker.EvaluateCompatibility(manifest, "1.0.0.0", 1, out string message);
            Assert.That(result, Is.EqualTo(VersionCompatibilityResult.IncompatibleSchema));
        }

        [Test]
        public void VersionChecker_EvaluatesCompatible()
        {
            var manifest = new GameVersionManifest
            {
                clientVersion = "1.0.0.0",
                minSupportedVersion = "1.0.0.0",
                schemaVersion = 1,
                maintenance = new GameVersionManifest.MaintenanceInfo { isActive = false }
            };

            var result = GameVersionChecker.EvaluateCompatibility(manifest, "1.0.0.0", 1, out string message);
            Assert.That(result, Is.EqualTo(VersionCompatibilityResult.Compatible));
        }

        [Test]
        public void PlayerSchemaMigrator_EnsuresBaselineDefaultsOnEmptySnapshot()
        {
            var snapshot = new PlayerSnapshot
            {
                playerId = "student_test"
            };

            PlayerSchemaMigrator.EnsureBaselineDefaults(snapshot);

            Assert.That(snapshot.profile, Is.Not.Null);
            Assert.That(snapshot.profile.displayName, Is.EqualTo("student_test"));
            Assert.That(snapshot.progression, Is.Not.Null);
            Assert.That(snapshot.progression.currentStage, Is.EqualTo(1));
            Assert.That(snapshot.wallet, Is.Not.Null);
            Assert.That(snapshot.wallet.silver, Is.EqualTo(0));
            Assert.That(snapshot.activeRun, Is.Not.Null);
            Assert.That(snapshot.activeRun.currentStage, Is.EqualTo(1));
            Assert.That(snapshot.academic, Is.Not.Null);
            Assert.That(snapshot.academic.silver, Is.Not.Null);
        }

        [Test]
        public void PlayerSchemaMigrator_V1ToV2_AddsSafePresentationDefaults()
        {
            var snapshot = new PlayerSnapshot
            {
                playerId = "student_test",
                activeRun = new PlayerSnapshot.ActiveRunData
                {
                    runId = "run-a",
                    currentStage = 4,
                    phase = "EnemyReady"
                },
                lastRunSettlement = new PlayerSnapshot.RunSettlementData()
            };

            PlayerSnapshot migrated = PlayerSchemaMigrator.Migrate(snapshot, 1, 2);

            Assert.That(migrated.activeRun.pendingPresentation, Is.Null);
            Assert.That(migrated.lastRunSettlement.presentationStatus,
                Is.EqualTo("None"));
            Assert.That(PlayerSessionStore.SupportedSchemaVersion, Is.EqualTo(3));
        }
    }
}
