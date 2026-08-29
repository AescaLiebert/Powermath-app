using System;
using UnityEngine.UIElements;

namespace PowerMath.UI.Core
{
    public static class UiElementQuery
    {
        public static T Require<T>(
            VisualElement root,
            string name,
            string owner)
            where T : VisualElement
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            T element = root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException(
                    $"{owner} requires {typeof(T).Name} '{name}' " +
                    "inside its assigned UI subtree.");
            }
            return element;
        }
    }
}
