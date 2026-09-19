using System;
using PowerMath.Gameplay.Tutorial;
using UnityEngine;

namespace PowerMath.UI.MainMenu.Tutorial
{
    [CreateAssetMenu(
        fileName = "TutorialCatalog",
        menuName = "PowerMath/Tutorial/Catalog")]
    public sealed class TutorialCatalogDefinition : ScriptableObject
    {
        [SerializeField] private TutorialSequenceDefinition[] sequences =
            Array.Empty<TutorialSequenceDefinition>();

        public bool TryGet(string tutorialId, out TutorialSequence sequence)
        {
            sequence = null;
            foreach (TutorialSequenceDefinition definition in
                sequences ?? Array.Empty<TutorialSequenceDefinition>())
            {
                if (definition == null || !definition.EnabledForPlayers ||
                    !string.Equals(definition.TutorialId, tutorialId,
                        StringComparison.Ordinal))
                    continue;
                try
                {
                    sequence = definition.Build();
                    return true;
                }
                catch (Exception exception)
                {
                    PowerMath.Diagnostics.AppLog.Error(
                        "Tutorial",
                        $"Tutorial definition '{tutorialId}' is invalid: {exception.Message}",
                        definition);
                    return false;
                }
            }
            return false;
        }
    }
}
