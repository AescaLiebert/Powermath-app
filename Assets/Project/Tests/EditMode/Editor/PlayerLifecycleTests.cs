using System;
using System.Collections.Generic;
using NUnit.Framework;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.Gameplay.Tutorial;
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
        public void EmptyMapInFirestoreDoesNotFailValidation()
        {
            var student = Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"tutorialMap\":{\"mapValue\":{}}}}}");
            Assert.DoesNotThrow(() => PlayerDefaultsPlanner.Plan(student, "student"));
        }

        [Test]
        public void NewUserWithEmptyTutorialMapMapsSafely()
        {
            var student = Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"tutorialMap\":{\"mapValue\":{}},\"schemaVersion\":{\"integerValue\":\"5\"}}}}");
            Assert.DoesNotThrow(() => PlayerDefaultsPlanner.Plan(student, "newuser"));
            bool mapped = FirestoreRestClient.TryMapPlayer("newuser", "level-1", "Grade 4-6", student, out var player);
            Assert.IsTrue(mapped);
            Assert.IsNotNull(player);
            Assert.IsNotNull(player.tutorialEntries);
            Assert.AreEqual(0, player.tutorialEntries.Length);
        }

        [Test]
        public void PopulatedTutorialMapMapsCorrectly()
        {
            var student = Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"schemaVersion\":{\"integerValue\":\"5\"},\"tutorialMap\":{\"mapValue\":{\"fields\":{\"OnFirstCreate\":{\"mapValue\":{\"fields\":{\"version\":{\"integerValue\":\"1\"},\"status\":{\"stringValue\":\"Active\"},\"currentStepId\":{\"stringValue\":\"step-1\"}}}}}}}}}}");
            bool mapped = FirestoreRestClient.TryMapPlayer("user1", "level-1", "Grade 4-6", student, out var player);
            Assert.IsTrue(mapped);
            Assert.IsNotNull(player);
            Assert.IsNotNull(player.tutorialEntries);
            Assert.AreEqual(1, player.tutorialEntries.Length);
            Assert.AreEqual("OnFirstCreate", player.tutorialEntries[0].tutorialId);
            Assert.AreEqual(1, player.tutorialEntries[0].version);
            Assert.AreEqual("Active", player.tutorialEntries[0].status);
            Assert.AreEqual("step-1", player.tutorialEntries[0].currentStepId);
        }

        [Test]
        public void GameplayReceiptsAndCountersRoundTripFromFirestore()
        {
            string challengeQuestions = FirestoreMap(string.Join(",", new[]
            {
                FirestoreField("silverCursor", FirestoreInteger(1)),
                FirestoreField("goldCursor", FirestoreInteger(2)),
                FirestoreField("diamondCursor", FirestoreInteger(3)),
                FirestoreField("reservedDocumentId", FirestoreString("challenge-gold")),
                FirestoreField("reservedQuestionId", FirestoreString("cg4"))
            }));
            string activeRun = FirestoreMap(string.Join(",", new[]
            {
                FirestoreField("questionContentId", FirestoreString("challenge-7")),
                FirestoreField("eventScheduleVersion", FirestoreInteger(1)),
                FirestoreField("eventScheduleCatalogVersion", FirestoreString("events-v1")),
                FirestoreField("eventScheduleEventId", FirestoreString("event-a")),
                FirestoreField("eventScheduleStages", FirestoreArray(
                    FirestoreInteger(4), FirestoreInteger(9))),
                FirestoreField("eventChanceBasisPoints", FirestoreInteger(1250)),
                FirestoreField("petEventMultiplierBasisPoints", FirestoreInteger(15000)),
                FirestoreField("challengeQuestions", challengeQuestions),
                FirestoreField("lastChallengeRewardAttemptId", FirestoreString("attempt-7")),
                FirestoreField("lastChallengeRewardPowerCoins", FirestoreInteger(10)),
                FirestoreField("lastChallengeRewardResultingPowerCoins", FirestoreInteger(90)),
                FirestoreField("stageAttackCount", FirestoreInteger(2)),
                FirestoreField("bigBossesDefeated", FirestoreInteger(1)),
                FirestoreField("pendingPetFollowUpDamage", FirestoreInteger(44))
            }));
            string gachaResult = FirestoreMap(string.Join(",", new[]
            {
                FirestoreField("petId", FirestoreString("sapphire")),
                FirestoreField("rarityId", FirestoreString("ssr")),
                FirestoreField("wasNew", FirestoreBoolean(true)),
                FirestoreField("previousCount", FirestoreInteger(0)),
                FirestoreField("resultingCount", FirestoreInteger(1))
            }));
            string economy = FirestoreMap(FirestoreField(
                "lastPetGachaResults", FirestoreArray(gachaResult)));
            string academic = FirestoreMap(FirestoreField(
                "auditCorrectCount", FirestoreInteger(3)));
            string gameData = FirestoreMap(string.Join(",", new[]
            {
                FirestoreField("schemaVersion", FirestoreInteger(6)),
                FirestoreField("activeRun", activeRun),
                FirestoreField("economy", economy),
                FirestoreField("academic", academic)
            }));

            bool mapped = FirestoreRestClient.TryMapPlayer(
                "student", "level-1", "Grade 4", Student(
                    FirestoreField("gamedata", gameData)), out PlayerSnapshot player);

            Assert.IsTrue(mapped);
            Assert.AreEqual("challenge-7", player.activeRun.questionContentId);
            Assert.AreEqual(1, player.activeRun.eventScheduleVersion);
            Assert.AreEqual("events-v1", player.activeRun.eventScheduleCatalogVersion);
            Assert.AreEqual("event-a", player.activeRun.eventScheduleEventId);
            CollectionAssert.AreEqual(new[] { 4, 9 }, player.activeRun.eventScheduleStages);
            Assert.AreEqual(1250, player.activeRun.eventChanceBasisPoints);
            Assert.AreEqual(15000, player.activeRun.petEventMultiplierBasisPoints);
            Assert.AreEqual(2, player.activeRun.challengeQuestions.goldCursor);
            Assert.AreEqual("challenge-gold", player.activeRun.challengeQuestions.reservedDocumentId);
            Assert.AreEqual("cg4", player.activeRun.challengeQuestions.reservedQuestionId);
            Assert.AreEqual("attempt-7", player.activeRun.lastChallengeRewardAttemptId);
            Assert.AreEqual(10, player.activeRun.lastChallengeRewardPowerCoins);
            Assert.AreEqual(90, player.activeRun.lastChallengeRewardResultingPowerCoins);
            Assert.AreEqual(2, player.activeRun.stageAttackCount);
            Assert.AreEqual(1, player.activeRun.bigBossesDefeated);
            Assert.AreEqual(44, player.activeRun.pendingPetFollowUpDamage);
            Assert.AreEqual(3, player.academic.auditCorrectCount);
            Assert.AreEqual(1, player.economy.lastPetGachaResults.Length);
            Assert.AreEqual("sapphire", player.economy.lastPetGachaResults[0].petId);
            Assert.AreEqual("ssr", player.economy.lastPetGachaResults[0].rarityId);
            Assert.IsTrue(player.economy.lastPetGachaResults[0].wasNew);
            Assert.AreEqual(1, player.economy.lastPetGachaResults[0].resultingCount);
        }

        [Test]
        public void MigrationPreservesPendingReceiptAndRejectsUnsupportedTargets()
        {
            var receipt = new PlayerSnapshot.AttemptPresentationData { attemptId = "attempt" };
            var player = new PlayerSnapshot { activeRun = new PlayerSnapshot.ActiveRunData { pendingPresentation = receipt } };
            PlayerSchemaMigrator.Migrate(player, 1, PlayerSchemaMigrator.CurrentSchemaVersion);
            Assert.AreSame(receipt, player.activeRun.pendingPresentation);
            Assert.Throws<NotSupportedException>(() => PlayerSchemaMigrator.Migrate(player, 3, 2));
            int future = PlayerSchemaMigrator.CurrentSchemaVersion + 1;
            Assert.Throws<NotSupportedException>(() =>
                PlayerSchemaMigrator.Migrate(player, future, future));
        }

        [Test]
        public void PetEventMultiplier_UsesUniqueOwnedSsrPassivesOnly()
        {
            var ssr = new PetGachaPet("ssr-a", "SSR A", 1, 0d, 2500);
            var rare = new PetGachaPet("rare-a", "Rare A", 1, 0d, 9000);
            var catalog = new PetGachaCatalog("pets-v1", new[]
            {
                new PetGachaRarity("rare", "Rare", 9700,
                    new List<PetGachaPet> { rare }),
                new PetGachaRarity("ssr", "SSR", 300,
                    new List<PetGachaPet> { ssr })
            });
            var player = new PlayerSnapshot
            {
                inventory = new[]
                {
                    new PlayerSnapshot.InventoryItemData { itemId = "ssr-a", owned = true },
                    new PlayerSnapshot.InventoryItemData { itemId = "ssr-a", owned = true },
                    new PlayerSnapshot.InventoryItemData { itemId = "rare-a", owned = true }
                }
            };

            Assert.That(PetCollectionEventMultiplierPolicy.Calculate(player, catalog),
                Is.EqualTo(12500));
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
        public void InvalidReleasePolicyFailsOpen()
        {
            Assert.AreEqual(VersionCompatibilityResult.Compatible,
                GameVersionChecker.EvaluateCompatibility(null, "1.0", 3, out string missingMessage));
            StringAssert.Contains("continuing", missingMessage.ToLowerInvariant());

            Assert.AreEqual(VersionCompatibilityResult.Compatible,
                GameVersionChecker.EvaluateCompatibility(new GameVersionManifest(), "1.0", 3, out string invalidMessage));
            StringAssert.Contains("continuing", invalidMessage.ToLowerInvariant());
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
        public void NullStringFieldInPlayerDataIsRepairedSafely()
        {
            var student = Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"activeRun\":{\"mapValue\":{\"fields\":{\"committedAttemptId\":{\"nullValue\":null}}}}}}}");
            var plan = PlayerDefaultsPlanner.Plan(student, "student");
            bool containsAttemptId = false;
            foreach (string path in plan.FieldPaths)
            {
                if (path.IndexOf("activeRun.committedAttemptId", StringComparison.Ordinal) >= 0)
                {
                    containsAttemptId = true;
                    break;
                }
            }
            Assert.IsTrue(containsAttemptId);
        }

        [Test]
        public void TutorialMigrationAndDefaultsCreateAnEmptyDynamicMap()
        {
            var player = new PlayerSnapshot();
            PlayerSchemaMigrator.Migrate(player, 4,
                PlayerSchemaMigrator.CurrentSchemaVersion);

            Assert.That(player.tutorialEntries, Is.Not.Null);
            Assert.That(player.tutorialEntries, Is.Empty);

            var plan = PlayerDefaultsPlanner.Plan(
                Student("\"gamedata\":{\"mapValue\":{\"fields\":{\"schemaVersion\":{\"integerValue\":\"4\"}}}}"),
                "student");
            Assert.That(plan.FieldPaths, Does.Contain("gamedata.tutorialMap"));
            StringAssert.Contains("\"tutorialMap\":{\"mapValue\":{\"fields\":{}}}",
                plan.ToJson());
        }

        [Test]
        public void V5CommittedNormalEncounter_RestoresItsPrematurelySpentCooldown()
        {
            var player = new PlayerSnapshot
            {
                activeRun = new PlayerSnapshot.ActiveRunData
                {
                    encounterKind = "NormalMonster",
                    phase = "Committed",
                    enemyRemainingCooldown = 1,
                    enemyMaximumCooldown = 3
                }
            };

            PlayerSchemaMigrator.Migrate(player, 5, 6);

            Assert.That(player.schemaVersion, Is.EqualTo(6));
            Assert.That(player.activeRun.enemyRemainingCooldown, Is.EqualTo(2));
        }

        [Test]
        public void V5CooldownCorrectionIsPersistedWithSchemaUpgrade()
        {
            string activeRun = FirestoreMap(string.Join(",", new[]
            {
                FirestoreField("encounterKind", FirestoreString("NormalMonster")),
                FirestoreField("phase", FirestoreString("Committed")),
                FirestoreField("enemyRemainingCooldown", FirestoreInteger(1)),
                FirestoreField("enemyMaximumCooldown", FirestoreInteger(3))
            }));
            string versionFive = FirestoreMap(string.Join(",", new[]
            {
                FirestoreField("schemaVersion", FirestoreInteger(5)),
                FirestoreField("revision", FirestoreInteger(7)),
                FirestoreField("activeRun", activeRun)
            }));

            FirestorePatchPlan migration = PlayerDefaultsPlanner.Plan(
                Student(FirestoreField("gamedata", versionFive)), "student");

            Assert.That(migration.FieldPaths,
                Does.Contain("gamedata.activeRun.enemyRemainingCooldown"));
            Assert.That(migration.FieldPaths, Does.Contain("gamedata.schemaVersion"));
            Assert.That(migration.FieldPaths, Does.Contain("gamedata.revision"));
            StringAssert.Contains(
                "\"enemyRemainingCooldown\":{\"integerValue\":\"2\"}",
                migration.ToJson());
            StringAssert.Contains(
                "\"schemaVersion\":{\"integerValue\":\"6\"}",
                migration.ToJson());
            StringAssert.Contains(
                "\"revision\":{\"integerValue\":\"8\"}",
                migration.ToJson());

            string versionSix = FirestoreMap(string.Join(",", new[]
            {
                FirestoreField("schemaVersion", FirestoreInteger(6)),
                FirestoreField("revision", FirestoreInteger(8)),
                FirestoreField("activeRun", activeRun)
            }));
            FirestorePatchPlan alreadyMigrated = PlayerDefaultsPlanner.Plan(
                Student(FirestoreField("gamedata", versionSix)), "student");
            Assert.That(alreadyMigrated.FieldPaths,
                Does.Not.Contain("gamedata.activeRun.enemyRemainingCooldown"));
        }

        [Test]
        public void TutorialTransitionWritesVersionedIdempotentEntry()
        {
            PlayerSnapshot player = Ready();
            var command = TutorialCommand(player, TutorialStatus.Active,
                "guide-attack", "attempt-1");

            FirestorePatchPlan plan = PlayerLifecyclePolicy.Plan(
                player, "student", command);

            Assert.That(plan.FieldPaths,
                Does.Contain("gamedata.tutorialMap.OnFirstCreate.currentStepId"));
            Assert.That(plan.FieldPaths,
                Does.Contain("gamedata.tutorialMap.OnFirstCreate.lastOperationId"));
            StringAssert.Contains("\"stringValue\":\"guide-attack\"", plan.ToJson());

            TutorialProgressPolicy.ApplyToSnapshot(player, command);
            player.revision++;
            Assert.That(PlayerLifecyclePolicy.Plan(player, "student", command).IsEmpty,
                Is.True);
        }

        [Test]
        public void CompletedTutorialCannotBeReopened()
        {
            PlayerSnapshot player = Ready();
            player.tutorialEntries = new[]
            {
                new PlayerSnapshot.TutorialEntryData
                {
                    tutorialId = "OnFirstCreate",
                    version = 1,
                    status = TutorialStatus.Completed.ToString(),
                    currentStepId = "threat-handoff"
                }
            };
            PlayerLifecycleCommand command = TutorialCommand(
                player, TutorialStatus.Active, "welcome-new", string.Empty);

            Assert.Throws<InvalidOperationException>(() =>
                PlayerLifecyclePolicy.Plan(player, "student", command));
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

        private static PlayerLifecycleCommand TutorialCommand(
            PlayerSnapshot player,
            TutorialStatus status,
            string stepId,
            string transactionId) => new PlayerLifecycleCommand
        {
            kind = PlayerLifecycleCommandKind.AdvanceTutorial,
            playerId = player.playerId,
            expectedRevision = player.revision,
            operationId = Guid.NewGuid().ToString("N"),
            value = "OnFirstCreate",
            tutorialVersion = 1,
            tutorialStatus = status.ToString(),
            tutorialStepId = stepId,
            tutorialTriggerRecordedAtUnixSeconds = 100,
            tutorialLastTransactionId = transactionId,
            tutorialFirstAttemptOutcome = TutorialAttemptOutcome.None.ToString()
        };

        private static string FirestoreField(string name, string value) =>
            "\"" + name + "\":" + value;

        private static string FirestoreMap(string fields) =>
            "{\"mapValue\":{\"fields\":{" + fields + "}}}";

        private static string FirestoreString(string value) =>
            "{\"stringValue\":\"" + value + "\"}";

        private static string FirestoreInteger(long value) =>
            "{\"integerValue\":\"" + value + "\"}";

        private static string FirestoreBoolean(bool value) =>
            "{\"booleanValue\":" + (value ? "true" : "false") + "}";

        private static string FirestoreArray(params string[] values) =>
            "{\"arrayValue\":{\"values\":[" + string.Join(",", values) + "]}}";
    }
}
