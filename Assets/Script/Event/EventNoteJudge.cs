using UnityEngine;

public class EventNoteJudge : MonoBehaviour
{
    public Note currentNote;
    public PlayerAnimController playerAnim;
    public GameObject hitEffectPrefab;
    public float perfectWindow = 0.04f, goodWindow = 0.12f;
    EventNoteData currentData;
    float eventTimer;
    bool isHolding;
    KeyCode holdKey;
    string startJudge;
    JudgeManager judge;
    RhythmClock clock;
    double eventTimeOrigin;

    public void StartEventJudge()
    {
        currentNote = null;
        currentData = null;
        eventTimer = 0f;
        isHolding = false;
        judge = FindObjectOfType<JudgeManager>();
        clock = RhythmClock.GetOrCreate();
        eventTimeOrigin = clock.SongTime;
        if (judge != null) { perfectWindow = judge.perfectWindow; goodWindow = judge.goodWindow; }
    }
    public void SetEventTime(float time)
    {
        eventTimer = time;
        if (clock != null) eventTimeOrigin = clock.SongTime - time;
    }
    public void SetNote(Note note, EventNoteData data)
    {
        currentNote = note;
        currentData = data;
        isHolding = false;
    }
    public bool HasCurrentNote() { return currentNote != null; }
    void Update()
    {
        if (Time.timeScale <= 0f || currentNote == null || currentData == null) return;
        // Sample at the input frame, rather than using the previous coroutine tick's time.
        float now = (clock == null ? eventTimer : (float)(clock.SongTime - eventTimeOrigin))
            + (judge == null ? 0f : judge.inputOffsetSeconds);
        if (isHolding)
        {
            if (now >= currentData.judgeTime + currentData.holdTime) Finish(startJudge);
            else if (!Input.GetKey(holdKey)) Finish("Miss");
            return;
        }
        bool up = Input.GetKeyDown(KeyCode.W), down = Input.GetKeyDown(KeyCode.S);
        if (up || down)
        {
            if (playerAnim != null) playerAnim.PlayInteract();
            string grade = judge != null ? judge.Grade(now - currentData.judgeTime) :
                Mathf.Abs(now - currentData.judgeTime) <= perfectWindow ? "Perfect" :
                Mathf.Abs(now - currentData.judgeTime) <= goodWindow ? "Good" : "";
            if (grade != "")
            {
                if (currentNote.kind == NoteKind.Hold)
                {
                    isHolding = true;
                    holdKey = up ? KeyCode.W : KeyCode.S;
                    startJudge = grade;
                }
                else Finish(grade);
                return;
            }
        }
        if (now > currentData.judgeTime + goodWindow) Finish("Miss");
    }
    void Finish(string grade)
    {
        if (currentNote == null) return;
        Vector3 pos = currentNote.transform.position;
        if (judge != null) judge.ReportEventJudge(grade, pos);
        if (grade != "Miss" && hitEffectPrefab != null)
            Destroy(Instantiate(hitEffectPrefab, pos, Quaternion.identity), 1f);
        currentNote.isJudged = true;
        Destroy(currentNote.gameObject);
        currentNote = null;
        currentData = null;
        isHolding = false;
    }
}
