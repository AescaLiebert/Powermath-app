using System;
using PowerMath.Gameplay.Combat.Presentation;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class UiToolkitLifecycleController
    {
        private readonly VisualElement _element;
        private readonly long _enterMilliseconds;
        private readonly long _exitMilliseconds;
        private IVisualElementScheduledItem _scheduled;

        public UiToolkitLifecycleController(
            VisualElement element,
            long enterMilliseconds = 180,
            long exitMilliseconds = 140)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _enterMilliseconds = Math.Max(0, enterMilliseconds);
            _exitMilliseconds = Math.Max(0, exitMilliseconds);
            CancelAndApply(UiLifecycleState.Hidden);
        }

        public UiLifecycleState State { get; private set; }
        public bool IsStable => State == UiLifecycleState.Hidden ||
            State == UiLifecycleState.Idle;

        public void Enter(Action completed = null)
        {
            CancelScheduled();
            _element.style.display = DisplayStyle.Flex;
            _element.pickingMode = PickingMode.Ignore;
            _element.RemoveFromClassList("ui-lifecycle--exiting");
            _element.AddToClassList("ui-lifecycle--entering");
            State = UiLifecycleState.Entering;
            _scheduled = _element.schedule.Execute(() =>
            {
                _scheduled = null;
                _element.RemoveFromClassList("ui-lifecycle--entering");
                _element.pickingMode = PickingMode.Position;
                State = UiLifecycleState.Idle;
                completed?.Invoke();
            }).StartingIn(_enterMilliseconds);
        }

        public void Exit(Action completed = null)
        {
            if (State == UiLifecycleState.Hidden)
            {
                completed?.Invoke();
                return;
            }
            CancelScheduled();
            _element.pickingMode = PickingMode.Ignore;
            _element.RemoveFromClassList("ui-lifecycle--entering");
            _element.AddToClassList("ui-lifecycle--exiting");
            State = UiLifecycleState.Exiting;
            _scheduled = _element.schedule.Execute(() =>
            {
                _scheduled = null;
                ApplyHidden();
                completed?.Invoke();
            }).StartingIn(_exitMilliseconds);
        }

        public void CancelAndApply(UiLifecycleState finalState)
        {
            CancelScheduled();
            _element.RemoveFromClassList("ui-lifecycle--entering");
            _element.RemoveFromClassList("ui-lifecycle--exiting");
            if (finalState == UiLifecycleState.Hidden)
            {
                ApplyHidden();
                return;
            }
            _element.style.display = DisplayStyle.Flex;
            _element.pickingMode = finalState == UiLifecycleState.Idle
                ? PickingMode.Position
                : PickingMode.Ignore;
            State = finalState;
        }

        private void ApplyHidden()
        {
            _element.RemoveFromClassList("ui-lifecycle--entering");
            _element.RemoveFromClassList("ui-lifecycle--exiting");
            _element.style.display = DisplayStyle.None;
            _element.pickingMode = PickingMode.Ignore;
            State = UiLifecycleState.Hidden;
        }

        private void CancelScheduled()
        {
            _scheduled?.Pause();
            _scheduled = null;
        }
    }
}
