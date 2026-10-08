using UnityEngine;
using DG.Tweening;

public class PauseMenuEffect : MonoBehaviour
{
    [Header("Background")]
    public CanvasGroup darkPanel;

    [Header("Frame")]
    public CanvasGroup frameGroup;
    public RectTransform frameRect;

    [Header("Buttons")]
    public CanvasGroup buttonGroup;

    private Sequence openSequence;
    private Sequence closeSequence;

    void OnEnable()
    {
        PlayOpenAnimation();
    }

    public void PlayOpenAnimation()
    {
        if (openSequence != null)
            openSequence.Kill();

        if (closeSequence != null)
            closeSequence.Kill();

        darkPanel.alpha = 0f;

        frameGroup.alpha = 0f;
        frameRect.localScale = Vector3.one * 0.9f;

        buttonGroup.alpha = 0f;
        buttonGroup.interactable = false;
        buttonGroup.blocksRaycasts = false;

        openSequence = DOTween.Sequence();
        openSequence.SetUpdate(true);

        openSequence.Append(
            darkPanel.DOFade(0.55f, 0.2f)
        );

        openSequence.Append(
            frameGroup.DOFade(1f, 0.35f)
        );

        openSequence.Join(
            frameRect.DOScale(1f, 0.35f)
                     .SetEase(Ease.OutBack)
        );

        openSequence.Append(
            buttonGroup.DOFade(1f, 0.25f)
        );

        openSequence.OnComplete(() =>
        {
            buttonGroup.interactable = true;
            buttonGroup.blocksRaycasts = true;
        });
    }

    public void CloseMenu()
    {
        if (openSequence != null)
            openSequence.Kill();

        if (closeSequence != null)
            closeSequence.Kill();

        buttonGroup.interactable = false;
        buttonGroup.blocksRaycasts = false;

        closeSequence = DOTween.Sequence();
        closeSequence.SetUpdate(true);

        closeSequence.Append(
            buttonGroup.DOFade(0f, 0.15f)
        );

        closeSequence.Append(
            frameGroup.DOFade(0f, 0.2f)
        );

        closeSequence.Join(
            frameRect.DOScale(0.9f, 0.2f)
        );

        closeSequence.Append(
            darkPanel.DOFade(0f, 0.15f)
        );

        closeSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}