using System;
using System.Collections.Generic;
using PowerMath.Gameplay.Combat.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class EnemyActionQueueView
    {
        private readonly VisualElement _root;
        private readonly List<Label> _tokens = new List<Label>();
        private IVisualElementScheduledItem _initiateStart;
        private IVisualElementScheduledItem _initiateComplete;

        public EnemyActionQueueView(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public int Count => _tokens.Count;
        public bool HasArmedToken =>
            _tokens.Exists(t => t.ClassListContains("hud-enemy-action--armed"));

        public void Rebuild(CombatSnapshot snapshot, bool entering)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            CancelInitiate();
            _root.Clear();
            _tokens.Clear();

            if (snapshot.IsEvent)
            {
                AddToken(EnemyActionTokenKind.EventRisk, entering, false, true);
                return;
            }

            int maximum = Mathf.Max(1, snapshot.EnemyMaximumCooldown);
            int remaining = Mathf.Clamp(snapshot.EnemyRemainingCooldown, 0, maximum);
            int spent = Mathf.Max(0, maximum - remaining);
            for (int index = spent; index < maximum; index++)
            {
                bool isAttack = index == maximum - 1;
                EnemyActionTokenKind kind = isAttack
                    ? EnemyActionTokenKind.Attack
                    : EnemyActionTokenKind.Walk;
                AddToken(kind, entering, false, remaining <= 1 && isAttack);
            }
        }

        public void PlayInitiate(bool reducedMotion, Action completed)
        {
            CancelInitiate();
            int startDelayMs = reducedMotion ? 1 : 16;
            int settleDelayMs = reducedMotion ? 1 : 220;
            _initiateStart = _root.schedule.Execute(() =>
            {
                CompleteReflow();
                _initiateStart = null;
            }).StartingIn(startDelayMs);
            _initiateComplete = _root.schedule.Execute(() =>
            {
                _initiateComplete = null;
                completed?.Invoke();
            }).StartingIn(startDelayMs + settleDelayMs);
        }

        public void CancelInitiate()
        {
            _initiateStart?.Pause();
            _initiateComplete?.Pause();
            _initiateStart = null;
            _initiateComplete = null;
        }

        public void ArmFirst()
        {
            Label token = _tokens.Find(t => !t.ClassListContains("hud-enemy-action--spent"));
            token?.AddToClassList("hud-enemy-action--armed");
        }

        public Vector2[] CaptureSurvivorPositions()
        {
            int armedIndex = _tokens.FindIndex(t =>
                t.ClassListContains("hud-enemy-action--armed") ||
                t.ClassListContains("hud-enemy-action--consuming") ||
                t.ClassListContains("hud-enemy-action--cancelled"));
            if (armedIndex < 0) armedIndex = 0;
            var list = new List<Vector2>();
            for (int index = 0; index < _tokens.Count; index++)
            {
                if (index != armedIndex)
                    list.Add(_tokens[index].worldBound.position);
            }
            return list.ToArray();
        }

        public void BeginFirstExit(bool cancelled)
        {
            Label armed = _tokens.Find(t => t.ClassListContains("hud-enemy-action--armed"));
            if (armed == null) return;
            armed.RemoveFromClassList("hud-enemy-action--armed");
            armed.AddToClassList(cancelled
                ? "hud-enemy-action--cancelled"
                : "hud-enemy-action--consuming");
        }

        public void RemoveFirst()
        {
            int index = _tokens.FindIndex(t =>
                t.ClassListContains("hud-enemy-action--consuming") ||
                t.ClassListContains("hud-enemy-action--cancelled"));
            if (index < 0)
            {
                index = _tokens.FindIndex(t => !t.ClassListContains("hud-enemy-action--spent"));
            }
            if (index < 0 && _tokens.Count > 0) index = 0;
            if (index >= 0 && index < _tokens.Count)
            {
                Label target = _tokens[index];
                _tokens.RemoveAt(index);
                target.RemoveFromHierarchy();
            }
        }

        public void ApplyInverseReflow(Vector2[] oldPositions)
        {
            int count = Mathf.Min(oldPositions?.Length ?? 0, _tokens.Count);
            for (int index = 0; index < count; index++)
            {
                Vector2 delta = oldPositions[index] - _tokens[index].worldBound.position;
                _tokens[index].style.translate = new Translate(delta.x, delta.y);
                _tokens[index].AddToClassList("hud-enemy-action--reflowing");
            }
        }

        public void CompleteReflow()
        {
            for (int index = 0; index < _tokens.Count; index++)
            {
                _tokens[index].style.translate = new Translate(0f, 0f);
                _tokens[index].RemoveFromClassList("hud-enemy-action--reflowing");
                _tokens[index].RemoveFromClassList("hud-enemy-action--entering");
            }
        }

        private void AddToken(
            EnemyActionTokenKind kind,
            bool entering,
            bool isSpent = false,
            bool isDanger = false)
        {
            string text = kind == EnemyActionTokenKind.Attack
                ? "⚔"
                : kind == EnemyActionTokenKind.EventRisk
                    ? "!"
                    : "→";
            var token = new Label(text)
            {
                pickingMode = PickingMode.Ignore,
                userData = kind
            };
            token.AddToClassList("hud-enemy-action");
            token.EnableInClassList("hud-enemy-action--attack",
                kind != EnemyActionTokenKind.Walk);
            token.EnableInClassList("hud-enemy-action--spent", isSpent);
            token.EnableInClassList("hud-enemy-action--danger", isDanger);
            token.EnableInClassList("hud-enemy-action--entering", entering);
            _tokens.Add(token);
            _root.Add(token);
        }
    }
}
