using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Real-time integration check; synthetic input exercises the existing judge APIs.
// Physical held-key input is not simulated: holds finish through JudgeManager.Complete.
[InitializeOnLoad]
public static class Stage1RhythmAcceptance
{
    const string Key = "BeatWave.Stage1.RhythmAcceptance";
    static readonly List<string> report = new List<string>();
    static readonly MethodInfo Complete = typeof(JudgeManager).GetMethod("Complete", BindingFlags.Instance|BindingFlags.NonPublic);
    static readonly MethodInfo FinishEvent = typeof(EventNoteJudge).GetMethod("Finish", BindingFlags.Instance|BindingFlags.NonPublic);
    static readonly FieldInfo EventData = typeof(EventNoteJudge).GetField("currentData", BindingFlags.Instance|BindingFlags.NonPublic);
    static Stage1RhythmBuilder.Timing timing;
    static RhythmClock clock;
    static PlayerMove mover;
    static JudgeManager judge;
    static EventNoteJudge eventJudge;
    static Note[] notes;
    static bool locked, cave, paused, resumed;
    static int registered, hits, holds, eventHits, shot;
    static double deadline, pauseAt, frozenTime, lastTime;
    static Vector3 frozenPosition;
    static float maxNoteError, maxHoldError;
    static readonly float[] Shots = {5.8f,26f,42f,53f,78f,98f,101f};

    static Stage1RhythmAcceptance()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            SessionState.SetBool(Key,false);
            if (locked) { locked=false; EditorApplication.UnlockReloadAssemblies(); }
        };
    }
    [MenuItem("BeatWave/Stage1/Validate Full Music Playback")]
    public static void Start()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Stage1") throw new InvalidOperationException("Open Stage1 in edit mode.");
        report.Clear(); timing=Stage1RhythmBuilder.ReadTiming();
        var placers=Object.FindObjectsOfType<NoteMapPlacer>();
        Check(placers.All(p=>p.chartFile != null && p.chartFile.name=="Stage1Notes"),"Both sections use the dedicated build-included Stage1 JSON");
        Check(placers.All(p=>p.songClip!=null && Mathf.Abs(p.songClip.length-timing.duration)<.001f),"Chart route duration equals imported audio: "+timing.duration+"s");
        var events=Object.FindObjectOfType<EventNoteSpawner>();
        Check(events.useAbsoluteSongTime && events.eventNotes.Length==8,"Eight event notes use absolute song timestamps");
        Check(events.eventNotes.All(n=>n.noteKind==0 && n.holdTime==0),"Fixed event chart contains Tap notes only");
        var chart=LitJson.JsonMapper.ToObject(placers[0].chartFile.text);
        bool ordered=true, attacks=true, overlaps=false, boundaries=true;
        float previous=-1;
        var laneEnd=new float[]{-1,-1};
        for(int i=0;i<chart.Count;i++)
        {
            float t=Read(chart[i],"createTime"), d=Read(chart[i],"holdTime"); int lane=(int)Read(chart[i],"noteType");
            ordered &= t>previous; previous=t;
            attacks &= timing.attackTimes.Any(a=>Mathf.Abs(a-t)<.001f);
            overlaps |= laneEnd[lane]+.16f>t; laneEnd[lane]=t+d;
            boundaries &= t+d<timing.eventEntry || t>timing.caveResume+.6f && t+d<timing.duration;
        }
        Check(chart.Count==timing.regularNotes && ordered,"Regular chart count and strict chronological order: "+chart.Count);
        Check(attacks,"Every regular note starts on a measured imported-audio attack");
        Check(!overlaps,"No same-lane taps overlap held notes or their release window");
        Check(boundaries,"Regular chart has clear event boundaries and no notes past music end");
        Check(events.eventNotes.All(n=>timing.attackTimes.Any(a=>Mathf.Abs(a-n.judgeTime)<.001f)),"Every event judgement starts on a measured audio attack");
        registered=hits=holds=eventHits=shot=0; maxNoteError=maxHoldError=0;lastTime=0;
        cave=paused=resumed=false;clock=null;notes=null;
        SessionState.SetBool(Key,true); EditorApplication.isPlaying=true;
    }
    static float Read(LitJson.JsonData d,string key) => float.Parse(d[key].ToString(),System.Globalization.CultureInfo.InvariantCulture);
    static void Check(bool pass,string message)
    {
        report.Add((pass?"PASS ":"FAIL ")+message);
        Directory.CreateDirectory("Reports");File.WriteAllLines("Reports/Stage1-rhythm-validation.txt",report);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try { Run(); } catch(Exception e) { Check(false,e.ToString()); Stop(); }
    }
    static void Run()
    {
        double now=EditorApplication.timeSinceStartup;
        if(clock==null && SceneManager.GetActiveScene().name=="Stage1")
        {
            clock=Object.FindObjectOfType<RhythmClock>();
            if(clock==null || clock.Source==null){clock=null;return;}
            if(!locked){EditorApplication.LockReloadAssemblies();locked=true;}
            if(report.Count==0)report.AddRange(File.ReadAllLines("Reports/Stage1-rhythm-validation.txt"));
            timing=Stage1RhythmBuilder.ReadTiming();
            Application.runInBackground=true;
            mover=Object.FindObjectOfType<PlayerMove>();judge=Object.FindObjectOfType<JudgeManager>();
            eventJudge=Object.FindObjectOfType<EventNoteJudge>();eventJudge.enabled=false;
            judge.enabled=false;CaptureNotes();deadline=now+timing.duration+50;
            Check(clock.Source.clip.name.Contains("mp3cut"),"Original selected Stage1 music is retained");
        }
        if(SceneManager.GetActiveScene().name=="ResultScene")
        {
            var result=GameResultManager.Instance.resultData;
            Check(lastTime>=timing.duration-.04,"Result opens only at the actual audio end (last sample "+lastTime.ToString("F3")+")");
            Check(registered==timing.regularNotes,"All "+registered+" regular notes spawned across both sections");
            Check(hits==timing.regularNotes && holds==timing.holds,"All regular timing judgements and "+holds+" holds completed");
            Check(eventHits==8,"All eight event notes completed before cave transition");
            Check(result.miss==0 && result.perfect+result.great+result.good==timing.regularNotes+8,"Result totals include every note; misses="+result.miss);
            Check(maxNoteError<.03f,"Note positions match their scheduled marker positions; max error="+maxNoteError.ToString("F4"));
            Check(maxHoldError<.2f,"Hold ribbons follow straight ground; max deviation="+maxHoldError.ToString("F4"));
            Check(cave && paused && resumed,"Cave continuation and pause/resume completed");
            Stop();return;
        }
        if(now>deadline){Check(false,"Runtime timeout at "+lastTime);Stop();return;}
        judge.enabled=false;eventJudge.enabled=false;
        double t=clock.SongTime;lastTime=t;
        if(!cave && clock.TravelEnabled && mover.Path!=null && mover.Path.StartTime>1)
        {
            cave=true;CaptureNotes();
            Check(Math.Abs(mover.Path.StartTime-timing.caveResume)<.08,"Cave resumes at authored phrase boundary: "+mover.Path.StartTime.ToString("F3"));
            Check(Vector2.Distance(mover.Path.Evaluate(timing.duration-.3),mover.Path.Evaluate(timing.duration))>1.4f,"Route continues through the final audio tail");
            Vector2 finish=mover.Path.Evaluate(timing.duration);
            var ground=Physics2D.RaycastAll(finish+Vector2.up*.1f,Vector2.down,1.4f,mover.groundLayer);
            Check(ground.Any(h=>!h.collider.isTrigger),"Existing tilemap covers the song ending at "+finish);
        }
        if(!paused && t>25)
        {
            Object.FindObjectOfType<PauseManager>().PauseGame();paused=true;pauseAt=now;
            frozenTime=clock.SongTime;frozenPosition=mover.transform.position;
        }
        if(paused&&!resumed)
        {
            if(now-pauseAt<.7)return;
            Check(Math.Abs(clock.SongTime-frozenTime)<.0001 && Vector3.Distance(mover.transform.position,frozenPosition)<.001f,"Pause freezes song and player together");
            Object.FindObjectOfType<PauseManager>().ResumeGame();resumed=true;
        }
        if(clock.IsPaused)return;
        foreach(var n in notes)
        {
            if(n==null||n.isJudged)continue;
            if(!n.isHolding&&t>=n.judgeTime)
            {
                Vector3 offset=(n.line==NoteLine.Up?mover.upMarker:mover.downMarker).position-mover.transform.position;
                offset.x=Mathf.Abs(offset.x)*mover.Path.DirectionAt(n.judgeTime);
                Vector3 expected=(Vector3)mover.Path.Evaluate(n.judgeTime)+offset;
                maxNoteError=Mathf.Max(maxNoteError,Vector3.Distance(expected,n.transform.position));
                if(!judge.TryHit(n.line))throw new InvalidOperationException("Synthetic input missed "+n.judgeTime+" now="+t);
                hits++;
            }
            if(n!=null&&n.isHolding&&t>=n.judgeTime+n.holdTime)
            { Complete.Invoke(judge,new object[]{n,n.startJudge});holds++; }
        }
        if(eventJudge.currentNote!=null)
        {
            var data=(EventNoteData)EventData.GetValue(eventJudge);
            if(eventJudge.currentNote.kind!=NoteKind.Tap)throw new InvalidOperationException("Fixed event spawned a non-Tap note");
            if(t>=data.judgeTime)
            {FinishEvent.Invoke(eventJudge,new object[]{"Perfect"});eventHits++;}
        }
        if(shot<Shots.Length&&t>=Shots[shot]){Capture("Stage1-"+(shot+1));shot++;}
    }
    static void CaptureNotes()
    {
        notes=Object.FindObjectsOfType<Note>().Where(n=>n.hasTiming&&!n.isEventNote&&!n.isJudged).OrderBy(n=>n.judgeTime).ToArray();
        registered+=notes.Length;
        foreach(var note in notes.Where(n=>n.kind==NoteKind.Hold))
        {
            var visual=note.GetComponent<HoldNoteVisual>();
            for(int i=0;i<=10;i++)
            {
                float u=i/10f;
                Vector3 start=visual.GetPoint(0);
                Vector3 expected=start+(Vector3)(mover.Path.Evaluate(note.judgeTime+note.holdTime*u)-mover.Path.Evaluate(note.judgeTime));
                maxHoldError=Mathf.Max(maxHoldError,Vector3.Distance(expected,visual.GetPoint(u)));
            }
        }
    }
    static void Capture(string name)
    {
        var cam=Camera.main; if(cam==null)return;
        var canvases=Object.FindObjectsOfType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=10;}
        var target=new RenderTexture(1280,720,24);var old=cam.targetTexture;var active=RenderTexture.active;
        cam.targetTexture=target;cam.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        Directory.CreateDirectory("Docs/Preview/Stage1-Rhythm");File.WriteAllBytes("Docs/Preview/Stage1-Rhythm/"+name+".png",image.EncodeToPNG());
        cam.targetTexture=old;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(target);
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
    }
    static void Stop(){SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
}
