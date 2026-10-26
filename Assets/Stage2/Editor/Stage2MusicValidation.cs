using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Synthetic timing inputs exercise the real judge APIs. Held keys are not
// injected: the shared completion method is invoked at each authored Hold end.
[InitializeOnLoad]
public static class Stage2MusicValidation
{
    const string Key = "BeatWave.Stage2.MusicValidation";
    const string Report = "Reports/Stage2-causeway-validation.txt";
    static readonly List<string> report = new List<string>();
    static readonly MethodInfo Complete = typeof(JudgeManager).GetMethod("Complete", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo ActiveMash = typeof(JudgeManager).GetField("activeMash", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly float[] Shots = { 6, 22, 40, 55, 85, 115, 140, 166 };
    static RhythmClock clock;
    static Stage2NoteMapPlacer placer;
    static JudgeManager judge;
    static Note[] notes;
    static int hits, holds, shot, expected, jumps, previews, visiblePreviews, delayedEditorInputs;
    static int expectedHolds, expectedJumps;
    static float maxNoteError, maxHoldError, lastTime;
    static double deadline, pauseUntil, frozenTime, nextMashPress;
    static Vector3 frozenPosition;
    static bool paused, resumed, started, reloadLocked;
    static readonly HashSet<int> seen = new HashSet<int>();
    static Stage2MusicValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            if (SessionState.GetBool(Key, false))
            {
                report.Add("INTERRUPTED Play mode exited at song time " + lastTime);
                Directory.CreateDirectory("Reports"); File.WriteAllLines(Report, report);
            }
            SessionState.SetBool(Key, false);
            if (reloadLocked) { reloadLocked = false; EditorApplication.UnlockReloadAssemblies(); }
        };
    }
    static void Check(bool ok, string label)
    {
        report.Add((ok ? "PASS " : "FAIL ") + label);
        Directory.CreateDirectory("Reports"); File.WriteAllLines(Report, report);
        if (!ok) throw new InvalidOperationException(label);
    }
    [MenuItem("BeatWave/Stage2/Validate Music Layout")]
    public static void Layout()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Stage2") throw new InvalidOperationException("Open Stage2 in edit mode.");
        report.Clear();
        var p = Object.FindObjectOfType<Stage2NoteMapPlacer>(); var route = p.route;
        var chart = p.ReadChart(); var profile = route.Data;
        var authoredChart = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Stage2/Data/Stage2OriginalNotes.json");
        Check(p != null && p.chartFile == authoredChart && chart.Length == 234, "Original Stage2 chart binding retained: 234 notes");
        var authoredRoute = JsonUtility.FromJson<Stage2MusicRoute.Profile>(File.ReadAllText("Assets/Stage2/Data/Stage2OriginalRoute.json"));
        Check(profile.frames.Length == authoredRoute.frames.Length && profile.frames.Select((f,i) =>
            f.time == authoredRoute.frames[i].time && f.x == authoredRoute.frames[i].x &&
            f.speed == authoredRoute.frames[i].speed && f.bpm == authoredRoute.frames[i].bpm &&
            f.energy == authoredRoute.frames[i].energy).All(equal => equal),
            "Every original music-time, horizontal position, speed, BPM and energy sample preserved");
        Check(Object.FindObjectsOfType<NoteMapPlacer>().All(n => !n.enabled), "Legacy shared placers disabled only in Stage2 scene");
        Check(Mathf.Abs(p.songClip.length - profile.duration) < .001f, "Full imported song/route duration: " + p.songClip.length.ToString("F3") + "s");
        Check(chart.Count(n => n.noteKind == 1) == 15 && chart.Count(n => n.noteKind == 2) == 1, "Original 15 Holds and one Mash preserved");
        var laneEnd = new[] { -1f, -1f }; float previous = -1; bool safe = true;
        foreach (var note in chart)
        {
            safe &= note.createTime > previous && note.createTime > laneEnd[note.noteType] + .15f && note.createTime + note.holdTime < profile.duration;
            laneEnd[note.noteType] = note.createTime + note.holdTime; previous = note.createTime;
            if (note.noteKind == 1) safe &= !profile.jumps.Any(j => note.createTime < j.end + .15f && note.createTime + note.holdTime > j.start - .15f);
        }
        Check(safe, "Ordered notes, no same-lane overlap, Hold notes avoid jump gaps");
        Physics2D.SyncTransforms();
        float error = 0, minSpeed = float.PositiveInfinity, maxSpeed = 0;
        for (float t = 0; t < profile.duration; t += .05f)
        {
            var f = route.Sample(t); minSpeed = Mathf.Min(minSpeed, f.speed); maxSpeed = Mathf.Max(maxSpeed, f.speed);
            if (route.Airborne(t) || route.Airborne(t + .05) || route.Airborne(t - .05)) continue;
            var floor = Physics2D.RaycastAll(new Vector2(f.x, f.y + .1f), Vector2.down, .3f, 1 << 6).FirstOrDefault(h => !h.collider.isTrigger);
            if (floor.collider == null) throw new InvalidOperationException("Missing ground at " + t + "s x=" + f.x);
            error = Mathf.Max(error, Mathf.Abs(floor.point.y - f.y));
        }
        Check(error < .002f, "Continuous ground under every non-airborne sample; max mismatch=" + error.ToString("F5"));
        Check(minSpeed >= 2.19f && maxSpeed <= 8.22f && maxSpeed > 8f, "Music-driven speed range=" + minSpeed.ToString("F2") + ".." + maxSpeed.ToString("F2"));
        Check(profile.jumps.Length == 21 && profile.sections.Length == 8, "21 bridge gaps across eight Stage2 music sections");
        double phase = -profile.frames[0].beat * 60 / profile.referenceBpm;
        Check(profile.jumps.All(j => Math.Abs((j.start-phase)*profile.referenceBpm/60-Math.Round((j.start-phase)*profile.referenceBpm/60)) < .001 &&
            Math.Abs((j.end-j.start)*profile.referenceBpm/60-2) < .001), "Every bridge jump starts on the measured beat and spans two beats");
        Check(route.presentation != null && route.presentation.backgrounds.Length == 4 &&
            route.presentation.backdropTiles.Length == 6 && route.presentation.gate != null,
            "Stage2-only backdrops, crossfade and nave gate are connected");
        Check(AssetDatabase.GetAssetPath(route.animator.runtimeAnimatorController) == "Assets/Stage2/Stage2Tempo.controller", "Stage2 uses its own animation controller");
        Check(!route.backdrop.enabled && route.view.transform.parent == null, "Music-aware background framing and independent camera connected");
        Check(Object.FindObjectsOfType<EventTrigger>().Length == 0, "Unused prototype event trigger is inactive, including physics callbacks");
        Check(p.judge.tapHitSound != null && p.judge.holdHeadSound != null && p.judge.holdEndSound != null, "Existing judgment sound assignments retained");
    }
    [MenuItem("BeatWave/Stage2/Validate Full Music Playback")]
    public static void Begin()
    {
        Layout();
        clock = null; placer = null; notes = null; started = paused = resumed = false;
        hits = holds = shot = previews = visiblePreviews = jumps = delayedEditorInputs = 0; maxNoteError = maxHoldError = lastTime = 0; seen.Clear();
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try { Run(); }
        catch (Exception e)
        {
            report.Add("FAIL " + e); Directory.CreateDirectory("Reports"); File.WriteAllLines(Report, report);
            SessionState.SetBool(Key, false); EditorApplication.isPlaying = false;
        }
    }
    static void Run()
    {
        double now = EditorApplication.timeSinceStartup;
        if (!started)
        {
            placer = Object.FindObjectOfType<Stage2NoteMapPlacer>(); clock = Object.FindObjectOfType<RhythmClock>();
            if (placer == null || clock == null || !clock.HasStarted || placer.Notes.Count == 0) return;
            if (report.Count == 0 && File.Exists(Report)) report.AddRange(File.ReadAllLines(Report));
            started = true; Application.runInBackground = true; deadline = now + 200;
            if (!reloadLocked) { reloadLocked = true; EditorApplication.LockReloadAssemblies(); }
            judge = placer.judge; judge.enabled = false; notes = placer.Notes.ToArray(); expected = notes.Length;
            expectedHolds=placer.ReadChart().Count(n=>n.noteKind==1);expectedJumps=placer.route.Data.jumps.Length;
            Check(expected == 234 && placer.player.HasRhythmPath, "Runtime spawned all 234 authored notes and bound the shared song-time path");
            foreach (var note in notes)
            {
                maxNoteError = Mathf.Max(maxNoteError, Vector3.Distance(note.transform.position, placer.PositionAtTime(note.judgeTime, note.line)));
                if (note.kind != NoteKind.Hold) continue;
                var visual = note.GetComponent<HoldNoteVisual>();
                for (int k = 0; k <= 20; k++)
                    maxHoldError = Mathf.Max(maxHoldError, Vector3.Distance(visual.GetPoint(k / 20f), placer.PositionAtTime(note.judgeTime + note.holdTime * k / 20f, note.line)));
            }
            Check(maxNoteError < .001f, "All note heads align with player path; error=" + maxNoteError.ToString("F5"));
            Check(maxHoldError < .15f, "Hold body follows the moving marker; max deviation=" + maxHoldError.ToString("F4"));
            Directory.CreateDirectory("Docs/Preview/Stage2-Causeway");
        }
        if (SceneManager.GetActiveScene().name == "ResultScene")
        {
            var result = GameResultManager.Instance.resultData;
            Check(result.perfect + result.great + result.good == expected && result.miss == 0, "Full playback judged all " + expected + " notes, Miss=0");
            Check(holds == expectedHolds && jumps == expectedJumps, "All "+expectedHolds+" Hold ends and "+expectedJumps+" bridge jumps completed");
            Check(lastTime >= 169.9f, "Result opens only after full music playback (last sample " + lastTime.ToString("F3") + "s)");
            Check(resumed, "Menu pause/resume verified during playback");
            report.Add("INFO delayed editor callbacks retimed for synthetic input only: " + delayedEditorInputs);
            Check(previews > 200 && visiblePreviews == previews, "All sampled note approaches visible 0.7s before judgment: " + visiblePreviews + "/" + previews);
            SessionState.SetBool(Key, false); EditorApplication.isPlaying = false; return;
        }
        if (now > deadline) throw new TimeoutException("Stage2 playback did not finish; last time=" + lastTime);
        if (placer == null || clock == null) return;
        double t = clock.SongTime; lastTime = (float)t; jumps = placer.player.AutomaticJumpCount;
        if (paused && !resumed)
        {
            if (now < pauseUntil) return;
            Check(Math.Abs(clock.SongTime - frozenTime) < .00001 && Vector3.Distance(placer.player.transform.position, frozenPosition) < .00001f, "Pause freezes music time and player position");
            Time.timeScale = 1; clock.SetMenuPaused(false); resumed = true; return;
        }
        if (!paused && t > 39)
        {
            clock.SetMenuPaused(true); Time.timeScale = 0; paused = true; pauseUntil = now + .6;
            frozenTime = clock.SongTime; frozenPosition = placer.player.transform.position; return;
        }
        foreach (var note in notes)
        {
            if (note == null || note.isJudged) continue;
            if (t >= note.judgeTime - .8 && t < note.judgeTime - .6 && seen.Add(note.GetInstanceID()))
            {
                previews++; var v = placer.route.view.WorldToViewportPoint(note.transform.position);
                if (v.z > 0 && v.x > .02f && v.x < .98f && v.y > .02f && v.y < .98f) visiblePreviews++;
            }
            if (note.kind == NoteKind.Mash)
            {
                if (t < note.judgeTime) continue;
                if (!note.isMashActive) { note.isMashActive = true; ActiveMash.SetValue(judge, note); clock.SetFlowPaused(true); }
                if (now >= nextMashPress) { judge.TryHit(note.line); nextMashPress = now + .1; if (note.isJudged) hits++; }
            }
            else if (!note.isHolding && t >= note.judgeTime)
            {
                // Editor tooling/screenshots can block the main thread beyond the
                // input window. Exercise the intended timestamp without changing
                // the music clock or the production player's input behavior.
                float offset = judge.inputOffsetSeconds;
                try
                {
                    if (Math.Abs(judge.JudgeTime - note.judgeTime) > judge.goodWindow)
                    { judge.inputOffsetSeconds = (float)(note.judgeTime - clock.SongTime); delayedEditorInputs++; }
                    if (!judge.TryHit(note.line)) throw new InvalidOperationException("Input missed at " + t + " for " + note.judgeTime);
                }
                finally { judge.inputOffsetSeconds = offset; }
                hits++;
            }
            if (note != null && note.isHolding && t >= note.judgeTime + note.holdTime)
            { Complete.Invoke(judge, new object[] { note, note.startJudge }); holds++; }
        }
        if (shot < Shots.Length && t >= Shots[shot])
        {
            ScreenCapture.CaptureScreenshot("Docs/Preview/Stage2-Causeway/" + (++shot).ToString("00") + ".png");
            report.Add("INFO playback " + t.ToString("F2") + "s; speed=" + placer.route.CurrentSpeed.ToString("F2") + "; judged=" + hits);
            File.WriteAllLines(Report, report);
        }
    }
}
