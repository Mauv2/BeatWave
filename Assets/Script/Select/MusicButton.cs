using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicButton : MonoBehaviour
{
    public MusicData musicData;
    public MusicSelectManager musicSelectManager;

    public void OnClickMusicButton()
    {
        musicSelectManager.SelectMusic(musicData);
    }
}