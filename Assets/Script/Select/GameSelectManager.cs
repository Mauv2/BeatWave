using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSelectManager : MonoBehaviour
{
    [Header("Cards")]
    public RectTransform[] cards;

    [Header("Music Data")]
    public MusicData[] musicDatas;

    [Header("UI")]
    public Image selectedMusicImage;

    [Header("Audio")]
    public AudioSource previewAudioSource;

    [Header("Vinyl Rotate")]
    public RectTransform vinylCircle;
    public float rotateAmount = 20f;
    public float rotateDuration = 0.35f;

    [Header("Move")]
    public float moveDuration = 0.35f;
    public Ease moveEase = Ease.OutBack;

    [Header("Position Settings")]
    public float baseX = -600f;
    public float cardSpacing = 250f;
    public float arcAmount = 120f;

    [Header("Scale Settings")]
    public float normalScale = 0.8f;
    public float selectedScale = 1.15f;

    int currentIndex = 0;

    void Start()
    {
        UpdateCards();
        UpdateSelectedMusic();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            currentIndex--;

            if (currentIndex < 0)
                currentIndex = cards.Length - 1;

            UpdateCards();
            UpdateSelectedMusic();
            RotateVinyl();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            currentIndex++;

            if (currentIndex >= cards.Length)
                currentIndex = 0;

            UpdateCards();
            UpdateSelectedMusic();
            RotateVinyl();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
    }

    void UpdateSelectedMusic()
    {
        if (musicDatas.Length == 0) return;

        MusicData currentMusic = musicDatas[currentIndex];

        if (currentMusic == null) return;

        SelectedMusic.Instance.selectedMusic = currentMusic;

        if (selectedMusicImage != null)
        {
            selectedMusicImage.sprite = currentMusic.musicImage;
        }

        if (previewAudioSource != null)
        {
            previewAudioSource.Stop();
            previewAudioSource.clip = currentMusic.previewClip;
            previewAudioSource.Play();
        }
    }

    void RotateVinyl()
    {
        if (vinylCircle == null) return;

        vinylCircle.DOKill();
        vinylCircle.localRotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();

        seq.Append(
            vinylCircle.DOLocalRotate(
                new Vector3(0, 0, -rotateAmount),
                rotateDuration * 0.45f
            ).SetEase(Ease.OutQuad)
        );

        seq.Append(
            vinylCircle.DOLocalRotate(
                Vector3.zero,
                rotateDuration * 0.55f
            ).SetEase(Ease.OutBack)
        );
    }

    void StartGame()
    {
        MusicData currentMusic = musicDatas[currentIndex];

        if (currentMusic == null) return;

        SelectedMusic.Instance.selectedMusic = currentMusic;
        SceneManager.LoadScene(currentMusic.sceneName);
    }

    void UpdateCards()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            RectTransform card = cards[i];

            int positionIndex = i - currentIndex;

            if (positionIndex < 0)
                positionIndex += cards.Length;

            int half = cards.Length / 2;
            int visualOffset = positionIndex;

            if (visualOffset > half)
                visualOffset -= cards.Length;

            float y = -visualOffset * cardSpacing;
            float x = baseX - Mathf.Abs(visualOffset) * arcAmount;

            Vector2 targetPos = new Vector2(x, y);

            float targetScale = visualOffset == 0 ? selectedScale : normalScale;

            card.DOAnchorPos(targetPos, moveDuration).SetEase(moveEase);
            card.DOScale(targetScale, moveDuration).SetEase(moveEase);
            card.DOLocalRotate(Vector3.zero, moveDuration);

            if (visualOffset == 0)
                card.SetSiblingIndex(cards.Length);
            else
                card.SetSiblingIndex(cards.Length - Mathf.Abs(visualOffset));
        }
    }
}