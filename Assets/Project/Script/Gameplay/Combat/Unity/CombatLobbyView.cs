using System;
using System.Collections;
using System.Collections.Generic;
using PowerMath.Gameplay.Combat.Presentation;
using PowerMath.UI.MainMenu;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class CombatLobbyView : IDisposable
    {
        private readonly VisualElement _root;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly VisualElement _combatLayer;
        private readonly VisualElement _enemyCard;
        private readonly Label _stageLabel;
        private readonly ProgressBar _stageProgress;
        private readonly Label _enemyName;
        private readonly ProgressBar _enemyHpBar;
        private readonly Label _enemyHpLabel;
        private readonly Label _cooldownLabel;
        private readonly VisualElement _enemyActions;
        private readonly EnemyActionQueuePresenter _enemyActionQueue;
        private readonly Label _heartsLabel;
        private readonly Button _attackButton;
        private readonly VisualElement _attemptPanel;
        private readonly VisualElement _answerContent;
        private readonly Label _timerLabel;
        private readonly Label _timerCaption;
        private readonly Label _answerLabel;
        private readonly Button _submitButton;
        private readonly Button _backspaceButton;
        private readonly Button _clearButton;
        private readonly Button[] _digitButtons;
        private readonly Action[] _digitHandlers;
        private readonly Label _resultLabel;
        private readonly VisualElement _feedbackCard;
        private readonly Label _feedbackIcon;
        private readonly Label _feedbackTitle;
        private readonly Label _feedbackSubtitle;
        private readonly VisualElement _scoreStack;
        private readonly Label _damageLabel;
        private readonly Label _criticalLabel;
        private readonly Label _battleBanner;
        private readonly Label _biomeLabel;
        private readonly Button _mapButton;
        private readonly VisualElement _mapModal;
        private readonly VisualElement _mapRoute;
        private readonly Button _mapClose;
        private readonly VisualElement _biomeTransition;
        private readonly Label _biomeTransitionTitle;
        private readonly Label _biomeTransitionKicker;
        private readonly Dictionary<string, Label> _mapNodes = new Dictionary<string, Label>();
        private Action<string> _backgroundRenderer;
        private Action<string> _encounterRenderer;
        private Func<string, float, IEnumerator> _backgroundCrossfader;
        private bool _bound;
        private readonly bool _reducedMotion;
        private readonly UiToolkitLifecycleController _attemptLifecycle;
        private readonly UiToolkitLifecycleController _feedbackLifecycle;
        private readonly UiToolkitLifecycleController _bannerLifecycle;
        private readonly UiToolkitLifecycleController _biomeLifecycle;

        public CombatLobbyView(
            VisualElement root,
            IMainMenuPanelHost panelHost = null,
            bool reducedMotion = false)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _panelHost = panelHost ?? new MainMenuPanelHost();
            _reducedMotion = reducedMotion;
            _combatLayer = Require<VisualElement>("combat-layer");
            _enemyCard = Require<VisualElement>("combat-enemy-card");
            _stageLabel = Require<Label>("combat-stage-label");
            _stageProgress = _root.Q<ProgressBar>("combat-stage-progress");
            _enemyName = Require<Label>("combat-enemy-name");
            _enemyHpBar = Require<ProgressBar>("combat-enemy-hp");
            _enemyHpLabel = Require<Label>("combat-enemy-hp-label");
            _cooldownLabel = _root.Q<Label>("combat-cooldown-label");
            _enemyActions = Require<VisualElement>("combat-enemy-actions");
            _enemyActionQueue = new EnemyActionQueuePresenter(
                new EnemyActionQueueView(_enemyActions), reducedMotion);
            _heartsLabel = Require<Label>("combat-hearts-label");
            _attackButton = _root.Q<Button>("combat-attack-button");
            _attemptPanel = Require<VisualElement>("combat-attempt-panel");
            _answerContent = Require<VisualElement>("combat-answer-content");
            _timerLabel = Require<Label>("combat-timer-label");
            _timerCaption = Require<Label>("combat-timer-caption");
            _answerLabel = Require<Label>("combat-answer-display");
            _submitButton = Require<Button>("combat-submit-button");
            _backspaceButton = Require<Button>("combat-backspace-button");
            _clearButton = Require<Button>("combat-clear-button");
            _resultLabel = Require<Label>("combat-result-label");
            _feedbackCard = Require<VisualElement>("combat-feedback-card");
            _feedbackIcon = Require<Label>("combat-feedback-icon");
            _feedbackTitle = Require<Label>("combat-feedback-title");
            _feedbackSubtitle = Require<Label>("combat-feedback-subtitle");
            _scoreStack = Require<VisualElement>("combat-score-stack");
            _damageLabel = Require<Label>("combat-damage-label");
            _criticalLabel = Require<Label>("combat-critical-label");
            _battleBanner = Require<Label>("combat-battle-banner");
            _biomeLabel = Require<Label>("combat-biome-label");
            _mapButton = Require<Button>("combat-map-button");
            _mapModal = Require<VisualElement>("combat-map-modal");
            _mapRoute = Require<VisualElement>("combat-map-route");
            _mapClose = Require<Button>("combat-map-close");
            _biomeTransition = Require<VisualElement>("combat-biome-transition");
            _biomeTransitionTitle = Require<Label>("combat-biome-transition-title");
            _biomeTransitionKicker = _root.Q<Label>("combat-biome-transition-kicker");
            _attemptLifecycle = new UiToolkitLifecycleController(_attemptPanel);
            _feedbackLifecycle = new UiToolkitLifecycleController(_feedbackCard);
            _bannerLifecycle = new UiToolkitLifecycleController(_battleBanner);
            _biomeLifecycle = new UiToolkitLifecycleController(
                _biomeTransition,
                enterMilliseconds: 420,
                exitMilliseconds: 320);

            _digitButtons = new Button[10];
            _digitHandlers = new Action[10];
            for (int digit = 0; digit <= 9; digit++)
            {
                int captured = digit;
                _digitButtons[digit] = Require<Button>($"combat-digit-{digit}");
                _digitHandlers[digit] = () => DigitRequested?.Invoke(captured);
            }
        }

        public event Action AttackRequested;
        public event Action<int> DigitRequested;
        public event Action BackspaceRequested;
        public event Action ClearRequested;
        public event Action SubmitRequested;

        public int EnemyMaximumHp { get; private set; }
        public bool CanAttack { get; private set; }

        public void RequestAttack()
        {
            if (!CanAttack) return;
            OnAttack();
        }

        public void Bind()
        {
            if (_bound)
            {
                return;
            }

            if (_attackButton != null)
            {
                _attackButton.clicked += OnAttack;
            }
            _submitButton.clicked += OnSubmit;
            _backspaceButton.clicked += OnBackspace;
            _clearButton.clicked += OnClear;
            _mapButton.clicked += OnShowMap;
            _mapClose.clicked += OnHideMap;
            for (int digit = 0; digit <= 9; digit++)
            {
                _digitButtons[digit].clicked += _digitHandlers[digit];
            }

            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            _bound = true;
        }

        public void Render(CombatSnapshot snapshot)
        {
            _combatLayer.style.display = DisplayStyle.Flex;
            _stageLabel.text = $"STAGE {snapshot.Stage.Value} / {StageId.Final}";
            if (_stageProgress != null)
                _stageProgress.value = snapshot.Stage.Value / (float)StageId.Final * 100f;
            _enemyName.text = snapshot.EnemyName;
            _biomeLabel.text = snapshot.BiomeTitle.ToUpperInvariant();
            ApplyEncounterVisuals(snapshot);
            foreach (KeyValuePair<string, Label> pair in _mapNodes)
                pair.Value.EnableInClassList("combat-map-node--current",
                    string.Equals(pair.Key, snapshot.BiomeId, StringComparison.Ordinal));
            EnemyMaximumHp = snapshot.EnemyMaximumHp;
            SetEnemyHp(snapshot.EnemyCurrentHp);
            if (_cooldownLabel != null)
            {
                _cooldownLabel.text = snapshot.EnemyRemainingCooldown <= 1
                    ? $"⚠ ATTACK IN {snapshot.EnemyRemainingCooldown}"
                    : $"Enemy attack: {snapshot.EnemyRemainingCooldown} / {snapshot.EnemyMaximumCooldown}";
                if (snapshot.IsEvent)
                    _cooldownLabel.text = "CHALLENGE: WRONG ANSWER COSTS 1 HEART";
                _cooldownLabel.EnableInClassList(
                    "combat-cooldown--danger",
                    !snapshot.IsEvent && snapshot.EnemyRemainingCooldown <= 1
                );
            }
            if (snapshot.Phase == CombatPhase.EnemyReady ||
                snapshot.Phase == CombatPhase.EventReady)
                _enemyActionQueue.Synchronize(snapshot, _enemyActions.childCount == 0);
            _heartsLabel.text = BuildHearts(
                snapshot.PlayerCurrentHearts,
                snapshot.PlayerMaximumHearts
            );
            CanAttack = snapshot.Phase == CombatPhase.EnemyReady ||
                snapshot.Phase == CombatPhase.EventReady;
            if (_attackButton != null)
            {
                _attackButton.text = "⚔";
                _attackButton.tooltip = snapshot.IsEvent
                    ? "Start Challenge"
                    : "Attack";
                _attackButton.SetEnabled(CanAttack);
            }
            _mapButton.SetEnabled(snapshot.Phase == CombatPhase.EnemyReady ||
                snapshot.Phase == CombatPhase.EventReady);
        }

        public void ConfigureStageMap(StageMapData map,
            Action<string> backgroundRenderer,
            Action<string> encounterRenderer,
            Func<string, float, IEnumerator> backgroundCrossfader = null)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            _backgroundRenderer = backgroundRenderer;
            _encounterRenderer = encounterRenderer;
            _backgroundCrossfader = backgroundCrossfader;
            _mapRoute.Clear();
            _mapNodes.Clear();
            for (int index = 0; index < map.Biomes.Count; index++)
            {
                BiomeData biome = map.Biomes[index];
                var node = new Label($"{index + 1}. {biome.Title}     STAGES {biome.FirstStage}-{biome.LastStage}");
                node.AddToClassList("combat-map-node");
                _mapRoute.Add(node);
                _mapNodes.Add(biome.Id, node);
            }
        }

        public IEnumerator PlayBiomeTransition(CombatSnapshot destination)
        {
            if (destination == null) yield break;
            _biomeTransitionTitle.text = destination.BiomeTitle.ToUpperInvariant();
            if (_biomeTransitionKicker != null)
            {
                _biomeTransitionKicker.text = "ENTERING NEW BIOME";
            }

            float crossfadeDuration = _reducedMotion ? 0.30f : 1.40f;
            float popUpHoldDuration = _reducedMotion ? 0.35f : 0.85f;

            _biomeLifecycle.Enter();

            if (_backgroundCrossfader != null)
            {
                IEnumerator crossfade = _backgroundCrossfader(destination.BiomeId, crossfadeDuration);
                if (crossfade != null)
                {
                    while (crossfade.MoveNext())
                    {
                        yield return crossfade.Current;
                    }
                }
            }
            else
            {
                _backgroundRenderer?.Invoke(destination.BiomeId);
                yield return new WaitForSecondsRealtime(crossfadeDuration);
            }

            _encounterRenderer?.Invoke(destination.EnemyId);
            yield return new WaitForSecondsRealtime(popUpHoldDuration);

            _biomeLifecycle.Exit();
            while (!_biomeLifecycle.IsStable) yield return null;
        }

        public void SetEnemyHp(int currentHp)
        {
            int safeMaximum = Mathf.Max(1, EnemyMaximumHp);
            int safeCurrent = Mathf.Clamp(currentHp, 0, safeMaximum);
            _enemyHpBar.value = safeCurrent / (float)safeMaximum * 100f;
            _enemyHpLabel.text = $"{safeCurrent} / {safeMaximum} HP";
        }

        public void ShowAttempt(bool visible)
        {
            if (visible)
            {
                CanAttack = false;
                _attackButton?.SetEnabled(false);
                ShowAnswerContent(true);
                HideAnswerFeedback();
                _attemptLifecycle.Enter(() => _attemptPanel.Focus());
            }
            else _attemptLifecycle.Exit();
        }

        public void SetRetainedQuestionLayout(bool retained)
        {
            _attemptPanel.EnableInClassList(
                "combat-attempt-panel--retained-video",
                retained);
        }

        public void ShowAnswerContent(bool visible)
        {
            _answerContent.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void ShowAnswerFeedback(
            string icon,
            string title,
            string subtitle,
            bool isPositive)
        {
            ShowAnswerContent(false);
            _resultLabel.text = string.Empty;
            _feedbackIcon.text = icon ?? string.Empty;
            _feedbackTitle.text = title ?? string.Empty;
            _feedbackSubtitle.text = subtitle ?? string.Empty;
            _feedbackCard.EnableInClassList(
                "combat-feedback-card--negative",
                !isPositive);
            _scoreStack.Clear();
            _feedbackLifecycle.Enter();
        }

        public void AddFeedbackStep(string label, string value, bool isFinal)
        {
            if (_scoreStack.childCount > 0)
            {
                var connector = new Label("▼");
                connector.pickingMode = PickingMode.Ignore;
                connector.AddToClassList("combat-score-connector");
                _scoreStack.Add(connector);
            }

            var row = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            row.AddToClassList("combat-score-row");
            row.EnableInClassList("combat-score-row--final", isFinal);

            var caption = new Label(label ?? string.Empty);
            caption.AddToClassList("combat-score-label");
            var amount = new Label(value ?? string.Empty);
            amount.AddToClassList("combat-score-value");
            row.Add(caption);
            row.Add(amount);
            _scoreStack.Add(row);
            row.schedule.Execute(() =>
                row.AddToClassList("combat-score-row--revealed"));
        }

        public void HideAnswerFeedback()
        {
            _feedbackLifecycle.Exit();
            _feedbackCard.RemoveFromClassList("combat-feedback-card--negative");
            _scoreStack.Clear();
        }

        public void ShowBattleBanner(string message, bool isPositive)
        {
            _battleBanner.text = message ?? string.Empty;
            _battleBanner.EnableInClassList(
                "combat-battle-banner--negative",
                !isPositive);
            _bannerLifecycle.Enter();
        }

        public void HideBattleBanner()
        {
            _bannerLifecycle.Exit();
            _battleBanner.RemoveFromClassList("combat-battle-banner--negative");
        }

        public void SetAnswer(string displayValue, bool canSubmit)
        {
            _answerLabel.text = string.IsNullOrEmpty(displayValue)
                ? "Enter your answer"
                : displayValue;
            _answerLabel.EnableInClassList(
                "combat-answer--placeholder",
                string.IsNullOrEmpty(displayValue)
            );
            _submitButton.SetEnabled(canSubmit);
        }

        public void SetAnswerInputEnabled(bool enabled)
        {
            for (int digit = 0; digit <= 9; digit++)
            {
                _digitButtons[digit].SetEnabled(enabled);
            }

            _backspaceButton.SetEnabled(enabled);
            _clearButton.SetEnabled(enabled);
            if (!enabled)
            {
                _submitButton.SetEnabled(false);
            }
        }

        public void SetTimer(AnswerTiming timing)
        {
            _timerLabel.text = timing.DisplayedSeconds.ToString();
            _timerCaption.text = timing.IsPreparation
                ? "GET READY - early answers score 10"
                : "SECONDS REMAINING";
            _timerLabel.EnableInClassList(
                "combat-timer--danger",
                !timing.IsPreparation && timing.RemainingSeconds <= 3d
            );
        }

        public void SetResult(string message, bool isPositive)
        {
            _resultLabel.text = message;
            _resultLabel.EnableInClassList("combat-result--positive", isPositive);
            _resultLabel.EnableInClassList("combat-result--negative", !isPositive);
        }

        public void ShowDamage(int damage, bool critical)
        {
            _damageLabel.text = damage > 0 ? $"-{damage}" : string.Empty;
            _damageLabel.style.display = damage > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _criticalLabel.style.display = critical
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _damageLabel.RemoveFromClassList("combat-damage--animate");
            _damageLabel.schedule.Execute(() =>
                _damageLabel.AddToClassList("combat-damage--animate"));
        }

        public void HideDamage()
        {
            _damageLabel.style.display = DisplayStyle.None;
            _criticalLabel.style.display = DisplayStyle.None;
            _damageLabel.RemoveFromClassList("combat-damage--animate");
        }

        public void SetEnemyHit(bool active, bool critical)
        {
            _enemyCard.EnableInClassList("combat-enemy--hit", active && !critical);
            _enemyCard.EnableInClassList("combat-enemy--critical", active && critical);
        }

        public bool IsEnemyActionQueueStable => _enemyActionQueue.IsStable;
        public bool IsBlockingUiStable => _attemptLifecycle.IsStable &&
            _feedbackLifecycle.IsStable && _bannerLifecycle.IsStable &&
            _biomeLifecycle.IsStable;

        public void ArmEnemyAction(string presentationId)
        {
            _enemyActionQueue.ArmNext(presentationId);
        }

        public IEnumerator ConsumeEnemyAction(
            string presentationId,
            EnemyActionTokenKind kind,
            bool cancelled)
        {
            yield return _enemyActionQueue.ConsumeArmed(
                presentationId, kind, cancelled);
        }

        public void InitiateEnemyActions(CombatSnapshot snapshot)
        {
            _enemyActionQueue.Synchronize(snapshot, true);
        }

        public void PrepareEncounterPresentation(CombatSnapshot snapshot)
        {
            if (snapshot == null) return;
            ApplyEncounterVisuals(snapshot);
            _enemyName.text = snapshot.EnemyName;
            EnemyMaximumHp = snapshot.EnemyMaximumHp;
            SetEnemyHp(snapshot.EnemyCurrentHp);
        }

        public void RebuildRecoveredEnemyActions(AttemptPresentationReceipt receipt)
        {
            if (receipt == null) return;
            CombatPresentationSnapshot source = receipt.Source;
            var snapshot = new CombatSnapshot(
                source.Stage,
                source.EncounterId,
                source.EncounterId,
                source.EnemyCurrentHp,
                source.EnemyMaximumHp,
                source.EnemyRemainingCooldown,
                source.EnemyMaximumCooldown,
                source.PlayerCurrentHearts,
                source.PlayerMaximumHearts,
                CombatPhase.EnemyReady,
                false,
                source.BiomeId,
                source.BiomeId,
                source.EncounterKind,
                string.Empty,
                0);
            _enemyActionQueue.Synchronize(snapshot, false);
            _enemyActionQueue.ArmNext(receipt.PresentationId);
        }

        public void SetUnavailable(string playerMessage)
        {
            _combatLayer.style.display = DisplayStyle.Flex;
            CanAttack = false;
            _attackButton?.SetEnabled(false);
            _resultLabel.text = playerMessage;
            _resultLabel.AddToClassList("combat-result--negative");
        }

        public void Dispose()
        {
            if (!_bound)
            {
                return;
            }

            if (_attackButton != null)
            {
                _attackButton.clicked -= OnAttack;
            }
            _submitButton.clicked -= OnSubmit;
            _backspaceButton.clicked -= OnBackspace;
            _clearButton.clicked -= OnClear;
            _mapButton.clicked -= OnShowMap;
            _mapClose.clicked -= OnHideMap;
            for (int digit = 0; digit <= 9; digit++)
            {
                _digitButtons[digit].clicked -= _digitHandlers[digit];
            }

            _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            _panelHost.ForceCloseAll();
            _bound = false;
        }

        private T Require<T>(string name) where T : VisualElement
        {
            T element = _root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException(
                    $"CombatLobbyView requires UI element '{name}'."
                );
            }

            return element;
        }

        private void OnAttack()
        {
            AttackRequested?.Invoke();
        }

        private void OnShowMap()
        {
            _panelHost.TryOpen(
                MainMenuPanelId.WorldMap,
                _mapModal,
                _mapButton
            );
        }

        private void OnHideMap()
        {
            _panelHost.TryClose(MainMenuPanelId.WorldMap, _mapButton);
        }

        private void ApplyEncounterVisuals(CombatSnapshot snapshot)
        {
            _backgroundRenderer?.Invoke(snapshot.BiomeId);
            _encounterRenderer?.Invoke(snapshot.EnemyId);
        }

        private void OnSubmit()
        {
            SubmitRequested?.Invoke();
        }

        private void OnBackspace()
        {
            BackspaceRequested?.Invoke();
        }

        private void OnClear()
        {
            ClearRequested?.Invoke();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode >= KeyCode.Alpha0 && evt.keyCode <= KeyCode.Alpha9)
            {
                DigitRequested?.Invoke((int)evt.keyCode - (int)KeyCode.Alpha0);
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode >= KeyCode.Keypad0 && evt.keyCode <= KeyCode.Keypad9)
            {
                DigitRequested?.Invoke((int)evt.keyCode - (int)KeyCode.Keypad0);
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Backspace)
            {
                BackspaceRequested?.Invoke();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Delete)
            {
                ClearRequested?.Invoke();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Return ||
                     evt.keyCode == KeyCode.KeypadEnter)
            {
                SubmitRequested?.Invoke();
                evt.StopPropagation();
            }
        }

        private static string BuildHearts(int current, int maximum)
        {
            char[] chars = new char[Mathf.Max(1, maximum) * 2 - 1];
            int cursor = 0;
            for (int index = 0; index < maximum; index++)
            {
                chars[cursor++] = index < current ? '♥' : '♡';
                if (index < maximum - 1)
                {
                    chars[cursor++] = ' ';
                }
            }

            return new string(chars);
        }
    }
}
