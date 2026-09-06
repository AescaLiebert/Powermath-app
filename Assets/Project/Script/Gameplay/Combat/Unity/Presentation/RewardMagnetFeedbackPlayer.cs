using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Unity;
using PowerMath.Gameplay.Combat.Presentation;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity
{
    public enum RewardCurrencyKind
    {
        RankSilver,
        RankGold,
        RankDiamond,
        PowerCoin
    }

    public sealed class RewardMagnetFeedbackPlayer : IDisposable
    {
        private readonly VisualElement _root;
        private readonly IUiMotionDriver _motionDriver;
        private readonly AcademicAudioPlayer _audio;
        private readonly CombatJuiceProfileDefinition _profile;
        private readonly Camera _uiCamera;
        private VisualElement _overlayLayer;
        private readonly List<UiMotionHandle> _activeTweens = new List<UiMotionHandle>();
        private bool _disposed;

        public RewardMagnetFeedbackPlayer(
            VisualElement root,
            IUiMotionDriver motionDriver,
            AcademicAudioPlayer audio = null,
            CombatJuiceProfileDefinition profile = null,
            Camera uiCamera = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _motionDriver = motionDriver;
            _audio = audio;
            _profile = profile;
            _uiCamera = uiCamera;

            EnsureOverlay();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelAll();
            if (_overlayLayer != null && _overlayLayer.parent != null)
            {
                _overlayLayer.RemoveFromHierarchy();
                _overlayLayer = null;
            }
        }

        public void CancelAll()
        {
            foreach (UiMotionHandle handle in _activeTweens)
            {
                handle?.Cancel();
            }
            _activeTweens.Clear();
            if (_overlayLayer != null)
            {
                _overlayLayer.Clear();
            }
        }

        public IEnumerator PlayRewardDropAndMagnet(
            RewardCurrencyKind kind,
            long startAmount,
            long grantAmount,
            Vector2 screenOrWorldOrigin,
            Action<long> onIncrement = null)
        {
            if (grantAmount <= 0) yield break;

            EnsureOverlay();
            VisualElement target = ResolveTargetElement(kind);
            VisualElement bounceTarget = ResolveBounceTarget(kind, target);
            long[] portions = RewardPortionCalculator.CalculatePortions(grantAmount);
            if (portions == null || portions.Length == 0) yield break;

            Vector2 originPanel = ConvertToPanelCoordinates(screenOrWorldOrigin);
            bool reducedMotion = _motionDriver != null && _motionDriver.ReducedMotion;
            float popSeconds = reducedMotion ? 0.08f : (_profile != null ? _profile.RewardPopSeconds : 0.30f);
            float flightSeconds = reducedMotion ? 0.15f : (_profile != null ? _profile.RewardMagnetFlightSeconds : 0.48f);
            float staggerSeconds = reducedMotion ? 0.02f : (_profile != null ? _profile.RewardStaggerSeconds : 0.06f);

            int completedCount = 0;
            long accumulated = 0;
            int totalIcons = portions.Length;

            for (int i = 0; i < totalIcons; i++)
            {
                int index = i;
                long portion = portions[index];
                VisualElement icon = CreateRewardIcon(kind);
                _overlayLayer.Add(icon);

                // Position at origin
                SetPosition(icon, originPanel);
                icon.style.scale = new Scale(Vector3.zero);
                icon.style.opacity = 1f;

                // Scatter calculation
                float angle = totalIcons == 1
                    ? 0f
                    : Mathf.Lerp(-55f, 55f, (float)index / (totalIcons - 1));
                float rad = angle * Mathf.Deg2Rad;
                float radius = reducedMotion ? 20f : UnityEngine.Random.Range(45f, 75f);
                Vector2 scatterOffset = new Vector2(Mathf.Sin(rad) * radius, -Mathf.Cos(rad) * radius * 0.7f + 15f);
                Vector2 scatterPos = originPanel + scatterOffset;

                // Launch Coroutine or Staggered Animation
                _overlayLayer.schedule.Execute(() =>
                {
                    if (_disposed) return;
                    AnimateSingleRewardIcon(
                        icon,
                        originPanel,
                        scatterPos,
                        target,
                        bounceTarget,
                        popSeconds,
                        flightSeconds,
                        portion,
                        () =>
                        {
                            completedCount++;
                            accumulated += portion;
                            long currentTotal = startAmount + accumulated;

                            // Target scale bounce on Rank Currency Icon
                            PlayTargetBounce(bounceTarget ?? target);

                            // Audio
                            _audio?.PlayCurrency();

                            // Incremental text callback
                            onIncrement?.Invoke(currentTotal);
                            UpdateTargetTextDirect(target, kind, currentTotal);

                            // Clean up icon
                            if (icon.parent != null)
                                icon.RemoveFromHierarchy();
                        });
                }).StartingIn((long)(index * staggerSeconds * 1000f));
            }

            // Wait until all icons complete
            while (completedCount < totalIcons && !_disposed)
            {
                yield return null;
            }
        }

        private void AnimateSingleRewardIcon(
            VisualElement icon,
            Vector2 startPos,
            Vector2 scatterPos,
            VisualElement textTarget,
            VisualElement bounceTarget,
            float popDuration,
            float flightDuration,
            long portion,
            Action onLanded)
        {
            if (_disposed || icon == null) return;

            VisualElement flightTarget = bounceTarget ?? textTarget;

            // Phase 1: Pop and Scatter
            if (_motionDriver != null)
            {
                UiMotionHandle popTween = _motionDriver.Tween(
                    icon,
                    UiMotionChannel.Feedback,
                    popDuration,
                    UiMotionEasing.OutBack,
                    t =>
                    {
                        if (_disposed || icon.parent == null) return;
                        float scale = Mathf.Lerp(0.2f, 1.15f, t);
                        icon.style.scale = new Scale(new Vector3(scale, scale, 1f));
                        Vector2 currentPos = Vector2.Lerp(startPos, scatterPos, t);
                        SetPosition(icon, currentPos);
                    },
                    () =>
                    {
                        if (_disposed || icon.parent == null) return;

                        // Phase 2: Magnet flight to target
                        Vector2 finalScatter = scatterPos;
                        UiMotionHandle flightTween = _motionDriver.Tween(
                            icon,
                            UiMotionChannel.Feedback,
                            flightDuration,
                            UiMotionEasing.OutCubic,
                            t =>
                            {
                                if (_disposed || icon.parent == null) return;
                                Vector2 targetPos = GetTargetPosition(flightTarget);
                                // Accelerated curve towards target
                                float curvedT = t * t;
                                Vector2 currentPos = Vector2.Lerp(finalScatter, targetPos, curvedT);
                                SetPosition(icon, currentPos);

                                float scale = Mathf.Lerp(1.15f, 0.65f, t);
                                icon.style.scale = new Scale(new Vector3(scale, scale, 1f));
                                icon.style.opacity = Mathf.Lerp(1f, 0.85f, t);
                            },
                            () =>
                            {
                                onLanded?.Invoke();
                            });
                        if (flightTween != null && flightTween.IsActive)
                            _activeTweens.Add(flightTween);
                    });
                if (popTween != null && popTween.IsActive)
                    _activeTweens.Add(popTween);
            }
            else
            {
                // Fallback instant
                onLanded?.Invoke();
            }
        }

        private void PlayTargetBounce(VisualElement target)
        {
            if (target == null || _motionDriver == null) return;

            bool reducedMotion = _motionDriver.ReducedMotion;
            float bounceScale = reducedMotion ? 1.08f : (_profile != null ? _profile.RewardTargetBounceScale : 1.35f);
            float duration = reducedMotion ? 0.10f : (_profile != null ? _profile.RewardTargetBounceSeconds : 0.22f);

            UiMotionHandle bounce = _motionDriver.Tween(
                target,
                UiMotionChannel.Feedback,
                duration,
                UiMotionEasing.OutBack,
                t =>
                {
                    if (target == null) return;
                    float s = t < 0.45f
                        ? Mathf.Lerp(1f, bounceScale, t / 0.45f)
                        : Mathf.Lerp(bounceScale, 1f, (t - 0.45f) / 0.55f);
                    target.style.scale = new Scale(new Vector3(s, s, 1f));
                },
                () =>
                {
                    if (target != null) target.style.scale = new Scale(Vector3.one);
                });
            if (bounce != null && bounce.IsActive)
                _activeTweens.Add(bounce);
        }

        private VisualElement CreateRewardIcon(RewardCurrencyKind kind)
        {
            var icon = new VisualElement();
            icon.pickingMode = PickingMode.Ignore;
            icon.AddToClassList("reward-magnet-icon");

            switch (kind)
            {
                case RewardCurrencyKind.RankSilver:
                    icon.AddToClassList("reward-magnet-icon--silver");
                    break;
                case RewardCurrencyKind.RankGold:
                    icon.AddToClassList("reward-magnet-icon--gold");
                    break;
                case RewardCurrencyKind.RankDiamond:
                    icon.AddToClassList("reward-magnet-icon--diamond");
                    break;
                case RewardCurrencyKind.PowerCoin:
                default:
                    icon.AddToClassList("reward-magnet-icon--coin");
                    break;
            }

            return icon;
        }

        private VisualElement ResolveTargetElement(RewardCurrencyKind kind)
        {
            if (_root == null) return null;

            switch (kind)
            {
                case RewardCurrencyKind.RankSilver:
                    return _root.Q<Label>("profile-silver-value") ??
                           _root.Q<Label>("academic-active-currency") ??
                           _root.Q<VisualElement>("academic-rank-label");
                case RewardCurrencyKind.RankGold:
                    return _root.Q<Label>("profile-gold-value") ??
                           _root.Q<Label>("academic-active-currency") ??
                           _root.Q<VisualElement>("academic-rank-label");
                case RewardCurrencyKind.RankDiamond:
                    return _root.Q<Label>("profile-diamond-value") ??
                           _root.Q<Label>("academic-active-currency") ??
                           _root.Q<VisualElement>("academic-rank-label");
                case RewardCurrencyKind.PowerCoin:
                default:
                    return _root.Q<Label>("Currency_Value") ??
                           _root.Q<Label>("player-menu-power-coins") ??
                           _root.Q<Label>("main-menu-utility-power-coins") ??
                           _root.Q<VisualElement>("Player Menu");
            }
        }

        public VisualElement ResolveBounceTarget(RewardCurrencyKind kind, VisualElement targetText)
        {
            if (_root == null) return targetText;

            switch (kind)
            {
                case RewardCurrencyKind.RankSilver:
                    return _root.Q<VisualElement>("sil_cur")?.Q<VisualElement>("icon") ??
                           _root.Q<VisualElement>("sil_cur") ??
                           _root.Q<VisualElement>("Rank Icon") ??
                           targetText;
                case RewardCurrencyKind.RankGold:
                    return _root.Q<VisualElement>("gold_cur")?.Q<VisualElement>("icon") ??
                           _root.Q<VisualElement>("gold_cur") ??
                           _root.Q<VisualElement>("Rank Icon") ??
                           targetText;
                case RewardCurrencyKind.RankDiamond:
                    return _root.Q<VisualElement>("dia_cur")?.Q<VisualElement>("icon") ??
                           _root.Q<VisualElement>("dia_cur") ??
                           _root.Q<VisualElement>("Rank Icon") ??
                           targetText;
                case RewardCurrencyKind.PowerCoin:
                default:
                    return _root.Q<VisualElement>("Power-Coin-Icon") ??
                           _root.Q<VisualElement>("Currency_Value")?.parent?.Q<VisualElement>("icon") ??
                           _root.Q<VisualElement>("player-menu-power-coins")?.parent?.Q<VisualElement>("icon") ??
                           _root.Q<VisualElement>("main-menu-utility-power-coins")?.parent?.Q<VisualElement>("icon") ??
                           targetText;
            }
        }

        private Vector2 GetTargetPosition(VisualElement target)
        {
            if (target != null && target.panel != null)
            {
                Rect bounds = target.worldBound;
                if (bounds.width > 0f && bounds.height > 0f)
                {
                    return bounds.center;
                }
            }
            // Fallback top-left
            return new Vector2(100f, 60f);
        }

        private Vector2 ConvertToPanelCoordinates(Vector2 screenPoint)
        {
            if (screenPoint.x != 0f || screenPoint.y != 0f)
            {
                // In Unity UI Toolkit, (0,0) is top-left while Screen is bottom-left
                float y = Screen.height > 0 ? (Screen.height - screenPoint.y) : screenPoint.y;
                return new Vector2(screenPoint.x, y);
            }

            // Fallback center of screen
            if (_root != null && _root.panel != null)
            {
                return new Vector2(
                    _root.resolvedStyle.width > 0 ? _root.resolvedStyle.width * 0.5f : 400f,
                    _root.resolvedStyle.height > 0 ? _root.resolvedStyle.height * 0.45f : 300f);
            }
            return new Vector2(400f, 300f);
        }

        private static void SetPosition(VisualElement element, Vector2 pos)
        {
            element.style.left = pos.x - 18f; // Half of 36px icon
            element.style.top = pos.y - 18f;
        }

        private static void UpdateTargetTextDirect(
            VisualElement target,
            RewardCurrencyKind kind,
            long amount)
        {
            if (target is Label label)
            {
                if (label.name == "academic-active-currency")
                {
                    string rankPrefix = kind == RewardCurrencyKind.RankSilver ? "Silver"
                        : kind == RewardCurrencyKind.RankGold ? "Gold" : "Diamond";
                    label.text = $"{rankPrefix}: {amount}";
                }
                else if (label.name == "main-menu-utility-power-coins")
                {
                    label.text = $"⚡ {amount:N0}";
                }
                else
                {
                    label.text = amount.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        private void EnsureOverlay()
        {
            if (_overlayLayer == null || _overlayLayer.parent == null)
            {
                _overlayLayer = _root.Q<VisualElement>("reward-magnet-overlay");
                if (_overlayLayer == null)
                {
                    _overlayLayer = new VisualElement
                    {
                        name = "reward-magnet-overlay",
                        pickingMode = PickingMode.Ignore
                    };
                    _overlayLayer.AddToClassList("reward-magnet-overlay");
                    _root.Add(_overlayLayer);
                }
            }
        }
    }
}
