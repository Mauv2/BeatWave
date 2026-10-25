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
    static readonly FieldInfo TransitionTail = typeof(CaveEventManager).GetField("transitionTail", BindingFlags.Instance|BindingFlags.NonPublic);
    static CaveEventManager caveManager;
    static bool transitionSeen, transitionPaused, transitionResumed, audioComplete;
    // Play mode reloads static fields after Start() has run in the editor.
    static bool tailContinuous=true, tailDecreases=true;
    static double transitionPauseAt, transitionSongTime;
    static float lastTailVolume, frozenTailVolume, tailPeak, cavePeak;
    static int frozenTailSample, previousAudioSample;
    static AudioSource originalSource;
    static float originalVolume;
    static double pauseSongTime;
    static readonly float[] audioSamples = new float[1024];
    static Stage1RhythmBuilder.Timing timing;
    static RhythmClock clock;
    static PlayerMove mover;
    static JudgeManager judge;
    static EventNoteJudge eventJudge;
    static Note[] notes;
    static bool locked, cave, paused, resumed, eventEffectsChecked;
    static int registered, hits, holds, eventHits, shot;
    static double deadline, pauseAt, frozenTime, lastTime;
    static Vector3 frozenPosition;
    static float maxNoteError, maxHoldError;
    static float maxPathStep;
    static int alignedJumps, totalCues, lastLandingCount, visibleNotes, checkedNotes;
    static readonly HashSet<int> phrases=new HashSet<int>();
    static readonly HashSet<int> previews=new HashSet<int>();
    static int visiblePreviews;
    static Vector3 frozenCamera;
    static float frozenSize, frozenEffect;
    static bool eventCameraSampled, eventPointsVisible=true;
    static Vector3 eventCameraPosition;
    static float eventCameraSize, eventCameraDrift, chainMinZoom=float.PositiveInfinity, chainMaxZoom;
    static int runningSamples, framedRunningSamples, visibleLandings;
    static bool tempoPlaybackValid=true;
    static int tempoSamples;
    static readonly HashSet<double> landingPreviews=new HashSet<double>();
    static readonly float[] Shots = {5.65f,8.1f,26f,42f,54f,61.7f,78f,98f,101f};

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
        var direction=Object.FindObjectOfType<Stage1MusicDirection>();
        Check(direction!=null && direction.timingFile!=null && direction.lineMaterial!=null && direction.noteRoot!=null,"Musical direction has timing, notes and existing art bindings");
        var director=Camera.main.GetComponent<Stage1CameraDirector>();
        Check(director!=null && director.eventPoints.Length>=4,"Phrase camera includes fixed-event note positions");
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
            boundaries &= t+d<timing.eventEntry || t>=timing.caveFirstNote-.001f && t+d<timing.duration;
        }
        Check(chart.Count==timing.regularNotes && ordered,"Regular chart count and strict chronological order: "+chart.Count);
        Check(attacks,"Every regular note starts on a measured imported-audio attack");
        Check(!overlaps,"No same-lane taps overlap held notes or their release window");
        Check(boundaries,"Regular chart has clear event boundaries and no notes past music end");
        Check(events.eventNotes.All(n=>timing.attackTimes.Any(a=>Mathf.Abs(a-n.judgeTime)<.001f)),"Every event judgement starts on a measured audio attack");
        registered=hits=holds=eventHits=shot=0; maxNoteError=maxHoldError=0;lastTime=0;
        cave=paused=resumed=eventEffectsChecked=false;clock=null;notes=null;
        transitionSeen=transitionPaused=transitionResumed=audioComplete=false;
        tailContinuous=tailDecreases=true;lastTailVolume=1f;tailPeak=cavePeak=0;
        maxPathStep=0;alignedJumps=totalCues=lastLandingCount=visibleNotes=checkedNotes=visiblePreviews=0;phrases.Clear();previews.Clear();
        eventCameraSampled=false;eventPointsVisible=true;eventCameraDrift=chainMaxZoom=0;chainMinZoom=float.PositiveInfinity;
        runningSamples=framedRunningSamples=visibleLandings=0;landingPreviews.Clear();
        tempoPlaybackValid=true;tempoSamples=0;
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
            caveManager=Object.FindObjectOfType<CaveEventManager>();
            eventJudge=Object.FindObjectOfType<EventNoteJudge>();eventJudge.enabled=false;
            judge.enabled=false;CaptureNotes();deadline=now+timing.duration+50;
            Check(clock.Source.clip.name.Contains("mp3cut"),"Original selected Stage1 music is retained");
            Check(mover.GetComponentsInChildren<SpriteRenderer>().All(s=>
            {
                var group=s.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
                while(group!=null && group.transform.parent!=null && group.transform.parent.GetComponentInParent<UnityEngine.Rendering.SortingGroup>()!=null)
                    group=group.transform.parent.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
                return group!=null ? group.sortingOrder>=30 : s.sortingOrder>=30;
            }),"Player and judge markers draw above foreground terrain, with character part order preserved");
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
            Check(transitionSeen && transitionResumed && audioComplete,"Audio transition, transition pause/resume and cave fade-in completed");
            // Dense chained jumps and the quiet outro deliberately retain their
            // safe timing when no nearby attack satisfies the speed bounds.
            Check(alignedJumps>=Mathf.CeilToInt(totalCues*.75f),"At least 75% of jump endpoints align with measured attacks: "+alignedJumps+" / "+totalCues);
            Check(maxPathStep<.65f,"Retimed trajectory stays continuous; largest 10ms step="+maxPathStep.ToString("F3"));
            Check(phrases.Count==6,"Running, switchback, jump, fixed event, fall and outro camera states exercised");
            Check(eventCameraSampled && eventCameraDrift<.02f && eventPointsVisible,
                "Fixed event holds one frame containing every spawn and judgement point; drift="+eventCameraDrift.ToString("F4"));
            Check(runningSamples>0 && framedRunningSamples==runningSamples,
                "Normal travel keeps the character in the left third: "+framedRunningSamples+" / "+runningSamples);
            Check(chainMinZoom<float.PositiveInfinity && chainMaxZoom-chainMinZoom<.15f,
                "Late chained jumps hold one zoom; size variation="+(chainMaxZoom-chainMinZoom).ToString("F4"));
            Check(landingPreviews.Count==totalCues/2 && visibleLandings==landingPreviews.Count,
                "Landing destinations visible before every launch: "+visibleLandings+" / "+landingPreviews.Count);
            Check(lastLandingCount>=11,"Music-driven landing accents executed: "+lastLandingCount);
            Check(checkedNotes>0 && visibleNotes==checkedNotes,"Judgement markers visible at every regular input: "+visibleNotes+" / "+checkedNotes);
            Check(previews.Count==timing.regularNotes && visiblePreviews==previews.Count,"Notes visible at least 0.6 seconds before input: "+visiblePreviews+" / "+previews.Count);
            Check(tempoSamples>0 && tempoPlaybackValid,"Tempo-paced running preserves original audio pitch and real-time clock: "+tempoSamples+" samples");
            Stop();return;
        }
        if(now>deadline){Check(false,"Runtime timeout at "+lastTime);Stop();return;}
        judge.enabled=false;eventJudge.enabled=false;
        double t=clock.SongTime;lastTime=t;
        var tempo=mover.GetComponent<Stage1TempoMotion>();
        if(tempo!=null && clock.HasStarted && clock.TravelEnabled && !clock.IsPaused && tempo.CurrentBpm>0)
        {
            tempoSamples++;
            float expected=tempo.runCycleSeconds*tempo.CurrentBpm/60f;
            bool sampleValid=Mathf.Abs(clock.Source.pitch-1)<.0001f && Mathf.Abs(Time.timeScale-1)<.0001f
                && Mathf.Abs(tempo.animator.GetFloat(Stage1TempoMotion.RunParameter)-expected)<.001f;
            if(!sampleValid && tempoPlaybackValid)Check(false,"Tempo sample at "+t.ToString("F3")+": pitch="+clock.Source.pitch+", timeScale="+Time.timeScale+", run="+tempo.animator.GetFloat(Stage1TempoMotion.RunParameter)+", expected="+expected);
            tempoPlaybackValid &= sampleValid;
        }
        CheckTransitionAudio(now);
        CheckCamera(t);
        phrases.Add(Camera.main.GetComponent<Stage1CameraDirector>().CurrentPhrase);
        lastLandingCount=mover.GetComponent<Stage1MusicDirection>().LandingCount;
        if(!eventEffectsChecked && t>41 && t<48)
        {
            eventEffectsChecked=true;
            Check(mover.GetComponent<Stage1MusicDirection>().EffectEnergy==0,"Landing effects finish during the fixed event instead of remaining frozen");
        }
        if(!cave && clock.TravelEnabled && mover.Path!=null && mover.Path.StartTime>1)
        {
            cave=true;CaptureNotes();
            Check(Math.Abs(mover.Path.StartTime-timing.caveArrival)<.3,"Cave arrives after uninterrupted fall: "+mover.Path.StartTime.ToString("F3"));
            Check(notes.Length>0 && notes[0].judgeTime-t>=1.0,"First cave note gives at least one second to prepare: "+(notes[0].judgeTime-t).ToString("F3"));
            var arrival=Camera.main.WorldToViewportPoint(mover.transform.position);
            Check(arrival.x>.27f && arrival.x<.4f && arrival.y>.2f && arrival.y<.45f,
                "Camera settles at cave arrival before the first input: "+arrival);
            Check(Vector2.Distance(mover.Path.Evaluate(timing.duration-.3),mover.Path.Evaluate(timing.duration))>1.4f,"Route continues through the final audio tail");
            Vector2 finish=mover.Path.Evaluate(timing.duration);
            var ground=Physics2D.RaycastAll(finish+Vector2.up*.1f,Vector2.down,1.4f,mover.groundLayer);
            Check(ground.Any(h=>!h.collider.isTrigger),"Existing tilemap covers the song ending at "+finish);
        }
        if(!paused && t>5.66)
        {
            Object.FindObjectOfType<PauseManager>().PauseGame();paused=true;pauseAt=now;
            frozenTime=clock.SongTime;frozenPosition=mover.transform.position;
            frozenCamera=Camera.main.transform.position;frozenSize=Camera.main.orthographicSize;frozenEffect=mover.GetComponent<Stage1MusicDirection>().EffectEnergy;
        }
        if(paused&&!resumed)
        {
            if(now-pauseAt<.7)return;
            Check(Math.Abs(clock.SongTime-frozenTime)<.0001 && Vector3.Distance(mover.transform.position,frozenPosition)<.001f,"Pause freezes song and player together");
            Check(frozenEffect>0 && Vector3.Distance(Camera.main.transform.position,frozenCamera)<.001f && Mathf.Abs(Camera.main.orthographicSize-frozenSize)<.001f && Mathf.Abs(mover.GetComponent<Stage1MusicDirection>().EffectEnergy-frozenEffect)<.001f,"Pause freezes camera zoom and an active landing effect");
            Object.FindObjectOfType<PauseManager>().ResumeGame();resumed=true;
        }
        if(clock.IsPaused)return;
        foreach(var n in notes)
        {
            if(n==null||n.isJudged)continue;
            if(t>=n.judgeTime-.75 && t<n.judgeTime-.6 && previews.Add(n.GetInstanceID()))
            {
                var v=Camera.main.WorldToViewportPoint(n.transform.position);
                if(v.z>0 && v.x>.03f && v.x<.97f && v.y>.03f && v.y<.97f)visiblePreviews++;
            }
            if(!n.isHolding&&t>=n.judgeTime)
            {
                Vector3 offset=(n.line==NoteLine.Up?mover.upMarker:mover.downMarker).position-mover.transform.position;
                offset.x=Mathf.Abs(offset.x)*mover.Path.DirectionAt(n.judgeTime);
                Vector3 expected=(Vector3)mover.Path.Evaluate(n.judgeTime)+offset;
                maxNoteError=Mathf.Max(maxNoteError,Vector3.Distance(expected,n.transform.position));
                var viewport=Camera.main.WorldToViewportPoint(n.transform.position);
                checkedNotes++;if(viewport.z>0 && viewport.x>.05f && viewport.x<.95f && viewport.y>.05f && viewport.y<.95f)visibleNotes++;
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
    static void CheckCamera(double time)
    {
        if(clock.IsPaused)return;
        var camera=Camera.main;
        var director=camera.GetComponent<Stage1CameraDirector>();
        if(time>41 && time<48 && director.CurrentShot==Stage1CameraDirector.Shot.FixedEvent)
        {
            if(!eventCameraSampled)
            {eventCameraSampled=true;eventCameraPosition=camera.transform.position;eventCameraSize=camera.orthographicSize;}
            eventCameraDrift=Mathf.Max(eventCameraDrift,Vector3.Distance(eventCameraPosition,camera.transform.position),Mathf.Abs(eventCameraSize-camera.orthographicSize));
            foreach(var point in director.eventPoints)eventPointsVisible &= InFrame(camera,point.position);
        }
        if((time>15 && time<30 || time>66 && time<88) && director.CurrentShot==Stage1CameraDirector.Shot.Running)
        {
            runningSamples++;
            float x=camera.WorldToViewportPoint(mover.transform.position).x;
            if(x>.28f && x<.4f)framedRunningSamples++;
        }
        if(time>94 && time<99)
        {chainMinZoom=Mathf.Min(chainMinZoom,camera.orthographicSize);chainMaxZoom=Mathf.Max(chainMaxZoom,camera.orthographicSize);}
        if(!clock.TravelEnabled || mover.Path==null)return;
        foreach(var cue in mover.Path.Jumps)
            if(time>=cue.time-.45 && time<cue.time-.25 && landingPreviews.Add(cue.time))
            {
                if(InFrame(camera,mover.Path.Evaluate(cue.endTime)))visibleLandings++;
                else Check(false,"Landing preview outside frame before launch "+cue.time.ToString("F3")
                    +": "+camera.WorldToViewportPoint(mover.Path.Evaluate(cue.endTime)));
            }
    }
    static bool InFrame(Camera camera,Vector3 point)
    {
        var v=camera.WorldToViewportPoint(point);
        return v.z>0 && v.x>.03f && v.x<.97f && v.y>.03f && v.y<.97f;
    }
    static void CheckTransitionAudio(double now)
    {
        if(!transitionSeen && caveManager.IsTransitioning)
        {
            transitionSeen=true;transitionSongTime=clock.SongTime;
            originalSource=clock.Source;originalVolume=clock.Source.volume;
            previousAudioSample=clock.Source.timeSamples;lastTailVolume=originalVolume;
            Check(caveManager.keepSongPlaying && !clock.FlowPaused,"Cave uses uninterrupted song playback");
        }
        if(!transitionSeen || audioComplete)return;
        var source=clock.Source;
        if(!clock.IsPaused && clock.HasStarted)
        {
            tailContinuous &= source==originalSource && source.isPlaying && source.timeSamples>=previousAudioSample
                && Math.Abs(source.time-clock.SongTime)<.15 && !clock.FlowPaused;
            previousAudioSample=source.timeSamples;
            if(!clock.TravelEnabled)
            {
                tailDecreases &= source.volume<=lastTailVolume+.001f;
                lastTailVolume=source.volume;tailPeak=Mathf.Max(tailPeak,AudioPeak(source));
            }
        }
        if(!transitionPaused && !clock.TravelEnabled && source.volume<originalVolume*.8f)
        {
            Object.FindObjectOfType<PauseManager>().PauseGame();
            transitionPaused=true;transitionPauseAt=now;pauseSongTime=clock.SongTime;
            frozenTailSample=source.timeSamples;frozenTailVolume=source.volume;
        }
        else if(transitionPaused && !transitionResumed && now-transitionPauseAt>.7)
        {
            Check(!source.isPlaying && Math.Abs(source.timeSamples-frozenTailSample)<source.clip.frequency*.1
                && Mathf.Abs(source.volume-frozenTailVolume)<.001f && Math.Abs(clock.SongTime-pauseSongTime)<.001,
                "Explicit menu pause freezes song and fade during falling");
            Object.FindObjectOfType<PauseManager>().ResumeGame();transitionResumed=true;previousAudioSample=0;
        }
        if(clock.TravelEnabled && !clock.IsPaused && !caveManager.IsTransitioning)
        {
            audioComplete=true;
            Check(tailContinuous && clock.SongTime>transitionSongTime+3.8,
                "Original audio advances through entire fall without rewind or replacement");
            Check(tailDecreases && tailPeak>.00001f && lastTailVolume>0 && Mathf.Abs(source.volume-originalVolume)<.03f,
                "Audible music ducks and returns to original volume");
            Check(TransitionTail.GetValue(caveManager)==null,"No duplicate transition source");
        }
    }
    static float AudioPeak(AudioSource source)
    {
        source.GetOutputData(audioSamples,0);
        float peak=0;
        foreach(float sample in audioSamples)peak=Mathf.Max(peak,Mathf.Abs(sample));
        return peak;
    }
    static void CaptureNotes()
    {
        notes=Object.FindObjectsOfType<Note>().Where(n=>n.hasTiming&&!n.isEventNote&&!n.isJudged).OrderBy(n=>n.judgeTime).ToArray();
        registered+=notes.Length;
        // Bind() applies rendering priority on the first Update after chart creation;
        // it is checked later at input time rather than during this creation callback.
        foreach(var cue in mover.Path.Jumps)
        {
            totalCues+=2;
            if(timing.attackTimes.Any(a=>Math.Abs(a-cue.time)<.015))alignedJumps++;
            if(timing.attackTimes.Any(a=>Math.Abs(a-cue.endTime)<.015))alignedJumps++;
        }
        double routeEnd=cave?timing.duration:timing.eventEntry;
        for(double t=mover.Path.StartTime+.01;t<routeEnd;t+=.01)
            maxPathStep=Mathf.Max(maxPathStep,Vector2.Distance(mover.Path.Evaluate(t-.01),mover.Path.Evaluate(t)));
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
        // Rendering/PNG encoding is blocking editor tooling. Freeze the clock for
        // that work so neither synthetic input nor preview samples can skip a beat.
        bool wasPaused=clock.MenuPaused;
        clock.SetMenuPaused(true);
        try
        {
        var canvases=Object.FindObjectsOfType<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=10;}
        var target=new RenderTexture(1280,720,24);var old=cam.targetTexture;var active=RenderTexture.active;
        cam.targetTexture=target;cam.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
        Directory.CreateDirectory("Docs/Preview/Stage1-Camera");File.WriteAllBytes("Docs/Preview/Stage1-Camera/"+name+".png",image.EncodeToPNG());
        cam.targetTexture=old;RenderTexture.active=active;Object.DestroyImmediate(image);Object.DestroyImmediate(target);
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
        }
        finally {clock.SetMenuPaused(wasPaused);}
    }
    static void Stop(){SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
}
