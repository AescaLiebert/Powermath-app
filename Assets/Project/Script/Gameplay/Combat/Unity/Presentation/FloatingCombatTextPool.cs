using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class FloatingCombatTextPool
    {
        private readonly RectTransform _root;
        private readonly FloatingCombatTextView _prefab;
        private readonly Queue<FloatingCombatTextView> _available =
            new Queue<FloatingCombatTextView>();
        private readonly HashSet<FloatingCombatTextView> _active =
            new HashSet<FloatingCombatTextView>();
        private readonly int _capacity;

        public FloatingCombatTextPool(
            RectTransform root,
            FloatingCombatTextView prefab,
            int capacity)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _prefab = prefab;
            _capacity = Mathf.Max(1, capacity);
            for (int index = 0; index < _capacity; index++)
                _available.Enqueue(CreateView(index));
        }

        public bool TryAcquire(out FloatingCombatTextView view)
        {
            if (_available.Count == 0)
            {
                view = null;
                return false;
            }
            view = _available.Dequeue();
            _active.Add(view);
            return true;
        }

        public void Release(FloatingCombatTextView view)
        {
            if (view == null || !_active.Remove(view)) return;
            view.ResetForPool();
            _available.Enqueue(view);
        }

        public void CancelAll()
        {
            if (_active.Count == 0) return;
            var copy = new FloatingCombatTextView[_active.Count];
            _active.CopyTo(copy);
            for (int index = 0; index < copy.Length; index++) Release(copy[index]);
        }

        private FloatingCombatTextView CreateView(int index)
        {
            FloatingCombatTextView view;
            if (_prefab != null)
            {
                view = UnityEngine.Object.Instantiate(_prefab, _root, false);
            }
            else
            {
                var instance = new GameObject(
                    "FloatingCombatText_" + index,
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(TextMeshProUGUI),
                    typeof(FloatingCombatTextView));
                instance.transform.SetParent(_root, false);
                TextMeshProUGUI text = instance.GetComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                text.fontStyle = FontStyles.Bold;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                view = instance.GetComponent<FloatingCombatTextView>();
                view.ConfigureRuntimeComponents(text, instance.GetComponent<CanvasGroup>());
            }
            view.ResetForPool();
            return view;
        }
    }
}
