#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace BeatWave.Stage3
{
    // Invoked explicitly from the editor. Does not ship in a player build.
    public static class Stage3PlayValidation
    {
        static Stage3Director game;
        static readonly List<string> report=new List<string>();
        static readonly float[] shots={5,18.4f,23,32,44,49.8f};
        static int shot;
        static bool pauseStarted,pauseVerified;
        static float pauseSong,pauseAudio,maxDrift;
        static Vector3 pausePosition;
        static double pauseAt,skipDriftUntil;
        static bool failed,sawAudio;
        public static void Start()
        {
            game=UnityEngine.Object.FindObjectOfType<Stage3Director>();
            if(game==null||!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Stage3 Play mode first.");
            report.Clear();shot=0;pauseStarted=pauseVerified=failed=sawAudio=false;maxDrift=0;
            Capture("Intro");game.StartRun(true);skipDriftUntil=EditorApplication.timeSinceStartup+4;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        static void Check(bool condition,string name){report.Add((condition?"PASS ":"FAIL ")+name);failed|=!condition;}
        static void Tick()
        {
            if(!EditorApplication.isPlaying||game==null){EditorApplication.update-=Tick;return;}
            if(game.SongTime>=8&&!pauseStarted)
            {
                game.Pause(true);pauseStarted=true;pauseAt=EditorApplication.timeSinceStartup;pauseSong=game.SongTime;pauseAudio=game.music.time;pausePosition=game.player.position;
            }
            if(pauseStarted&&!pauseVerified)
            {
                if(EditorApplication.timeSinceStartup-pauseAt<.7)return;
                Check(game.IsPaused&&!game.music.isPlaying,"pause stops audio");
                Check(Mathf.Abs(game.SongTime-pauseSong)<.001f&&Vector3.Distance(game.player.position,pausePosition)<.001f&&Mathf.Abs(game.music.time-pauseAudio)<.04f,"pause freezes music, path and clock");
                game.Pause(false);pauseVerified=true;skipDriftUntil=EditorApplication.timeSinceStartup+1;
            }
            if(game.IsRunning&&!game.IsPaused&&game.music.isPlaying&&game.SongTime>1&&game.SongTime<game.music.clip.length-.3f&&EditorApplication.timeSinceStartup>skipDriftUntil){maxDrift=Mathf.Max(maxDrift,Mathf.Abs(game.music.time-game.SongTime));sawAudio|=game.music.time>1;}
            if(shot<shots.Length&&game.SongTime>=shots[shot]){Capture("Act-"+(shot+1));shot++;skipDriftUntil=EditorApplication.timeSinceStartup+1;}
            if(!game.Finished)return;
            Check(game.Session.Resolved==135&&game.Session.Perfect==135&&game.Session.Miss==0,"demo judges the complete song without misses");
            Check(game.Session.Score==1000000&&game.Session.MaxCombo==135,"full-combo score and maximum combo");
            Check(!game.music.isPlaying&&game.overlay.activeSelf,"song end stops audio and shows results");
            Check(pauseVerified,"pause/resume exercise completed");
            Check(sawAudio,"audio playback position advances");
            Check(maxDrift<.15f,"audio/clock drift below 150 ms; observed "+(maxDrift*1000).ToString("F1")+" ms");
            Check(game.animator.runtimeAnimatorController!=null,"character animator is bound");
            Capture("Result");Directory.CreateDirectory("Reports");File.WriteAllText("Reports/Stage3-play-validation.txt",string.Join("\n",report));
            Debug.Log(failed?"STAGE3_PLAY_VALIDATION_FAILED":"STAGE3_PLAY_VALIDATION_PASSED");EditorApplication.update-=Tick;
        }
        public static void Capture(string name)
        {
            Directory.CreateDirectory("Docs/Preview/Stage3");var cam=game!=null?game.view:Camera.main;
            var rt=new RenderTexture(1600,900,24);var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
            var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try{cam.targetTexture=rt;foreach(var text in UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Text>())text.ForceMeshUpdate();Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Docs/Preview/Stage3/"+name+".png",tex.EncodeToPNG());}
            finally{cam.targetTexture=oldTarget;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}
#endif
