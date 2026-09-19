using System;
using System.Collections.Generic;
using PowerMath.Gameplay.Tutorial;
using UnityEngine;

namespace PowerMath.UI.MainMenu.Tutorial
{
    [CreateAssetMenu(
        fileName = "TutorialSequence",
        menuName = "PowerMath/Tutorial/Sequence")]
    public sealed class TutorialSequenceDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class RuleData
        {
            public TutorialSignalKind signal;
            public string nextStepId;
            public string targetId;
            public TutorialAttemptOutcome requiredOutcome;
            public bool requireMatchingTransaction;
            public bool requireDifferentEncounter;
            public bool completesSequence;

            public TutorialRule Build() => new TutorialRule(
                signal,
                nextStepId,
                targetId,
                requiredOutcome,
                requireMatchingTransaction,
                requireDifferentEncounter,
                completesSequence);
        }

        [Serializable]
        public sealed class StepData
        {
            public string id;
            public TutorialStepKind kind;
            public string speakerKey;
            public string textKey;
            public string emotionId;
            public string spriteResourcePath;
            public string audioCueId;
            public string targetId;
            public string fallbackTargetId;
            public bool hidesPresentation;
            public RuleData[] rules = Array.Empty<RuleData>();

            public TutorialStep Build()
            {
                var builtRules = new List<TutorialRule>(rules?.Length ?? 0);
                foreach (RuleData rule in rules ?? Array.Empty<RuleData>())
                {
                    if (rule != null) builtRules.Add(rule.Build());
                }
                return new TutorialStep(
                    id, kind, speakerKey, textKey, emotionId,
                    spriteResourcePath, audioCueId, targetId,
                    fallbackTargetId, hidesPresentation, builtRules);
            }
        }

        [SerializeField] private string tutorialId = "OnFirstCreate";
        [SerializeField, Min(1)] private int version = 1;
        [SerializeField] private bool enabledForPlayers = true;
        [SerializeField] private string startStepId = "welcome-new";
        [SerializeField] private string returningStartStepId = "welcome-returning";
        [Tooltip("Tutorials that must be completed before this sequence can take UI ownership.")]
        [SerializeField] private string[] prerequisiteTutorialIds = Array.Empty<string>();
        [SerializeField] private StepData[] steps = Array.Empty<StepData>();

        public string TutorialId => tutorialId;
        public int Version => version;
        public bool EnabledForPlayers => enabledForPlayers;

        public TutorialSequence Build()
        {
            var builtSteps = new List<TutorialStep>(steps?.Length ?? 0);
            foreach (StepData step in steps ?? Array.Empty<StepData>())
            {
                if (step != null) builtSteps.Add(step.Build());
            }
            return new TutorialSequence(
                tutorialId, version, startStepId, returningStartStepId, builtSteps,
                prerequisiteTutorialIds);
        }
    }

}
