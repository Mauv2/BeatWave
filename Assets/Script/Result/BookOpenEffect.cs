using UnityEngine;
using DG.Tweening;

public class BookOpenEffect : MonoBehaviour
{
    [Header("Book Objects")]
    [SerializeField] private GameObject closedBook;
    [SerializeField] private GameObject openBook;
    [SerializeField] private CanvasGroup contentPanel;

    [Header("Auto Play")]
    [SerializeField] private bool openOnStart = true;
    [SerializeField] private float startDelay = 0.3f;

    [Header("Open Animation")]
    [SerializeField] private float closedShrinkScale = 0.8f;
    [SerializeField] private float closedShrinkDuration = 0.2f;
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private Ease openEase = Ease.OutBack;

    [Header("Content Animation")]
    [SerializeField] private float contentFadeDelay = 0.2f;
    [SerializeField] private float contentFadeDuration = 0.3f;

    private bool isOpen = false;

    private void Start()
    {
        ResetState();

        if (openOnStart)
        {
            Invoke(nameof(OpenBook), startDelay);
        }
    }

    private void ResetState()
    {
        closedBook.SetActive(true);
        openBook.SetActive(false);

        closedBook.transform.localScale = Vector3.one;
        openBook.transform.localScale = new Vector3(0f, 1f, 1f);

        if (contentPanel != null)
        {
            contentPanel.alpha = 0f;
            contentPanel.interactable = false;
            contentPanel.blocksRaycasts = false;
        }

        isOpen = false;
    }

    public void OpenBook()
    {
        if (isOpen) return;
        isOpen = true;

        Sequence seq = DOTween.Sequence();

        seq.Append(
            closedBook.transform
                .DOScale(closedShrinkScale, closedShrinkDuration)
                .SetEase(Ease.InBack)
        );

        seq.AppendCallback(() =>
        {
            closedBook.SetActive(false);
            openBook.SetActive(true);
            openBook.transform.localScale = new Vector3(0f, 1f, 1f);
        });

        seq.Append(
            openBook.transform
                .DOScaleX(1f, openDuration)
                .SetEase(openEase)
        );

        if (contentPanel != null)
        {
            seq.AppendInterval(contentFadeDelay);

            seq.Append(
                contentPanel.DOFade(1f, contentFadeDuration)
            );

            seq.AppendCallback(() =>
            {
                contentPanel.interactable = true;
                contentPanel.blocksRaycasts = true;
            });

            seq.AppendCallback(() =>
            {
                contentPanel.interactable = true;
                contentPanel.blocksRaycasts = true;
            });
        }
    }

    public void CloseBook()
    {
        if (!isOpen) return;
        isOpen = false;

        Sequence seq = DOTween.Sequence();

        if (contentPanel != null)
        {
            contentPanel.interactable = false;
            contentPanel.blocksRaycasts = false;
            seq.Append(contentPanel.DOFade(0f, 0.2f));
        }

        seq.Append(
            openBook.transform
                .DOScaleX(0f, 0.3f)
                .SetEase(Ease.InBack)
        );

        seq.AppendCallback(() =>
        {
            openBook.SetActive(false);
            closedBook.SetActive(true);
            closedBook.transform.localScale = Vector3.one * closedShrinkScale;
        });

        seq.Append(
            closedBook.transform
                .DOScale(1f, 0.25f)
                .SetEase(Ease.OutBack)
        );
    }
}