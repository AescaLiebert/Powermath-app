using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Academic.Unity
{
    public sealed class AcademicProgressionView : IDisposable
    {
        private readonly Label _rankLabel;
        private readonly Label _multiplierLabel;
        private readonly Label _activeCurrencyLabel;
        private readonly Label _questionMeta;
        private readonly Label _questionPrompt;
        private readonly Label _currencyGain;
        private readonly VisualElement _rankModal;
        private readonly Label _rankModalHeader;
        private readonly Label _rankModalRoute;
        private readonly Label _rankModalBody;
        private readonly Label _rankModalMultiplier;
        private readonly Button _rankContinueButton;
        private readonly VisualElement _rankRouteContainer;
        private readonly VisualElement _rankBadgePrev;
        private readonly VisualElement _rankBadgeCurr;
        private readonly Label _rankIconPrev;
        private readonly Label _rankIconCurr;
        private readonly Label _rankNamePrev;
        private readonly Label _rankNameCurr;
        private readonly Label _rankTapPrompt;
        private readonly VisualElement _dashboardRankIcon;
        private readonly VisualElement _root;
        private bool _bound;
        private IVisualElementScheduledItem _scheduledEntrance;

        public AcademicProgressionView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _root = root;
            _rankLabel = Require<Label>(root, "academic-rank-label");
            _multiplierLabel = Require<Label>(root, "academic-rank-multiplier");
            _activeCurrencyLabel = Require<Label>(root, "academic-active-currency");
            _questionMeta = Require<Label>(root, "academic-question-meta");
            _questionPrompt = Require<Label>(root, "academic-question-prompt");
            _currencyGain = Require<Label>(root, "academic-currency-gain");
            _rankModal = Require<VisualElement>(root, "academic-rank-modal");
            _rankModalHeader = Require<Label>(root, "academic-rank-modal-header");
            _rankModalRoute = root.Q<Label>("academic-rank-modal-route");
            _rankModalBody = root.Q<Label>("academic-rank-modal-body");
            _rankModalMultiplier = root.Q<Label>("academic-rank-modal-multiplier");
            _rankContinueButton = root.Q<Button>("academic-rank-continue-button");

            _rankRouteContainer = root.Q<VisualElement>("academic-rank-route-container");
            _rankBadgePrev = root.Q<VisualElement>("academic-rank-badge-prev");
            _rankBadgeCurr = root.Q<VisualElement>("academic-rank-badge-curr");
            _rankIconPrev = root.Q<Label>("academic-rank-icon-prev");
            _rankIconCurr = root.Q<Label>("academic-rank-icon-curr");
            _rankNamePrev = root.Q<Label>("academic-rank-name-prev");
            _rankNameCurr = root.Q<Label>("academic-rank-name-curr");
            _rankTapPrompt = root.Q<Label>("academic-rank-tap-prompt");
            _dashboardRankIcon = root.Q<VisualElement>("Rank Icon");

            HideRankTransition();
        }

        public event Action ContinueRequested;

        public void Bind()
        {
            if (_bound)
            {
                return;
            }

            if (_rankContinueButton != null)
            {
                _rankContinueButton.clicked += OnTap;
                _rankContinueButton.RegisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.TrickleDown);
                _rankContinueButton.RegisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.NoTrickleDown);
                _rankContinueButton.RegisterCallback<ClickEvent>(OnClickEvent, TrickleDown.TrickleDown);
                _rankContinueButton.RegisterCallback<ClickEvent>(OnClickEvent, TrickleDown.NoTrickleDown);
                _rankContinueButton.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
                _rankContinueButton.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.NoTrickleDown);
                _rankContinueButton.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
                _rankContinueButton.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.NoTrickleDown);
            }

            _rankModal.RegisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.TrickleDown);
            _rankModal.RegisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.NoTrickleDown);
            _rankModal.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            _rankModal.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.NoTrickleDown);
            _rankModal.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
            _rankModal.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.NoTrickleDown);
            _rankModal.RegisterCallback<ClickEvent>(OnClickEvent, TrickleDown.TrickleDown);
            _rankModal.RegisterCallback<ClickEvent>(OnClickEvent, TrickleDown.NoTrickleDown);

            if (_root != null)
            {
                _root.RegisterCallback<PointerDownEvent>(OnRootPointerDownEvent, TrickleDown.TrickleDown);
                _root.RegisterCallback<ClickEvent>(OnRootClickEvent, TrickleDown.TrickleDown);
                _root.RegisterCallback<NavigationSubmitEvent>(OnRootSubmitEvent, TrickleDown.TrickleDown);
            }

            _bound = true;
        }

        public void Render(AcademicProgressionProjection projection)
        {
            AcademicRank rank = projection.ActiveRank;
            _rankLabel.text = $"RANK {rank.ToString().ToUpperInvariant()}";
            _multiplierLabel.text = $"Damage ×{rank.DamageMultiplier:0.0}";
            _activeCurrencyLabel.text =
                $"{rank}: {projection.Balances.Get(rank)}";

            if (_dashboardRankIcon != null)
            {
                string tier = rank.ToString().ToLowerInvariant();
                _dashboardRankIcon.RemoveFromClassList("hud-slot-icon--rank-silver");
                _dashboardRankIcon.RemoveFromClassList("hud-slot-icon--rank-gold");
                _dashboardRankIcon.RemoveFromClassList("hud-slot-icon--rank-diamond");
                _dashboardRankIcon.AddToClassList($"hud-slot-icon--rank-{tier}");
            }
        }

        public void ShowQuestion(QuestionPresentationDescriptor question)
        {
            _questionMeta.text = $"{question.Id} - {question.Rank}";
            _questionPrompt.text = question.DevelopmentPrompt;
            _questionMeta.style.display = DisplayStyle.Flex;
            _questionPrompt.style.display = string.IsNullOrEmpty(question.DevelopmentPrompt)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        public void ClearQuestion()
        {
            _questionMeta.text = string.Empty;
            _questionPrompt.text = string.Empty;
            _questionMeta.style.display = DisplayStyle.None;
            _questionPrompt.style.display = DisplayStyle.None;
        }

        public void ShowCurrencyGain(AcademicAttemptResult result)
        {
            if (result.CurrencyDelta <= 0)
            {
                _currencyGain.text = string.Empty;
                _currencyGain.style.display = DisplayStyle.None;
                return;
            }

            _currencyGain.text = $"+{result.CurrencyDelta} {result.RankAtCommit}";
            _currencyGain.style.display = DisplayStyle.Flex;
        }

        public void ClearCurrencyGain()
        {
            _currencyGain.text = string.Empty;
            _currencyGain.style.display = DisplayStyle.None;
        }

        public void ShowRankTransition(RankTransition transition)
        {
            if (!transition.Changed)
            {
                return;
            }

            CancelScheduled();

            _rankModalHeader.text = transition.IsPromotion
                ? "RANK UP"
                : "RANK ADJUSTED";

            if (_rankModalRoute != null)
            {
                _rankModalRoute.text = $"{transition.Previous}  →  {transition.Current}";
            }

            if (_rankModalBody != null)
            {
                _rankModalBody.text = string.Empty;
            }

            if (_rankModalMultiplier != null)
            {
                _rankModalMultiplier.text = string.Empty;
            }

            ConfigureRankBadge(
                _rankBadgePrev,
                _rankIconPrev,
                _rankNamePrev,
                transition.Previous
            );
            ConfigureRankBadge(
                _rankBadgeCurr,
                _rankIconCurr,
                _rankNameCurr,
                transition.Current
            );

            _rankModal.RemoveFromClassList("academic-rank-modal--dismissing");
            _rankModal.EnableInClassList(
                "academic-rank-modal--promotion",
                transition.IsPromotion
            );
            _rankModal.EnableInClassList(
                "academic-rank-modal--adjustment",
                transition.IsDemotion
            );

            _rankRouteContainer?.RemoveFromClassList("academic-rank-route--revealed");
            _rankModal.AddToClassList("academic-rank-modal--entering");
            _rankModal.style.display = DisplayStyle.Flex;
            _rankModal.BringToFront();
            if (_rankContinueButton != null)
            {
                _rankContinueButton.BringToFront();
                _rankContinueButton.Focus();
            }

            // Settle rank banner entrance and fade in text route
            _scheduledEntrance = _rankModal.schedule.Execute(() =>
            {
                _scheduledEntrance = null;
                _rankRouteContainer?.AddToClassList("academic-rank-route--revealed");
            }).StartingIn(280);
        }

        public IEnumerator PlayDismissAnimation()
        {
            CancelScheduled();

            // Clear text immediately
            _rankRouteContainer?.RemoveFromClassList("academic-rank-route--revealed");

            // Slide away animation
            _rankModal.AddToClassList("academic-rank-modal--dismissing");

            float elapsed = 0f;
            while (elapsed < 0.24f)
            {
                elapsed += Time.unscaledDeltaTime > 0 ? Time.unscaledDeltaTime : 0.02f;
                yield return null;
            }
        }

        public void HideRankTransition()
        {
            CancelScheduled();
            _rankModal.style.display = DisplayStyle.None;
            _rankModal.RemoveFromClassList("academic-rank-modal--entering");
            _rankModal.RemoveFromClassList("academic-rank-modal--revealed");
            _rankModal.RemoveFromClassList("academic-rank-modal--dismissing");
            _rankModal.RemoveFromClassList("academic-rank-modal--promotion");
            _rankModal.RemoveFromClassList("academic-rank-modal--adjustment");
            _rankRouteContainer?.RemoveFromClassList("academic-rank-route--revealed");
        }

        public void Dispose()
        {
            if (!_bound)
            {
                return;
            }

            CancelScheduled();
            if (_rankContinueButton != null)
            {
                _rankContinueButton.clicked -= OnTap;
                _rankContinueButton.UnregisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.TrickleDown);
                _rankContinueButton.UnregisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.NoTrickleDown);
                _rankContinueButton.UnregisterCallback<ClickEvent>(OnClickEvent, TrickleDown.TrickleDown);
                _rankContinueButton.UnregisterCallback<ClickEvent>(OnClickEvent, TrickleDown.NoTrickleDown);
                _rankContinueButton.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
                _rankContinueButton.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.NoTrickleDown);
                _rankContinueButton.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
                _rankContinueButton.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.NoTrickleDown);
            }

            _rankModal.UnregisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.TrickleDown);
            _rankModal.UnregisterCallback<NavigationSubmitEvent>(OnSubmitEvent, TrickleDown.NoTrickleDown);
            _rankModal.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            _rankModal.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.NoTrickleDown);
            _rankModal.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
            _rankModal.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.NoTrickleDown);
            _rankModal.UnregisterCallback<ClickEvent>(OnClickEvent, TrickleDown.TrickleDown);
            _rankModal.UnregisterCallback<ClickEvent>(OnClickEvent, TrickleDown.NoTrickleDown);

            if (_root != null)
            {
                _root.UnregisterCallback<PointerDownEvent>(OnRootPointerDownEvent, TrickleDown.TrickleDown);
                _root.UnregisterCallback<ClickEvent>(OnRootClickEvent, TrickleDown.TrickleDown);
                _root.UnregisterCallback<NavigationSubmitEvent>(OnRootSubmitEvent, TrickleDown.TrickleDown);
            }

            _bound = false;
        }

        private void OnRootPointerDownEvent(PointerDownEvent evt)
        {
            if (IsRankModalVisible())
            {
                OnTap();
            }
        }

        private void OnRootClickEvent(ClickEvent evt)
        {
            if (IsRankModalVisible())
            {
                OnTap();
            }
        }

        private void OnRootSubmitEvent(NavigationSubmitEvent evt)
        {
            if (IsRankModalVisible())
            {
                OnTap();
            }
        }

        private bool IsRankModalVisible()
        {
            return _rankModal != null && _rankModal.style.display.value == DisplayStyle.Flex;
        }

        private void OnSubmitEvent(NavigationSubmitEvent evt)
        {
            OnTap();
        }

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            OnTap();
        }

        private void OnPointerUpEvent(PointerUpEvent evt)
        {
            OnTap();
        }

        private void OnClickEvent(ClickEvent evt)
        {
            OnTap();
        }

        public void RequestContinue()
        {
            OnTap();
        }

        private void OnTap()
        {
            ContinueRequested?.Invoke();
        }

        private void CancelScheduled()
        {
            _scheduledEntrance?.Pause();
            _scheduledEntrance = null;
        }

        private static void ConfigureRankBadge(
            VisualElement badge,
            Label icon,
            Label name,
            AcademicRank rank)
        {
            string rankStr = rank.ToString();
            string upper = rankStr.ToUpperInvariant();
            string initial = rankStr.Length > 0 ? rankStr.Substring(0, 1).ToUpperInvariant() : string.Empty;
            string tierClass = rankStr.ToLowerInvariant();

            if (name != null)
            {
                name.text = upper;
            }

            if (icon != null)
            {
                icon.text = initial;
                icon.RemoveFromClassList("hud-rank-icon--silver");
                icon.RemoveFromClassList("hud-rank-icon--gold");
                icon.RemoveFromClassList("hud-rank-icon--diamond");
                icon.AddToClassList($"hud-rank-icon--{tierClass}");
            }

            if (badge != null)
            {
                badge.RemoveFromClassList("academic-rank-badge--silver");
                badge.RemoveFromClassList("academic-rank-badge--gold");
                badge.RemoveFromClassList("academic-rank-badge--diamond");
                badge.AddToClassList($"academic-rank-badge--{tierClass}");
            }
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException(
                    $"AcademicProgressionView requires UI element '{name}'."
                );
            }

            return element;
        }
    }
}
