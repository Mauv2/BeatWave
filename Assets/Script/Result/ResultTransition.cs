using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Shared by every stage through JudgeManager's result path.
public static class ResultTransition
{
    public const float FadeSeconds = 0.5f;
    public const float BlackHoldSeconds = 0.35f;

    public static IEnumerator FadeToBlack()
    {
        // Stage HUDs can render behind world sprites. A dedicated overlay covers
        // the entire screen, including scenery, notes and menus in every scene.
        var root = new GameObject("Result Transition", typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var image = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(root.transform, false);
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        yield return image.DOFade(1f, FadeSeconds).SetUpdate(true).WaitForCompletion();
        yield return new WaitForSecondsRealtime(BlackHoldSeconds);
        // The overlay remains opaque until the result scene replaces this scene.
    }
}
