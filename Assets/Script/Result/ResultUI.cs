using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class ResultUI : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI perfectText;
    public TextMeshProUGUI greatText;
    public TextMeshProUGUI goodText;
    public TextMeshProUGUI missText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI rankText;

    [Header("Transition")]
    public BookOpenEffect bookEffect;
    public Image blackFade;
    public float fadeDuration = 0.5f;
    public float sceneChangeDelay = 0.9f;

    private bool isChangingScene = false;

    void Start()
    {
        GameResultData data = GameResultManager.Instance.resultData;

        scoreText.text = "Score : " + data.score;
        perfectText.text = "Perfect : " + data.perfect;
        greatText.text = "Great : " + data.great;
        goodText.text = "Good : " + data.good;
        missText.text = "Miss : " + data.miss;
        accuracyText.text = "Accuracy : " + data.accuracy.ToString("F2") + "%";
        rankText.text = "Rank : " + data.rank;

        if (blackFade != null)
        {
            Color c = blackFade.color;
            c.a = 0f;
            blackFade.color = c;
        }
    }

    public void RetryGame()
    {
        if (isChangingScene) return;
        StartCoroutine(ChangeSceneRoutine(GameResultManager.Instance.lastStageName));
    }

    public void QuitGame()
    {
        if (isChangingScene) return;
        StartCoroutine(ChangeSceneRoutine("Title"));
    }

    IEnumerator ChangeSceneRoutine(string sceneName)
    {
        isChangingScene = true;
        Time.timeScale = 1f;

        if (bookEffect != null)
        {
            bookEffect.CloseBook();
        }

        if (blackFade != null)
        {
            blackFade.raycastTarget = true;
            blackFade.DOFade(1f, fadeDuration);
        }

        yield return new WaitForSeconds(sceneChangeDelay);

        SceneManager.LoadScene(sceneName);
    }
}