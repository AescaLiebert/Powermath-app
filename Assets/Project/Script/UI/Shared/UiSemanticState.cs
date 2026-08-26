using UnityEngine.UIElements;

namespace PowerMath.UI.Shared
{
    public enum UiSemanticState
    {
        Ready,
        Busy,
        Success,
        Error,
        Blocked
    }

    public static class UiSemanticStateExtensions
    {
        private static readonly string[] StateClasses =
        {
            "is-ready",
            "is-busy",
            "is-success",
            "is-error",
            "is-blocked"
        };

        public static void SetSemanticState(
            this VisualElement element,
            UiSemanticState state)
        {
            if (element == null)
            {
                return;
            }

            foreach (string stateClass in StateClasses)
            {
                element.RemoveFromClassList(stateClass);
            }

            element.AddToClassList(ToClassName(state));
        }

        private static string ToClassName(UiSemanticState state)
        {
            switch (state)
            {
                case UiSemanticState.Busy:
                    return "is-busy";
                case UiSemanticState.Success:
                    return "is-success";
                case UiSemanticState.Error:
                    return "is-error";
                case UiSemanticState.Blocked:
                    return "is-blocked";
                default:
                    return "is-ready";
            }
        }
    }
}
