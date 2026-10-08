using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMusicData", menuName = "RhythmGame/MusicData")]
public class MusicData : ScriptableObject
{
    public string musicName;      // 곡 이름
    public AudioClip previewClip; // 선택 씬에서 미리듣기용
    public AudioClip gameClip;    // 게임 플레이용
    // public float bpm;             // 필요하면 사용
    public string sceneName;       // 씬 이름
    public Sprite musicImage;
}
