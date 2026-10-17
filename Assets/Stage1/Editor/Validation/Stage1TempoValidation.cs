using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1TempoValidation
{
    [MenuItem("BeatWave/Stage1/Validate Tempo Pacing")]
    public static void Run()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="Stage1")throw new InvalidOperationException("Open Stage1 in edit mode.");
        var mover=UnityEngine.Object.FindObjectOfType<PlayerMove>();
        var tempo=mover.GetComponent<Stage1TempoMotion>();
        var timing=Stage1RhythmBuilder.ReadTiming();
        var placer=UnityEngine.Object.FindObjectsOfType<NoteMapPlacer>().Single(p=>p.placeOnStart);
        var report=new List<string>();
        Action<bool,string> check=(ok,label)=>report.Add((ok?"PASS ":"FAIL ")+label);
        var origin=mover.transform.position;bool enabled=tempo.enabled;
        try
        {
            check(tempo.tempoFile!=null && tempo.chartFile==placer.chartFile,"Tempo profile and original note chart are bound");
            for(int section=0;section<2;section++)
            {
                float start=section==0?0:timing.caveResume,end=section==0?timing.eventEntry:timing.duration;
                mover.transform.position=section==0?origin:placer.caveStartPoint.position;
                tempo.enabled=false;
                var baseline=RhythmPath.Build(mover,mover.GetComponent<CapsuleCollider2D>(),mover.groundLayer,start,end-start);
                tempo.enabled=true;
                var paced=RhythmPath.Build(mover,mover.GetComponent<CapsuleCollider2D>(),mover.groundLayer,start,end-start);
                check(Vector2.Distance(baseline.Evaluate(start),paced.Evaluate(start))<.0001f && Vector2.Distance(baseline.Evaluate(end),paced.Evaluate(end))<.0001f,"Section "+section+" start/end coordinates and times preserved");
                bool sameJumps=baseline.Jumps.Count==paced.Jumps.Count;
                for(int j=0;j<baseline.Jumps.Count;j++)
                {
                    var a=baseline.Jumps[j];var b=paced.Jumps[j];
                    sameJumps &= a.time==b.time && a.endTime==b.endTime;
                    for(int i=0;i<=50;i++){double t=a.time+(a.endTime-a.time)*i/50;sameJumps &= Vector2.Distance(baseline.Evaluate(t),paced.Evaluate(t))<.0001f;}
                }
                check(sameJumps,"Section "+section+" every jump's timing and airborne trajectory unchanged");
                var notes=LitJson.JsonMapper.ToObject(tempo.chartFile.text);bool holds=true;
                for(int i=0;i<notes.Count;i++)
                {
                    float t=Convert.ToSingle(notes[i]["createTime"].ToString(),System.Globalization.CultureInfo.InvariantCulture);
                    float duration=Convert.ToSingle(notes[i]["holdTime"].ToString(),System.Globalization.CultureInfo.InvariantCulture);
                    if(duration<=0 || t<start || t+duration>end)continue;
                    for(int j=0;j<=50;j++){double at=t+duration*j/50;holds &= Vector2.Distance(baseline.Evaluate(at),paced.Evaluate(at))<.0001f;}
                }
                check(holds,"Section "+section+" held-note path and travel speed unchanged");
                float maxStep=0,deviation=0,minGround=float.PositiveInfinity,maxGround=0;
                for(double t=start+.03;t<end-.03;t+=.01)
                {
                    float current=Vector2.Distance(paced.Evaluate(t),paced.Evaluate(t+.01));
                    maxStep=Mathf.Max(maxStep,current);
                    deviation=Mathf.Max(deviation,Vector2.Distance(baseline.Evaluate(t),paced.Evaluate(t)));
                    if(!paced.Jumps.Any(j=>t>=j.time-.03 && t<=j.endTime+.03))
                    {float speed=Mathf.Abs(paced.Evaluate(t+.01).x-paced.Evaluate(t).x)*100;minGround=Mathf.Min(minGround,speed);maxGround=Mathf.Max(maxGround,speed);}
                }
                // Compare source-time rates, not world displacement at different
                // positions: the same route contains slopes and terrain steps.
                check(tempo.MinTimeRate>=.719f && tempo.MaxTimeRate<=1.281f,"Section "+section+" monotonic, bounded song-time mapping="+tempo.MinTimeRate.ToString("F3")+".."+tempo.MaxTimeRate.ToString("F3"));
                check(maxStep<.65f,"Section "+section+" no route discontinuity; largest 10ms displacement="+maxStep.ToString("F3"));
                check(deviation>.05f,"Section "+section+" pacing changes traversal; maximum position difference="+deviation.ToString("F3")+"; ground speed="+minGround.ToString("F2")+".."+maxGround.ToString("F2"));
            }
            var controller=(AnimatorController)tempo.animator.runtimeAnimatorController;
            var states=controller.layers.SelectMany(l=>l.stateMachine.states).Select(s=>s.state).ToArray();
            check(AssetDatabase.GetAssetPath(controller)=="Assets/Stage1/Animation/Stage1Tempo.controller" && states.Single(s=>s.name=="run").speedParameter==Stage1TempoMotion.RunParameter,"Stage1 uses an isolated controller with a run-only tempo multiplier");
            check(states.Where(s=>s.name!="run").All(s=>s.speedParameter!=Stage1TempoMotion.RunParameter),"Hit, interaction and flight animations are not tempo-scaled");
            bool valid=true;
            for(float t=0;t<timing.duration;t+=.1f){var f=tempo.Sample(t);valid &= f.bpm>120 && f.bpm<160 && f.energy>=0 && f.energy<=1;}
            check(valid,"Tempo profile finite and within safe musical range for the full song");
        }
        finally
        {
            tempo.enabled=enabled;mover.transform.position=origin;Physics2D.SyncTransforms();
            Directory.CreateDirectory("Reports");File.WriteAllLines("Reports/Stage1-tempo-validation.txt",report);
        }
        if(report.Any(s=>s.StartsWith("FAIL")))throw new InvalidOperationException("See Reports/Stage1-tempo-validation.txt");
    }
}
