using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.Audio
{
    /// <summary>
    /// Helper for UI Toolkit that scans a VisualElement hierarchy and binds hover/click SFX
    /// based on configured USS class names or UI style keys.
    /// </summary>
    public static class UiSfxAudioBinder
    {
        private const string BoundMarkerClass = "sfx-audio-bound";

        private static readonly string[] DefaultStyleClasses = new[]
        {
            "hud-icon-button",
            "auth-utility",
            "mw-utility-button",
            "mw-button",
            "hud-player-menu-button",
            "hud-player-menu-toggle",
            "leaderboard-icon-button",
            "settings-tab",
            "settings-action",
            "settings-close"
        };

        public static void Bind(VisualElement root, SfxLibraryDefinition library = null)
        {
            if (root == null) return;

            SfxLibraryDefinition lib = library ?? SfxController.Instance?.Library;

            // Gather all target class names from defaults plus library.UiStyles
            var targetClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < DefaultStyleClasses.Length; i++)
            {
                targetClasses.Add(DefaultStyleClasses[i]);
            }

            if (lib != null && lib.UiStyles != null)
            {
                for (int i = 0; i < lib.UiStyles.Count; i++)
                {
                    UiAnimationSfxStyle style = lib.UiStyles[i];
                    if (style != null && !string.IsNullOrWhiteSpace(style.StyleClass))
                    {
                        targetClasses.Add(style.StyleClass.TrimStart('.'));
                    }
                }
            }

            foreach (string className in targetClasses)
            {
                List<VisualElement> matches = root.Query<VisualElement>(className: className).ToList();

                for (int m = 0; m < matches.Count; m++)
                {
                    VisualElement element = matches[m];
                    if (element == null || element.ClassListContains(BoundMarkerClass)) continue;

                    element.AddToClassList(BoundMarkerClass);
                    string classKey = className;

                    element.RegisterCallback<MouseEnterEvent>(evt =>
                    {
                        SfxController.Instance?.PlayUiStyle(classKey, isClick: false);
                    });

                    if (element is Button btn)
                    {
                        btn.clicked += () =>
                        {
                            SfxController.Instance?.PlayUiStyle(classKey, isClick: true);
                        };
                    }
                    else
                    {
                        element.RegisterCallback<ClickEvent>(evt =>
                        {
                            SfxController.Instance?.PlayUiStyle(classKey, isClick: true);
                        });
                    }
                }
            }

            // Universal Button Fallback: Bind any remaining un-bound Button elements
            List<Button> allButtons = root.Query<Button>().ToList();
            for (int i = 0; i < allButtons.Count; i++)
            {
                Button btn = allButtons[i];
                if (btn == null || btn.ClassListContains(BoundMarkerClass)) continue;

                btn.AddToClassList(BoundMarkerClass);
                btn.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    SfxController.Instance?.PlayUiStyle("btn-default", isClick: false);
                });
                btn.clicked += () =>
                {
                    SfxController.Instance?.PlayUiStyle("btn-default", isClick: true);
                };
            }
        }
    }
}
