using System;
using NUnit.Framework;
using PowerMath.PlayerData;
using PowerMath.Session;
using PowerMath.UI.MainMenu;
using PowerMath.UI.MainMenu.Admin;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class AdminResetTests
    {
        [Test]
        public void BuildGameDataResetPlan_GeneratesCompleteInitialDefaults()
        {
            const string username = "student_alpha";
            const string publicId = "11223344556677889900aabbccddeeff";

            FirestorePatchPlan plan = PlayerResetPayloadBuilder.BuildGameDataResetPlan(username, publicId);

            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.IsEmpty, Is.False);
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.revision"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.profile.displayName"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.profile.publicPlayerId"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.progression.currentStage"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.progression.highestStage"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.wallet.silver"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.wallet.powerCoins"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.inventory"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.activeRun.currentStage"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.academic.auditScore"));
            Assert.That(plan.FieldPaths, Contains.Item("student_alpha.gamedata.analytics.totalQuestionsResolved"));

            string json = plan.ToJson();
            Assert.That(json, Contains.Substring("\"displayName\":{\"stringValue\":\"student_alpha\"}"));
            Assert.That(json, Contains.Substring("\"publicPlayerId\":{\"stringValue\":\"11223344556677889900aabbccddeeff\"}"));
            Assert.That(json, Contains.Substring("\"currentStage\":{\"integerValue\":\"1\"}"));
            Assert.That(json, Contains.Substring("\"silver\":{\"integerValue\":\"0\"}"));
            Assert.That(json, Contains.Substring("\"inventory\":{\"arrayValue\":{\"values\":[]}}"));
        }

        [Test]
        public void BuildGameDataResetPlan_ThrowsOnEmptyUsername()
        {
            Assert.Throws<ArgumentException>(() => PlayerResetPayloadBuilder.BuildGameDataResetPlan(null));
            Assert.Throws<ArgumentException>(() => PlayerResetPayloadBuilder.BuildGameDataResetPlan("   "));
        }

        [Test]
        public void BuildLeaderboardDeletionPlan_CreatesMaskForTargetPlayer()
        {
            const string publicId = "deadbeef12345678deadbeef12345678";
            FirestorePatchPlan plan = PlayerResetPayloadBuilder.BuildLeaderboardDeletionPlan(publicId);

            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.IsEmpty, Is.False);
            Assert.That(plan.FieldPaths, Contains.Item(publicId));

            string json = plan.ToJson();
            Assert.That(json, Contains.Substring("\"deadbeef12345678deadbeef12345678\":{\"nullValue\":null}"));
        }

        [Test]
        public void BuildLeaderboardDeletionPlan_ThrowsOnEmptyId()
        {
            Assert.Throws<ArgumentException>(() => PlayerResetPayloadBuilder.BuildLeaderboardDeletionPlan(null));
            Assert.Throws<ArgumentException>(() => PlayerResetPayloadBuilder.BuildLeaderboardDeletionPlan("  "));
        }

        [Test]
        public void AdminPanelController_OpenAndClose_ControlsVisibility()
        {
            var hostObject = new GameObject("TestHost");
            var host = hostObject.AddComponent<DummyMonoBehaviour>();

            try
            {
                var root = CreateMockAdminVisualTree();
                var panelHost = new MainMenuPanelHost();
                var player = new PlayerSnapshot
                {
                    playerId = "level1:student1",
                    profile = new PlayerSnapshot.ProfileData
                    {
                        displayName = "Test Student",
                        gradeBand = "Grade 4",
                        publicPlayerId = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4"
                    }
                };

                var controller = new AdminPanelController(
                    root,
                    host,
                    null,
                    player,
                    panelHost);

                Assert.That(controller.IsValid, Is.True);
                controller.Bind();

                var modal = root.Q<VisualElement>("admin-panel-modal");
                Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.None));

                controller.Open();
                Assert.That(panelHost.OpenPanel, Is.EqualTo(MainMenuPanelId.Settings));
                Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.Flex));

                var summary = root.Q<Label>("admin-account-summary");
                Assert.That(summary.text, Contains.Substring("Test Student"));
                Assert.That(summary.text, Contains.Substring("Grade 4"));

                controller.Close();
                Assert.That(panelHost.OpenPanel, Is.EqualTo(MainMenuPanelId.None));
                Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.None));

                controller.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void AdminPanelController_ResetButton_EntersConfirmationState()
        {
            var hostObject = new GameObject("TestHost");
            var host = hostObject.AddComponent<DummyMonoBehaviour>();

            try
            {
                var root = CreateMockAdminVisualTree();
                var panelHost = new MainMenuPanelHost();
                var player = new PlayerSnapshot
                {
                    playerId = "level1:student1",
                    profile = new PlayerSnapshot.ProfileData
                    {
                        displayName = "Test Student"
                    }
                };

                var controller = new AdminPanelController(
                    root,
                    host,
                    null,
                    player,
                    panelHost);

                controller.Bind();
                controller.Open();

                var button = root.Q<Button>("admin-reset-button");
                var status = root.Q<Label>("admin-reset-status");

                Assert.That(button.text, Is.EqualTo("RESET USER DATA"));

                // Trigger first click
                using (var clickEvent = ClickEvent.GetPooled())
                {
                    clickEvent.target = button;
                    button.SendEvent(clickEvent);
                }

                Assert.That(button.text, Is.EqualTo("CONFIRM RESET USER DATA"));
                Assert.That(button.ClassListContains("is-confirming"), Is.True);
                Assert.That(status.text, Contains.Substring("Warning"));

                controller.Dispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        private static VisualElement CreateMockAdminVisualTree()
        {
            var root = new VisualElement();

            var settingButton = new Button { name = "setting" };
            root.Add(settingButton);

            var modal = new VisualElement { name = "admin-panel-modal" };
            modal.AddToClassList("is-hidden");

            var closeButton = new Button { name = "admin-panel-close" };
            modal.Add(closeButton);

            var accountSummary = new Label { name = "admin-account-summary" };
            modal.Add(accountSummary);

            var resetButton = new Button { name = "admin-reset-button", text = "RESET USER DATA" };
            modal.Add(resetButton);

            var statusLabel = new Label { name = "admin-reset-status", text = "Ready" };
            modal.Add(statusLabel);

            root.Add(modal);
            return root;
        }

        private sealed class DummyMonoBehaviour : MonoBehaviour { }
    }
}
