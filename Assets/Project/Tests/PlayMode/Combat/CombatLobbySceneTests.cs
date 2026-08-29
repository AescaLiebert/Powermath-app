using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using PowerMath.Gameplay.Combat.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.PlayModeTests
{
    public sealed class CombatLobbySceneTests
    {
        [UnityTest]
        public IEnumerator HydratedMainMenu_RunsCorrectQuestionThroughDamageAndCurrency()
        {
            GameObject sessionObject = null;
            VisualElement root = null;
            yield return LoadMainMenu(
                "Silver",
                (session, visualRoot) =>
                {
                    sessionObject = session;
                    root = visualRoot;
                }
            );

            Assert.That(root.Q<Label>("combat-stage-label").text, Is.EqualTo("STAGE 1 / 200"));
            Assert.That(root.Q<Label>("academic-rank-label").text, Is.EqualTo("RANK SILVER"));
            Assert.That(root.Q<VisualElement>("audit-score"), Is.Null);
            Assert.That(root.Q<VisualElement>("audit-count"), Is.Null);

            string hpBefore = root.Q<Label>("combat-enemy-hp-label").text;
            yield return CompleteCorrectAttempt(root, false);

            Assert.That(
                root.Q<Label>("combat-enemy-hp-label").text,
                Is.Not.EqualTo(hpBefore)
            );
            Assert.That(
                root.Q<Label>("academic-active-currency").text,
                Is.EqualTo("Silver: 1")
            );

            UnityEngine.Object.Destroy(sessionObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FiveFastCorrectAnswers_PromoteAndRequireAcknowledgement()
        {
            GameObject sessionObject = null;
            VisualElement root = null;
            yield return LoadMainMenu(
                "Silver",
                (session, visualRoot) =>
                {
                    sessionObject = session;
                    root = visualRoot;
                }
            );

            for (int index = 0; index < 4; index++)
            {
                yield return CompleteCorrectAttempt(root, false);
            }

            yield return CompleteCorrectAttempt(root, true);
            VisualElement modal = root.Q<VisualElement>("academic-rank-modal");
            Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(
                root.Q<Label>("academic-rank-modal-header").text,
                Is.EqualTo("RANK UP")
            );
            Assert.That(
                root.Q<Label>("academic-rank-modal-route").text,
                Does.Contain("Silver").And.Contain("Gold")
            );

            SendSubmit(root.Q<Button>("academic-rank-continue-button"));
            yield return WaitFor(
                () => IsCombatReady(root),
                4f,
                "Combat did not return to ready after acknowledging Rank promotion."
            );
            Assert.That(modal.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q<Label>("academic-rank-label").text, Is.EqualTo("RANK GOLD"));
            Assert.That(
                root.Q<Label>("academic-active-currency").text,
                Is.EqualTo("Gold: 0")
            );

            UnityEngine.Object.Destroy(sessionObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FiveIncorrectGoldAnswers_DemoteWithNeutralAdjustment()
        {
            GameObject sessionObject = null;
            VisualElement root = null;
            yield return LoadMainMenu(
                "Gold",
                (session, visualRoot) =>
                {
                    sessionObject = session;
                    root = visualRoot;
                }
            );

            for (int index = 0; index < 4; index++)
            {
                yield return CompleteIncorrectAttempt(root, false);
            }

            yield return CompleteIncorrectAttempt(root, true);
            Assert.That(
                root.Q<Label>("academic-rank-modal-header").text,
                Is.EqualTo("RANK ADJUSTED")
            );
            Assert.That(
                root.Q<Label>("academic-rank-modal-body").text,
                Does.Not.Contain("failed").And.Not.Contain("lost")
            );
            Assert.That(
                root.Q<Label>("academic-rank-modal-route").text,
                Does.Contain("Gold").And.Contain("Silver")
            );

            SendSubmit(root.Q<Button>("academic-rank-continue-button"));
            yield return WaitFor(
                () => IsCombatReady(root),
                4f,
                "Combat did not return to ready after acknowledging Rank adjustment."
            );
            Assert.That(root.Q<Label>("academic-rank-label").text, Is.EqualTo("RANK SILVER"));
            Assert.That(root.Q<Label>("academic-active-currency").text, Is.EqualTo("Silver: 0"));

            UnityEngine.Object.Destroy(sessionObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Timeout_DealsZeroDamageAndAwardsNoCurrency()
        {
            GameObject sessionObject = null;
            VisualElement root = null;
            yield return LoadMainMenu(
                "Silver",
                (session, visualRoot) =>
                {
                    sessionObject = session;
                    root = visualRoot;
                }
            );

            string hpBefore = root.Q<Label>("combat-enemy-hp-label").text;
            TriggerAttack();
            yield return WaitFor(
                () => IsCombatReady(root),
                15f,
                "Timed-out attempt did not return to EnemyReady."
            );

            Assert.That(root.Q<Label>("combat-enemy-hp-label").text, Is.EqualTo(hpBefore));
            Assert.That(root.Q<Label>("academic-active-currency").text, Is.EqualTo("Silver: 0"));
            Assert.That(root.Q<VisualElement>("academic-rank-modal").style.display.value, Is.EqualTo(DisplayStyle.None));

            UnityEngine.Object.Destroy(sessionObject);
            yield return null;
        }

        private static bool IsCombatReady(VisualElement root)
        {
            VisualElement attemptPanel = root.Q<VisualElement>("combat-attempt-panel");
            VisualElement rankModal = root.Q<VisualElement>("academic-rank-modal");
            bool rankClosed = rankModal == null || rankModal.resolvedStyle.display == DisplayStyle.None;
            return rankClosed && attemptPanel != null && attemptPanel.ClassListContains("is-hidden");
        }

        private static void TriggerAttack()
        {
            ActorPresentationController actor =
                GameObject.Find("playerPresentation")?.GetComponent<ActorPresentationController>() ??
                GameObject.Find("monsterPrefab")?.GetComponent<ActorPresentationController>() ??
                UnityEngine.Object.FindAnyObjectByType<ActorPresentationController>();
            if (actor != null)
            {
                actor.TriggerClick();
            }
        }

        private static IEnumerator CompleteCorrectAttempt(
            VisualElement root,
            bool expectRankPopup)
        {
            TriggerAttack();
            Label prompt = root.Q<Label>("academic-question-prompt");
            const string prefix = "QA target answer: ";
            yield return WaitFor(
                () => prompt != null && prompt.text != null && prompt.text.StartsWith(prefix),
                4f,
                "Question prompt was not displayed after triggering attack."
            );
            string answer = prompt.text.Substring(prefix.Length);
            foreach (char digit in answer)
            {
                SendSubmit(root.Q<Button>($"combat-digit-{digit}"));
                yield return null;
            }

            SendSubmit(root.Q<Button>("combat-submit-button"));
            if (expectRankPopup)
            {
                yield return WaitFor(
                    () => root.Q<VisualElement>("academic-rank-modal")
                        .resolvedStyle.display == DisplayStyle.Flex,
                    5f,
                    "Expected Rank transition popup was not shown."
                );
            }
            else
            {
                yield return WaitFor(
                    () => IsCombatReady(root),
                    5f,
                    "Correct attempt did not return to EnemyReady."
                );
            }
        }

        private static IEnumerator CompleteIncorrectAttempt(
            VisualElement root,
            bool expectRankPopup)
        {
            TriggerAttack();
            Label prompt = root.Q<Label>("academic-question-prompt");
            const string prefix = "QA target answer: ";
            yield return WaitFor(
                () => prompt != null && prompt.text != null && prompt.text.StartsWith(prefix),
                4f,
                "Question prompt was not displayed after triggering attack."
            );
            SendSubmit(root.Q<Button>("combat-digit-0"));
            yield return null;
            SendSubmit(root.Q<Button>("combat-submit-button"));

            if (expectRankPopup)
            {
                yield return WaitFor(
                    () => root.Q<VisualElement>("academic-rank-modal")
                        .resolvedStyle.display == DisplayStyle.Flex,
                    5f,
                    "Expected Rank adjustment popup was not shown."
                );
            }
            else
            {
                yield return WaitFor(
                    () => IsCombatReady(root),
                    5f,
                    "Incorrect attempt did not return to EnemyReady."
                );
            }
        }

        private static IEnumerator LoadMainMenu(
            string rank,
            Action<GameObject, VisualElement> completed)
        {
            GameObject sessionObject = CreateHydratedSession(rank);
            AsyncOperation load = SceneManager.LoadSceneAsync("MainMenuScene");
            Assert.That(load, Is.Not.Null, "MainMenuScene must be in Build Settings.");
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            UIDocument document = UnityEngine.Object.FindAnyObjectByType<UIDocument>();
            Assert.That(document, Is.Not.Null);
            VisualElement root = document.rootVisualElement;

            Type transitionType = Type.GetType("PowerMath.UI.MainMenu.MainMenuTransitionController, Assembly-CSharp");
            if (transitionType != null)
            {
                Component transition = UnityEngine.Object.FindAnyObjectByType(transitionType) as Component;
                if (transition != null)
                {
                    transitionType.GetMethod("CancelAndApplyFinalState")?.Invoke(transition, null);
                }
            }

            yield return WaitFor(() => IsCombatReady(root), 5f, "Combat surface was not ready.");
            completed(sessionObject, root);
        }

        private static IEnumerator WaitFor(
            Func<bool> condition,
            float timeoutSeconds,
            string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(condition(), Is.True, failureMessage);
        }

        private static GameObject CreateHydratedSession(string rank)
        {
            Type storeType = RequireType(
                "PowerMath.PlayerData.PlayerSessionStore, Assembly-CSharp"
            );
            Type responseType = RequireType(
                "PowerMath.Session.BootstrapResponse, Assembly-CSharp"
            );
            Type snapshotType = RequireType(
                "PowerMath.PlayerData.PlayerSnapshot, Assembly-CSharp"
            );
            Type profileType = snapshotType.GetNestedType("ProfileData");
            Type progressionType = snapshotType.GetNestedType("ProgressionData");
            Type walletType = snapshotType.GetNestedType("WalletData");
            Type activeRunType = snapshotType.GetNestedType("ActiveRunData");

            object snapshot = Activator.CreateInstance(snapshotType);
            object profile = Activator.CreateInstance(profileType);
            object progression = Activator.CreateInstance(progressionType);
            object wallet = Activator.CreateInstance(walletType);
            object activeRun = Activator.CreateInstance(activeRunType);
            SetField(profile, "displayName", "Combat Test Student");
            SetField(snapshot, "playerId", "combat-test-player");
            SetField(snapshot, "profile", profile);
            SetField(progression, "currentStage", 1);
            SetField(progression, "activeRank", rank);
            SetField(snapshot, "progression", progression);
            SetField(wallet, "silver", 0L);
            SetField(wallet, "gold", 0L);
            SetField(wallet, "diamond", 0L);
            SetField(snapshot, "wallet", wallet);
            SetField(activeRun, "currentStage", 1);
            SetField(snapshot, "activeRun", activeRun);

            object response = Activator.CreateInstance(responseType);
            SetField(response, "schemaVersion", 1);
            SetField(response, "remembered", false);
            SetField(response, "player", snapshot);

            var sessionObject = new GameObject("Combat Test Session");
            Component store = sessionObject.AddComponent(storeType);
            MethodInfo hydrate = storeType.GetMethod("TryHydrate");
            object result = hydrate.Invoke(store, new[] { response });
            Assert.That(result.ToString(), Is.EqualTo("Success"));
            return sessionObject;
        }

        private static void SendSubmit(Button button)
        {
            Assert.That(button, Is.Not.Null);
            button.Focus();
            NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
            submit.Dispose();
        }

        private static Type RequireType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            Assert.That(type, Is.Not.Null, $"Missing runtime type {assemblyQualifiedName}.");
            return type;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name);
            Assert.That(field, Is.Not.Null, $"Missing field {target.GetType().Name}.{name}.");
            field.SetValue(target, value);
        }
    }
}
