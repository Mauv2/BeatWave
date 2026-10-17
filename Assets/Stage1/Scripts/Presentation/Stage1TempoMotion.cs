using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Stage1-only pacing. Retimes safe ground spans on the original song clock.</summary>
[DefaultExecutionOrder(-50)]
public sealed class Stage1TempoMotion : MonoBehaviour
{
    public const string RunParameter="Stage1RunTempo";
    public TextAsset tempoFile, chartFile;
    public Animator animator;
    public float runCycleSeconds=.5f;
    [Range(0,1)] public float pacingStrength=1f;
    [Serializable] public struct Frame { public float time,bpm,energy,beat; }
    [Serializable] class Profile { public float referenceBpm=138.5f; public Frame[] frames; }
    [Serializable] class Chart { public Hold[] notes; }
    [Serializable] struct Hold { public float createTime,holdTime; public int noteKind; }
    Profile profile;
    Hold[] chart;
    TextAsset loadedTempo,loadedChart;
    RhythmClock clock;
    bool hasRunParameter;
    public float CurrentBpm {get;private set;}
    public float MinTimeRate {get;private set;}=1;
    public float MaxTimeRate {get;private set;}=1;

    void Awake()
    {
        clock=RhythmClock.GetOrCreate();
        if(animator==null)animator=GetComponentInChildren<Animator>();
        if(animator!=null)foreach(var p in animator.parameters)
            if(p.name==RunParameter && p.type==AnimatorControllerParameterType.Float)hasRunParameter=true;
    }
    void Update()
    {
        if(clock==null || clock.IsPaused || !clock.HasStarted)return;
        var frame=Sample((float)clock.SongTime);
        CurrentBpm=frame.bpm;
        // Only the run state's multiplier changes. Interact, flight and music stay intact.
        // Keep the multiplier ready during events so resumed travel starts at the right tempo.
        if(hasRunParameter)animator.SetFloat(RunParameter,runCycleSeconds*frame.bpm/60f);
    }
    void OnDisable(){if(hasRunParameter && animator!=null)animator.SetFloat(RunParameter,1f);}
    void Load()
    {
        if(profile==null || loadedTempo!=tempoFile)
        { loadedTempo=tempoFile;profile=tempoFile!=null?JsonUtility.FromJson<Profile>(tempoFile.text):new Profile(); }
        if(chart==null || loadedChart!=chartFile)
        { loadedChart=chartFile;chart=chartFile!=null?JsonUtility.FromJson<Chart>("{\"notes\":"+chartFile.text+"}").notes:new Hold[0]; }
    }
    public Frame Sample(float time)
    {
        Load();var frames=profile.frames;
        if(frames==null || frames.Length==0)return new Frame{bpm=138.5f,energy=.5f,beat=time*138.5f/60};
        int lo=0,hi=frames.Length-1;
        while(lo<hi){int mid=(lo+hi)/2;if(frames[mid].time<time)lo=mid+1;else hi=mid;}
        var a=frames[Mathf.Max(0,lo-1)];var b=frames[lo];
        float u=Mathf.InverseLerp(a.time,b.time,time);
        return new Frame{time=time,bpm=Mathf.Lerp(a.bpm,b.bpm,u),energy=Mathf.Lerp(a.energy,b.energy,u),beat=Mathf.Lerp(a.beat,b.beat,u)};
    }
    float Pace(float time)
    {
        var f=Sample(time);
        return f.bpm/Mathf.Max(1,profile.referenceBpm)*(.82f+.36f*f.energy)*(1+.1f*Mathf.Cos(f.beat*Mathf.PI*2));
    }
    public void Retime(List<Vector2> positions,IReadOnlyList<RhythmPath.JumpCue> jumps,double start,float step)
    {
        MinTimeRate=MaxTimeRate=1;
        Load();if(profile.frames==null || profile.frames.Length<2 || pacingStrength<=0)return;
        var protectedSamples=new bool[positions.Count];
        Action<double,double> protect=(a,b)=>
        {
            int first=Mathf.Max(0,Mathf.FloorToInt((float)(a-start)/step)-2);
            int last=Mathf.Min(positions.Count-1,Mathf.CeilToInt((float)(b-start)/step)+2);
            for(int i=first;i<=last;i++)protectedSamples[i]=true;
        };
        foreach(var cue in jumps)protect(cue.time,cue.endTime);
        foreach(var hold in chart)if(hold.noteKind==1 && hold.holdTime>0)protect(hold.createTime,hold.createTime+hold.holdTime);
        // Pin both section boundaries, preserving event entry, cave resume and song end.
        protectedSamples[0]=protectedSamples[positions.Count-1]=true;
        var original=positions.ToArray();
        for(int first=0;first<positions.Count-1;)
        {
            if(protectedSamples[first+1]){first++;continue;}
            int last=first+1;
            while(last<positions.Count-1 && !protectedSamples[last])last++;
            int length=last-first;
            if(length*step>=.5f)
            {
                var integral=new float[length+1];
                for(int i=1;i<=length;i++)integral[i]=integral[i-1]+Pace((float)start+(first+i-.5f)*step);
                var mapped=new float[length+1];
                float strength=pacingStrength;
                // Bound acceleration in song-time mapping, including phrase boundaries.
                for(int attempt=0;attempt<12;attempt++)
                {
                    bool safe=true;
                    for(int i=0;i<=length;i++)
                    {
                        float window=Mathf.Sin(Mathf.PI*i/length);window*=window;
                        mapped[i]=i+strength*window*(integral[i]/integral[length]*length-i);
                        if(i>0){float rate=mapped[i]-mapped[i-1];safe &= rate>=.72f && rate<=1.28f;}
                    }
                    if(safe)break;
                    strength*=.5f;
                }
                for(int i=1;i<length;i++)
                {
                    float source=first+mapped[i];int a=Mathf.FloorToInt(source);
                    positions[first+i]=Vector2.Lerp(original[a],original[Mathf.Min(a+1,last)],source-a);
                }
                for(int i=1;i<=length;i++)
                { MinTimeRate=Mathf.Min(MinTimeRate,mapped[i]-mapped[i-1]);MaxTimeRate=Mathf.Max(MaxTimeRate,mapped[i]-mapped[i-1]); }
            }
            first=last;
        }
    }
}
