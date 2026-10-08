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
    bool isPlayingEvent;
    AudioSource transitionTail, fadedSource;
    float restoreVolume;

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

        // Preserve the cave's first beat while a separate audible tail fades through the fall.
        // Pausing the only AudioSource here would make a volume tween inaudible.
        CreateTransitionTail(music1);
        clock.SetFlowPaused(true);
        float fadeOutTime = fallTransition != null
            ? stopDelay + fallTransition.jumpDuration + fallTransition.fallDuration + fallTransition.fadeInDuration
            : Mathf.Max(stopDelay, crossFadeTime);
        StartCoroutine(FadeTransitionTail(clock, Mathf.Max(0.01f, fadeOutTime)));

        if (useSecondMusic && music2 == null)
        {
            var obj = GameObject.Find("Music2");
            if (obj != null) music2 = obj.GetComponent<AudioSource>();
        }
        yield return new WaitForSeconds(stopDelay);
        if (fallTransition != null) yield return fallTransition.PlayFallTransition(caveStartPoint);
        else if (player != null && caveStartPoint != null) player.position = caveStartPoint.position;
        if (restartDelay > 0f) yield return new WaitForSeconds(restartDelay);
        StopTransitionTail();
        if (playerRb != null) playerRb.velocity = Vector2.zero;

        float chartStart = (float)clock.SongTime;
        bool switchSong = useSecondMusic && music2 != null && caveMusic != null;
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
        if (fadedSource != null) fadedSource.volume = 0f;
        if (switchSong) clock.BeginSong(music2);
        else clock.SetFlowPaused(false);
        clock.BeginTravel(playerMove);
        if (judgeManager != null) judgeManager.enabled = true;
        if (playerAnim != null) playerAnim.SetRunning(true);
        yield return FadeCaveMusic(clock);
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
    }

    IEnumerator FadeTransitionTail(RhythmClock clock, float duration)
    {
        float volume = transitionTail != null ? transitionTail.volume : 0f;
        float elapsed = 0f;
        bool menuPaused = false;
        while (transitionTail != null && elapsed < duration)
        {
            bool paused = clock.MenuPaused || Time.timeScale <= 0f;
            if (paused != menuPaused)
            {
                if (paused) transitionTail.Pause(); else transitionTail.UnPause();
                menuPaused = paused;
            }
            if (!paused)
            {
                elapsed += Time.deltaTime;
                transitionTail.volume = Mathf.Lerp(volume, 0f, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            }
            yield return null;
        }
        StopTransitionTail();
    }

    IEnumerator FadeCaveMusic(RhythmClock clock)
    {
        float elapsed = 0f;
        while (fadedSource != null && elapsed < crossFadeTime)
        {
            if (!clock.IsPaused && clock.HasStarted)
            {
                elapsed += Time.deltaTime;
                fadedSource.volume = Mathf.Lerp(0f, restoreVolume, Mathf.SmoothStep(0f, 1f, elapsed / crossFadeTime));
            }
            yield return null;
        }
        if (fadedSource != null) fadedSource.volume = restoreVolume;
        fadedSource = null;
    }

    void StopTransitionTail()
    {
        if (transitionTail == null) return;
        transitionTail.Stop();
        Destroy(transitionTail.gameObject);
        transitionTail = null;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        StopTransitionTail();
        if (fadedSource != null) fadedSource.volume = restoreVolume;
        fadedSource = null;
        isPlayingEvent = false;
    }
}
