using System;
using System.Collections.Generic;
using UnityEngine;

// Stage2's measured song clock drives both traversal and note placement.
[DefaultExecutionOrder(-50)]
public sealed class Stage2MusicRoute : MonoBehaviour
{
    public const string RunParameter = "Stage2RunTempo";
    public TextAsset routeFile;
    public Animator animator;
    public Camera view;
    public BackgroundLoop backdrop;
    public Stage2StageDirector presentation;
    public float runCycleSeconds = .5f;
    [Serializable] public struct Frame { public float time, x, y, speed, bpm, beat, energy; }
    [Serializable] public struct Leap { public float start, end, fromY, toY, height; }
    [Serializable] public struct Section { public string name; public float start, end; public int background; }
    [Serializable] public class Profile
    {
        public float duration, referenceBpm;
        public bool useSampledHeight;
        public Frame[] frames;
        public Leap[] jumps;
        public Section[] sections;
    }
    [NonSerialized] Profile profile;
    [NonSerialized] TextAsset loadedRoute;
    RhythmClock clock;
    PlayerMove mover;
    bool hasRunParameter;
    int background = -1;
    public Profile Data
    {
        get
        {
            if (profile == null || loadedRoute != routeFile || profile.frames == null || profile.frames.Length < 2)
            {
                if (routeFile == null) throw new InvalidOperationException("Stage2 route data is not assigned.");
                profile = JsonUtility.FromJson<Profile>(routeFile.text); loadedRoute = routeFile;
                if (profile.frames == null || profile.frames.Length < 2 || profile.duration <= 0)
                    throw new InvalidOperationException("Stage2 route data is invalid.");
            }
            return profile;
        }
    }
    public void ReloadProfile() { profile=null; loadedRoute=null; }
    public float CurrentSpeed { get; private set; }
    // Traversal waits at the Mash stage while the audio timeline continues.
    // Rejoin before the next jump without moving any chart timestamps.
    public float MashStart { get; private set; } = -1;
    public float MashEnd { get; private set; }
    public float MashRejoin { get; private set; }
    public void ConfigureMash(float start, float end)
    {
        MashStart=start;MashEnd=end;
        MashRejoin=Data.jumps[0].start-.5f;
        if(MashRejoin<=end)throw new InvalidOperationException("Mash needs recovery space before the next jump.");
    }
    public Frame SampleTraversal(double time)
    {
        var musical=Sample(time);
        if(MashStart<0 || time<MashStart || time>=MashRejoin)return musical;
        double mapped=MashStart;float rate=0;
        if(time>=MashEnd)
        {
            float span=MashRejoin-MashEnd, u=Mathf.Clamp01(((float)time-MashEnd)/span);
            mapped=time-(MashEnd-MashStart)*(1-Mathf.SmoothStep(0,1,u));
            rate=1+(MashEnd-MashStart)*6*u*(1-u)/span;
        }
        var spatial=Sample(mapped);
        musical.x=spatial.x;musical.y=spatial.y;musical.speed=spatial.speed*rate;
        return musical;
    }
    public Frame Sample(double time)
    {
        var frames = Data.frames;
        int lo = 0, hi = frames.Length - 1;
        while (lo < hi) { int mid = (lo + hi) / 2; if (frames[mid].time < time) lo = mid + 1; else hi = mid; }
        var a = frames[Mathf.Max(0, lo - 1)]; var b = frames[lo];
        float u = Mathf.InverseLerp(a.time, b.time, (float)time);
        return new Frame { time = (float)time, x = Mathf.Lerp(a.x, b.x, u), y = Data.useSampledHeight ? Mathf.Lerp(a.y, b.y, u) : FootY(time),
            speed = Mathf.Lerp(a.speed, b.speed, u), bpm = Mathf.Lerp(a.bpm, b.bpm, u),
            beat = Mathf.Lerp(a.beat, b.beat, u), energy = Mathf.Lerp(a.energy, b.energy, u) };
    }
    float FootY(double time)
    {
        float ground = Data.frames[0].y;
        foreach (var jump in Data.jumps)
        {
            if (time < jump.start) return ground;
            if (time < jump.end)
            {
                float u = Mathf.Clamp01((float)(time - jump.start) / (jump.end - jump.start));
                return Mathf.Lerp(jump.fromY, jump.toY, u) + 4f * jump.height * u * (1f - u);
            }
            ground = jump.toY;
        }
        return ground;
    }
    public bool Airborne(double time)
    {
        foreach (var jump in Data.jumps) if (time > jump.start && time < jump.end) return true;
        return false;
    }
    public RhythmPath BuildPath(CapsuleCollider2D body, double start, double end)
    {
        float feetOffset = transform.position.y - body.bounds.min.y;
        var points = new List<Vector2>();
        int count = Mathf.CeilToInt((float)(end - start) / .01f);
        for (int i = 0; i <= count; i++)
        {
            var f = SampleTraversal(Math.Min(end, start + i * .01d));
            points.Add(new Vector2(f.x, f.y + feetOffset));
        }
        var cues = new List<RhythmPath.JumpCue>();
        foreach (var jump in Data.jumps)
            if (jump.start >= start && jump.start < end)
                cues.Add(new RhythmPath.JumpCue { time = jump.start, endTime = jump.end });
        return RhythmPath.FromSamples(start, feetOffset, points, cues);
    }
    void Awake()
    {
        clock = RhythmClock.GetOrCreate(); mover = GetComponent<PlayerMove>();
        if (animator != null) foreach (var p in animator.parameters) if (p.name == RunParameter) hasRunParameter = true;
    }
    void Update()
    {
        bool traveling = clock.HasStarted && !clock.IsPaused && clock.TravelEnabled && !mover.isPaused;
        var f = SampleTraversal(clock.SongTime);
        CurrentSpeed = traveling ? f.speed : 0f;
        foreach (var section in Data.sections)
            if (clock.SongTime >= section.start && clock.SongTime < section.end)
            { background = section.background; break; }
        if (animator != null)
        {
            if (hasRunParameter) animator.SetFloat(RunParameter, runCycleSeconds * f.bpm / 60f);
            animator.SetBool("isFlying", Airborne(clock.SongTime));
            animator.SetBool("isRunning", traveling && f.speed>.01f);
        }
    }
    void LateUpdate()
    {
        if (view == null) return;
        if (!clock.HasStarted || clock.IsPaused || !clock.TravelEnabled || mover.isPaused)
        { PresentBackdrop(); return; }
        var f = SampleTraversal(clock.SongTime);
        var ahead = SampleTraversal(clock.SongTime + .65);
        float size = Mathf.Lerp(5.7f, 6.6f, Mathf.InverseLerp(4.4f, 8.2f, f.speed));
        if (presentation != null) size += presentation.CameraExpansion((float)clock.SongTime);
        if (Airborne(clock.SongTime) || Airborne(clock.SongTime + .65)) size += .35f;
        float blend = 1f - Mathf.Exp(-5f * Time.deltaTime);
        // Keep the player on the left third and reveal landing platforms early.
        var target = new Vector3(transform.position.x + 4.5f + .12f * f.speed,
            Mathf.Max(f.y, ahead.y) + 3f, -10f);
        if(presentation!=null&&presentation.stageEvents!=null)target=presentation.stageEvents.CameraTarget((float)clock.SongTime,target);
        view.transform.position = Vector3.Lerp(view.transform.position, target, blend);
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, size, blend);
        PresentBackdrop();
    }
    void PresentBackdrop()
    {
        if (presentation != null) { presentation.Present((float)clock.SongTime); return; }
        if (backdrop == null || backdrop.backgrounds.Length == 0) return;
        var sprite = backdrop.backgrounds[Mathf.Clamp(background, 0, backdrop.backgrounds.Length - 1)];
        float scale = Mathf.Max(view.orthographicSize * 2.1f / sprite.bounds.size.y,
            view.orthographicSize * 2.1f * view.aspect / sprite.bounds.size.x);
        float width = sprite.bounds.size.x * scale;
        float offset = Mathf.Repeat(Sample(clock.SongTime).x * .22f, width);
        int tile = Mathf.FloorToInt(Sample(clock.SongTime).x * .22f / width);
        var renderers = new[] { backdrop.background1, backdrop.background2 };
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i]; renderer.sprite = sprite; renderer.sortingLayerID = 0; renderer.sortingOrder = -50;
            // Mirrored adjacent tiles meet at the same image edge, without a hard seam.
            renderer.flipX = ((tile + i) & 1) != 0;
            renderer.color = new Color(.72f, .78f, .92f, 1f);
            renderer.transform.localScale = Vector3.one * scale;
            renderer.transform.position = new Vector3(view.transform.position.x - offset + i * width, view.transform.position.y, 10f);
        }
    }
    void OnDisable() { if (hasRunParameter && animator != null) animator.SetFloat(RunParameter, 1f); }
}
