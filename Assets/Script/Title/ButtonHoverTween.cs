using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonHoverTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverMultiplier = 1.08f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * hoverMultiplier, 0.15f)
                 .SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(originalScale, 0.25f)
                 .SetEase(Ease.OutQuad);
    }
}