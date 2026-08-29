using UnityEngine;

namespace PowerMath.UI.MainMenu
{
    [DisallowMultipleComponent]
    public sealed class MainMenuInteractionGateProvider : MonoBehaviour
    {
        public IMainMenuInteractionGate Gate { get; } = new MainMenuInteractionGate();
    }
}
