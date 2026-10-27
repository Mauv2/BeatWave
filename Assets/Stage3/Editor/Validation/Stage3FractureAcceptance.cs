#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BeatWave.Stage3
{
    // Focused real-time playback, with API input; does not simulate hardware keys.
    [InitializeOnLoad]
    public static class Stage3FractureAcceptance
    {
        const string Key="BeatWave.Stage3.FractureAcceptance";
        static readonly List<string> report=new List<string>();
        static Stage3Gameplay game;
        static MashNote gate;
        static Note[] notes;
        static int scenario, shot, hits;
        static bool paused, resumed, resolved, locked;
        static double deadline, pauseAt, frozenTime;
        static float nextPress;
        static Vector3 frozenPosition;
        static Color frozenCrack;
        static LineRenderer crack;
        static readonly float[] Shots={Stage3Route.FractureStart+1.2f,Stage3Route.FractureRelease+.25f,
            Stage3Route.FractureRelease+1.4f,Stage3Route.FractureLanding-.55f,Stage3Route.FractureResume-.5f};

        static Stage3FractureAcceptance()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.ExitingPlayMode)return;
                SessionState.SetBool(Key,false);
                if(locked){locked=false;EditorApplication.UnlockReloadAssemblies();}
            };
        }
        [MenuItem("BeatWave/Validate Stage3 - First Fracture")]
        public static void Start()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="Stage3")
                throw new InvalidOperationException("Open Stage3 in edit mode first.");
            report.Clear();scenario=0;game=null;
            Directory.CreateDirectory("Reports");
            File.WriteAllText("Reports/Stage3-fracture-validation.txt","");
            SessionState.SetBool(Key,true);
            EditorApplication.isPlaying=true;
        }
        static void Check(bool ok,string message)
        {
            report.Add((ok?"PASS ":"FAIL ")+message);
            Directory.CreateDirectory("Reports");
            File.WriteAllLines("Reports/Stage3-fracture-validation.txt",report);
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try { Run(); } catch(Exception e){Check(false,e.ToString());Stop();}
        }
        static void Run()
        {
            double now=EditorApplication.timeSinceStartup;
            if(game==null)
            {
                game=Object.FindObjectOfType<Stage3Gameplay>();
                if(game==null || game.Clock==null || !game.Clock.HasStarted){game=null;return;}
                if(!locked){EditorApplication.LockReloadAssemblies();locked=true;}
                if(report.Count==0 && File.Exists("Reports/Stage3-fracture-validation.txt"))
                    report.AddRange(File.ReadAllLines("Reports/Stage3-fracture-validation.txt"));
                Application.runInBackground=true;
                game.judge.enabled=false;
                gate=game.transitions.gates[0];
                notes=Object.FindObjectsOfType<Note>().Where(n=>n.hasTiming&&!n.isEventNote).OrderBy(n=>n.judgeTime).ToArray();
                foreach(var n in notes.Where(n=>n.judgeTime<gate.startTime)){n.isJudged=true;n.gameObject.SetActive(false);}
                Check(!notes.Any(n=>n.judgeTime>=gate.startTime && n.judgeTime<Stage3Route.FractureResume-.001),"No input required during the fall or at touchdown");
                Check(!gate.Resolved && gate.Hits==0,"Fresh gate state after "+(scenario==0?"start":"retry"));
                if(scenario==0)CheckCameraLead();
                crack=game.transitions.collapsePieces[2].GetComponentsInChildren<LineRenderer>().First(l=>l.name=="Luminous fissure");
                game.Clock.BeginSong(game.music,gate.startTime-.65f);
                paused=resumed=resolved=false;shot=hits=0;nextPress=gate.startTime+.1f;deadline=now+25;
            }
            if(now>deadline){Check(false,"Focused playback timed out");Stop();return;}
            if(paused&&!resumed)
            {
                if(now-pauseAt<.4)return;
                Check(Math.Abs(game.Clock.SongTime-frozenTime)<.001 && Vector3.Distance(game.player.position,frozenPosition)<.001 && crack.startColor==frozenCrack && !gate.Press(),"Pause freezes music, character, crack pulse and rejects Mash input");
                Object.FindObjectOfType<PauseManager>().ResumeGame();resumed=true;
            }
            if(game.Clock.IsPaused || !game.Clock.HasStarted)return;
            float time=(float)game.Clock.SongTime;
            if(scenario==0 && gate.Active && time>=nextPress)
            {
                int before=gate.Hits;
                float beforeAlpha=crack.startColor.a;
                gate.Press();nextPress=time+.14f;
                if(before==0)
                {
                    game.transitions.Present((float)game.Clock.SongTime);
                    Check(gate.Hits==1 && crack.startColor.a>beforeAlpha+.3f && crack.startWidth>.06f,"First Mash press immediately lights and widens the floor cracks (before="+beforeAlpha+", after="+crack.startColor.a+", width="+crack.startWidth+", elapsed="+((float)game.Clock.SongTime-gate.LastPressTime)+")");
                }
                if(gate.Hits==4&&!paused)
                {
                    Object.FindObjectOfType<PauseManager>().PauseGame();paused=true;pauseAt=now;
                    frozenTime=game.Clock.SongTime;frozenPosition=game.player.position;frozenCrack=crack.startColor;
                }
            }
            if(gate.Resolved&&!resolved)
            {
                resolved=true;
                Check(gate.Success==(scenario==0),"Gate "+(scenario==0?"success":"failure")+" resolves correctly");
                Check((scenario==0?game.judge.perfectCount:game.judge.missCount)==1,"Gate reports exactly one judgement");
                Check(!game.Clock.FlowPaused,"Music continues through gate resolution");
                if(scenario==0)Check(gate.Hits>gate.requiredHits && gate.HitEffectsSpawned==gate.Hits,"Every continued Mash press has feedback beyond the threshold");
            }
            if(shot<Shots.Length && time>=Shots[shot])
            {
                if(scenario==0 || shot==1)Stage3Acceptance.Capture("Fracture-"+(scenario==0?"Success-":"Miss-")+(shot+1));
                shot++;
            }
            foreach(var n in notes)
                if(n!=null&&!n.isJudged&&n.judgeTime>=Stage3Route.FractureResume-.001&&n.judgeTime<=Stage3Route.Beat(124)+.001&&time>=n.judgeTime)
                {
                    if(!game.judge.TryHit(n.line))throw new InvalidOperationException("Post-landing input missed at "+time+" target="+n.judgeTime);
                    hits++;
                }
            if(time<Stage3Route.Beat(124)+.2f)return;
            Check(hits==3,"First three post-landing Tap inputs accepted after "+(scenario==0?"success":"failure"));
            Check(game.player.position.y<0 && game.transitions.collapseColliders.All(c=>!c.enabled),"Both outcomes land in the lower route and disable broken ground collision");
            Check(!game.transform.Find("Fracture landing guidance").GetComponentsInChildren<LineRenderer>().Any(l=>l.enabled),"Temporary guides clear before the next phrase");
            if(scenario==0){scenario=1;SceneManager.LoadScene("Stage3");game=null;}
            else Stop();
        }
        static void CheckCameraLead()
        {
            var camera=game.view;var position=camera.transform.position;float size=camera.orthographicSize,aspect=camera.aspect;
            var first=notes.First(n=>n.judgeTime>=Stage3Route.FractureResume-.001);
            foreach(float ratio in new[]{16f/9,4f/3})foreach(int rate in new[]{30,60,144})
            {
                camera.aspect=ratio;camera.orthographicSize=3.9f;
                camera.transform.position=(Vector3)Stage3Route.Position(Stage3Route.FractureRelease)+new Vector3(1.1f,2.4f,-10);
                bool readable=true,playerVisible=true;
                for(float t=Stage3Route.FractureRelease;t<first.judgeTime;t+=1f/rate)
                {
                    game.PresentCamera(t,1f/rate);
                    Vector3 player=camera.WorldToViewportPoint((Vector3)Stage3Route.Position(t)+Vector3.up*(game.footOffset+1));
                    playerVisible &= player.x>.04f&&player.x<.96f&&player.y>.04f&&player.y<.96f;
                    if(t<first.judgeTime-1.2f)continue;
                    Vector3 note=camera.WorldToViewportPoint(first.transform.position);
                    readable &= note.x>.08f&&note.x<.92f&&note.y>.08f&&note.y<.92f;
                }
                Check(readable&&playerVisible,"Player stays framed; first note readable for >=1.2 s at "+rate+" FPS, aspect "+ratio.ToString("F2"));
            }
            camera.aspect=aspect;camera.transform.position=position;camera.orthographicSize=size;
        }
        static void Stop(){SessionState.SetBool(Key,false);Time.timeScale=1;EditorApplication.isPlaying=false;}
    }
}
#endif
