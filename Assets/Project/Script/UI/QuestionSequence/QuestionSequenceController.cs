using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using PowerMath.Gameplay.Academic;
using PowerMath.Gameplay.Academic.Infrastructure;

namespace PowerMath.UI.QuestionSequence
{
    /// <summary>
    /// Interactive controller driving the Question Sequence UI Toolkit screen.
    /// Integrates with the project's Question Fallback Catalog (InMemoryQuestionCatalogRepository)
    /// during Editor and standalone runs, supporting academic progression, dynamic equations,
    /// animated keypad entry, countdown lock-in, score multiplying rewards, and failure penalties.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    public sealed class QuestionSequenceController : MonoBehaviour
    {
        [Header("Fallback Catalog Configuration")]
        [SerializeField] private bool _useFallbackCatalog = true;
        [SerializeField] private AcademicRankTier _initialRankTier = AcademicRankTier.Silver;

        [Header("Combat Settings")]
        [SerializeField] private string _stageName = "STAGE 12";
        [SerializeField] private string _bossName = "STONEWARD";
        [SerializeField] private int _maxHp = 1000;
        [SerializeField] private int _currentHp = 780;

        [Header("Fallback Default Question (if catalog disabled)")]
        [SerializeField] private string _defaultEquation = "48 ÷ 6 = ?";
        [SerializeField] private string _defaultAnswer = "8";

        [Header("Timing Settings")]
        [SerializeField] private float _countdownSeconds = 6f;
        [SerializeField] private float _autoAdvanceDelay = 2.5f;

        private UIDocument _document;
        private VisualElement _root;
        private Label _inputValueLabel;
        private Label _healthValueLabel;
        private VisualElement _healthBarFill;
        private VisualElement _challengeCard;
        private Label _cardTitleLabel;
        private Label _stageLabel;
        private Label _bossLabel;
        private Label _equationLabel;
        private Label _academicMetaLabel;

        // Overlays
        private VisualElement _overlayCountdown;
        private Label _countdownEqLabel;
        private Label _timerSecondsLabel;

        private VisualElement _overlayReward;
        private Label _rewardEqLabel;
        private Label _rewardScoreLabel;
        private Label _rewardFormulaLabel;
        private Label _rewardDamageLabel;

        private VisualElement _overlayFailure;
        private Label _failureEqLabel;
        private Label _failureWrongValLabel;

        // Debug Buttons
        private Button _btnState1;
        private Button _btnState2;
        private Button _btnState3;
        private Button _btnState4;
        private Button _btnPrevQ;
        private Button _btnNextQ;
        private Button _btnRank;

        // Runtime State & Catalog Data
        private QuestionCatalog _catalog;
        private readonly List<QuestionDefinition> _activeQuestions = new List<QuestionDefinition>();
        private int _questionIndex = 0;
        private AcademicRankTier _currentRankTier = AcademicRankTier.Silver;
        private QuestionDefinition _currentDefinition;

        private string _currentEquation = "48 ÷ 6 = ?";
        private string _currentSolvedEquation = "48 ÷ 6 = 8";
        private string _correctAnswer = "8";
        private string _currentInput = "";
        private QuestionSequenceState _currentState = QuestionSequenceState.NumkeypadPopUp;

        private Coroutine _countdownCoroutine;
        private Coroutine _hpAnimationCoroutine;
        private Coroutine _sequenceCoroutine;

        public QuestionSequenceState CurrentState => _currentState;
        public string CurrentEquation => _currentEquation;
        public string CorrectAnswer => _correctAnswer;
        public AcademicRankTier CurrentRankTier => _currentRankTier;
        public int CurrentQuestionIndex => _questionIndex;
        public int TotalQuestionsInRank => _activeQuestions.Count;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _currentRankTier = _initialRankTier;
        }

        private void OnEnable()
        {
            if (_document == null) return;
            _root = _document.rootVisualElement;
            if (_root == null) return;

            BindElements();
            RegisterEvents();

            if (_useFallbackCatalog)
            {
                LoadFallbackCatalog();
            }
            else
            {
                SetStandaloneQuestion(_defaultEquation, _defaultAnswer, "CUSTOM • STANDALONE");
            }

            SetState(QuestionSequenceState.NumkeypadPopUp);
        }

        private void OnDisable()
        {
            UnregisterEvents();
            StopAllRunningCoroutines();
        }

        private void Update()
        {
            HandleKeyboardInput();
        }

        /// <summary>
        /// Loads the question catalog from InMemoryQuestionCatalogRepository.
        /// </summary>
        public void LoadFallbackCatalog()
        {
            var repo = new InMemoryQuestionCatalogRepository();
            repo.Load(result =>
            {
                if (result != null && result.IsSuccess && result.Catalog != null)
                {
                    _catalog = result.Catalog;
                    ApplyRank(_currentRankTier);
                }
                else
                {
                    Debug.LogWarning("[QuestionSequence] Failed to load Fallback Question Catalog. Falling back to default question.");
                    SetStandaloneQuestion(_defaultEquation, _defaultAnswer, "FALLBACK CATALOG UNAVAILABLE");
                }
            });
        }

        /// <summary>
        /// Applies an Academic Rank tier to load its respective questions.
        /// </summary>
        public void ApplyRank(AcademicRankTier tier)
        {
            _currentRankTier = tier;
            _activeQuestions.Clear();

            if (_catalog != null)
            {
                var rank = new AcademicRank(tier);
                IReadOnlyList<QuestionDefinition> questions = _catalog.GetRankQuestions(rank);
                if (questions != null && questions.Count > 0)
                {
                    _activeQuestions.AddRange(questions);
                }
            }

            if (_activeQuestions.Count > 0)
            {
                _questionIndex = 0;
                LoadQuestionAtIndex(_questionIndex);
            }
            else
            {
                SetStandaloneQuestion(_defaultEquation, _defaultAnswer, $"{tier.ToString().ToUpperInvariant()} (DEFAULT)");
            }

            UpdateRankButtonDisplay();
        }

        /// <summary>
        /// Loads a specific question definition from the active list.
        /// </summary>
        public void LoadQuestionAtIndex(int index)
        {
            if (_activeQuestions.Count == 0) return;

            _questionIndex = Mathf.Clamp(index, 0, _activeQuestions.Count - 1);
            _currentDefinition = _activeQuestions[_questionIndex];
            int ans = _currentDefinition.CorrectAnswer;
            _correctAnswer = ans.ToString();

            // Format a clean, standard arithmetic equation matching the target answer
            _currentEquation = FormatEquationForAnswer(ans, (int)_currentDefinition.Id.Value);
            _currentSolvedEquation = _currentEquation.Replace("?", _correctAnswer);

            string rankName = _currentDefinition.Rank.ToString().ToUpperInvariant();
            string metaText = $"{rankName} • QUESTION #{_currentDefinition.Id.Value} (TARGET: {ans})";

            if (_cardTitleLabel != null)
            {
                _cardTitleLabel.text = $"✦ {rankName} CHALLENGE ✦";
            }

            if (_academicMetaLabel != null)
            {
                _academicMetaLabel.text = metaText;
            }

            if (_equationLabel != null)
            {
                _equationLabel.text = _currentEquation;
            }

            if (_countdownEqLabel != null)
            {
                _countdownEqLabel.text = _currentEquation;
            }

            if (_rewardEqLabel != null)
            {
                _rewardEqLabel.text = _currentSolvedEquation;
            }

            if (_failureEqLabel != null)
            {
                _failureEqLabel.text = _currentSolvedEquation;
            }

            _currentInput = "";
            UpdateInputDisplay();
            SetState(QuestionSequenceState.NumkeypadPopUp);
        }

        public void NextQuestion()
        {
            if (_activeQuestions.Count == 0) return;
            int next = (_questionIndex + 1) % _activeQuestions.Count;
            LoadQuestionAtIndex(next);
        }

        public void PreviousQuestion()
        {
            if (_activeQuestions.Count == 0) return;
            int prev = (_questionIndex - 1 + _activeQuestions.Count) % _activeQuestions.Count;
            LoadQuestionAtIndex(prev);
        }

        public void CycleRank()
        {
            AcademicRankTier nextTier = _currentRankTier switch
            {
                AcademicRankTier.Silver => AcademicRankTier.Gold,
                AcademicRankTier.Gold => AcademicRankTier.Diamond,
                _ => AcademicRankTier.Silver
            };
            ApplyRank(nextTier);
        }

        private void SetStandaloneQuestion(string eq, string ans, string meta)
        {
            _currentEquation = eq;
            _correctAnswer = ans;
            _currentSolvedEquation = eq.Replace("?", ans);

            if (_equationLabel != null) _equationLabel.text = _currentEquation;
            if (_countdownEqLabel != null) _countdownEqLabel.text = _currentEquation;
            if (_rewardEqLabel != null) _rewardEqLabel.text = _currentSolvedEquation;
            if (_failureEqLabel != null) _failureEqLabel.text = _currentSolvedEquation;
            if (_academicMetaLabel != null) _academicMetaLabel.text = meta;

            _currentInput = "";
            UpdateInputDisplay();
        }

        private void BindElements()
        {
            _inputValueLabel = _root.Q<Label>(className: "input-value") ?? _root.Q<Label>(".input-value");
            _healthValueLabel = _root.Q<Label>(className: "health-value") ?? _root.Q<Label>(".health-value");
            _healthBarFill = _root.Q<VisualElement>(className: "health-bar-fill") ?? _root.Q<VisualElement>(".health-bar-fill");
            _challengeCard = _root.Q<VisualElement>(className: "math-challenge-card") ?? _root.Q<VisualElement>(".math-challenge-card");
            _cardTitleLabel = _root.Q<Label>(className: "card-title") ?? _root.Q<Label>(".card-title");

            _stageLabel = _root.Q<Label>(className: "stage-label") ?? _root.Q<Label>(".stage-label");
            if (_stageLabel != null) _stageLabel.text = _stageName;

            _bossLabel = _root.Q<Label>(className: "boss-name") ?? _root.Q<Label>(".boss-name");
            if (_bossLabel != null) _bossLabel.text = _bossName;

            _equationLabel = _root.Q<Label>(className: "equation-text") ?? _root.Q<Label>(".equation-text");
            _academicMetaLabel = _root.Q<Label>("academic-meta");

            // Overlays
            _overlayCountdown = _root.Q<VisualElement>("overlay-countdown");
            _countdownEqLabel = _overlayCountdown?.Q<Label>("countdown-eq");
            _timerSecondsLabel = _overlayCountdown?.Q<Label>("timer-seconds");

            _overlayReward = _root.Q<VisualElement>("overlay-reward");
            _rewardEqLabel = _overlayReward?.Q<Label>("reward-eq");
            _rewardScoreLabel = _overlayReward?.Q<Label>("reward-score");
            _rewardFormulaLabel = _overlayReward?.Q<Label>("reward-formula");
            _rewardDamageLabel = _overlayReward?.Q<Label>("reward-damage");

            _overlayFailure = _root.Q<VisualElement>("overlay-failure");
            _failureEqLabel = _overlayFailure?.Q<Label>("failure-eq");
            _failureWrongValLabel = _overlayFailure?.Q<Label>("failure-wrong-val");

            // Debug Buttons
            _btnState1 = _root.Q<Button>("btn-state-1");
            _btnState2 = _root.Q<Button>("btn-state-2");
            _btnState3 = _root.Q<Button>("btn-state-3");
            _btnState4 = _root.Q<Button>("btn-state-4");
            _btnPrevQ = _root.Q<Button>("btn-prev-q");
            _btnNextQ = _root.Q<Button>("btn-next-q");
            _btnRank = _root.Q<Button>("btn-rank");

            UpdateHpDisplay(_currentHp);
            UpdateInputDisplay();
            UpdateRankButtonDisplay();
        }

        private void RegisterEvents()
        {
            // Number Buttons 0..9
            for (int i = 0; i <= 9; i++)
            {
                int digit = i;
                var btn = _root.Q<Button>($"btn-{digit}");
                if (btn != null)
                {
                    btn.clicked += () => OnDigitClicked(digit.ToString());
                }
            }

            // Clear & Backspace
            var btnClear = _root.Q<Button>("btn-clear");
            if (btnClear != null) btnClear.clicked += OnClearClicked;

            var btnBackspace = _root.Q<Button>("btn-backspace");
            if (btnBackspace != null) btnBackspace.clicked += OnBackspaceClicked;

            // Submit
            var btnSubmit = _root.Q<Button>(className: "btn-submit") ?? _root.Q<Button>(".btn-submit");
            if (btnSubmit != null) btnSubmit.clicked += OnSubmitClicked;

            // Debug Buttons
            if (_btnState1 != null) _btnState1.clicked += () => SetState(QuestionSequenceState.NumkeypadPopUp);
            if (_btnState2 != null) _btnState2.clicked += () => SetState(QuestionSequenceState.CountingDown);
            if (_btnState3 != null) _btnState3.clicked += () => SetState(QuestionSequenceState.ScoreMultiplying);
            if (_btnState4 != null) _btnState4.clicked += () => SetState(QuestionSequenceState.Failure);
            if (_btnPrevQ != null) _btnPrevQ.clicked += PreviousQuestion;
            if (_btnNextQ != null) _btnNextQ.clicked += NextQuestion;
            if (_btnRank != null) _btnRank.clicked += CycleRank;
        }

        private void UnregisterEvents()
        {
            // Managed by UI Toolkit lifecycle
        }

        private void HandleKeyboardInput()
        {
            if (_currentState != QuestionSequenceState.NumkeypadPopUp) return;

            // Digits 0..9
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    OnDigitClicked(i.ToString());
                }
            }

            // Backspace / Delete
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                OnBackspaceClicked();
            }

            // Clear
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Escape))
            {
                OnClearClicked();
            }

            // Submit
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSubmitClicked();
            }
        }

        private void OnDigitClicked(string digit)
        {
            if (_currentInput.Length < 6)
            {
                if (_currentInput == "0") _currentInput = digit;
                else _currentInput += digit;
                UpdateInputDisplay();
                PunchCard(1.015f);
            }
        }

        private void OnBackspaceClicked()
        {
            if (_currentInput.Length > 0)
            {
                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                if (_currentInput.Length == 0) _currentInput = "";
                UpdateInputDisplay();
            }
        }

        private void OnClearClicked()
        {
            _currentInput = "";
            UpdateInputDisplay();
        }

        public void OnSubmitClicked()
        {
            if (string.IsNullOrEmpty(_currentInput)) return;

            PunchCard(0.97f);
            if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = StartCoroutine(SubmitSequenceCoroutine());
        }

        private IEnumerator SubmitSequenceCoroutine()
        {
            yield return new WaitForSeconds(0.12f);
            PunchCard(1f);

            // Transition to Countdown (State 2)
            SetState(QuestionSequenceState.CountingDown);
            yield return new WaitForSeconds(_countdownSeconds);

            // Evaluate Answer
            bool isCorrect = string.Equals(_currentInput.Trim(), _correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                SetState(QuestionSequenceState.ScoreMultiplying);
                yield return new WaitForSeconds(_autoAdvanceDelay);
                NextQuestion();
            }
            else
            {
                SetState(QuestionSequenceState.Failure);
                yield return new WaitForSeconds(_autoAdvanceDelay);
                // Allow user retry on failure
                _currentInput = "";
                UpdateInputDisplay();
                SetState(QuestionSequenceState.NumkeypadPopUp);
            }
        }

        public void SetState(QuestionSequenceState state)
        {
            _currentState = state;

            // Hide all overlays
            _overlayCountdown?.AddToClassList("is-hidden");
            _overlayReward?.AddToClassList("is-hidden");
            _overlayFailure?.AddToClassList("is-hidden");

            // Update debug buttons
            _btnState1?.RemoveFromClassList("state-btn--active");
            _btnState2?.RemoveFromClassList("state-btn--active");
            _btnState3?.RemoveFromClassList("state-btn--active");
            _btnState4?.RemoveFromClassList("state-btn--active");

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            switch (state)
            {
                case QuestionSequenceState.NumkeypadPopUp:
                    _btnState1?.AddToClassList("state-btn--active");
                    UpdateHpDisplay(_currentHp);
                    PunchCard(1.03f);
                    break;

                case QuestionSequenceState.CountingDown:
                    _btnState2?.AddToClassList("state-btn--active");
                    _overlayCountdown?.RemoveFromClassList("is-hidden");
                    if (_countdownEqLabel != null) _countdownEqLabel.text = _currentEquation;
                    _countdownCoroutine = StartCoroutine(CountdownRoutine());
                    break;

                case QuestionSequenceState.ScoreMultiplying:
                    _btnState3?.AddToClassList("state-btn--active");
                    _overlayReward?.RemoveFromClassList("is-hidden");
                    if (_rewardEqLabel != null) _rewardEqLabel.text = _currentSolvedEquation;

                    // Calculate Academic Multiplier & Damage
                    var rank = new AcademicRank(_currentRankTier);
                    double multiplier = rank.DamageMultiplier;
                    int baseScore = 100;
                    int finalScore = Mathf.RoundToInt((float)(baseScore * multiplier));
                    int damage = Mathf.RoundToInt((float)(140 * multiplier));

                    if (_rewardScoreLabel != null) _rewardScoreLabel.text = finalScore.ToString();
                    if (_rewardFormulaLabel != null) _rewardFormulaLabel.text = $"{baseScore} × {multiplier:0.0} = {finalScore} PTS";
                    if (_rewardDamageLabel != null) _rewardDamageLabel.text = $"× {multiplier:0.0}";

                    int targetHp = Mathf.Max(0, _currentHp - damage);
                    AnimateHpReduction(_currentHp, targetHp);
                    _currentHp = targetHp;
                    PunchCard(1.04f);
                    break;

                case QuestionSequenceState.Failure:
                    _btnState4?.AddToClassList("state-btn--active");
                    _overlayFailure?.RemoveFromClassList("is-hidden");
                    if (_failureEqLabel != null) _failureEqLabel.text = _currentSolvedEquation;
                    if (_failureWrongValLabel != null)
                    {
                        _failureWrongValLabel.text = string.IsNullOrEmpty(_currentInput) ? "—" : _currentInput;
                    }
                    StartCoroutine(ShakeCardRoutine());
                    break;
            }
        }

        private IEnumerator CountdownRoutine()
        {
            float remaining = _countdownSeconds;
            while (remaining > 0f)
            {
                if (_timerSecondsLabel != null)
                {
                    _timerSecondsLabel.text = Mathf.CeilToInt(remaining).ToString();
                }
                yield return null;
                remaining -= Time.deltaTime;
            }
            if (_timerSecondsLabel != null) _timerSecondsLabel.text = "0";
        }

        private void UpdateInputDisplay()
        {
            if (_inputValueLabel != null)
            {
                _inputValueLabel.text = string.IsNullOrEmpty(_currentInput) ? "_" : _currentInput;
            }
        }

        private void UpdateRankButtonDisplay()
        {
            if (_btnRank != null)
            {
                _btnRank.text = $"Rank: {_currentRankTier}";
            }
        }

        private void UpdateHpDisplay(int hp)
        {
            if (_healthValueLabel != null)
            {
                _healthValueLabel.text = $"{hp} / {_maxHp}";
            }
            if (_healthBarFill != null)
            {
                float pct = Mathf.Clamp01((float)hp / _maxHp) * 100f;
                _healthBarFill.style.width = new Length(pct, LengthUnit.Percent);
            }
        }

        private void AnimateHpReduction(int startHp, int targetHp)
        {
            if (_hpAnimationCoroutine != null) StopCoroutine(_hpAnimationCoroutine);
            _hpAnimationCoroutine = StartCoroutine(HpReductionRoutine(startHp, targetHp));
        }

        private IEnumerator HpReductionRoutine(int startHp, int targetHp)
        {
            float elapsed = 0f;
            float duration = 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int val = Mathf.RoundToInt(Mathf.Lerp(startHp, targetHp, t));
                UpdateHpDisplay(val);
                yield return null;
            }
            UpdateHpDisplay(targetHp);
        }

        private void PunchCard(float scaleTarget)
        {
            if (_challengeCard == null) return;
            _challengeCard.style.scale = new Scale(new Vector3(scaleTarget, scaleTarget, 1f));
            if (scaleTarget != 1f)
            {
                StartCoroutine(ResetScaleAfterDelay(0.12f));
            }
        }

        private IEnumerator ResetScaleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_challengeCard != null)
            {
                _challengeCard.style.scale = new Scale(Vector3.one);
            }
        }

        private IEnumerator ShakeCardRoutine()
        {
            if (_challengeCard == null) yield break;
            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * 45f) * (1f - (elapsed / duration)) * 8f;
                _challengeCard.style.translate = new Translate(offset, 0f, 0f);
                yield return null;
            }
            _challengeCard.style.translate = new Translate(0f, 0f, 0f);
        }

        private void StopAllRunningCoroutines()
        {
            if (_countdownCoroutine != null) { StopCoroutine(_countdownCoroutine); _countdownCoroutine = null; }
            if (_hpAnimationCoroutine != null) { StopCoroutine(_hpAnimationCoroutine); _hpAnimationCoroutine = null; }
            if (_sequenceCoroutine != null) { StopCoroutine(_sequenceCoroutine); _sequenceCoroutine = null; }
        }

        /// <summary>
        /// Produces a clean grade-school arithmetic equation matching the target integer answer.
        /// </summary>
        public static string FormatEquationForAnswer(int answer, int questionId)
        {
            // If answer is 8, always reproduce the exact wireframe equation "48 ÷ 6 = ?"
            if (answer == 8)
            {
                return "48 ÷ 6 = ?";
            }

            // Variety of clean math operations matching the catalog's target answers
            switch (questionId % 3)
            {
                case 1:
                    // Clean division: (answer * 6) / 6 = answer
                    int divisor = 6;
                    return $"{answer * divisor} ÷ {divisor} = ?";
                case 2:
                    // Clean addition: (answer - 4) + 4 = answer
                    int addend = Math.Max(2, answer / 3);
                    return $"{answer - addend} + {addend} = ?";
                default:
                    // Clean subtraction: (answer + 10) - 10 = answer
                    int subtrahend = 10;
                    return $"{answer + subtrahend} - {subtrahend} = ?";
            }
        }
    }
}
