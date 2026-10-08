using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{

    [Header("Fade")]
    public Image blackFade;
    public float fadeDuration = 0.5f;


    public GameObject PauseMenu;

    public AudioSource gameAudio;

    // 추가
    public PauseMenuEffect pauseEffect;

    bool isPaused = false;

    void Start()
    {
        if (PauseMenu != null)
        {
            PauseMenu.SetActive(false);
        }

        if (blackFade != null)
        {
            Color c = blackFade.color;
            c.a = 0f;
            blackFade.color = c;
        }

        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        RhythmClock.GetOrCreate().SetMenuPaused(true);
        if (PauseMenu != null)
        {
            PauseMenu.SetActive(true);
        }

        if (gameAudio != null && gameAudio != RhythmClock.GetOrCreate().Source)
        {
            gameAudio.Pause();
        }

        Time.timeScale = 0f;

        isPaused = true;
    }

    public void ResumeGame()
    {
        if (pauseEffect != null)
        {
            pauseEffect.CloseMenu();

            StartCoroutine(ResumeAfterAnimation());
        }
        else
        {
            FinishResume();
        }
    }

    IEnumerator ResumeAfterAnimation()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        FinishResume();
    }

    void FinishResume()
    {
        RhythmClock.GetOrCreate().SetMenuPaused(false);
        if (gameAudio != null && gameAudio != RhythmClock.GetOrCreate().Source)
        {
            gameAudio.UnPause();
        }

        Time.timeScale = 1f;

        isPaused = false;
    }

    public void RetryGame()
    {
        StartCoroutine(
            FadeAndLoadScene(
                SceneManager.GetActiveScene().name
            )
        );
    }

    public void GoToSelectScene()
    {
        StartCoroutine(
            FadeAndLoadScene("Select")
        );
    }

    public void GoToTitle()
    {
        StartCoroutine(
            FadeAndLoadScene("Title")
        );
    }

    IEnumerator FadeAndLoadScene(string sceneName)
    {
        Time.timeScale = 1f;

        if (blackFade != null)
        {
            blackFade.raycastTarget = true;

            yield return blackFade
                .DOFade(1f, fadeDuration)
                .SetUpdate(true)
                .WaitForCompletion();
        }

        SceneManager.LoadScene(sceneName);
    }
}
