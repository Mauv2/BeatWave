using System.Collections.Generic;
using UnityEngine;

/// <summary>Stable gameplay framing; jump groups share one zoom and event shots stay fixed.</summary>
[DefaultExecutionOrder(500)]
public class Stage1CameraDirector : MonoBehaviour
{
    public enum Shot { Running, Switchback, Jumps, FixedEvent, Fall, Outro }
    public PlayerMove player;
    public float lookAhead=4.2f, heightOffset=2.9f, normalSize=6.5f, jumpSize=7.2f;
    public Transform[] eventPoints;
    [Range(0,1)] public float presentationStrength=1f;
    public Shot CurrentShot { get; private set; }
    public int CurrentPhrase => (int)CurrentShot;

    const float Anticipation=1.2f, Release=.8f, Margin=1.15f;
    class JumpGroup
    {
        public int first, last;
        public double start, end;
        public float size;
        public bool reverses;
    }
    readonly List<JumpGroup> groups=new List<JumpGroup>();
    Camera view;
    RhythmClock clock;
    RhythmPath path;
    FallTransitionEffect fall;
    float depth, cachedAspect;
    Vector3 velocity, previousPlayer, eventCenter;
    float eventSize;
    bool initialized, eventFramed, caveArrived;

    void Awake()
    {
        view=GetComponent<Camera>();depth=transform.position.z;
        transform.SetParent(null,true);clock=RhythmClock.GetOrCreate();
        fall=FindObjectOfType<FallTransitionEffect>();
    }

    void LateUpdate()
    {
        if(player==null || view==null || clock.MenuPaused || (clock.FlowPaused && clock.TravelEnabled))return;
        float aspect=Mathf.Max(.5f,view.aspect);
        if(path!=player.Path || Mathf.Abs(aspect-cachedAspect)>.01f) BindPath(aspect);
        double time=clock.SongTime;
        Vector3 position=player.transform.position;
        float ahead=lookAhead*aspect/(16f/9f);
        Vector3 target=position+new Vector3(ahead,heightOffset,0);
        float size=normalSize;
        Bounds safe=PlayerBounds(position);
        CurrentShot=Shot.Running;

        if(clock.FlowPaused && !clock.TravelEnabled)
        {
            CurrentShot=Shot.Fall;
            // Settle on the cave teleport behind black, before the reveal.
            if(initialized && Vector3.Distance(previousPlayer,position)>20f
                && fall!=null && fall.blackFade!=null && fall.blackFade.color.a>.99f)
            {
                caveArrived=true;velocity=Vector3.zero;
                transform.position=new Vector3(target.x,target.y,depth);
                view.orthographicSize=normalSize;
            }
            if(!caveArrived) {target.y=position.y+.8f;size=Mathf.Max(normalSize,jumpSize);}
        }
        else if(clock.Source!=null && !clock.TravelEnabled)
        {
            CurrentShot=Shot.FixedEvent;
            if(!eventFramed)
            {
                if(eventPoints!=null)
                    foreach(var point in eventPoints)
                        if(point!=null) safe.Encapsulate(new Bounds(point.position,new Vector3(2,2,0)));
                eventCenter=safe.center;
                eventSize=FitSize(safe,Mathf.Max(normalSize,7.5f),aspect);
                eventFramed=true;
            }
            target=eventCenter;size=eventSize;
        }
        else if(path!=null)
        {
            eventFramed=false;caveArrived=false;
            // Keep the player near the left third. Reversals use route bounds,
            // rather than flipping the look-ahead bias when the character turns.
            Vector3 next=path.Evaluate(time+1.05);
            IncludePlayer(ref safe,next);
            target.y=Mathf.Lerp(target.y,next.y+heightOffset,.25f);
            foreach(var group in groups)
            {
                if(time<group.start-Anticipation || time>group.end+Release)continue;
                CurrentShot=group.reverses?Shot.Switchback:Shot.Jumps;
                float weight=Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(time-group.start+Anticipation)/Anticipation))
                    *Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(group.end+Release-time)/Release));
                weight*=presentationStrength;
                size=Mathf.Lerp(normalSize,group.size,weight);
                int index=group.first;
                while(index<group.last && time>path.Jumps[index].endTime)index++;
                var cue=path.Jumps[index];
                Bounds frame=JumpBounds(cue.time,cue.endTime);
                // Anticipate platform height instead of following each jump apex.
                Vector3 center=frame.center;
                if(index<group.last)
                {
                    var following=path.Jumps[index+1];
                    float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((float)(time-following.time+Anticipation)/Anticipation));
                    center=Vector3.Lerp(center,JumpBounds(following.time,following.endTime).center,blend);
                }
                target.y=Mathf.Lerp(target.y,center.y,weight);
                if(group.reverses)target.x=Mathf.Lerp(target.x,center.x,weight*.65f);
                if(time<=cue.endTime)
                    safe.Encapsulate(JumpBounds(System.Math.Max(time,cue.time),cue.endTime));
                if(index<group.last && path.Jumps[index+1].time-time<Anticipation)
                    IncludePlayer(ref safe,path.Evaluate(path.Jumps[index+1].endTime));
                break;
            }
            double lastLanding=path.Jumps.Count>0?path.Jumps[path.Jumps.Count-1].endTime:0;
            if(clock.Source!=null && clock.Source.clip!=null && time>System.Math.Max(lastLanding,clock.Source.clip.length-1.2))
            {
                CurrentShot=Shot.Outro;
                size=Mathf.Max(size,Mathf.Lerp(normalSize,8.3f,
                    Mathf.SmoothStep(0,1,(float)(time-lastLanding)/1.1f)*presentationStrength));
            }
        }
        size=FitSize(safe,size,aspect);
        target.x=Mathf.Clamp(target.x,safe.max.x-size*aspect+Margin,safe.min.x+size*aspect-Margin);
        target.y=Mathf.Clamp(target.y,safe.max.y-size+Margin,safe.min.y+size-Margin);
        target.z=depth;
        if(!initialized) {transform.position=target;view.orthographicSize=size;initialized=true;}
        else
        {
            transform.position=Vector3.SmoothDamp(transform.position,target,ref velocity,.3f);
            view.orthographicSize=Mathf.Lerp(view.orthographicSize,size,1-Mathf.Exp(-Time.unscaledDeltaTime*4));
        }
        previousPlayer=position;
    }

    void BindPath(float aspect)
    {
        path=player.Path;cachedAspect=aspect;groups.Clear();
        if(path==null)return;
        for(int i=0;i<path.Jumps.Count;i++)
        {
            var cue=path.Jumps[i];
            JumpGroup group;
            if(groups.Count==0 || cue.time-groups[groups.Count-1].end>1.5)
            {
                group=new JumpGroup {first=i,start=cue.time,size=jumpSize+.6f};groups.Add(group);
            }
            else group=groups[groups.Count-1];
            group.last=i;group.end=cue.endTime;
            group.reverses |= path.Evaluate(cue.endTime).x<path.Evaluate(cue.time).x;
            group.size=Mathf.Max(group.size,FitSize(JumpBounds(cue.time,cue.endTime),jumpSize+.6f,aspect));
        }
        foreach(var group in groups)
        {
            if(group.reverses)group.size=Mathf.Max(group.size,9.2f);
            // Size the entire group for overlapping landing previews once, so
            // revealing a following platform doesn't pump the zoom mid-flight.
            for(double time=group.start-Anticipation;time<=group.end;time+=.08)
            {
                var visible=PlayerBounds(path.Evaluate(time));
                IncludePlayer(ref visible,path.Evaluate(time+1.05));
                int index=group.first;
                while(index<group.last && time>path.Jumps[index].endTime)index++;
                var cue=path.Jumps[index];
                if(time<=cue.endTime)visible.Encapsulate(JumpBounds(System.Math.Max(time,cue.time),cue.endTime));
                if(index<group.last && path.Jumps[index+1].time-time<Anticipation)
                    IncludePlayer(ref visible,path.Evaluate(path.Jumps[index+1].endTime));
                group.size=Mathf.Max(group.size,FitSize(visible,group.size,aspect));
            }
        }
    }
    static Bounds PlayerBounds(Vector3 position) => new Bounds(position+Vector3.up*.7f,new Vector3(3.6f,4.4f,0));
    static void IncludePlayer(ref Bounds bounds,Vector3 position) => bounds.Encapsulate(PlayerBounds(position));
    Bounds JumpBounds(double start,double end)
    {
        var bounds=PlayerBounds(path.Evaluate(start));
        for(int i=1;i<=12;i++)IncludePlayer(ref bounds,path.Evaluate(start+(end-start)*i/12));
        return bounds;
    }
    static float FitSize(Bounds bounds,float size,float aspect) => Mathf.Max(size,bounds.extents.y+Margin,(bounds.extents.x+Margin)/aspect);
}
