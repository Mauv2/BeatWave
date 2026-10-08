using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class TitleButtonHandler : MonoBehaviour
{
    [Header("Fade")]
    public Image blackFade;
    public float fadeDuration = 1f;

    [Header("BGM")]
    public AudioSource bgmSource;

    [Header("Scene")]
    public string selectSceneName = "Select";

    private bool isLoading = false;

    void Start()
    {
        if (blackFade != null)
        {
            Color c = blackFade.color;
            c.a = 0f;
            blackFade.color = c;

            blackFade.raycastTarget = false;
        }
    }

    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;

        Sequence seq = DOTween.Sequence();

        if (bgmSource != null)
        {
            seq.Join(bgmSource.DOFade(0f, fadeDuration));
        }

        if (blackFade != null)
        {
            seq.Join(blackFade.DOFade(1f, fadeDuration));
        }

        seq.OnComplete(() =>
        {
            SceneManager.LoadScene(selectSceneName);
        });
    }

    public void Option()
    {
        Debug.Log("설정창 열기");
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}