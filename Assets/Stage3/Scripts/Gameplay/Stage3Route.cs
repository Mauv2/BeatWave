using System;
using System.Collections.Generic;
using UnityEngine;
namespace BeatWave.Stage3
{
    public enum Stage3LegKind { Ground, Jump, Gate, Collapse, Transfer, Finale, Lift }
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
        public const float Bpm=152f, Offset=.014f;
        public static float Beat(int n)=>Offset+n*60f/Bpm;
        public static float Duration { get; private set; }=168.996712f;
        public static float FractureStart => Beat(104);
        public static float FractureRelease => Beat(112);
        public static float LiftStart => Beat(296);
        public static float LiftRelease => Beat(304);
        public static float FinaleStart => Beat(368);
        public static float FinaleRelease => Beat(376);
        public static Stage3Leg[] Legs { get; private set; }=BuildLegs();
        public static void ConfigureDuration(float duration)
        {
            if(duration<=Beat(416))throw new ArgumentOutOfRangeException(nameof(duration),"Stage3 requires the full original Last Wish recording.");
            Duration=duration;Legs=BuildLegs();
        }
        static Stage3Leg[] BuildLegs()
        {
            var legs=new List<Stage3Leg>();
            Vector2 p=Vector2.zero;float time=0;
            void Add(int beat,float dx,float y,Stage3LegKind kind=Stage3LegKind.Ground,float arc=0)
            {
                var q=new Vector2(p.x+dx,y);float end=Beat(beat);
                legs.Add(new Stage3Leg(time,end,p,q,arc,kind));p=q;time=end;
            }
            // Quiet colonnade: room to read alternating notes before the arrangement builds.
            Add(16,32,0);Add(32,32,3);Add(48,32,3);Add(60,20,6);
            Add(64,10,9,Stage3LegKind.Jump,2);Add(80,26,9);Add(96,22,14);Add(104,14,16);
            Add(112,0,16,Stage3LegKind.Gate);Add(120,14,-6,Stage3LegKind.Collapse);
            // Submerged archive: rising accompaniment and a wider chorus passage.
            Add(136,32,-8);Add(152,32,-8);Add(168,8,4,Stage3LegKind.Lift);
            Add(172,10,6,Stage3LegKind.Jump,2.5f);Add(184,24,6);Add(200,-30,13);
            Add(216,-26,20);Add(232,32,25);Add(236,10,28,Stage3LegKind.Jump,2);
            Add(248,24,28);Add(264,32,24);Add(280,32,24);Add(296,32,29);
            Add(304,0,29,Stage3LegKind.Gate);Add(312,22,43,Stage3LegKind.Transfer,5);
            // Full arrangement: exposed terraces, then release into the quiet coda.
            Add(328,32,43);Add(340,24,47);Add(344,10,50,Stage3LegKind.Jump,2.5f);
            Add(360,32,53);Add(368,16,53);Add(376,0,53,Stage3LegKind.Gate);
            Add(380,10,55,Stage3LegKind.Finale,1.5f);
            Add(396,24,55);Add(412,18,55);
            legs.Add(new Stage3Leg(time,Duration,p,p+Vector2.right*6));
            return legs.ToArray();
        }
        public static Stage3Leg Leg(float time){foreach(var l in Legs)if(time<l.end)return l;return Legs[Legs.Length-1];}
        public static Vector2 Position(float time)=>Leg(time).At(time);
        public static float Facing(float time)=>Leg(time).to.x>=Leg(time).from.x?1:-1;
        public static Vector3 Marker(float time,int lane)=>(Vector3)Position(time)+new Vector3(Facing(time)*1.15f,lane==0?.55f:2.05f,0);
        public static int Section(float time)=>time<FractureStart?0:time<Beat(120)?1:time<LiftRelease?2:3;
        public static readonly int[] ActBeats={0,48,112,168,232,304,380};
        public static int Act(float time)
        {
            for(int i=ActBeats.Length-1;i>0;i--)if(time>=Beat(ActBeats[i]))return i;
            return 0;
        }
    }
}
