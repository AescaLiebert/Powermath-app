using System;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Academic.Unity
{
    public sealed class AcademicProgressionView : IDisposable
    {
        private readonly Label _rankLabel;
        private readonly Label _multiplierLabel;
        private readonly Label _activeCurrencyLabel;
        private readonly Label _localBadge;
        private readonly Label _questionMeta;
        private readonly Label _questionPrompt;
        private readonly Label _currencyGain;
        private readonly VisualElement _rankModal;
        private readonly Label _rankModalHeader;
        private readonly Label _rankModalRoute;
        private readonly Label _rankModalBody;
        private readonly Label _rankModalMultiplier;
        private readonly Button _rankContinueButton;
        private bool _bound;

        public AcademicProgressionView(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _rankLabel = Require<Label>(root, "academic-rank-label");
            _multiplierLabel = Require<Label>(root, "academic-rank-multiplier");
            _activeCurrencyLabel = Require<Label>(root, "academic-active-currency");
            _localBadge = Require<Label>(root, "academic-local-badge");
            _questionMeta = Require<Label>(root, "academic-question-meta");
            _questionPrompt = Require<Label>(root, "academic-question-prompt");
            _currencyGain = Require<Label>(root, "academic-currency-gain");
            _rankModal = Require<VisualElement>(root, "academic-rank-modal");
            _rankModalHeader = Require<Label>(root, "academic-rank-modal-header");
            _rankModalRoute = Require<Label>(root, "academic-rank-modal-route");
            _rankModalBody = Require<Label>(root, "academic-rank-modal-body");
            _rankModalMultiplier = Require<Label>(
                root,
                "academic-rank-modal-multiplier"
            );
            _rankContinueButton = Require<Button>(
                root,
                "academic-rank-continue-button"
            );
        }

        public event Action ContinueRequested;

        public void Bind()
        {
            if (_bound)
            {
                return;
            }

            _rankContinueButton.clicked += OnContinue;
            _bound = true;
        }

        public void Render(AcademicProgressionProjection projection)
        {
            AcademicRank rank = projection.ActiveRank;
            _rankLabel.text = $"RANK {rank.ToString().ToUpperInvariant()}";
            _multiplierLabel.text = $"Damage ×{rank.DamageMultiplier:0.0}";
            _activeCurrencyLabel.text =
                $"{rank}: {projection.Balances.Get(rank)}";
            _localBadge.style.display = projection.IsLocal
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void ShowQuestion(QuestionPresentationDescriptor question)
        {
            _questionMeta.text = $"{question.Id} • {question.Rank}";
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

            _rankModalHeader.text = transition.IsPromotion
                ? "RANK UP"
                : "RANK ADJUSTED";
            _rankModalRoute.text = $"{transition.Previous}  →  {transition.Current}";
            _rankModalBody.text = transition.IsPromotion
                ? "Your next questions will match your new Rank."
                : "Questions have been adjusted to your current level.";
            _rankModalMultiplier.text =
                $"New damage multiplier: ×{transition.Current.DamageMultiplier:0.0}";
            _rankModal.EnableInClassList(
                "academic-rank-modal--promotion",
                transition.IsPromotion
            );
            _rankModal.EnableInClassList(
                "academic-rank-modal--adjustment",
                transition.IsDemotion
            );
            _rankModal.style.display = DisplayStyle.Flex;
            _rankContinueButton.Focus();
        }

        public void HideRankTransition()
        {
            _rankModal.style.display = DisplayStyle.None;
            _rankModal.RemoveFromClassList("academic-rank-modal--promotion");
            _rankModal.RemoveFromClassList("academic-rank-modal--adjustment");
        }

        public void Dispose()
        {
            if (!_bound)
            {
                return;
            }

            _rankContinueButton.clicked -= OnContinue;
            _bound = false;
        }

        private void OnContinue()
        {
            ContinueRequested?.Invoke();
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
