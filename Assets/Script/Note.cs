using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NoteLine
{
    Up,
    Down
}

public enum NoteKind
{
    Tap,    // 한 번 누르는 노트
    Hold,   // 길게 누르는 노트
    Mash    // 여러 번 눌러 깨는 노트
}

public class Note : MonoBehaviour
{
    public bool hasTiming { get; private set; }
    public double judgeTime { get; private set; }
    public RhythmClock clock { get; private set; }
    public string startJudge = "Perfect";
    public GameObject holdEffect;
    public bool isEventNote;
    public void ConfigureTiming(RhythmClock rhythmClock, double time, JudgeManager manager)
    {
        clock = rhythmClock;
        judgeTime = time;
        hasTiming = true;
        if (manager != null) manager.RegisterNote(this);
    }
    void OnDestroy() { if (holdEffect != null) Destroy(holdEffect); }
    public NoteLine line;
    public NoteKind kind;

    public bool isJudged = false;   // 이미 판정났는지 확인

    // Hold 노트용
    public float holdTime = 1.5f;
    public float holdTimer = 0f;
    public bool isHolding = false;

    // Mash 노트용
    public int needHitCount = 5;     // 몇 번 눌러야 깨지는지
    public int currentHitCount = 0;  // 현재 몇 번 눌렀는지

    public float mashLimitTime = 2.0f;
    public float mashTimer = 0f;
    public bool isMashActive = false;
}
