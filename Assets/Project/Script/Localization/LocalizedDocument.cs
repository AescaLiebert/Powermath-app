using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Localization
{
    public sealed class LocalizedDocument : MonoBehaviour
    {
        private VisualElement _root;
        public void Bind(VisualElement root)
        {
            _root = root;
            LocalizationService.Changed -= Refresh;
            LocalizationService.Changed += Refresh;
            Refresh();
        }
        public void Refresh()
        {
            if (_root == null) return;
            _root.Query<TextElement>().ForEach(element =>
            {
                foreach (string name in element.GetClasses())
                    if (name.StartsWith("loc-"))
                    { element.text = LocalizationService.Get(name.Substring(4)); break; }
            });
        }
        private void OnDestroy() => LocalizationService.Changed -= Refresh;
    }
}
