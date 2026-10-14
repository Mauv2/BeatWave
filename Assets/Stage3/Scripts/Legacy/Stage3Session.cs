using System;
using UnityEngine;
namespace BeatWave.Stage3
{
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
}
