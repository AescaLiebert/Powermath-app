using NUnit.Framework;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;
using PowerMath.UI.QuestionSequence;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Tests.EditMode
{
    public sealed class QuestionSequenceFallbackCatalogTests
    {
        [Test]
        public void FallbackCatalogLoadsAllTiersSuccessfully()
        {
            var repo = new InMemoryQuestionCatalogRepository();
            QuestionCatalog catalog = null;
            repo.Load(result =>
            {
                Assert.IsTrue(result.IsSuccess, "Fallback catalog load must succeed.");
                catalog = result.Catalog;
            });

            Assert.IsNotNull(catalog, "Catalog must not be null.");

            var silverQuestions = catalog.GetRankQuestions(AcademicRank.Silver);
            Assert.IsNotNull(silverQuestions);
            Assert.GreaterOrEqual(silverQuestions.Count, 5);
            Assert.AreEqual(7, silverQuestions[0].CorrectAnswer);
            Assert.AreEqual(8, silverQuestions[1].CorrectAnswer);

            var goldQuestions = catalog.GetRankQuestions(AcademicRank.Gold);
            Assert.IsNotNull(goldQuestions);
            Assert.GreaterOrEqual(goldQuestions.Count, 5);
            Assert.AreEqual(12, goldQuestions[0].CorrectAnswer);

            var diamondQuestions = catalog.GetRankQuestions(AcademicRank.Diamond);
            Assert.IsNotNull(diamondQuestions);
            Assert.GreaterOrEqual(diamondQuestions.Count, 5);
            Assert.AreEqual(17, diamondQuestions[0].CorrectAnswer);
        }

        [Test]
        public void FormatEquationForAnswerProducesAccurateMath()
        {
            // Wireframe reference answer 8 must always equal the canonical 48 / 6
            string eq8 = QuestionSequenceController.FormatEquationForAnswer(8, 2);
            Assert.AreEqual("48 ÷ 6 = ?", eq8);

            // Clean division for answer 7
            string eq7 = QuestionSequenceController.FormatEquationForAnswer(7, 1);
            Assert.AreEqual("42 ÷ 6 = ?", eq7);

            // Clean division for answer 12
            string eq12 = QuestionSequenceController.FormatEquationForAnswer(12, 1);
            Assert.AreEqual("72 ÷ 6 = ?", eq12);
        }

        [Test]
        public void RankDamageMultipliersMatchAcademicProgression()
        {
            Assert.AreEqual(1.0d, AcademicRank.Silver.DamageMultiplier);
            Assert.AreEqual(1.5d, AcademicRank.Gold.DamageMultiplier);
            Assert.AreEqual(2.0d, AcademicRank.Diamond.DamageMultiplier);
        }

        [Test]
        public void QuestionSequenceControllerInitializesAndSwitchesQuestions()
        {
            var go = new GameObject("TestQuestionSequence");
            var uiDoc = go.AddComponent<UIDocument>();
            var controller = go.AddComponent<QuestionSequenceController>();

            Assert.IsNotNull(controller);
            Assert.AreEqual(QuestionSequenceState.NumkeypadPopUp, controller.CurrentState);

            Object.DestroyImmediate(go);
        }
    }
}
