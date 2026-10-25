using System.Collections;
using UnityEngine;

public class CaveEventManager : MonoBehaviour
{
    public FallTransitionEffect fallTransition;
    public PlayerMove playerMove;
    public Transform player;
    public Rigidbody2D playerRb;
    public PlayerAnimController playerAnim;
    public Transform caveStartPoint;
    public AudioSource music1, music2;
    public AudioClip caveMusic;
    public float normalVolume = 1f, crossFadeTime = 1f;
    public bool useSecondMusic = true;
    public JudgeManager judgeManager;
    public float stopDelay = 0.5f, restartDelay = 0.5f;
    public NoteMapPlacer noteMapPlacer;
    [Header("Continuous song transition")]
    public bool keepSongPlaying;
    public float continuousTravelTime;
    [Range(0.01f, 1f)] public float transitionVolumeRatio = 0.22f;
    public bool IsTransitioning => isPlayingEvent;
    bool isPlayingEvent;
    AudioSource transitionTail, fadedSource;
    float restoreVolume;
    bool tailMenuPaused;
    Coroutine tailFade;
    RhythmClock transitionClock;

    public void StartCaveEvent()
    {
        if (isPlayingEvent) return;
        StartCoroutine(CaveEventRoutine());
    }

    IEnumerator CaveEventRoutine()
    {
        isPlayingEvent = true;
        var clock = RhythmClock.GetOrCreate();
        clock.SuspendTravel();
        if (playerMove != null) playerMove.isPaused = true;
        if (playerRb != null) playerRb.velocity = Vector2.zero;
        if (judgeManager != null) { judgeManager.ClearActiveNotes(); judgeManager.enabled = false; }
        music1 = clock.Source != null ? clock.Source : music1;
        float originalVolume = music1 != null ? music1.volume : normalVolume;

        fadedSource = music1;
        restoreVolume = originalVolume;
        // Legacy song switches retain their tail; continuous playback uses the original source.
        if (!keepSongPlaying)
        {
            CreateTransitionTail(music1);
            clock.SetFlowPaused(true);
        }
        float fadeOutTime = stopDelay + restartDelay
            + (fallTransition != null && caveStartPoint != null ? fallTransition.Duration : 0f);
        var fading = keepSongPlaying ? music1 : transitionTail;
        tailFade = StartCoroutine(FadeCaveMusic(clock, fading,
            originalVolume * (keepSongPlaying ? transitionVolumeRatio : .08f), fadeOutTime));

        if (useSecondMusic && music2 == null)
        {
            var obj = GameObject.Find("Music2");
            if (obj != null) music2 = obj.GetComponent<AudioSource>();
        }
        yield return new WaitForSeconds(stopDelay);
        if (fallTransition != null) yield return fallTransition.PlayFallTransition(caveStartPoint);
        else if (player != null && caveStartPoint != null) player.position = caveStartPoint.position;
        if (restartDelay > 0f) yield return new WaitForSeconds(restartDelay);
        if (keepSongPlaying)
            while (clock.IsPaused || clock.SongTime < continuousTravelTime) yield return null;
        if (tailFade != null) { StopCoroutine(tailFade); tailFade = null; }
        if (playerRb != null) playerRb.velocity = Vector2.zero;

        float chartStart = (float)clock.SongTime;
        bool switchSong = !keepSongPlaying && useSecondMusic && music2 != null && caveMusic != null;
        if (switchSong)
        {
            music2.Stop();
            music2.clip = caveMusic;
            chartStart = 0f;
        }
        if (noteMapPlacer != null)
        {
            noteMapPlacer.LoadJson();
            noteMapPlacer.PlaceNotesFromTime(chartStart, caveStartPoint);
        }
        fadedSource = switchSong ? music2 : music1;
        restoreVolume = switchSong ? normalVolume : originalVolume;
        if (!keepSongPlaying && fadedSource != null) fadedSource.volume = 0f;
        if (switchSong) clock.BeginSong(music2);
        else if (!keepSongPlaying) clock.SetFlowPaused(false);
        clock.BeginTravel(playerMove);
        if (judgeManager != null) judgeManager.enabled = true;
        if (playerAnim != null) playerAnim.SetRunning(true);
        yield return FadeCaveMusic(clock, fadedSource, restoreVolume, crossFadeTime, transitionTail);
        fadedSource = null;
        StopTransitionTail();
        isPlayingEvent = false;
    }

    void CreateTransitionTail(AudioSource source)
    {
        if (source == null || source.clip == null) return;
        var obj = new GameObject("CaveTransitionMusicTail");
        obj.transform.SetParent(transform, false);
        transitionTail = obj.AddComponent<AudioSource>();
        transitionTail.playOnAwake = false;
        transitionTail.clip = source.clip;
        transitionTail.outputAudioMixerGroup = source.outputAudioMixerGroup;
        transitionTail.volume = source.volume;
        transitionTail.mute = source.mute;
        transitionTail.pitch = source.pitch;
        transitionTail.priority = source.priority;
        transitionTail.spatialBlend = 0f;
        transitionTail.timeSamples = source.timeSamples;
        transitionTail.Play();
        tailMenuPaused = false;
        transitionClock = RhythmClock.GetOrCreate();
        transitionClock.MenuPauseChanged += SetTailPaused;
        SetTailPaused(transitionClock.MenuPaused);
    }

    void Update()
    {
        if (transitionTail == null) return;
        SetTailPaused(transitionClock.MenuPaused || Time.timeScale <= 0f);
    }

    void SetTailPaused(bool paused)
    {
        if (transitionTail == null) return;
        if (paused == tailMenuPaused) return;
        if (paused) transitionTail.Pause(); else transitionTail.UnPause();
        tailMenuPaused = paused;
    }

    // One volume tween handles fade-out, recovery and legacy crossfade.
    IEnumerator FadeCaveMusic(RhythmClock clock, AudioSource source, float target,
        float duration, AudioSource outgoing = null)
    {
        if (source == null) yield break;
        float initial = source.volume;
        float outgoingVolume = outgoing != null ? outgoing.volume : 0f;
        float elapsed = 0f;
        while (source != null && elapsed < duration)
        {
            if (!clock.MenuPaused && Time.timeScale > 0f)
            {
                elapsed += Time.deltaTime;
                float blend = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                source.volume = Mathf.Lerp(initial, target, blend);
                if (outgoing != null) outgoing.volume = Mathf.Lerp(outgoingVolume, 0f, blend);
            }
            yield return null;
        }
        if (source != null) source.volume = target;
        if (outgoing != null) outgoing.volume = 0f;
    }

    void StopTransitionTail()
    {
        if (transitionClock != null) transitionClock.MenuPauseChanged -= SetTailPaused;
        transitionClock = null;
        if (transitionTail == null) return;
        transitionTail.Stop();
        Destroy(transitionTail.gameObject);
        transitionTail = null;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        tailFade = null;
        StopTransitionTail();
        if (fadedSource != null) fadedSource.volume = restoreVolume;
        fadedSource = null;
        isPlayingEvent = false;
    }
}
