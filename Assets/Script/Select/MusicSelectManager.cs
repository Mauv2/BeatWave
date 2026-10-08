using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class MusicSelectManager : MonoBehaviour
{
    public AudioSource previewAudioSource;

    public MusicData firstMusic;

    [Header("Fade")]
    public Image blackFade;
    public float fadeDuration = 0.5f;

    private MusicData currentPreviewMusic;
    private bool isTransitioning = false;

    void Start()
    {
        if (blackFade != null)
        {
            Color c = blackFade.color;
            c.a = 0f;
            blackFade.color = c;
            blackFade.raycastTarget = false;
        }

        if (firstMusic != null)
        {
            PlayPreview(firstMusic);
        }
    }

    public void SelectMusic(MusicData musicData)
    {
        if (isTransitioning) return;

        if (currentPreviewMusic == musicData)
        {
            SelectedMusic.Instance.selectedMusic = musicData;
            FadeToScene(musicData.sceneName);
            return;
        }

        PlayPreview(musicData);
    }

    void PlayPreview(MusicData musicData)
    {
        currentPreviewMusic = musicData;
        SelectedMusic.Instance.selectedMusic = musicData;

        if (previewAudioSource.isPlaying)
        {
            previewAudioSource.Stop();
        }

        previewAudioSource.clip = musicData.previewClip;
        previewAudioSource.Play();
    }

    void FadeToScene(string sceneName)
    {
        isTransitioning = true;

        if (blackFade != null)
        {
            blackFade.raycastTarget = true;

            blackFade.DOFade(1f, fadeDuration)
                .OnComplete(() =>
                {
                    SceneManager.LoadScene(sceneName);
                });
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public void GoToTitle()
    {
        if (isTransitioning) return;

        FadeToScene("Title");
    }
}