using System.Reflection;
using NUnit.Framework;
using PowerMath.PlayerData;
using PowerMath.UI.MainMenu.SocialProfile;

namespace PowerMath.Tests.EditMode
{
    public sealed class LeaderboardRuntimeTests
    {
        [Test]
        public void AuthoritativeSelfEntry_UsesLoadedFirebaseSnapshot()
        {
            var player = new PlayerSnapshot
            {
                profile = new PlayerSnapshot.ProfileData
                {
                    publicPlayerId = "2e877a104ce14d9aa1fefce94d388fd1",
                    displayName = "test1",
                    iconId = "avatar-default"
                },
                progression = new PlayerSnapshot.ProgressionData
                {
                    currentStage = 12,
                    highestStage = 18,
                    totalDamage = 345
                },
                wallet = new PlayerSnapshot.WalletData
                {
                    silver = 2,
                    gold = 3,
                    diamond = 4
                },
                loadout = new PlayerSnapshot.LoadoutData()
            };
            Assembly assembly = typeof(SocialProfileCompositionRoot).Assembly;
            System.Type controller = assembly.GetType(
                "PowerMath.UI.MainMenu.SocialProfile.LeaderboardPanelController");
            MethodInfo method = controller?.GetMethod(
                "CreateAuthoritativeSelfEntry",
                BindingFlags.NonPublic | BindingFlags.Static);

            object entry = method?.Invoke(null, new object[] { player });

            Assert.That(entry, Is.Not.Null);
            Assert.That(ReadField<string>(entry, "PlayerId"),
                Is.EqualTo(player.profile.publicPlayerId));
            Assert.That(ReadField<int>(entry, "CurrentStage"), Is.EqualTo(12));
            Assert.That(ReadField<int>(entry, "HighestStage"), Is.EqualTo(18));
            Assert.That(ReadField<long>(entry, "WeightedScore"), Is.EqualTo(71));
        }

        private static T ReadField<T>(object instance, string name)
        {
            FieldInfo field = instance.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(instance);
        }
    }
}
