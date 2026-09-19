using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class FloatingRewardTextPool
    {
        private readonly RectTransform _root;
        private readonly FloatingRewardTextView _prefab;
        private readonly Queue<FloatingRewardTextView> _available =
            new Queue<FloatingRewardTextView>();
        private readonly HashSet<FloatingRewardTextView> _active =
            new HashSet<FloatingRewardTextView>();
        private readonly int _capacity;

        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;

        public FloatingRewardTextPool(
            RectTransform root,
            FloatingRewardTextView prefab,
            int capacity)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _prefab = prefab;
            _capacity = Mathf.Max(1, capacity);
            for (int index = 0; index < _capacity; index++)
                _available.Enqueue(CreateView(index));
        }

        public bool TryAcquire(out FloatingRewardTextView view)
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

        public void Release(FloatingRewardTextView view)
        {
            if (view == null || !_active.Remove(view)) return;
            view.ResetForPool();
            _available.Enqueue(view);
        }

        public void CancelAll()
        {
            if (_active.Count == 0) return;
            var copy = new FloatingRewardTextView[_active.Count];
            _active.CopyTo(copy);
            for (int index = 0; index < copy.Length; index++) Release(copy[index]);
        }

        private FloatingRewardTextView CreateView(int index)
        {
            FloatingRewardTextView view;
            if (_prefab != null)
            {
                view = UnityEngine.Object.Instantiate(_prefab, _root, false);
            }
            else
            {
                var instance = new GameObject(
                    "FloatingRewardText_" + index,
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(TextMeshProUGUI),
                    typeof(FloatingRewardTextView));
                instance.transform.SetParent(_root, false);
                TextMeshProUGUI text = instance.GetComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                text.fontStyle = FontStyles.Bold;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.richText = true;
                text.extraPadding = true;
                view = instance.GetComponent<FloatingRewardTextView>();
                view.ConfigureRuntimeComponents(text, instance.GetComponent<CanvasGroup>());
            }
            view.ResetForPool();
            return view;
        }
    }
}
