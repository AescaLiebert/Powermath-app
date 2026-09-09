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
        public static void Bind(VisualElement root, SfxLibraryDefinition library = null)
        {
            if (root == null) return;

            SfxLibraryDefinition lib = library ?? SfxController.Instance?.Library;
            if (lib == null) return;

            IReadOnlyList<UiAnimationSfxStyle> styles = lib.UiStyles;
            if (styles == null || styles.Count == 0) return;

            for (int i = 0; i < styles.Count; i++)
            {
                UiAnimationSfxStyle style = styles[i];
                if (style == null || string.IsNullOrWhiteSpace(style.StyleClass)) continue;

                string className = style.StyleClass.TrimStart('.');
                List<VisualElement> matches = root.Query<VisualElement>(className: className).ToList();

                for (int m = 0; m < matches.Count; m++)
                {
                    VisualElement element = matches[m];
                    string classKey = className;

                    element.RegisterCallback<MouseEnterEvent>(evt =>
                    {
                        SfxController.Instance.PlayUiStyle(classKey, isClick: false);
                    });

                    if (element is Button btn)
                    {
                        btn.clicked += () =>
                        {
                            SfxController.Instance.PlayUiStyle(classKey, isClick: true);
                        };
                    }
                    else
                    {
                        element.RegisterCallback<ClickEvent>(evt =>
                        {
                            SfxController.Instance.PlayUiStyle(classKey, isClick: true);
                        });
                    }
                }
            }
        }
    }
}
