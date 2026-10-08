using UnityEngine;

[DefaultExecutionOrder(-200)]
public class PlayNote : MonoBehaviour
{
    public JudgeManager judgeManager;
    public PlayerMove playerMover;
    public AudioSource musicSource;
    public bool isNoteFlowPaused;
    RhythmClock clock;
    bool resultCalled;
    void Awake()
    {
        clock = RhythmClock.GetOrCreate();
        foreach (var audio in FindObjectsOfType<AudioSource>()) { audio.Stop(); audio.playOnAwake = false; }
    }
    System.Collections.IEnumerator Start()
    {
        // Finish chart creation and the first rendered frame before starting the audio timeline.
        yield return null;
        yield return null;
        if (playerMover == null) playerMover = FindObjectOfType<PlayerMove>();
        if (judgeManager == null) judgeManager = FindObjectOfType<JudgeManager>();
        if (musicSource == null)
            foreach (var audio in GetComponents<AudioSource>())
                if (audio.enabled && audio.clip != null) { musicSource = audio; break; }
        if (musicSource == null)
        {
            var musicObject = GameObject.Find("Music1");
            if (musicObject != null) musicSource = musicObject.GetComponent<AudioSource>();
        }
        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        clock.BeginSong(musicSource);
        clock.BeginTravel(playerMover);
    }
    public void ReadNoteJsonLoad() { }
    void Update()
    {
        if (!resultCalled && clock.Finished && clock.TravelEnabled && judgeManager != null && !judgeManager.HasPendingNotes)
        {
            resultCalled = true;
            judgeManager.SaveResultAndGoToResult();
        }
    }
    public void PauseNoteFlow() { isNoteFlowPaused = true; clock.SetFlowPaused(true); }
    public void ResumeNoteFlow() { isNoteFlowPaused = false; clock.SetFlowPaused(false); }
}
