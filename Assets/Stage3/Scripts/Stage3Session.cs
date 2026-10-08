using System;
using UnityEngine;
namespace BeatWave.Stage3
{
    [Serializable] public class Stage3Note { public float time; public int lane; public float duration; }
    [Serializable] public class Stage3Chart { public string title; public float bpm, offset, duration; public Stage3Note[] notes; }
    public enum NoteState { Pending, Holding, Hit, Miss }
    // Deterministic judgement independent of rendering, frame rate and colliders.
    public sealed class Stage3Session
    {
        public const float PerfectWindow=.065f, GoodWindow=.13f, HitWindow=.17f;
        public readonly Stage3Chart Chart;
        public readonly NoteState[] States;
        readonly float[] quality;
        public int Combo {get;private set;} public int MaxCombo {get;private set;}
        public int Perfect {get;private set;} public int Good {get;private set;} public int Miss {get;private set;}
        public int Resolved => Perfect+Good+Miss;
        public float Health {get;private set;}=100;
        public float Accuracy => Resolved==0?100:(Perfect+Good*.65f)*100/Resolved;
        public int Score => Mathf.RoundToInt((Perfect+Good*.65f)*1000000/Mathf.Max(1,States.Length));
        public bool Failed => Health<=0;
        public event Action<int,string> Judged;
        public Stage3Session(Stage3Chart chart) { Chart=chart;States=new NoteState[chart.notes.Length];quality=new float[States.Length]; }
        public void Press(int lane,float time)
        {
            if(Failed)return;
            int nearest=-1;float distance=HitWindow;
            for(int i=0;i<States.Length;i++)
            {
                var n=Chart.notes[i]; if(n.time>time+HitWindow)break;
                if(States[i]!=NoteState.Pending||n.lane!=lane)continue;
                float d=Mathf.Abs(n.time-time);if(d<=distance){nearest=i;distance=d;}
            }
            if(nearest<0)return;
            quality[nearest]=distance<=PerfectWindow?1:.65f;
            if(Chart.notes[nearest].duration>0) { States[nearest]=NoteState.Holding;Judged?.Invoke(nearest,"HOLD"); }
            else Resolve(nearest,true);
        }
        public void Tick(float time,bool lowHeld,bool highHeld)
        {
            if(Failed)return;
            for(int i=0;i<States.Length;i++)
            {
                var n=Chart.notes[i];if(n.time>time+HitWindow)break;
                if(States[i]==NoteState.Pending && time>n.time+HitWindow)Resolve(i,false);
                else if(States[i]==NoteState.Holding)
                {
                    bool held=n.lane==0?lowHeld:highHeld;
                    if(!held && time<n.time+n.duration-.075f)Resolve(i,false);
                    else if(time>=n.time+n.duration)Resolve(i,true);
                }
                if(Failed)break;
            }
        }
        void Resolve(int i,bool success)
        {
            if(States[i]==NoteState.Hit||States[i]==NoteState.Miss)return;
            States[i]=success?NoteState.Hit:NoteState.Miss;
            string label;
            if(success){Combo++;MaxCombo=Mathf.Max(MaxCombo,Combo);Health=Mathf.Min(100,Health+1.1f);if(quality[i]>=1){Perfect++;label="PERFECT";}else{Good++;label="GOOD";}}
            else{Combo=0;Health=Mathf.Max(0,Health-6);Miss++;label="MISS";}
            Judged?.Invoke(i,label);
        }
    }
    public enum Stage3LegKind { Ground, Jump, Gate, Collapse, Transfer, Finale }
    [Serializable] public struct Stage3Leg
    {
        public float start,end,arc; public Vector2 from,to; public Stage3LegKind kind;
        public bool Airborne => kind==Stage3LegKind.Jump || kind==Stage3LegKind.Collapse || kind==Stage3LegKind.Transfer || kind==Stage3LegKind.Finale;
        public Stage3Leg(float a,float b,Vector2 p,Vector2 q,float height=0,Stage3LegKind type=Stage3LegKind.Ground)
        { start=a;end=b;from=p;to=q;arc=height;kind=type; }
        public Vector2 At(float t)
        {
            float u=Mathf.InverseLerp(start,end,t);
            if(kind==Stage3LegKind.Collapse) return new Vector2(Mathf.Lerp(from.x,to.x,u),Mathf.Lerp(from.y,to.y,u*u));
            return Vector2.Lerp(from,to,u)+Vector2.up*(4*arc*u*(1-u));
        }
    }
    public static class Stage3Route
    {
        public static float Beat(int n)=>.055f+n*60f/152f;
        public static float Duration { get; private set; }=53.16f;
        public static float FinaleStart => Duration-2.35f;
        public static float FinaleRelease => Duration-.55f;
        public static Stage3Leg[] Legs { get; private set; }=BuildLegs();
        public static void ConfigureDuration(float duration)
        {
            // The choreography is authored for Last Wish; the tail follows the
            // imported clip length, including its last audible decay.
            if(duration<=Beat(128)+2.35f)throw new ArgumentOutOfRangeException(nameof(duration),"Last Wish clip is shorter than its authored chart.");
            Duration=duration;Legs=BuildLegs();
        }
        static Stage3Leg[] BuildLegs()=>new[]{
            new Stage3Leg(0,Beat(14),new Vector2(0,0),new Vector2(25,0)),
            new Stage3Leg(Beat(14),Beat(22),new Vector2(25,0),new Vector2(40,4)),
            new Stage3Leg(Beat(22),Beat(26),new Vector2(40,4),new Vector2(49,4)),
            new Stage3Leg(Beat(26),Beat(32),new Vector2(49,4),new Vector2(61,2)),
            new Stage3Leg(Beat(32),Beat(36),new Vector2(61,2),new Vector2(71,5),2,Stage3LegKind.Jump),
            new Stage3Leg(Beat(36),Beat(44),new Vector2(71,5),new Vector2(86,8)),
            new Stage3Leg(Beat(44),Beat(50),new Vector2(86,8),new Vector2(86,8),0,Stage3LegKind.Gate),
            new Stage3Leg(Beat(50),Beat(56),new Vector2(86,8),new Vector2(94,-5),0,Stage3LegKind.Collapse),
            new Stage3Leg(Beat(56),Beat(62),new Vector2(94,-5),new Vector2(108,-8)),
            new Stage3Leg(Beat(62),Beat(74),new Vector2(108,-8),new Vector2(134,-8)),
            new Stage3Leg(Beat(74),Beat(82),new Vector2(134,-8),new Vector2(148,-3)),
            new Stage3Leg(Beat(82),Beat(88),new Vector2(148,-3),new Vector2(159,-3)),
            new Stage3Leg(Beat(88),Beat(94),new Vector2(159,-3),new Vector2(159,-3),0,Stage3LegKind.Gate),
            new Stage3Leg(Beat(94),Beat(100),new Vector2(159,-3),new Vector2(183,8),5,Stage3LegKind.Transfer),
            new Stage3Leg(Beat(100),Beat(106),new Vector2(183,8),new Vector2(195,8)),
            new Stage3Leg(Beat(106),Beat(112),new Vector2(195,8),new Vector2(209,3)),
            new Stage3Leg(Beat(112),Beat(118),new Vector2(209,3),new Vector2(222,6)),
            new Stage3Leg(Beat(118),FinaleStart,new Vector2(222,6),new Vector2(244,6)),
            new Stage3Leg(FinaleStart,FinaleRelease,new Vector2(244,6),new Vector2(244,6),0,Stage3LegKind.Gate),
            new Stage3Leg(FinaleRelease,Duration,new Vector2(244,6),new Vector2(248,8),.6f,Stage3LegKind.Finale)};
        public static Stage3Leg Leg(float time){foreach(var l in Legs)if(time<l.end)return l;return Legs[Legs.Length-1];}
        public static Vector2 Position(float time)=>Leg(time).At(time);
        public static float Facing(float time)=>Leg(time).to.x>=Leg(time).from.x?1:-1;
        public static Vector3 Marker(float time,int lane)=>(Vector3)Position(time)+new Vector3(Facing(time)*1.15f,lane==0?.55f:2.05f,0);
        public static int Section(float time)=>time<Beat(44)?0:time<Beat(56)?1:time<Beat(94)?2:3;
    }
}
