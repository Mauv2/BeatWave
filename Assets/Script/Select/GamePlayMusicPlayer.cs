using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayMusicPlayer : MonoBehaviour
{
    public AudioSource gameAudioSource;

    void Awake()
    {
        if (SelectedMusic.Instance != null && SelectedMusic.Instance.selectedMusic != null)
        {
            gameAudioSource.clip = SelectedMusic.Instance.selectedMusic.gameClip;
            gameAudioSource.playOnAwake = false;
        }
    }
}
