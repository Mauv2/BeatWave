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
    [Header("Regular W / S appearance")]
    public Sprite upSprite, downSprite;
    public Material laneMaterial;
    public Color upHoldColor = new Color(1f, .4f, .85f, .85f);
    public Color downHoldColor = new Color(.35f, .7f, 1f, .85f);

    // Spawners assign line/kind/event status after Instantiate, before Start.
    void Start() { ApplyLaneAppearance(); }

    public void ApplyLaneAppearance()
    {
        if (isEventNote || kind == NoteKind.Mash) return;
        var image = line == NoteLine.Up ? upSprite : downSprite;
        if (image == null) return;
        var hold = GetComponent<HoldNoteVisual>();
        if (kind == NoteKind.Hold && hold != null)
        {
            SetImage(hold.startCircle, image);
            var body = hold.holdBody != null ? hold.holdBody.GetComponent<SpriteRenderer>() : null;
            if (body != null) body.color = line == NoteLine.Up ? upHoldColor : downHoldColor;
        }
        else SetImage(transform, image);
    }

    void SetImage(Transform target, Sprite image)
    {
        if (target == null) return;
        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer == null) return;
        renderer.sprite = image;
        renderer.color = Color.white;
        if (laneMaterial != null) renderer.sharedMaterial = laneMaterial;
    }

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
