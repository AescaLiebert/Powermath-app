using System.Collections;
using NUnit.Framework;
using PowerMath.PlayerData;
using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class PlayerPreparationUiTests
    {
        [UnityTest]
        public IEnumerator BothCharactersCanCompletePlaceholderPreparation()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            var camera = new GameObject("Preview Camera").AddComponent<Camera>();
            var owner = new GameObject("Preparation UI Test");
            var store = owner.AddComponent<PlayerSessionStore>();
            var document = owner.AddComponent<UIDocument>();
            var settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Project/UI/Panel Settings.asset"));
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            settings.targetTexture = target;
            document.panelSettings = settings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Project/UI/Bootstrap/BootstrapScreen.uxml");
            yield return null;
            foreach (string id in new[] { "ricko", "stellar" })
            {
                bool completed = false;
                var player = new PlayerSnapshot
                {
                    schemaVersion = 3, playerId = "preview:" + id, revision = 1,
                    profile = new PlayerSnapshot.ProfileData { displayName = "Preview" },
                    preferences = new PlayerSnapshot.PreferencesData(),
                    onboarding = new PlayerSnapshot.OnboardingData { version = 1, phase = "opening" }
                };
                Assert.AreEqual(PlayerSessionStore.HydrationResult.Success, store.TryHydrate(new BootstrapResponse { schemaVersion = 3, player = player }));
                owner.AddComponent<PlayerPreparationPresenter>().Initialize(document.rootVisualElement, store, new EditorLifecycleCommands(), () => completed = true);
                yield return null;
                Submit(document, "common.continue");
                yield return null;
                Assert.AreEqual("character", store.Snapshot.onboarding.phase);
                if (id == "ricko")
                {
                    yield return null;
                    yield return null;
                    yield return new WaitForSeconds(0.2f);
                    Assert.Greater(document.rootVisualElement.Q<Image>("portrait-ricko").resolvedStyle.width, 0);
                    Assert.IsTrue(document.rootVisualElement.Q<Image>("portrait-ricko").sprite != null);
                    Assert.Greater(document.rootVisualElement.Q<Image>("portrait-ricko").sprite.vertices.Length, 0);
                    var previous = RenderTexture.active;
                    RenderTexture.active = target;
                    var capture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                    capture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    capture.Apply();
                    RenderTexture.active = previous;
                    System.IO.File.WriteAllBytes("Logs/LifecycleValidation/character-selection.png", capture.EncodeToPNG());
                    Object.Destroy(capture);
                }
                Submit(document, id);
                yield return null;
                Submit(document, "onboarding.choose");
                yield return null;
                Assert.AreEqual("name", store.Snapshot.onboarding.phase);
                document.rootVisualElement.Q<TextField>("preparation-display-name").value = "ผู้กล้า";
                Submit(document, "common.confirm");
                yield return null;
                Assert.IsTrue(completed);
                Assert.AreEqual(id, store.Snapshot.profile.characterId);
                Assert.AreEqual("ผู้กล้า", store.Snapshot.profile.displayName);
                Assert.AreEqual("complete", store.Snapshot.onboarding.phase);
            }
            Object.Destroy(owner);
            Object.Destroy(camera.gameObject);
            Object.Destroy(settings);
            target.Release();
            Object.Destroy(target);
            yield return new ExitPlayMode();
        }
        private static void Submit(UIDocument document, string name)
        {
            var button = document.rootVisualElement.Q<Button>(name);
            Assert.IsNotNull(button, name);
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }
    }
}
