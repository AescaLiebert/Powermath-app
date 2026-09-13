using System.IO;
using NUnit.Framework;
using PowerMath.PlayerData;
using PowerMath.UI.Settings;
using UnityEditor;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class SharedSettingsPanelTests
    {
        private const string SettingsUxml = "Assets/Project/UI/Shared/SettingsPanel.uxml";

        [Test]
        public void SharedTemplate_ContainsGeneralSoundAndAdminContracts()
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingsUxml);
            Assert.That(asset, Is.Not.Null);
            TemplateContainer root = asset.CloneTree();

            foreach (string name in new[]
            {
                "settings-panel-modal", "settings-panel-close",
                "settings-tab-general", "settings-tab-sound", "settings-tab-admin",
                "settings-fullscreen-toggle", "settings-language-th", "settings-language-en",
                "settings-account-logout", "settings-account-status",
                "settings-music-slider", "settings-sfx-slider",
                "settings-admin-atk", "settings-admin-cr", "settings-admin-cd",
                "settings-admin-invincible", "settings-admin-restore-hp",
                "settings-admin-stage", "settings-admin-apply-stage",
                "settings-rank-silver", "settings-rank-gold", "settings-rank-diamond",
                "settings-admin-apply-currency",
                "settings-admin-reset"
            })
            {
                Assert.That(root.Q(name), Is.Not.Null, name);
            }
        }

        [Test]
        public void LocalizationCatalog_ContainsSharedGeneralSettingsCopy()
        {
            string json = File.ReadAllText("Assets/Project/Resources/Localization/UI.json");
            foreach (string key in new[]
            {
                "settings.language", "settings.languageHelp", "settings.account",
                "settings.logoutHelp", "settings.logout", "settings.signingOut"
            })
            {
                Assert.That(json, Contains.Substring("\"key\": \"" + key + "\""), key);
            }
        }

        [Test]
        public void AuthenticationAndMainMenu_ReferenceTheSameSettingsTemplate()
        {
            string auth = File.ReadAllText("Assets/Project/UI/Authentication/AuthenticationScreen.uxml");
            string main = File.ReadAllText("Assets/Project/UI/MainMenu/LegacyFeaturePanels.uxml");
            const string shared = "Assets/Project/UI/Shared/SettingsPanel.uxml";
            Assert.That(auth, Contains.Substring(shared));
            Assert.That(main, Contains.Substring(shared));
            Assert.That(main, Does.Not.Contain("template=\"AdminPanel\""));
        }

        [TestCase("level-1:teacher", true, true)]
        [TestCase("level-1:test1", true, true)]
        [TestCase("level-1:test1", false, false)]
        [TestCase("test1", true, false)]
        [TestCase("level-1:student", false, false)]
        public void AdminPolicy_RequiresFirebaseAdminFlag(
            string playerId,
            bool isAdmin,
            bool expected)
        {
            var player = new PlayerSnapshot
            {
                playerId = playerId,
                isAdmin = isAdmin,
                profile = new PlayerSnapshot.ProfileData { displayName = "test1" }
            };
            Assert.That(AdminAccountAccessPolicy.IsAuthorized(player), Is.EqualTo(expected));
        }

        [Test]
        public void AdminPolicy_DoesNotTrustDisplayName()
        {
            var player = new PlayerSnapshot
            {
                playerId = "level-1:student",
                isAdmin = false,
                profile = new PlayerSnapshot.ProfileData { displayName = "test1" }
            };
            Assert.That(AdminAccountAccessPolicy.IsAuthorized(player), Is.False);
        }

        [Test]
        public void AdminImplementation_HasNoTemporaryQuickTestRuntime()
        {
            string settings = File.ReadAllText(
                "Assets/Project/Script/UI/Settings/SettingsPanelController.cs");
            string combat = File.ReadAllText(
                "Assets/Project/Script/UI/MainMenu/CombatLobbyCompositionRoot.cs");
            Assert.That(settings, Does.Not.Contain("AdminTestRuntime"));
            Assert.That(combat, Does.Not.Contain("AdminTestRuntime"));
            Assert.That(settings, Contains.Substring("SetCombat"));
        }

        [Test]
        public void AdminCommands_RefreshFirebaseAndDoNotWriteStage()
        {
            string service = File.ReadAllText(
                "Assets/Project/Script/UI/Settings/FirestoreAdminTuningService.cs");
            Assert.That(service, Contains.Substring("PlayerSessionStore.HydrationResult.Success"));
            Assert.That(service, Does.Not.Contain("Join(root, \"progression\", \"currentStage\")"));
            Assert.That(service, Does.Not.Contain("Join(root, \"activeRun\", \"currentStage\")"));
        }

        [Test]
        public void TryMapPlayer_MapsAdminFlagAndAdminTuningFromFirebase()
        {
            string studentJson = "{\"mapValue\":{\"fields\":{" +
                "\"userdata\":{\"mapValue\":{\"fields\":{" +
                "\"username\":{\"stringValue\":\"teacher\"}," +
                "\"password\":{\"stringValue\":\"pass\"}," +
                "\"admin\":{\"booleanValue\":true}" +
                "}}}," +
                "\"gamedata\":{\"mapValue\":{\"fields\":{" +
                "\"schemaVersion\":{\"integerValue\":\"3\"}," +
                "\"revision\":{\"integerValue\":\"5\"}," +
                "\"profile\":{\"mapValue\":{\"fields\":{\"displayName\":{\"stringValue\":\"Teacher\"}}}}," +
                "\"adminTuning\":{\"mapValue\":{\"fields\":{" +
                "\"combatOverrideEnabled\":{\"booleanValue\":true}," +
                "\"attack\":{\"integerValue\":\"999\"}," +
                "\"criticalRateBasisPoints\":{\"integerValue\":\"5000\"}," +
                "\"criticalDamageBasisPoints\":{\"integerValue\":\"20000\"}," +
                "\"invincible\":{\"booleanValue\":true}," +
                "\"bypassVideoQuestion\":{\"booleanValue\":true}" +
                "}}}" +
                "}}}" +
                "}}}";

            Assert.IsTrue(PowerMath.Session.FirestoreJsonNavigator.TryParse(studentJson, out var student, out _));
            bool mapped = PowerMath.Session.FirestoreRestClient.TryMapPlayer("teacher", "level-1", "Grade 6", student, out PlayerSnapshot player);

            Assert.IsTrue(mapped);
            Assert.IsNotNull(player);
            Assert.IsTrue(player.isAdmin);
            Assert.IsTrue(AdminAccountAccessPolicy.IsAuthorized(player));
            Assert.IsNotNull(player.adminTuning);
            Assert.IsTrue(player.adminTuning.combatOverrideEnabled);
            Assert.AreEqual(999, player.adminTuning.attack);
            Assert.AreEqual(5000, player.adminTuning.criticalRateBasisPoints);
            Assert.AreEqual(20000, player.adminTuning.criticalDamageBasisPoints);
            Assert.IsTrue(player.adminTuning.invincible);
            Assert.IsTrue(player.adminTuning.bypassVideoQuestion);
        }

        [Test]
        public void TryMapPlayer_WhenNotAdmin_IsAdminIsFalse()
        {
            string studentJson = "{\"mapValue\":{\"fields\":{" +
                "\"userdata\":{\"mapValue\":{\"fields\":{" +
                "\"username\":{\"stringValue\":\"student\"}," +
                "\"password\":{\"stringValue\":\"pass\"}" +
                "}}}," +
                "\"gamedata\":{\"mapValue\":{\"fields\":{" +
                "\"schemaVersion\":{\"integerValue\":\"3\"}," +
                "\"profile\":{\"mapValue\":{\"fields\":{\"displayName\":{\"stringValue\":\"Student\"}}}}" +
                "}}}" +
                "}}}";

            Assert.IsTrue(PowerMath.Session.FirestoreJsonNavigator.TryParse(studentJson, out var student, out _));
            bool mapped = PowerMath.Session.FirestoreRestClient.TryMapPlayer("student", "level-1", "Grade 6", student, out PlayerSnapshot player);

            Assert.IsTrue(mapped);
            Assert.IsNotNull(player);
            Assert.IsFalse(player.isAdmin);
            Assert.IsFalse(AdminAccountAccessPolicy.IsAuthorized(player));
            Assert.IsNull(player.adminTuning);
        }
    }
}
