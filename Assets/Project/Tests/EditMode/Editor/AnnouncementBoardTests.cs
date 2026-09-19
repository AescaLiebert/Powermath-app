using System;
using NUnit.Framework;
using PowerMath.UI.Authentication;
using PowerMath.UI.Authentication.Announcements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class AnnouncementBoardTests
    {
        [Test]
        public void CatalogValidationRejectsDuplicatePatchIds()
        {
            var catalog = new AnnouncementCatalog
            {
                schemaVersion = 1,
                catalogRevision = "test",
                patches = new[]
                {
                    Patch("same"),
                    Patch("same")
                }
            };

            bool valid = AnnouncementCatalogLoader.TryValidate(
                catalog,
                out string error);

            Assert.IsFalse(valid);
            StringAssert.Contains("Duplicate", error);
        }

        [Test]
        public void DailySuppressionExpiresWhenLocalDateChanges()
        {
            DateTime today = new DateTime(2026, 9, 17, 23, 59, 0);

            Assert.IsTrue(AnnouncementPreferenceStore.IsSuppressed(
                "2026-09-17",
                "old-revision",
                "new-revision",
                today));
            Assert.IsFalse(AnnouncementPreferenceStore.IsSuppressed(
                "2026-09-17",
                "revision",
                "revision",
                today.AddMinutes(2)));
        }

        [Test]
        public void InlineMarkdownEscapesRawTagsBeforeFormatting()
        {
            string rendered = AnnouncementMarkdownRenderer.SanitizeInline(
                "<script>bad</script> **safe**");

            StringAssert.Contains("&lt;script&gt;", rendered);
            StringAssert.DoesNotContain("<script>", rendered);
            StringAssert.Contains("<b>safe</b>", rendered);
        }

        [Test]
        public void AuthenticationScreenContainsIndependentAnnouncementScrollViews()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<
                VisualTreeAsset>(
                "Assets/Project/UI/Authentication/AuthenticationScreen.uxml");
            Assert.IsNotNull(tree);

            TemplateContainer root = tree.Instantiate();
            ScrollView patches = root.Q<ScrollView>(
                "announcement-patch-scroll");
            ScrollView content = root.Q<ScrollView>(
                "announcement-content-scroll");

            Assert.IsNotNull(patches);
            Assert.IsNotNull(content);
            Assert.AreNotSame(patches, content);
            Assert.IsNotNull(root.Q<VisualElement>("announcement-overlay").parent);
            Assert.AreEqual(
                PickingMode.Ignore,
                root.Q<VisualElement>("announcement-overlay").parent.pickingMode);
            Assert.IsNotNull(root.Q<Button>("announcement-open-button"));
            Assert.IsNotNull(root.Q<Button>("announcement-close-button"));
            Assert.IsNotNull(root.Q<Button>("announcement-suppress-button"));
        }

        [Test]
        public void ManualOpenDisplaysBoardAndSelectsPatch()
        {
            var owner = new GameObject("Announcement Test Host");
            try
            {
                UIDocument document = owner.AddComponent<UIDocument>();
                VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<
                    VisualTreeAsset>(
                    "Assets/Project/UI/Authentication/AuthenticationScreen.uxml");
                document.visualTreeAsset = tree;
                AuthenticationView view = owner.AddComponent<
                    AuthenticationView>();
                view.RenderReady();

                AuthenticationAnnouncementController controller =
                    owner.GetComponent<AuthenticationAnnouncementController>();
                Assert.IsNotNull(controller);
                controller.OpenManual();

                VisualElement overlay = document.rootVisualElement.Q(
                    "announcement-overlay");
                Assert.AreEqual(
                    DisplayStyle.Flex,
                    overlay.style.display.value);
                Assert.IsNotNull(document.rootVisualElement.Q<Button>(
                    "announcement-patch-patch-1.1-announcement-board"));
                Assert.IsNotEmpty(document.rootVisualElement.Q<Label>(
                    "announcement-patch-title").text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static AnnouncementPatch Patch(string id)
        {
            return new AnnouncementPatch
            {
                id = id,
                version = "1.0",
                titleEn = "Title",
                bodyEn = "Body"
            };
        }
    }
}
