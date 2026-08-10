using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Gameplay.Combat.Unity
{
    public sealed class CombatLobbyView : IDisposable
    {
        private readonly VisualElement _root;
        private readonly VisualElement _combatLayer;
        private readonly VisualElement _enemyCard;
        private readonly Image _enemyImage;
        private readonly Label _stageLabel;
        private readonly ProgressBar _stageProgress;
        private readonly Label _enemyName;
        private readonly ProgressBar _enemyHpBar;
        private readonly Label _enemyHpLabel;
        private readonly Label _cooldownLabel;
        private readonly Label _heartsLabel;
        private readonly Button _attackButton;
        private readonly VisualElement _attemptPanel;
        private readonly Label _timerLabel;
        private readonly Label _timerCaption;
        private readonly Label _answerLabel;
        private readonly Button _submitButton;
        private readonly Button _backspaceButton;
        private readonly Button _clearButton;
        private readonly Button[] _digitButtons;
        private readonly Action[] _digitHandlers;
        private readonly Label _resultLabel;
        private readonly Label _damageLabel;
        private readonly Label _criticalLabel;
        private readonly Label _simulationBadge;
        private bool _bound;

        public CombatLobbyView(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _combatLayer = Require<VisualElement>("combat-layer");
            _enemyCard = Require<VisualElement>("combat-enemy-card");
            _enemyImage = Require<Image>("combat-enemy-image");
            _stageLabel = Require<Label>("combat-stage-label");
            _stageProgress = Require<ProgressBar>("combat-stage-progress");
            _enemyName = Require<Label>("combat-enemy-name");
            _enemyHpBar = Require<ProgressBar>("combat-enemy-hp");
            _enemyHpLabel = Require<Label>("combat-enemy-hp-label");
            _cooldownLabel = Require<Label>("combat-cooldown-label");
            _heartsLabel = Require<Label>("combat-hearts-label");
            _attackButton = Require<Button>("combat-attack-button");
            _attemptPanel = Require<VisualElement>("combat-attempt-panel");
            _timerLabel = Require<Label>("combat-timer-label");
            _timerCaption = Require<Label>("combat-timer-caption");
            _answerLabel = Require<Label>("combat-answer-display");
            _submitButton = Require<Button>("combat-submit-button");
            _backspaceButton = Require<Button>("combat-backspace-button");
            _clearButton = Require<Button>("combat-clear-button");
            _resultLabel = Require<Label>("combat-result-label");
            _damageLabel = Require<Label>("combat-damage-label");
            _criticalLabel = Require<Label>("combat-critical-label");
            _simulationBadge = Require<Label>("combat-simulation-badge");

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

        public void Bind()
        {
            if (_bound)
            {
                return;
            }

            _attackButton.clicked += OnAttack;
            _submitButton.clicked += OnSubmit;
            _backspaceButton.clicked += OnBackspace;
            _clearButton.clicked += OnClear;
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
            _stageProgress.value = snapshot.Stage.Value / (float)StageId.Final * 100f;
            _enemyName.text = snapshot.EnemyName;
            EnemyMaximumHp = snapshot.EnemyMaximumHp;
            SetEnemyHp(snapshot.EnemyCurrentHp);
            _cooldownLabel.text = snapshot.EnemyRemainingCooldown <= 1
                ? $"⚠ ATTACK IN {snapshot.EnemyRemainingCooldown}"
                : $"Enemy attack: {snapshot.EnemyRemainingCooldown} / {snapshot.EnemyMaximumCooldown}";
            _cooldownLabel.EnableInClassList(
                "combat-cooldown--danger",
                snapshot.EnemyRemainingCooldown <= 1
            );
            _heartsLabel.text = BuildHearts(
                snapshot.PlayerCurrentHearts,
                snapshot.PlayerMaximumHearts
            );
            _simulationBadge.style.display = snapshot.IsSimulation
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _attackButton.SetEnabled(snapshot.Phase == CombatPhase.EnemyReady);
        }

        public void SetEnemyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                _enemyImage.image = texture;
            }
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
            _attemptPanel.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _attackButton.SetEnabled(!visible);
            if (visible)
            {
                _attemptPanel.Focus();
            }
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

        public void SetUnavailable(string playerMessage)
        {
            _combatLayer.style.display = DisplayStyle.Flex;
            _attackButton.SetEnabled(false);
            _resultLabel.text = playerMessage;
            _resultLabel.AddToClassList("combat-result--negative");
        }

        public void Dispose()
        {
            if (!_bound)
            {
                return;
            }

            _attackButton.clicked -= OnAttack;
            _submitButton.clicked -= OnSubmit;
            _backspaceButton.clicked -= OnBackspace;
            _clearButton.clicked -= OnClear;
            for (int digit = 0; digit <= 9; digit++)
            {
                _digitButtons[digit].clicked -= _digitHandlers[digit];
            }

            _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
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
