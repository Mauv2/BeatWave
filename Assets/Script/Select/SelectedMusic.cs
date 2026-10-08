using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedMusic : MonoBehaviour
{
    public static SelectedMusic Instance;

    public MusicData selectedMusic;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
