using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SafeAreaController : MonoBehaviour
{
    private VisualElement safeAreaElement;

    private Rect previousSafeArea;
    private int previousScreenWidth;
    private int previousScreenHeight;

    private void OnEnable()
    {
        UIDocument document = GetComponent<UIDocument>();

        safeAreaElement =
            document.rootVisualElement.Q<VisualElement>("safe-area");

        if (safeAreaElement == null)
        {
            Debug.LogError(
                "SafeAreaController could not find an element " +
                "named 'safe-area' in the UXML document."
            );

            return;
        }

        ApplySafeArea();
    }

    private void Update()
    {
        if (Screen.safeArea != previousSafeArea ||
            Screen.width != previousScreenWidth ||
            Screen.height != previousScreenHeight)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (safeAreaElement == null ||
            Screen.width <= 0 ||
            Screen.height <= 0)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;

        float leftPercent =
            safeArea.xMin / Screen.width * 100f;

        float rightPercent =
            (Screen.width - safeArea.xMax) / Screen.width * 100f;

        // Screen.safeArea starts from the bottom-left.
        // UI Toolkit starts from the top-left.
        float topPercent =
            (Screen.height - safeArea.yMax) / Screen.height * 100f;

        float bottomPercent =
            safeArea.yMin / Screen.height * 100f;

        safeAreaElement.style.left =
            Length.Percent(leftPercent);

        safeAreaElement.style.right =
            Length.Percent(rightPercent);

        safeAreaElement.style.top =
            Length.Percent(topPercent);

        safeAreaElement.style.bottom =
            Length.Percent(bottomPercent);

        previousSafeArea = safeArea;
        previousScreenWidth = Screen.width;
        previousScreenHeight = Screen.height;
    }
}