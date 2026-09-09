using System;
using System.Collections.Generic;
using UnityEngine;

namespace PowerMath.Audio
{
    [Serializable]
    public sealed class EncounterKindSfxBinding
    {
        [Tooltip("Encounter kind name: e.g. 'NormalMonster', 'EliteMonster', 'Miniboss', 'BigBoss', 'FinalBoss', 'ChallengeEvent'.")]
        [SerializeField] private string encounterKind = "BigBoss";
        [SerializeField] private SfxCueConfig appearSfx = new SfxCueConfig();
        [SerializeField] private SfxCueConfig hurtSfx = new SfxCueConfig();
        [SerializeField] private SfxCueConfig attackSfx = new SfxCueConfig();
        [SerializeField] private SfxCueConfig dieSfx = new SfxCueConfig();

        public string EncounterKind => encounterKind;
        public SfxCueConfig AppearSfx => appearSfx;
        public SfxCueConfig HurtSfx => hurtSfx;
        public SfxCueConfig AttackSfx => attackSfx;
        public SfxCueConfig DieSfx => dieSfx;

        public EncounterKindSfxBinding() { }
        public EncounterKindSfxBinding(string kind, SfxCueConfig appear, SfxCueConfig hurt, SfxCueConfig attack, SfxCueConfig die)
        {
            encounterKind = kind;
            appearSfx = appear ?? new SfxCueConfig();
            hurtSfx = hurt ?? new SfxCueConfig();
            attackSfx = attack ?? new SfxCueConfig();
            dieSfx = die ?? new SfxCueConfig();
        }
    }

    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "PowerMath/Audio/SFX Library")]
    public sealed class SfxLibraryDefinition : ScriptableObject
    {
        private const int SampleRate = 22050;

        [Header("Player State SFX")]
        [SerializeField] private SfxCueConfig playerAttackSwing = new SfxCueConfig(null, 0.75f, 0.05f);
        [SerializeField] private SfxCueConfig playerAttackFail = new SfxCueConfig(null, 0.70f, 0.03f);
        [SerializeField] private SfxCueConfig playerHit = new SfxCueConfig(null, 0.85f, 0.04f);
        [SerializeField] private SfxCueConfig playerCriticalHit = new SfxCueConfig(null, 1.0f, 0.02f);
        [SerializeField] private SfxCueConfig playerHurt = new SfxCueConfig(null, 0.80f, 0.05f);
        [SerializeField] private SfxCueConfig playerDie = new SfxCueConfig(null, 0.95f, 0.02f);

        [Header("Default Enemy State SFX")]
        [SerializeField] private SfxCueConfig enemyAppear = new SfxCueConfig(null, 0.65f, 0.04f);
        [SerializeField] private SfxCueConfig enemyHurt = new SfxCueConfig(null, 0.75f, 0.05f);
        [SerializeField] private SfxCueConfig enemyAttack = new SfxCueConfig(null, 0.85f, 0.04f);
        [SerializeField] private SfxCueConfig enemyDieNormal = new SfxCueConfig(null, 0.80f, 0.03f);
        [SerializeField] private SfxCueConfig enemyDieMajor = new SfxCueConfig(null, 1.0f, 0.02f);

        [Header("Encounter Kind Overrides")]
        [SerializeField] private EncounterKindSfxBinding[] encounterKindBindings = Array.Empty<EncounterKindSfxBinding>();

        [Header("Question Sequence SFX")]
        [SerializeField] private SfxCueConfig questionPopUp = new SfxCueConfig(null, 0.75f, 0.02f);
        [SerializeField] private SfxCueConfig questionKeypadTap = new SfxCueConfig(null, 0.50f, 0.04f);
        [SerializeField] private SfxCueConfig questionKeypadClear = new SfxCueConfig(null, 0.45f, 0.03f);
        [SerializeField] private SfxCueConfig questionKeypadSubmit = new SfxCueConfig(null, 0.70f, 0.02f);
        [SerializeField] private SfxCueConfig questionCountdownTick = new SfxCueConfig(null, 0.40f, 0.01f);
        [SerializeField] private SfxCueConfig questionCountdownWarning = new SfxCueConfig(null, 0.60f, 0.01f);
        [SerializeField] private SfxCueConfig questionResultSuccess = new SfxCueConfig(null, 0.85f, 0.02f);
        [SerializeField] private SfxCueConfig questionResultFail = new SfxCueConfig(null, 0.85f, 0.02f);
        [SerializeField] private SfxCueConfig questionDamageMultiplying = new SfxCueConfig(null, 0.80f, 0.03f);
        [SerializeField] private SfxCueConfig questionRewardSticker = new SfxCueConfig(null, 0.80f, 0.03f);

        [Header("Battle & Progression SFX")]
        [SerializeField] private SfxCueConfig battleBiomeTransition = new SfxCueConfig(null, 0.85f, 0.02f);
        [SerializeField] private SfxCueConfig battleStageAdvance = new SfxCueConfig(null, 0.75f, 0.03f);
        [SerializeField] private SfxCueConfig battleRunDefeat = new SfxCueConfig(null, 0.90f, 0.02f);
        [SerializeField] private SfxCueConfig battleRunComplete = new SfxCueConfig(null, 1.0f, 0.01f);
        [SerializeField] private SfxCueConfig actorInteractiveClick = new SfxCueConfig(null, 0.55f, 0.05f);

        [Header("Character Selection & Onboarding SFX")]
        [SerializeField] private SfxCueConfig characterCardHover = new SfxCueConfig(null, 0.40f, 0.03f);
        [SerializeField] private SfxCueConfig characterCardClick = new SfxCueConfig(null, 0.65f, 0.04f);
        [SerializeField] private SfxCueConfig characterAccepted = new SfxCueConfig(null, 0.80f, 0.02f);
        [SerializeField] private SfxCueConfig nameConfirmed = new SfxCueConfig(null, 0.85f, 0.02f);
        [SerializeField] private SfxCueConfig uiButtonClick = new SfxCueConfig(null, 0.60f, 0.03f);
        [SerializeField] private SfxCueConfig uiNavigationBack = new SfxCueConfig(null, 0.55f, 0.02f);

        [Header("Generic UI Animation SFX (USS Styles)")]
        [SerializeField] private UiAnimationSfxStyle[] uiStyles = Array.Empty<UiAnimationSfxStyle>();

        // Procedural Fallback Clips Cache
        private static AudioClip _fbSwing;
        private static AudioClip _fbFail;
        private static AudioClip _fbHit;
        private static AudioClip _fbCrit;
        private static AudioClip _fbPlayerHurt;
        private static AudioClip _fbEnemyAppear;
        private static AudioClip _fbEnemyHurt;
        private static AudioClip _fbEnemyAttack;
        private static AudioClip _fbDeathNormal;
        private static AudioClip _fbDeathMajor;
        private static AudioClip _fbPopUp;
        private static AudioClip _fbClick;
        private static AudioClip _fbTick;
        private static AudioClip _fbSuccess;
        private static AudioClip _fbFailure;
        private static AudioClip _fbMultiplier;
        private static AudioClip _fbBiomeTransition;

        public SfxCueConfig PlayerAttackSwing => playerAttackSwing;
        public SfxCueConfig PlayerAttackFail => playerAttackFail;
        public SfxCueConfig PlayerHit => playerHit;
        public SfxCueConfig PlayerCriticalHit => playerCriticalHit;
        public SfxCueConfig PlayerHurt => playerHurt;
        public SfxCueConfig PlayerDie => playerDie;

        public SfxCueConfig EnemyAppear => enemyAppear;
        public SfxCueConfig EnemyHurt => enemyHurt;
        public SfxCueConfig EnemyAttack => enemyAttack;
        public SfxCueConfig EnemyDieNormal => enemyDieNormal;
        public SfxCueConfig EnemyDieMajor => enemyDieMajor;
        public IReadOnlyList<EncounterKindSfxBinding> EncounterKindBindings => encounterKindBindings ?? Array.Empty<EncounterKindSfxBinding>();

        public SfxCueConfig QuestionPopUp => questionPopUp;
        public SfxCueConfig QuestionKeypadTap => questionKeypadTap;
        public SfxCueConfig QuestionKeypadClear => questionKeypadClear;
        public SfxCueConfig QuestionKeypadSubmit => questionKeypadSubmit;
        public SfxCueConfig QuestionCountdownTick => questionCountdownTick;
        public SfxCueConfig QuestionCountdownWarning => questionCountdownWarning;
        public SfxCueConfig QuestionResultSuccess => questionResultSuccess;
        public SfxCueConfig QuestionResultFail => questionResultFail;
        public SfxCueConfig QuestionDamageMultiplying => questionDamageMultiplying;
        public SfxCueConfig QuestionRewardSticker => questionRewardSticker;

        public SfxCueConfig BattleBiomeTransition => battleBiomeTransition;
        public SfxCueConfig BattleStageAdvance => battleStageAdvance;
        public SfxCueConfig BattleRunDefeat => battleRunDefeat;
        public SfxCueConfig BattleRunComplete => battleRunComplete;
        public SfxCueConfig ActorInteractiveClick => actorInteractiveClick;

        public SfxCueConfig CharacterCardHover => characterCardHover;
        public SfxCueConfig CharacterCardClick => characterCardClick;
        public SfxCueConfig CharacterAccepted => characterAccepted;
        public SfxCueConfig NameConfirmed => nameConfirmed;
        public SfxCueConfig UiButtonClick => uiButtonClick;
        public SfxCueConfig UiNavigationBack => uiNavigationBack;
        public IReadOnlyList<UiAnimationSfxStyle> UiStyles => uiStyles ?? Array.Empty<UiAnimationSfxStyle>();

        public EncounterKindSfxBinding FindEncounterBinding(string kind)
        {
            if (string.IsNullOrEmpty(kind) || encounterKindBindings == null) return null;
            for (int i = 0; i < encounterKindBindings.Length; i++)
            {
                if (encounterKindBindings[i] != null &&
                    string.Equals(encounterKindBindings[i].EncounterKind, kind, StringComparison.OrdinalIgnoreCase))
                {
                    return encounterKindBindings[i];
                }
            }
            return null;
        }

        public UiAnimationSfxStyle FindUiStyle(string styleClass)
        {
            if (string.IsNullOrEmpty(styleClass) || uiStyles == null) return null;
            for (int i = 0; i < uiStyles.Length; i++)
            {
                if (uiStyles[i] != null &&
                    string.Equals(uiStyles[i].StyleClass, styleClass, StringComparison.OrdinalIgnoreCase))
                {
                    return uiStyles[i];
                }
            }
            return null;
        }

        #region Procedural Fallback Waveform Generators

        public AudioClip GetFallbackClip(string cueKey)
        {
            switch (cueKey)
            {
                case "swing":
                    if (_fbSwing == null) _fbSwing = CreateSweep("SFX_FB_Swing", 920f, 260f, 0.12f, 0.15f, 0.20f);
                    return _fbSwing;
                case "fail":
                    if (_fbFail == null) _fbFail = CreateSweep("SFX_FB_Fail", 320f, 150f, 0.18f, 0.12f, 0.05f);
                    return _fbFail;
                case "hit":
                    if (_fbHit == null) _fbHit = CreateTransient("SFX_FB_Hit", 110f, 0.12f, 0.20f, 0.65f);
                    return _fbHit;
                case "crit":
                    if (_fbCrit == null) _fbCrit = CreateTransient("SFX_FB_Crit", 70f, 0.25f, 0.26f, 0.80f);
                    return _fbCrit;
                case "hurt":
                    if (_fbPlayerHurt == null) _fbPlayerHurt = CreateTransient("SFX_FB_Hurt", 140f, 0.14f, 0.18f, 0.50f);
                    return _fbPlayerHurt;
                case "enemy_appear":
                    if (_fbEnemyAppear == null) _fbEnemyAppear = CreateSweep("SFX_FB_EnemyAppear", 180f, 440f, 0.22f, 0.16f, 0.10f);
                    return _fbEnemyAppear;
                case "enemy_attack":
                    if (_fbEnemyAttack == null) _fbEnemyAttack = CreateSweep("SFX_FB_EnemyAttack", 220f, 80f, 0.22f, 0.18f, 0.12f);
                    return _fbEnemyAttack;
                case "enemy_hurt":
                    if (_fbEnemyHurt == null) _fbEnemyHurt = CreateTransient("SFX_FB_EnemyHurt", 120f, 0.10f, 0.16f, 0.60f);
                    return _fbEnemyHurt;
                case "die_normal":
                    if (_fbDeathNormal == null) _fbDeathNormal = CreateSweep("SFX_FB_DieNormal", 500f, 120f, 0.30f, 0.15f, 0.05f);
                    return _fbDeathNormal;
                case "die_major":
                    if (_fbDeathMajor == null) _fbDeathMajor = CreateTransient("SFX_FB_DieMajor", 65f, 0.45f, 0.26f, 0.70f);
                    return _fbDeathMajor;
                case "popup":
                    if (_fbPopUp == null) _fbPopUp = CreateHarmonicChord("SFX_FB_PopUp", new[] { 440f, 660f }, 0.15f, 0.14f);
                    return _fbPopUp;
                case "click":
                    if (_fbClick == null) _fbClick = CreateTone("SFX_FB_Click", 580f, 0.035f, 0.04f);
                    return _fbClick;
                case "tick":
                    if (_fbTick == null) _fbTick = CreateTone("SFX_FB_Tick", 880f, 0.025f, 0.035f);
                    return _fbTick;
                case "success":
                    if (_fbSuccess == null) _fbSuccess = CreateHarmonicChord("SFX_FB_Success", new[] { 523.25f, 659.25f, 783.99f }, 0.25f, 0.15f);
                    return _fbSuccess;
                case "failure":
                    if (_fbFailure == null) _fbFailure = CreateSweep("SFX_FB_Failure", 300f, 140f, 0.25f, 0.14f, 0.05f);
                    return _fbFailure;
                case "multiplier":
                    if (_fbMultiplier == null) _fbMultiplier = CreateHarmonicChord("SFX_FB_Multiplier", new[] { 392f, 523.25f, 659.25f, 1046.50f }, 0.35f, 0.16f);
                    return _fbMultiplier;
                case "biome_transition":
                    if (_fbBiomeTransition == null) _fbBiomeTransition = CreateHarmonicChord("SFX_FB_Biome", new[] { 392f, 493.88f, 587.33f, 783.99f }, 0.70f, 0.16f);
                    return _fbBiomeTransition;
                default:
                    return GetFallbackClip("click");
            }
        }

        private static AudioClip CreateTone(string name, float frequency, float duration, float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float fade = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate) * amplitude * fade;
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateHarmonicChord(string name, float[] frequencies, float duration, float amplitude)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            float compAmp = amplitude / Mathf.Max(1, frequencies.Length);
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float envelope = Mathf.Sin(progress * Mathf.PI);
                float val = 0f;
                for (int f = 0; f < frequencies.Length; f++)
                {
                    val += Mathf.Sin(2f * Mathf.PI * frequencies[f] * i / SampleRate) * compAmp * envelope;
                }
                samples[i] = val;
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateSweep(string name, float startFreq, float endFreq, float duration, float amplitude, float noiseMix)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);
                phase += 2f * Mathf.PI * freq / SampleRate;
                float envelope = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.35f);
                float noise = PseudoNoise(i) * noiseMix;
                samples[i] = (Mathf.Sin(phase) * (1f - noiseMix) + noise) * amplitude * envelope;
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateTransient(string name, float bodyFreq, float duration, float amplitude, float noiseMix)
        {
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float envelope = Mathf.Exp(-7f * progress);
                float body = Mathf.Sin(2f * Mathf.PI * bodyFreq * i / SampleRate);
                samples[i] = (body * (1f - noiseMix) + PseudoNoise(i) * noiseMix) * amplitude * envelope;
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float PseudoNoise(int index)
        {
            float val = Mathf.Sin(index * 12.9898f + 78.233f) * 43758.5453f;
            return (val - Mathf.Floor(val)) * 2f - 1f;
        }

        #endregion
    }
}
