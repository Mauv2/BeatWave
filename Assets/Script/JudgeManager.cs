using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class JudgeManager : MonoBehaviour
{
    const float NoteJudgmentVolume = .35f;
    public JudgePoint judgePointUp, judgePointDown;
    public GameObject judgeTextPrefab, holdEffectPrefab, hitEffectPrefab;
    public int score, combo, perfectCount, greatCount, goodCount, missCount;
    public TextMeshProUGUI scoreText, comboText, hpText;
    public int maxHp = 10, currentHp = 10;
    public PlayerAnimController playerAnim;
    public Image blackFade;
    public float fadeDuration = 0.5f;
    [Header("Timing (seconds)")]
    public float perfectWindow = 0.04f, greatWindow = 0.08f, goodWindow = 0.12f;
    public float inputOffsetSeconds;
    [Header("Note judgment sounds")]
    public AudioClip tapHitSound;
    [Tooltip("Played once when the Hold head is accepted. Falls back to Tap Hit Sound.")]
    public AudioClip holdHeadSound;
    [Tooltip("Played once on successful Hold completion. Falls back to Hold Head Sound, then Tap Hit Sound.")]
    public AudioClip holdEndSound;
    [Tooltip("Played for each accepted Mash press. Falls back to Tap Hit Sound.")]
    public AudioClip mashHitSound;
    AudioSource noteSoundSource;
    bool isGameOver, isResultSaved;
    readonly List<Note> notes = new List<Note>();
    RhythmClock clock;
    public bool HasPendingNotes => notes.Exists(n => n != null && !n.isJudged);
    public double JudgeTime => clock == null ? 0d : clock.SongTime + inputOffsetSeconds;

    void Awake()
    {
        clock = RhythmClock.GetOrCreate();
        // Fixed events disable normal judging but still use its judgment audio.
        clock.MenuPauseChanged += OnMenuPauseChanged;
    }
    void OnDisable()
    {
        if (noteSoundSource != null) noteSoundSource.Stop();
    }
    void OnDestroy()
    {
        if (clock != null) clock.MenuPauseChanged -= OnMenuPauseChanged;
    }
    void OnMenuPauseChanged(bool paused)
    {
        if (paused && noteSoundSource != null) noteSoundSource.Stop();
    }
    public void PlayNoteHitSound(NoteKind kind, bool holdEnd = false)
    {
        if (!gameObject.activeInHierarchy || Time.timeScale <= 0f || (clock != null && clock.MenuPaused) || isGameOver || isResultSaved) return;
        AudioClip clip = tapHitSound;
        if (kind == NoteKind.Hold)
            clip = holdEnd && holdEndSound != null ? holdEndSound : holdHeadSound != null ? holdHeadSound : tapHitSound;
        else if (kind == NoteKind.Mash)
            clip = mashHitSound != null ? mashHitSound : tapHitSound;
        if (clip == null) return;
        if (noteSoundSource == null)
        {
            // Own source: music fades/flow pauses must not change judgment audio.
            var soundObject = new GameObject("Note Judgment Audio");
            soundObject.transform.SetParent(transform, false);
            noteSoundSource = soundObject.AddComponent<AudioSource>();
            noteSoundSource.playOnAwake = false;
            noteSoundSource.loop = false;
            noteSoundSource.spatialBlend = 0f;
        }
        noteSoundSource.PlayOneShot(clip, GameAudioManager.ClipVolume(clip) * NoteJudgmentVolume);
    }
    void Start()
    {
        currentHp = maxHp;
        UpdateUI();
        if (blackFade != null) { var c = blackFade.color; c.a = 0f; blackFade.color = c; blackFade.raycastTarget = false; }
    }
    public void RegisterNote(Note note)
    {
        if (!notes.Contains(note)) notes.Add(note);
        if (note.kind == NoteKind.Mash) MashNote.Attach(note, clock, this);
    }
    public string Grade(double difference)
    {
        double diff = System.Math.Abs(difference);
        return diff <= perfectWindow ? "Perfect" : diff <= greatWindow ? "Great" : diff <= goodWindow ? "Good" : "";
    }
    void Update()
    {
        if (Time.timeScale <= 0f || clock == null || clock.MenuPaused || !clock.HasStarted || isGameOver || isResultSaved) return;
        double now = JudgeTime;
        notes.RemoveAll(n => n == null || n.isJudged);
        if (PicoBoxInputManager.GetDown(NoteLine.Up)) TryHit(NoteLine.Up);
        if (PicoBoxInputManager.GetDown(NoteLine.Down)) TryHit(NoteLine.Down);
        // No debug key to bypass the chart and write an incomplete result.
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            var note = notes[i];
            if (note == null || note.isJudged) continue;
            if (note.kind == NoteKind.Mash) continue;
            if (note.isHolding)
            {
                note.holdTimer = Mathf.Max(0f, (float)(now - note.judgeTime));
                var visual = note.GetComponent<HoldNoteVisual>();
                float progress = Mathf.Clamp01(note.holdTimer / Mathf.Max(0.001f, note.holdTime));
                if (visual != null)
                {
                    visual.UpdateHold(progress);
                    if (note.holdEffect != null) note.holdEffect.transform.position = visual.GetPoint(progress);
                }
                if (now >= note.judgeTime + note.holdTime) Complete(note, note.startJudge);
                else if (!PicoBoxInputManager.GetHeld(note.line)) Miss(note);
            }
            else if (now > note.judgeTime + goodWindow || clock.Finished) Miss(note);
            if (isGameOver) break;
        }
        UpdateCurrentNotes();
    }
    Note FindCandidate(NoteLine line)
    {
        Note nearest = null;
        double best = double.MaxValue;
        foreach (var note in notes)
        {
            if (note == null || note.isJudged || note.line != line || note.isHolding || note.kind == NoteKind.Mash) continue;
            double diff = System.Math.Abs(JudgeTime - note.judgeTime);
            if (diff <= goodWindow && diff < best) { nearest = note; best = diff; }
        }
        return nearest;
    }
    public bool TryHit(NoteLine line)
    {
        if (Time.timeScale <= 0f || clock == null || clock.MenuPaused || !clock.HasStarted || isGameOver || isResultSaved) return false;
        if (playerAnim != null) playerAnim.PlayInteract();
        foreach (var candidate in notes)
        {
            if (candidate == null || candidate.isJudged || candidate.kind != NoteKind.Mash) continue;
            var mash = candidate.GetComponent<MashNote>();
            if (mash != null && mash.Active) return mash.Press();
        }
        var note = FindCandidate(line);
        if (note == null) return false;
        string result = Grade(JudgeTime - note.judgeTime);
        if (note.kind == NoteKind.Hold)
        {
            note.isHolding = true;
            note.holdTimer = 0f;
            note.startJudge = result;
            PlayNoteHitSound(NoteKind.Hold);
            var visual = note.GetComponent<HoldNoteVisual>();
            if (visual != null) visual.StartHold();
            if (holdEffectPrefab != null)
            {
                note.holdEffect = Instantiate(holdEffectPrefab,
                    visual != null ? visual.GetPoint(0f) : note.transform.position, Quaternion.identity);
                // Keep the authored yellow colour and render above the note/terrain.
                foreach (var renderer in note.holdEffect.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    renderer.sortingOrder = 60;
            }
        }
        else Complete(note, result);
        UpdateCurrentNotes();
        return true;
    }
    void UpdateCurrentNotes()
    {
        if (judgePointUp != null) judgePointUp.currentNote = FindHeldOrCandidate(NoteLine.Up);
        if (judgePointDown != null) judgePointDown.currentNote = FindHeldOrCandidate(NoteLine.Down);
    }
    Note FindHeldOrCandidate(NoteLine line)
    {
        foreach (var note in notes)
            if (note != null && !note.isJudged && note.line == line && (note.isHolding || note.isMashActive)) return note;
        return FindCandidate(line);
    }
    void Complete(Note note, string result)
    {
        if (note == null || note.isJudged) return;
        Vector3 pos = note.transform.position;
        var visual = note.GetComponent<HoldNoteVisual>();
        if (note.kind == NoteKind.Hold && visual != null && visual.endCircle != null) pos = visual.endCircle.position;
        if (note.kind == NoteKind.Hold && visual != null) visual.CompleteHold();
        ApplyJudge(result);
        ShowJudgeText(result, pos);
        SpawnHitEffect(pos, note.line);
        // Mash already plays on each press, including the completing press.
        if (note.kind != NoteKind.Mash) PlayNoteHitSound(note.kind, holdEnd: true);
        Release(note);
    }
    void Miss(Note note)
    {
        if (note == null || note.isJudged) return;
        Vector3 pos = note.transform.position;
        Release(note);
        OnMiss(pos);
        ShowJudgeText("Miss", pos);
    }
    void Release(Note note)
    {
        note.isJudged = true;
        if (note.holdEffect != null) Destroy(note.holdEffect);
        if (judgePointUp != null && judgePointUp.currentNote == note) judgePointUp.currentNote = null;
        if (judgePointDown != null && judgePointDown.currentNote == note) judgePointDown.currentNote = null;
        note.gameObject.SetActive(false);
        Destroy(note.gameObject);
    }
    public void ClearActiveNotes()
    {
        foreach (var note in notes)
            if (note != null)
            {
                note.isJudged = true;
                if (note.holdEffect != null) Destroy(note.holdEffect);
                note.gameObject.SetActive(false);
                Destroy(note.gameObject);
            }
        notes.Clear();
        if (judgePointUp != null) judgePointUp.currentNote = null;
        if (judgePointDown != null) judgePointDown.currentNote = null;
    }
    public void ReportEventJudge(string result, Vector3 position)
    {
        if (isGameOver || isResultSaved) return;
        if (result == "Miss") OnMiss(position); else ApplyJudge(result);
        ShowJudgeText(result, position);
    }
    public void OnMiss(Vector3 missPos)
    {
        if (isGameOver || isResultSaved) return;
        combo = 0;
        missCount++;
        currentHp = Mathf.Max(0, currentHp - 1);
        UpdateUI();
        if (currentHp == 0)
        {
            isGameOver = true;
            clock.SuspendTravel();
            clock.SetFlowPaused(true);
            if (playerAnim != null) playerAnim.SetRunning(false);
            SaveResultAndGoToResult();
        }
    }
    void ApplyJudge(string result)
    {
        if (result == "Perfect") { score += 100; perfectCount++; }
        else if (result == "Great") { score += 80; greatCount++; }
        else if (result == "Good") { score += 50; goodCount++; }
        else return;
        combo++;
        UpdateUI();
    }
    void ShowJudgeText(string result, Vector3 pos)
    {
        if (judgeTextPrefab == null) return;
        var obj = Instantiate(judgeTextPrefab, pos + Vector3.up * 0.8f, Quaternion.identity);
        var popup = obj.GetComponent<JudgeTextPopup>();
        if (popup != null) popup.SetText(result);
    }
    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "Score : " + score;
        if (comboText != null) comboText.text = combo > 0 ? combo + " Combo" : "";
        if (hpText != null) hpText.text = "HP : " + currentHp;
    }
    void ColorEffect(GameObject obj, NoteLine line)
    {
        foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startColor = line == NoteLine.Up ? new Color(0.8f, 0.2f, 1f, 1f) : new Color(0.2f, 0.8f, 1f, 1f);
        }
    }
    void SpawnHitEffect(Vector3 pos, NoteLine line)
    {
        if (hitEffectPrefab == null) return;
        var effect = Instantiate(hitEffectPrefab, pos, Quaternion.identity);
        ColorEffect(effect, line);
        Destroy(effect, 1f);
    }
    public void SaveResultAndGoToResult()
    {
        if (GameResultManager.Instance == null)
        {
            Debug.LogError("GameResultManager가 씬에 없습니다.");
            return;
        }

        if (GameResultManager.Instance.resultData == null)
        {
            Debug.LogError("resultData가 null입니다.");
            return;
        }

        if (isResultSaved) return;
        isResultSaved = true;

        int totalNotes = perfectCount + greatCount + goodCount + missCount;

        float accuracy = 0f;

        if (totalNotes > 0)
        {
            int totalScore = perfectCount * 100 + greatCount * 80 + goodCount * 50;
            int maxScore = totalNotes * 100;
            accuracy = (float)totalScore / maxScore * 100f;
        }

        string rank = CalculateRank(accuracy);

        GameResultManager.Instance.resultData.score = score;
        GameResultManager.Instance.resultData.perfect = perfectCount;
        GameResultManager.Instance.resultData.great = greatCount;
        GameResultManager.Instance.resultData.good = goodCount;
        GameResultManager.Instance.resultData.miss = missCount;
        GameResultManager.Instance.resultData.accuracy = accuracy;
        GameResultManager.Instance.resultData.rank = rank;

        GameResultManager.Instance.lastStageName = SceneManager.GetActiveScene().name;

        StartCoroutine(FadeAndGoToResult());
    }

    IEnumerator FadeAndGoToResult()
    {
        clock.SuspendTravel();
        clock.SetFlowPaused(true);
        if (playerAnim != null) playerAnim.SetRunning(false);
        foreach (var pause in FindObjectsOfType<PauseManager>())
        {
            pause.StopAllCoroutines();
            pause.enabled = false;
            if (pause.PauseMenu != null) pause.PauseMenu.SetActive(false);
        }
        Time.timeScale = 0f;
        yield return ResultTransition.FadeToBlack();
        Time.timeScale = 1f;
        SceneManager.LoadScene("ResultScene");
    }

    string CalculateRank(float accuracy)
    {
        if (accuracy >= 95f)
            return "S";
        else if (accuracy >= 85f)
            return "A";
        else if (accuracy >= 70f)
            return "B";
        else if (accuracy >= 50f)
            return "C";
        else
            return "D";
    }

}
