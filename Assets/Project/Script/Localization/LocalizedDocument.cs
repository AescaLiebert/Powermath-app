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
                    if (name.StartsWith("loc-") && !name.StartsWith("loc-tip-"))
                    { element.text = LocalizationService.Get(name.Substring(4)); break; }
            });
            _root.Query<VisualElement>().ForEach(element =>
            {
                foreach (string name in element.GetClasses())
                    if (name.StartsWith("loc-tip-"))
                    { element.tooltip = LocalizationService.Get(name.Substring(8)); break; }
            });
        }
        private void OnDestroy() => LocalizationService.Changed -= Refresh;
    }
}
