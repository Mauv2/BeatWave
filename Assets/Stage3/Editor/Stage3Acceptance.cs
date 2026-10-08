#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BeatWave.Stage3
{
    [InitializeOnLoad]
    public static class Stage3Acceptance
    {
        const string Key = "BeatWave.Stage3.Acceptance";
        static readonly List<string> report = new List<string>();
        static Stage3Gameplay game;
        static Note[] notes;
        static int phase, shot;
        static double deadline, at, frozen;
        static Vector3 frozenPosition;
        static float drift;
        static bool paused, resumed, mashVerified, failureZoom;
        static float nextMashPress;
        static int frozenHits, lateSyntheticInputs;
        static float clipDuration, lastSongTime;
        static readonly MethodInfo Complete = typeof(JudgeManager).GetMethod("Complete", BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly MethodInfo Miss = typeof(JudgeManager).GetMethod("Miss", BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly float[] Shots = {7,18.6f,20.6f,27,35.8f,42,51.7f,52.9f};
        static Stage3Acceptance() { EditorApplication.update += Tick; }
        public static void Start()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before starting acceptance.");
            ValidateScene();
            phase=shot=lateSyntheticInputs=0;drift=lastSongTime=nextMashPress=0;
            paused=resumed=mashVerified=failureZoom=false;
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        static void Check(bool ok,string message)
        {
            report.Add((ok?"PASS ":"FAIL ")+message);
            Directory.CreateDirectory("Reports");
            File.WriteAllLines("Reports/Stage3-transitions-validation.txt",report);
        }
        public static void ValidateScene()
        {
            report.Clear();
            var active = SceneManager.GetActiveScene();
            if (active.name != "Stage3") throw new InvalidOperationException("Open Stage3 first.");
            game = Object.FindObjectOfType<Stage3Gameplay>();
            Stage3Route.ConfigureDuration(game.music.clip.length);
            Check(game != null && game.music.clip.name == "lastwish_inst", "Stage3 uses the requested Last Wish clip");
            var stage1 = EditorSceneManager.OpenScene("Assets/Scenes/Stage1.unity", OpenSceneMode.Additive);
            try
            {
                var sourceRoots = stage1.GetRootGameObjects();
                var sourceUI = sourceRoots.First(r=>r.name=="Canvas");
                var targetUI = active.GetRootGameObjects().First(r=>r.name=="Canvas");
                foreach(string branch in new[]{"text","PauseMenu","Image"})
                    Check(UISignature(sourceUI.transform.Find(branch)) == UISignature(targetUI.transform.Find(branch)), "Stage1 UI identical: "+branch);
                var sourcePlacer = sourceRoots.SelectMany(r=>r.GetComponentsInChildren<NoteMapPlacer>()).First(p=>p.placeOnStart);
                Check(game.tapPrefab == sourcePlacer.tapNotePrefab && game.holdPrefab == sourcePlacer.holdNotePrefab && game.mashPrefab == sourcePlacer.mashNotePrefab, "Exact Stage1 tap and hold prefab references");
            }
            finally { EditorSceneManager.CloseScene(stage1,true); SceneManager.SetActiveScene(active); }
            var pause = Object.FindObjectOfType<PauseManager>();
            foreach(string method in new[]{"ResumeGame","RetryGame","GoToSelectScene","GoToTitle"})
            {
                var buttons = pause.PauseMenu.GetComponentsInChildren<Button>(true);
                Check(buttons.Any(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)==method && b.onClick.GetPersistentTarget(i)==pause)), "Stage1 button binding: "+method);
            }
            Check(game.GetComponentsInChildren<SpriteRenderer>(true).All(s=>s.GetComponentInParent<Note>(true)!=null || s.sprite == null || AssetDatabase.GetAssetPath(s.sprite).StartsWith("Assets/Stage3/Art/")), "All environment sprite assets owned by Stage3");
            Check(!game.GetComponentsInChildren<SpriteRenderer>(true).Any(s=>s.sprite != null && AssetDatabase.GetAssetPath(s.sprite).Contains("S1BackGround")), "No old background or ground reused");
            Check(game.seesaws.Length==1, "Only one seesaw remains");
            Check(Stage3Route.Legs.Count(l=>l.kind==Stage3LegKind.Ground && l.to.y>l.from.y)==4 && Stage3Route.Legs.Count(l=>l.kind==Stage3LegKind.Ground && l.to.y<l.from.y)==3 && Stage3Route.Legs.Count(l=>l.kind==Stage3LegKind.Ground && l.to.y==l.from.y)==6, "Six straight, four uphill and three downhill sections");
            Check(Mathf.Abs(Stage3Route.Legs.Last().end-game.music.clip.length)<.001f && Vector2.Distance(Stage3Route.Position(game.music.clip.length-.3f),Stage3Route.Position(game.music.clip.length))>1,"Route extends to the exact clip end, with motion in the final audio tail");
            Check(Stage3Route.Legs.All(l=>l.end>l.start) && Enumerable.Range(1,Stage3Route.Legs.Length-1).All(i=>Mathf.Abs(Stage3Route.Legs[i-1].end-Stage3Route.Legs[i].start)<.001f),"Entire song has continuous time coverage without gaps");
            Check(game.transitions.gates.All(g=>g.caption.text=="MASH  W / S" && !g.presentation.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Mash progress")),"Mash presents only an input hint, without count or completion ring");
            Check(game.transitions.gates.Length==3 && game.transitions.collapsePieces.Length==5, "Three musical Mash gates and five falling ground slabs");
            Physics2D.SyncTransforms();
            bool grounded=true;
            foreach(var leg in Stage3Route.Legs.Where(l=>l.kind==Stage3LegKind.Ground))
                for(int i=1;i<5;i++){var pos=leg.At(Mathf.Lerp(leg.start,leg.end,i/5f));var hits=Physics2D.RaycastAll(pos+Vector2.up*.1f,Vector2.down,.3f);grounded&=hits.Any(h=>h.collider!=null && !h.collider.isTrigger && h.transform.IsChildOf(game.transform.Find("New floating walkways")));}
            Check(grounded,"Playable route follows actual inclined ground colliders");
            Check(Enumerable.Range(1,Stage3Route.Legs.Length-1).All(i=>Vector2.Distance(Stage3Route.Legs[i-1].to,Stage3Route.Legs[i].from)<.001f),"Route remains continuous across slopes, collapse and transfer");
            var chart = JsonUtility.FromJson<Stage3Chart>(game.chartFile.text);
            Check(chart.notes.Length==105 && chart.notes.Count(n=>n.duration>0)==5, "Chart: 105 regular notes including 5 holds, plus 3 Mash gates");
            Check(chart.notes.All(n=>game.transitions.gates.All(g=>n.time+n.duration<g.startTime || n.time>g.releaseTime)),"Mash phrases do not overlap regular notes or holds");
            Check(chart.notes.All(n=>n.time+n.duration<game.music.clip.length), "All notes finish before the actual audio clip ends");
            Check(!active.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0), "No missing scripts");
        }
        static string UISignature(Transform root)
        {
            return string.Join("\n",root.GetComponentsInChildren<Transform>(true).Select(t=>
            {
                string value=t.name+"|"+t.gameObject.activeSelf;
                var rect=t as RectTransform;
                if(rect!=null) value+="|"+rect.anchorMin+"|"+rect.anchorMax+"|"+rect.anchoredPosition+"|"+rect.sizeDelta+"|"+rect.pivot;
                var image=t.GetComponent<Image>();if(image!=null)value+="|image:"+AssetDatabase.GetAssetPath(image.sprite)+"|"+image.color;
                var text=t.GetComponent<TMPro.TMP_Text>();if(text!=null)value+="|tmp:"+text.text+"|"+AssetDatabase.GetAssetPath(text.font)+"|"+text.fontSize+"|"+text.color;
                var legacy=t.GetComponent<Text>();if(legacy!=null)value+="|text:"+legacy.text+"|"+AssetDatabase.GetAssetPath(legacy.font)+"|"+legacy.fontSize+"|"+legacy.color;
                return value;
            }));
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try { Run(); }
            catch(Exception e) { Check(false,e.ToString()); Finish(); }
        }
        static void Run()
        {
            double now=EditorApplication.timeSinceStartup;
            if(phase==0)
            {
                game=Object.FindObjectOfType<Stage3Gameplay>();
                Application.runInBackground=true;
                if(game==null || game.NoteCount==0 || game.Clock.Source==null)return;
                if(File.Exists("Reports/Stage3-transitions-validation.txt"))report.AddRange(File.ReadAllLines("Reports/Stage3-transitions-validation.txt"));
                Application.runInBackground=true;
                // Exercise the original timing/score APIs with synthetic presses.
                // Holds complete via the original Complete method; physical held-key
                // input is not simulated by this editor harness.
                game.judge.enabled=false;
                clipDuration=game.music.clip.length;
                notes=Object.FindObjectsOfType<Note>().Where(n=>!n.isEventNote).OrderBy(n=>n.judgeTime).ToArray();
                Check(notes.Length==105,"All 105 ordinary runtime notes registered");
                Check(!game.judge.TryHit(NoteLine.Up),"Out-of-window press rejected");
                deadline=now+130;phase=1;
            }
            if(now>deadline){Check(false,"Acceptance timeout in phase "+phase);Finish();return;}
            if(phase==1)
            {
                if(SceneManager.GetActiveScene().name=="ResultScene")
                {
                    var data=GameResultManager.Instance.resultData;
                    Check(data.perfect+data.great+data.good+data.miss==108 && data.miss==lateSyntheticInputs,"Full chart resolved through Stage1 judgement APIs; late editor-driven inputs: "+lateSyntheticInputs);
                    Check(lastSongTime>=clipDuration-.02f,"Result scene opens after full audio playback ("+clipDuration.ToString("F3")+" s), never at the final note or Mash threshold");
                    Check(data.score==data.perfect*100+data.great*80+data.good*50,"Stage1 score copied to result scene");
                    Check(GameResultManager.Instance.lastStageName=="Stage3","Result retry destination is Stage3");
                    Check(paused&&resumed,"Original pause/resume animation completed");
                    Check(drift<.15f,"Audio / DSP clock drift <150 ms; observed "+(drift*1000).ToString("F1")+" ms");
                    SceneManager.LoadScene("Stage3");phase=2;at=now;return;
                }
                if(game==null)return;
                float time=(float)game.Clock.SongTime;
                lastSongTime=time;
                if(!game.Clock.IsPaused)
                {
                    foreach(var note in notes)
                    {
                        if(note==null||note.isJudged)continue;
                        if(!note.isHolding && time>=note.judgeTime && time-note.judgeTime<.12)
                            game.judge.TryHit(note.line);
                        if(note.isHolding && time>=note.judgeTime+note.holdTime)
                            Complete.Invoke(game.judge,new object[]{note,note.startJudge});
                        // Judge.Update is disabled to allow synthetic held notes.
                        // Keep its real late-note behavior instead of leaving an
                        // orphan pending note when the editor stalls past a window.
                        if(note!=null && !note.isJudged && !note.isHolding && time>note.judgeTime+game.judge.goodWindow)
                        { Miss.Invoke(game.judge,new object[]{note});lateSyntheticInputs++; }
                    }
                    var active=game.transitions.ActiveGate;
                    if(active!=null && time>=nextMashPress){active.Press();nextMashPress=time+.13f;}
                    if(!mashVerified && time>game.transitions.gates[2].releaseTime+.1f)
                    {
                        Check(game.transitions.gates.All(g=>g.Resolved && g.Success && g.Hits>g.requiredHits),"All three Mash gates accept continued hits beyond the hidden threshold and resolve once");
                        Check(game.transitions.gates.All(g=>g.HitEffectsSpawned==g.Hits),"Every accepted Mash press spawns a hit effect, including hits after the threshold");
                        Check(game.transitions.gates.All(g=>g.caption.text==""),"Resolved Mash hides its hint without showing a count or clear label");
                        Check(!game.Clock.FlowPaused,"Mash gates preserve continuous music playback");mashVerified=true;
                    }
                    if(time>2 && game.music.isPlaying && time<game.music.clip.length-.3f)
                        drift=Mathf.Max(drift,Mathf.Abs(game.music.time-(float)game.Clock.SongTime));
                }
                if(game.transitions.gates[0].Active && game.transitions.gates[0].Hits>=2 && !paused)
                {
                    Object.FindObjectOfType<PauseManager>().PauseGame();
                    frozen=game.Clock.SongTime;frozenPosition=game.player.position;frozenHits=game.transitions.gates[0].Hits;at=now;paused=true;
                }
                if(paused && !resumed && now-at>=.8)
                {
                    Check(game.Clock.MenuPaused && !game.music.isPlaying && Time.timeScale==0,"ESC menu pauses audio and gameplay");
                    Check(Math.Abs(game.Clock.SongTime-frozen)<.001 && Vector3.Distance(game.player.position,frozenPosition)<.001,"Clock and player remain frozen while menu open");
                    Check(game.transitions.gates[0].Hits==frozenHits && !game.transitions.gates[0].Press(),"Mash cannot advance or accept input while ESC menu is open");
                    Capture("V3-Pause-during-Mash");
                    InvokeButton("ResumeGame");resumed=true;
                }
                if(shot<Shots.Length && time>=Shots[shot] && !game.Clock.IsPaused)
                {
                    if(shot==1||shot==4||shot==6)Check(game.view.orthographicSize<4.6f,"Camera zooms toward Mash gate "+shot);
                    if(shot==2)Check(game.transitions.collapseColliders.All(c=>!c.enabled) && game.transitions.collapsePieces[2].position.y<7,"Collapse moves visible terrain and disables its collision");
                    if(shot==3)Check(game.transitions.depths.color.a>.95f,"Collapse reveals the new underground background");
                    game.Clock.SetMenuPaused(true);Capture("V3-Stage3-"+(shot+1));game.Clock.SetMenuPaused(false);shot++;
                }
                return;
            }
            if(now-at<1)return;
            if(phase==2)
            {
                game=Object.FindObjectOfType<Stage3Gameplay>();if(game==null)return;
                game.judge.enabled=false;game.judge.ClearActiveNotes();
                game.Clock.BeginSong(game.music,game.transitions.gates[0].startTime-.2f);
                Check(!game.transitions.gates[0].Press(),"Mash rejects presses before its musical window");
                phase=21;at=now;return;
            }
            if(phase==21)
            {
                var gate=game.transitions.gates[0];
                if(!failureZoom && game.Clock.SongTime>gate.startTime+.8f){Check(game.view.orthographicSize<4.6f,"Mash focus also works without any hits");failureZoom=true;}
                if(!gate.Resolved)return;
                Check(!gate.Success && gate.Hits==0 && game.judge.missCount==1,"Unplayed Mash produces one Miss, not a forced success");
                Check(!game.Clock.IsPaused && game.Clock.SongTime>=gate.releaseTime,"Failed Mash still advances into the next stage without softlock");
                Object.FindObjectOfType<PauseManager>().PauseGame();InvokeButton("RetryGame");phase=3;at=now;return;
            }
            if(phase==3)
            {
                var fresh=Object.FindObjectOfType<Stage3Gameplay>();
                Check(fresh!=null && fresh!=game && fresh.Clock.SongTime<2,"Retry button reloads Stage3 and restarts music");
                Check(fresh.transitions.gates.All(g=>g.Hits==0 && !g.Resolved) && fresh.transitions.collapseColliders.All(c=>c.enabled),"Retry resets Mash progress and reconstructs collapsed ground");
                Object.FindObjectOfType<PauseManager>().PauseGame();InvokeButton("GoToSelectScene");phase=4;at=now;return;
            }
            if(phase==4)
            {
                Check(SceneManager.GetActiveScene().name=="Select","Song select button opens Select");
                SceneManager.LoadScene("Stage3");phase=5;at=now;return;
            }
            if(phase==5)
            {
                Object.FindObjectOfType<PauseManager>().PauseGame();InvokeButton("GoToTitle");phase=6;at=now;return;
            }
            if(phase==6)
            {
                Check(SceneManager.GetActiveScene().name=="Title","Title button opens Title");
                Finish();
            }
        }
        static void InvokeButton(string method)
        {
            var pause=Object.FindObjectOfType<PauseManager>();
            var button=pause.PauseMenu.GetComponentsInChildren<Button>(true).First(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentMethodName(i)==method));
            button.onClick.Invoke();
        }
        static void Finish()
        {
            SessionState.SetBool(Key,false);Time.timeScale=1;
            Debug.Log(report.Any(s=>s.StartsWith("FAIL"))?"STAGE3_ACCEPTANCE_FAILED":"STAGE3_ACCEPTANCE_PASSED");
            EditorApplication.isPlaying=false;
            phase=0;
        }
        public static void Capture(string name)
        {
            Directory.CreateDirectory("Docs/Preview/Stage3");
            var cam=Camera.main;var canvases=Object.FindObjectsOfType<Canvas>();
            var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();
            var distances=canvases.Select(c=>c.planeDistance).ToArray();
            int width=Screen.width,height=Screen.height;
            var rt=new RenderTexture(width,height,24);var tex=new Texture2D(width,height,TextureFormat.RGB24,false);
            var target=cam.targetTexture;var active=RenderTexture.active;
            try
            {
                foreach(var c in canvases)if(c.renderMode==RenderMode.ScreenSpaceOverlay){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}
                cam.targetTexture=rt;Canvas.ForceUpdateCanvases();
                foreach(var text in Object.FindObjectsOfType<TMPro.TMP_Text>())text.ForceMeshUpdate();
                cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();
                File.WriteAllBytes("Docs/Preview/Stage3/"+name+".png",tex.EncodeToPNG());
            }
            finally
            {
                cam.targetTexture=target;RenderTexture.active=active;
                for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}
                Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);
            }
        }
    }
}
#endif
