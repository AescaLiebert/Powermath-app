using System.Collections;
using NUnit.Framework;
using PowerMath.Localization;
using PowerMath.UI.Authentication;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class AuthenticationLanguageUiTests
    {
        private GameObject _owner;
        private UIDocument _document;
        private AuthenticationView _view;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("Auth Test Host");
            _document = _owner.AddComponent<UIDocument>();
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Project/UI/Panel Settings.asset");
            if (panelSettings != null)
            {
                _document.panelSettings = Object.Instantiate(panelSettings);
            }
            var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Project/UI/Authentication/AuthenticationScreen.uxml");
            _document.visualTreeAsset = vta;
            _view = _owner.AddComponent<AuthenticationView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_document != null && _document.panelSettings != null)
            {
                Object.DestroyImmediate(_document.panelSettings);
            }
            if (_owner != null)
            {
                Object.DestroyImmediate(_owner);
            }
        }

        [Test]
        public void WorldwideButtonAndLanguageFlyoutExistInAuthenticationScreen()
        {
            _view.RenderReady();

            var root = _document.rootVisualElement;
            var worldwideBtn = root.Q<Button>("worldwide-button");
            Assert.IsNotNull(worldwideBtn, "worldwide-button should exist in AuthenticationScreen.");
            Assert.AreEqual("🌐", worldwideBtn.text);

            var flyout = root.Q<VisualElement>("auth-language-flyout");
            Assert.IsNotNull(flyout, "auth-language-flyout should exist.");

            var thBtn = root.Q<Button>("language-button-th");
            var enBtn = root.Q<Button>("language-button-en");
            Assert.IsNotNull(thBtn, "language-button-th should exist.");
            Assert.IsNotNull(enBtn, "language-button-en should exist.");
        }

        private static void Click(Button button)
        {
            Assert.IsNotNull(button);
            button.Focus();
            using var evt = NavigationSubmitEvent.GetPooled();
            evt.target = button;
            button.SendEvent(evt);
        }

        [Test]
        public void ClickingWorldwideButtonTogglesFlyout()
        {
            _view.RenderReady();

            var root = _document.rootVisualElement;
            var worldwideBtn = root.Q<Button>("worldwide-button");
            var flyout = root.Q<VisualElement>("auth-language-flyout");

            Assert.IsFalse(flyout.ClassListContains("is-open"));

            // Open
            Click(worldwideBtn);
            Assert.AreEqual(DisplayStyle.Flex, flyout.style.display.value);

            // Close
            _view.CloseLanguageFlyout();
            Assert.IsFalse(flyout.ClassListContains("is-open"));
        }

        [Test]
        public void SelectingThaiAndEnglishSwitchesLocaleAndUpdatesActiveStyle()
        {
            _view.RenderReady();

            var root = _document.rootVisualElement;
            var thBtn = root.Q<Button>("language-button-th");
            var enBtn = root.Q<Button>("language-button-en");

            // Click TH
            Click(thBtn);
            Assert.AreEqual("th", LocalizationService.Locale);
            Assert.IsTrue(thBtn.ClassListContains("is-active"));
            Assert.IsFalse(enBtn.ClassListContains("is-active"));

            // Click EN
            Click(enBtn);
            Assert.AreEqual("en", LocalizationService.Locale);
            Assert.IsTrue(enBtn.ClassListContains("is-active"));
            Assert.IsFalse(thBtn.ClassListContains("is-active"));
        }

        [Test]
        public void DocumentDoesNotContainLanguageToolbar()
        {
            _view.RenderReady();
            var root = _document.rootVisualElement;
            var toolbar = root.Q("language-toolbar");
            Assert.IsNull(toolbar, "language-toolbar should not be present in the document.");
        }
    }
}
