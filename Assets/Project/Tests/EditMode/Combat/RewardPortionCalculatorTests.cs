using NUnit.Framework;
using PowerMath.Gameplay.Combat.Presentation;
using System.Linq;

namespace PowerMath.Gameplay.Combat.Tests
{
    public sealed class RewardPortionCalculatorTests
    {
        [TestCase(0L)]
        [TestCase(-5L)]
        public void CalculatePortions_NonPositiveAmount_ReturnsEmpty(long amount)
        {
            long[] portions = RewardPortionCalculator.CalculatePortions(amount);
            Assert.That(portions, Is.Empty);
        }

        [Test]
        public void CalculatePortions_SingleAmount_ReturnsSinglePortion()
        {
            long[] portions = RewardPortionCalculator.CalculatePortions(1);
            Assert.That(portions, Is.EqualTo(new[] { 1L }));
        }

        [Test]
        public void CalculatePortions_SmallAmounts_PreservesExactSum()
        {
            for (long amount = 2; amount <= 10; amount++)
            {
                long[] portions = RewardPortionCalculator.CalculatePortions(amount);
                Assert.That(portions.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(portions.Sum(), Is.EqualTo(amount));
                Assert.That(portions.All(p => p > 0), Is.True, $"All portions for {amount} must be > 0");
            }
        }

        [Test]
        public void CalculatePortions_PromptExample25Coins_ReturnsValidSteppingPortions()
        {
            long[] portions = RewardPortionCalculator.CalculatePortions(25, 5);
            Assert.That(portions.Length, Is.EqualTo(5));
            Assert.That(portions.Sum(), Is.EqualTo(25));
            Assert.That(portions.All(p => p > 0), Is.True);

            // Verify individual cumulative amounts step up progressively
            long accumulated = 0;
            var steps = portions.Select(p => accumulated += p).ToArray();
            Assert.That(steps[steps.Length - 1], Is.EqualTo(25));
            Assert.That(steps.Distinct().Count(), Is.EqualTo(steps.Length), "Every step must increase the total");
        }

        [TestCase(100L)]
        [TestCase(1200L)]
        [TestCase(50000L)]
        public void CalculatePortions_LargeAmounts_SumsExactlyToTarget(long amount)
        {
            long[] portions = RewardPortionCalculator.CalculatePortions(amount, 6);
            Assert.That(portions.Sum(), Is.EqualTo(amount));
            Assert.That(portions.All(p => p > 0), Is.True);
        }
    }
}
