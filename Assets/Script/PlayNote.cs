using UnityEngine;

[DefaultExecutionOrder(-200)]
public class PlayNote : MonoBehaviour
{
    [Header("Song ending")]
    public bool walkOffAfterSong;
    [Min(.1f)] public float endingWalkSpeed = 3.5f;
    [Min(0f)] public float endingExitMargin = .15f;
    public UnityEngine.Events.UnityEvent onWalkOffStarted = new UnityEngine.Events.UnityEvent();
    Camera endingCamera;
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
        if (endingCamera != null)
        {
            if (clock.MenuPaused || Time.timeScale <= 0) return;
            if (playerMover.HasExitedScreen(endingCamera, endingExitMargin))
            {
                endingCamera = null;
                judgeManager.SaveResultAndGoToResult();
            }
            return;
        }
        if (!resultCalled && clock.Finished && clock.TravelEnabled && judgeManager != null && !judgeManager.HasPendingNotes)
        {
            resultCalled = true;
            if (walkOffAfterSong && playerMover != null && playerMover.isActiveAndEnabled && Camera.main != null)
            {
                endingCamera = Camera.main;
                clock.SuspendTravel();
                playerMover.BeginEndingWalk(endingWalkSpeed);
                onWalkOffStarted.Invoke();
                return;
            }
            judgeManager.SaveResultAndGoToResult();
        }
    }
    public void PauseNoteFlow() { isNoteFlowPaused = true; clock.SetFlowPaused(true); }
    public void ResumeNoteFlow() { isNoteFlowPaused = false; clock.SetFlowPaused(false); }
}
