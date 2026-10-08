using UnityEngine;

// Shared audio timeline for notes, judging and horizontal movement.
[DefaultExecutionOrder(-1000)]
public class RhythmClock : MonoBehaviour
{
    public AudioSource Source { get; private set; }
    public bool MenuPaused { get; private set; }
    public bool FlowPaused { get; private set; }
    public bool TravelEnabled { get; private set; }
    public bool HasStarted => Source != null && Source.clip != null && AudioSettings.dspTime >= dspStart;
    public bool IsPaused => MenuPaused || FlowPaused;
    public double SongTime => Source == null ? 0d : System.Math.Min(Source.clip == null ? 0d : Source.clip.length,
        IsPaused ? pausedTime : timeOrigin + System.Math.Max(0d, AudioSettings.dspTime - dspStart));
    public bool Finished => HasStarted && !IsPaused && Source != null && Source.clip != null && SongTime >= Source.clip.length;
    double dspStart, timeOrigin, pausedTime, travelTime;
    float travelX;

    public static RhythmClock GetOrCreate()
    {
        var clock = FindObjectOfType<RhythmClock>();
        if (clock != null) return clock;
        var play = FindObjectOfType<PlayNote>();
        return (play != null ? play.gameObject : new GameObject("RhythmClock")).AddComponent<RhythmClock>();
    }
    public void BeginSong(AudioSource source, float startTime = 0f)
    {
        if (Source != null && Source != source) Source.Stop();
        Source = source;
        MenuPaused = FlowPaused = false;
        timeOrigin = pausedTime = startTime;
        dspStart = AudioSettings.dspTime + 0.1d;
        if (Source == null || Source.clip == null) { Debug.LogError("RhythmClock: gameplay music is missing."); return; }
        Source.Stop();
        Source.enabled = true;
        Source.playOnAwake = false;
        Source.loop = false;
        Source.pitch = 1f;
        Source.time = Mathf.Clamp(startTime, 0f, Source.clip.length);
        Source.PlayScheduled(dspStart);
    }
    public void BeginTravel(PlayerMove mover)
    {
        if (mover == null) return;
        travelX = mover.transform.position.x;
        travelTime = SongTime;
        TravelEnabled = true;
        mover.isPaused = false;
    }
    public float PlayerX(float speed) => travelX + (float)(SongTime - travelTime) * speed;
    public void SuspendTravel() { TravelEnabled = false; }
    public void SetMenuPaused(bool value) { ChangePause(value, FlowPaused); }
    public void SetFlowPaused(bool value) { ChangePause(MenuPaused, value); }
    void ChangePause(bool menu, bool flow)
    {
        bool wasPaused = IsPaused;
        double now = SongTime;
        MenuPaused = menu;
        FlowPaused = flow;
        if (!wasPaused && IsPaused) { pausedTime = now; Source?.Pause(); }
        else if (wasPaused && !IsPaused && Source != null)
        {
            timeOrigin = pausedTime;
            dspStart = AudioSettings.dspTime + 0.05d;
            Source.Stop();
            Source.time = (float)pausedTime;
            Source.PlayScheduled(dspStart);
        }
    }
}
