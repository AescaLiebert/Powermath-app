using System;
using PowerMath.UI.MainMenu;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class InteractionShieldView : IDisposable
    {
        private readonly VisualElement _shield;
        private readonly IMainMenuInteractionGate _gate;

        public InteractionShieldView(
            VisualElement root,
            IMainMenuInteractionGate gate)
        {
            _shield = root?.Q<VisualElement>("main-menu-interaction-shield") ??
                throw new ArgumentNullException(nameof(root));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _gate.Changed += OnGateChanged;
            OnGateChanged(_gate.Snapshot);
        }

        public void Dispose()
        {
            _gate.Changed -= OnGateChanged;
            SetBlocked(false);
        }

        private void OnGateChanged(InteractionGateSnapshot snapshot)
        {
            InteractionScope fullSurface = InteractionScope.Lobby |
                InteractionScope.Navigation | InteractionScope.Question;
            bool terminalActionAllowed =
                (snapshot.BlockedScopes & InteractionScope.TerminalAction) == 0;
            SetBlocked((snapshot.BlockedScopes & fullSurface) == fullSurface &&
                !terminalActionAllowed);
        }

        private void SetBlocked(bool blocked)
        {
            _shield.style.display = blocked ? DisplayStyle.Flex : DisplayStyle.None;
            _shield.pickingMode = blocked ? PickingMode.Position : PickingMode.Ignore;
            _shield.focusable = blocked;
            if (blocked) _shield.Focus();
        }
    }
}
