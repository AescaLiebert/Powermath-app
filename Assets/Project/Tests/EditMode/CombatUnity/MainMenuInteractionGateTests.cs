using NUnit.Framework;
using PowerMath.UI.MainMenu;

namespace PowerMath.Gameplay.Combat.Unity.Tests
{
    public sealed class MainMenuInteractionGateTests
    {
        [Test]
        public void Leases_CombineScopesUntilEachOwnerReleases()
        {
            var gate = new MainMenuInteractionGate();
            IInteractionLock combat = gate.Acquire(
                "combat", InteractionScope.Lobby | InteractionScope.Navigation);
            IInteractionLock modal = gate.Acquire(
                "modal", InteractionScope.Navigation | InteractionScope.ModalDismiss);

            Assert.That(gate.IsAllowed(InteractionScope.Lobby), Is.False);
            Assert.That(gate.IsAllowed(InteractionScope.Navigation), Is.False);
            Assert.That(gate.IsAllowed(InteractionScope.Question), Is.True);
            Assert.That(gate.Snapshot.LockCount, Is.EqualTo(2));

            combat.Dispose();
            Assert.That(gate.IsAllowed(InteractionScope.Lobby), Is.True);
            Assert.That(gate.IsAllowed(InteractionScope.Navigation), Is.False);

            modal.Dispose();
            Assert.That(gate.Snapshot.IsBlocked, Is.False);
        }

        [Test]
        public void Lease_DoubleDispose_DoesNotReleaseAnotherOwner()
        {
            var gate = new MainMenuInteractionGate();
            IInteractionLock first = gate.Acquire("first", InteractionScope.Lobby);
            IInteractionLock second = gate.Acquire("second", InteractionScope.Lobby);

            first.Dispose();
            first.Dispose();

            Assert.That(gate.IsAllowed(InteractionScope.Lobby), Is.False);
            Assert.That(gate.Snapshot.LockCount, Is.EqualTo(1));
            second.Dispose();
        }
    }
}
