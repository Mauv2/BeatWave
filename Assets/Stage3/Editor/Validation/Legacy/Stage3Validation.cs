#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace BeatWave.Stage3
{
    public static class Stage3Validation
    {
        public static string Run()
        {
            var lines=new List<string>();
            Action<bool,string> check=(ok,label)=>{lines.Add((ok?"PASS ":"FAIL ")+label);if(!ok)Debug.LogError("Stage3 validation: "+label);};
            Func<Stage3Note[],Stage3Session> session=ns=>new Stage3Session(new Stage3Chart{notes=ns});
            Func<float,int,float,Stage3Note> note=(t,l,d)=>new Stage3Note{time=t,lane=l,duration=d};
            var s=session(new[]{note(1,0,0)});s.Press(1,1);check(s.Resolved==0,"wrong lane cannot hit");s.Press(0,.7f);check(s.Resolved==0,"too early cannot hit");s.Press(0,1);check(s.Perfect==1&&s.Score==1000000,"exact tap scores perfect");s.Press(0,1);check(s.Resolved==1,"tap resolves once");
            s=session(new[]{note(1,0,0)});s.Press(0,1.12f);check(s.Good==1,"late tap inside window scores good");
            s=session(new[]{note(1,0,0)});s.Tick(1.18f,false,false);check(s.Miss==1&&s.Health==94,"expired note reduces health");
            s=session(new[]{note(1,1,1)});s.Press(1,1);s.Tick(1.5f,false,true);check(s.States[0]==NoteState.Holding&&s.Resolved==0,"hold requires full duration");s.Tick(2,false,true);check(s.Perfect==1,"held ribbon completes");
            s=session(new[]{note(1,1,1)});s.Press(1,1);s.Tick(1.4f,false,false);check(s.Miss==1,"early hold release misses");s.Tick(2,false,true);check(s.Resolved==1,"broken hold cannot be recovered");
            s=session(Enumerable.Range(0,25).Select(i=>note(i,0,0)).ToArray());s.Tick(30,false,false);check(s.Failed&&s.Health==0&&s.Resolved==17,"zero health stops further judgement");
            s=session(new[]{note(1,0,0),note(2,0,0),note(3,0,0)});s.Press(0,1);s.Press(0,2);s.Tick(3.3f,false,false);check(s.Combo==0&&s.MaxCombo==2,"miss resets combo but keeps maximum");
            var chart=JsonUtility.FromJson<Stage3Chart>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Stage3/Data/LastWishChart.json").text);
            check(chart.notes.Length==135&&chart.notes.Count(n=>n.duration>0)==8,"chart contains 135 notes and 8 holds");
            check(chart.notes.Zip(chart.notes.Skip(1),(a,b)=>a.time<=b.time).All(v=>v),"chart sorted by time");
            check(chart.notes.All(n=>n.time>=0&&n.time+n.duration<chart.duration&&n.lane>=0&&n.lane<=1),"all notes inside song and valid lanes");
            bool overlaps=false;for(int lane=0;lane<2;lane++){var ns=chart.notes.Where(n=>n.lane==lane).ToArray();for(int i=1;i<ns.Length;i++)if(ns[i].time<ns[i-1].time+ns[i-1].duration+.17f)overlaps=true;}check(!overlaps,"same-lane holds and taps have safe spacing");
            var route=Stage3Route.Legs;check(route.Zip(route.Skip(1),(a,b)=>Mathf.Abs(a.end-b.start)<.0001f&&Vector2.Distance(a.to,b.from)<.001f).All(v=>v),"all route joins are continuous");
            check(route.Count(l=>l.arc>0)==6,"six authored jumps");check(route.Any(l=>l.to.x<l.from.x),"route includes leftward travel");
            foreach(int rate in new[]{30,60,144})
            {
                s=new Stage3Session(chart);
                for(int frame=0;frame<Mathf.CeilToInt((chart.duration+.3f)*rate);frame++)
                {
                    float time=frame/(float)rate;
                    foreach(int i in Enumerable.Range(0,chart.notes.Length))if(s.States[i]==NoteState.Pending&&time>=chart.notes[i].time)s.Press(chart.notes[i].lane,time);
                    s.Tick(time,true,true);
                }
                check(s.Resolved==135&&s.Perfect==135&&s.Score==1000000,"complete chart at "+rate+" FPS without dropped notes");
            }
            var g=UnityEngine.Object.FindObjectOfType<Stage3Director>();check(g!=null&&g.music.clip!=null&&g.chartFile!=null,"scene audio and chart references exist");
            check(UnityEditor.EditorBuildSettings.scenes.Any(x=>x.enabled&&x.path=="Assets/Scenes/Stage3.unity"),"Stage3 registered in build");
            var text=string.Join("\n",lines);Directory.CreateDirectory("Reports");File.WriteAllText("Reports/Stage3-validation.txt",text);return text;
        }
    }
}
#endif
