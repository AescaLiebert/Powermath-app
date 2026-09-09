using System.Collections.Generic;
using NUnit.Framework;
using PowerMath.Diagnostics;
using PowerMath.UI.Authentication;
using PowerMath.UI.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class StatusToastTests
    {
        [SetUp]
        public void SetUp()
        {
            AppLog.ResetToDefaults();
            AppLog.ClearSinks();
            StatusMessageService.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            StatusMessageService.Reset();
            AppLog.ResetToDefaults();
        }

        [Test]
        public void StatusMessageServiceDispatchesToSubscribersAndLogsToAppLog()
        {
            string publishedMessage = null;
            StatusSeverity publishedSeverity = StatusSeverity.Info;
            int publishedDuration = 0;

            StatusMessageService.MessagePublished += (msg, sev, dur) =>
            {
                publishedMessage = msg;
                publishedSeverity = sev;
                publishedDuration = dur;
            };

            StatusMessageService.ShowSuccess("Operation succeeded!");

            Assert.AreEqual("Operation succeeded!", publishedMessage);
            Assert.AreEqual(StatusSeverity.Success, publishedSeverity);
            Assert.AreEqual(2600, publishedDuration);

            var recentLogs = AppLog.GetRecentLogs();
            Assert.IsTrue(recentLogs.Count > 0);
            var lastLog = recentLogs[recentLogs.Count - 1];
            Assert.AreEqual("StatusUI", lastLog.Category);
            StringAssert.Contains("[Success] Operation succeeded!", lastLog.Text);
        }

        [Test]
        public void StatusMessageServiceSeverityHelpersWorkCorrectly()
        {
            var received = new List<(string Msg, StatusSeverity Sev)>();
            StatusMessageService.MessagePublished += (msg, sev, _) => received.Add((msg, sev));

            StatusMessageService.ShowInfo("Info alert");
            StatusMessageService.ShowWarning("Warning alert");
            StatusMessageService.ShowError("Error alert");

            Assert.AreEqual(3, received.Count);
            Assert.AreEqual(StatusSeverity.Info, received[0].Sev);
            Assert.AreEqual(StatusSeverity.Warning, received[1].Sev);
            Assert.AreEqual(StatusSeverity.Error, received[2].Sev);
        }

        [Test]
        public void StatusToastOverlayAttachesToRootAndRendersInitialState()
        {
            var root = new VisualElement();
            var overlay = StatusToastOverlay.Attach(root);

            Assert.IsNotNull(overlay);
            Assert.IsNotNull(overlay.Container);
            Assert.IsNotNull(overlay.Banner);
            Assert.IsNotNull(overlay.Badge);
            Assert.IsNotNull(overlay.Text);

            Assert.IsTrue(root.Contains(overlay.Container));
            Assert.AreEqual(DisplayStyle.None, overlay.Banner.style.display.value);
            Assert.IsFalse(overlay.IsVisible);

            // Re-attaching to the same root returns the existing overlay
            var same = StatusToastOverlay.Attach(root);
            Assert.AreSame(overlay, same);

            overlay.Dispose();
        }

        [Test]
        public void StatusToastOverlayShowAppliesSeverityAndSetsDisplayFlex()
        {
            var root = new VisualElement();
            var overlay = StatusToastOverlay.Attach(root);

            overlay.Show("Password is required", StatusSeverity.Error);

            Assert.AreEqual("Password is required", overlay.Text.text);
            Assert.AreEqual("X", overlay.Badge.text);
            Assert.IsTrue(overlay.Banner.ClassListContains("status-toast--error"));
            Assert.AreEqual(DisplayStyle.Flex, overlay.Banner.style.display.value);

            overlay.Show("Saved successfully", StatusSeverity.Success);

            Assert.AreEqual("Saved successfully", overlay.Text.text);
            Assert.AreEqual("OK", overlay.Badge.text);
            Assert.IsTrue(overlay.Banner.ClassListContains("status-toast--success"));
            Assert.IsFalse(overlay.Banner.ClassListContains("status-toast--error"));

            overlay.Dispose();
        }

        [Test]
        public void StatusToastOverlayHideImmediateHidesBanner()
        {
            var root = new VisualElement();
            var overlay = StatusToastOverlay.Attach(root);

            overlay.Show("Temporary alert", StatusSeverity.Warning);
            Assert.AreEqual(DisplayStyle.Flex, overlay.Banner.style.display.value);

            overlay.HideImmediate();
            Assert.AreEqual(DisplayStyle.None, overlay.Banner.style.display.value);
            Assert.IsFalse(overlay.IsVisible);

            overlay.Dispose();
        }

        [Test]
        public void AuthenticationViewRenderFailureDispatchesToStatusToast()
        {
            var host = new GameObject("Auth Test Host");
            try
            {
                var doc = host.AddComponent<UIDocument>();
                var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Project/UI/Panel Settings.asset");
                if (panelSettings != null)
                {
                    doc.panelSettings = Object.Instantiate(panelSettings);
                }
                var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Project/UI/Authentication/AuthenticationScreen.uxml");
                doc.visualTreeAsset = vta;

                var view = host.AddComponent<AuthenticationView>();
                view.RenderReady();

                var root = doc.rootVisualElement;
                var banner = root.Q(StatusToastOverlay.BannerName);
                var text = root.Q<Label>(StatusToastOverlay.TextName);

                Assert.IsNotNull(banner, "Status toast banner should be attached to Authentication view root.");
                Assert.IsNotNull(text, "Status toast text should be present.");

                view.RenderFailure("Incorrect school credentials.", false);

                Assert.AreEqual("Incorrect school credentials.", text.text);
                Assert.AreEqual(DisplayStyle.Flex, banner.style.display.value);
                Assert.IsTrue(banner.ClassListContains("status-toast--error"));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
