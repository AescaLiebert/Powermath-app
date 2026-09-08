#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PowerMath.UI.QuestionSequence.Editor
{
    /// <summary>
    /// Editor utility providing one-click menu access to the Question Sequence wireframe test scene.
    /// </summary>
    public static class QuestionSequenceSceneLauncher
    {
        private const string ScenePath = "Assets/Project/Scenes/QuestionSequenceScene.unity";

        [MenuItem("PowerMath/UI/Open Question Sequence Scene", priority = 10)]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
                Debug.Log($"[QuestionSequence] Opened scene: {ScenePath}");
            }
        }
    }
}
#endif
