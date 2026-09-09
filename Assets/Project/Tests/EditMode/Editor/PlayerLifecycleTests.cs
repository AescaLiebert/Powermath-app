using System;
using NUnit.Framework;
using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.Bootstrap;

namespace PowerMath.Tests.EditMode
{
    public sealed class PlayerLifecycleTests
    {
        private static JsonValue Student(string fields)
        {
            Assert.IsTrue(FirestoreJsonNavigator.TryParse("{\"mapValue\":{\"fields\":{" + fields + "}}}", out var value, out _));
            return value;
        }
        [Test]
        public void MissingGameDataStartsOpeningButExistingDataDoesNot()
        {
            var fresh = PlayerDefaultsPlanner.Plan(Student(""), "student").ToJson();
            var legacy = PlayerDefaultsPlanner.Plan(Student("\"gamedata\":{\"mapValue\":{\"fields\":{}}}"), "student").ToJson();
            StringAssert.Contains("\"stringValue\":\"opening\"", fresh);
            StringAssert.Contains("\"stringValue\":\"character\"", legacy);
            StringAssert.Contains("\"schemaVersion\"", fresh);
        }
        [Test]
        public void FutureAndMalformedSavesAreRejectedBeforePlanning()
        {
            Assert.Throws<NotSupportedException>(() => PlayerDefaultsPlanner.Plan(
                Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"schemaVersion\":{\"integerValue\":\"999\"}}}}"), "student"));
            Assert.Throws<FormatException>(() => PlayerDefaultsPlanner.Plan(Student("\"gamedata\":{\"nullValue\":null}"), "student"));
            Assert.Throws<FormatException>(() => PlayerDefaultsPlanner.Plan(
                Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"wallet\":{\"stringValue\":\"broken\"}}}}"), "student"));
        }
        [Test]
        public void MigrationPreservesPendingReceiptAndRejectsUnsupportedTargets()
        {
            var receipt = new PlayerSnapshot.AttemptPresentationData { attemptId = "attempt" };
            var player = new PlayerSnapshot { activeRun = new PlayerSnapshot.ActiveRunData { pendingPresentation = receipt } };
            PlayerSchemaMigrator.Migrate(player, 1, 3);
            Assert.AreSame(receipt, player.activeRun.pendingPresentation);
            Assert.Throws<NotSupportedException>(() => PlayerSchemaMigrator.Migrate(player, 3, 2));
            Assert.Throws<NotSupportedException>(() => PlayerSchemaMigrator.Migrate(player, 4, 4));
        }
        [Test]
        public void CompletionRetriesDoNotGrantOrResetAnything()
        {
            var player = Ready();
            var command = Command(player);
            var plan = PlayerLifecyclePolicy.Plan(player, "student", command);
            foreach (var path in plan.FieldPaths)
            {
                StringAssert.DoesNotContain("wallet", path);
                StringAssert.DoesNotContain("activeRun", path);
                StringAssert.DoesNotContain("displayNameChangedAt", path);
            }
            player.onboarding.phase = "complete";
            player.onboarding.completionOperationId = command.operationId;
            player.profile.characterId = command.value;
            player.profile.displayName = command.displayName;
            player.revision++;
            Assert.IsTrue(PlayerLifecyclePolicy.Plan(player, "student", command).IsEmpty);
            command.value = "ricko";
            Assert.Throws<InvalidOperationException>(() => PlayerLifecyclePolicy.Plan(player, "student", command));
        }
        [Test]
        public void ConflictAndAccountSwitchCannotOverwritePreparation()
        {
            var player = Ready();
            var command = Command(player);
            command.expectedRevision--;
            Assert.Throws<InvalidOperationException>(() => PlayerLifecyclePolicy.Plan(player, "student", command));
            command.playerId = "different";
            Assert.Throws<ArgumentException>(() => PlayerLifecyclePolicy.Plan(player, "student", command));
        }
        [TestCase("ผู้กล้า", true)]
        [TestCase("12345678901234567890", true)]
        [TestCase("123456789012345678901", false)]
        [TestCase("   ", false)]
        [TestCase("<b>Name</b>", false)]
        [TestCase("Name\nOther", false)]
        [TestCase("bad_fuck", false)]
        [TestCase("สัส", false)]
        public void DisplayNamesSupportThaiWithoutMarkup(string value, bool valid) =>
            Assert.AreEqual(valid, PlayerLifecyclePolicy.TryNormalizeName(value, out _));

        [Test]
        public void InvalidReleasePolicyFailsClosed()
        {
            Assert.AreEqual(VersionCompatibilityResult.NetworkError,
                GameVersionChecker.EvaluateCompatibility(null, "1.0", 3, out _));
            Assert.AreEqual(VersionCompatibilityResult.NetworkError,
                GameVersionChecker.EvaluateCompatibility(new GameVersionManifest(), "1.0", 3, out _));
        }
        [Test]
        public void DefaultsDoNotOverwriteExistingWalletOrUnknownFeatureData()
        {
            var student = Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"wallet\":{\"mapValue\":{\"fields\":{\"silver\":{\"integerValue\":\"90\"}}}},\"futureFeature\":{\"stringValue\":\"keep\"}}}}");
            var plan = PlayerDefaultsPlanner.Plan(student, "student");
            foreach (string path in plan.FieldPaths)
            {
                StringAssert.DoesNotContain("wallet.silver", path);
                StringAssert.DoesNotContain("futureFeature", path);
            }
            Assert.Throws<FormatException>(() => PlayerDefaultsPlanner.Plan(Student(
                "\"gamedata\":{\"mapValue\":{\"fields\":{\"wallet\":{\"mapValue\":{\"fields\":{\"silver\":{\"stringValue\":\"bad\"}}}}}}}"), "student"));
        }

        [Test]
        public void CharacterPresentationBinding_DoesNotApplyToBackgroundGameObject()
        {
            var go = new UnityEngine.GameObject("bg", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image));
            try
            {
                var controller = go.AddComponent<PowerMath.Gameplay.Combat.Unity.ActorPresentationController>();
                controller.Initialize(PowerMath.Gameplay.Combat.Presentation.PresentationActor.Player, true);
                PowerMath.PlayerLifecycle.CharacterPresentationBinding.ApplyToPlayerActor(controller, "ricko");
                var image = go.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNull(image.sprite);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static PlayerSnapshot Ready() => new PlayerSnapshot
        {
            playerId = "level1:student", revision = 10,
            profile = new PlayerSnapshot.ProfileData(),
            onboarding = new PlayerSnapshot.OnboardingData { phase = "name", selectedCharacterId = "stellar" }
        };
        private static PlayerLifecycleCommand Command(PlayerSnapshot player) => new PlayerLifecycleCommand
        {
            kind = PlayerLifecycleCommandKind.CompletePreparation, playerId = player.playerId,
            expectedRevision = player.revision, operationId = Guid.NewGuid().ToString("N"),
            value = "stellar", displayName = "ผู้กล้า"
        };
    }
}
