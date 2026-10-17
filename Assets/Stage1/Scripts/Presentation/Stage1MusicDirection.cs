using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Music-clock-only scenery. It never changes judgement or input.</summary>
[DefaultExecutionOrder(250)]
public class Stage1MusicDirection : MonoBehaviour
{
    public TextAsset timingFile;
    public Material lineMaterial;
    public Transform noteRoot;
    [Serializable] class Timing { public float[] attackTimes; }
    [NonSerialized] float[] attacks;
    [NonSerialized] TextAsset loadedTiming;
    public float[] AttackTimes
    {
        get
        {
            if(attacks==null || loadedTiming!=timingFile)
            {
                loadedTiming=timingFile;
                attacks=timingFile!=null ? JsonUtility.FromJson<Timing>(timingFile.text).attackTimes : new float[0];
            }
            return attacks;
        }
    }
    public int LandingCount { get; private set; }
    public float EffectEnergy { get; private set; }
    public int EffectVariant { get; private set; }
    PlayerMove mover;
    RhythmClock clock;
    RhythmPath boundPath;
    Transform effectsRoot;
    readonly List<Landing> landings=new List<Landing>();
    class Landing
    {
        public double time;
        public Vector3 foot;
        public LineRenderer[] lines;
        public bool counted;
        public int style;
    }
    void Awake()
    {
        mover=GetComponent<PlayerMove>();clock=RhythmClock.GetOrCreate();
        LiftGameplaySprites(transform);
    }
    static void LiftGameplaySprites(Transform root)
    {
        if(root==null)return;
        foreach(var group in root.GetComponentsInChildren<SortingGroup>(true))
            if(group.transform.parent==null || group.transform.parent.GetComponentInParent<SortingGroup>()==null)
                group.sortingOrder=Mathf.Max(30,group.sortingOrder);
        foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
            if(sprite.GetComponentInParent<SortingGroup>()==null && sprite.sortingOrder<30)
                sprite.sortingOrder+=30;
    }
    void Update()
    {
        if(mover.Path==null)return;
        if(boundPath!=mover.Path) Bind(mover.Path);
        // Freeze visual geometry as well as its phase while a menu/transition pauses.
        if(!clock.HasStarted || clock.IsPaused)return;
        double time=clock.SongTime;EffectEnergy=0;
        foreach(var landing in landings)
        {
            float elapsed=(float)(time-landing.time);
            bool visible=elapsed>=-.30f && elapsed<.75f;
            if(elapsed>=0 && !landing.counted){landing.counted=true;LandingCount++;}
            foreach(var line in landing.lines)line.enabled=visible;
            if(!visible)continue;
            EffectVariant=landing.style;
            float release=Mathf.Clamp01(elapsed/.75f);
            float alpha=elapsed<0 ? .13f*(1+elapsed/.30f) : .6f*(1-release)*(1-release);
            EffectEnergy=Mathf.Max(EffectEnergy,alpha);
            var color=landing.style==0?new Color(1,.79f,.4f,alpha):landing.style==1?new Color(.35f,.86f,1,alpha):new Color(.65f,1,.82f,alpha);
            for(int n=0;n<landing.lines.Length;n++)
            {
                var line=landing.lines[n];line.startColor=line.endColor=color;
                for(int i=0;i<line.positionCount;i++)
                {
                    float u=i/(float)(line.positionCount-1);
                    Vector3 p;
                    if(landing.style==1)
                    {
                        // A fan of icy glints instead of another circular pulse.
                        float side=n-1;
                        p=new Vector3(side*(.4f+release*.8f)+side*u*.35f, .08f+Mathf.Sin(release*Mathf.PI)*.5f+u*.45f,0);
                    }
                    else
                    {
                        float angle=u*Mathf.PI*2;
                        float radius=(elapsed<0?.7f: .7f+release*1.8f)+n*.22f;
                        p=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius*(landing.style==2?.3f:.11f)+.05f,0);
                    }
                    line.SetPosition(i,landing.foot+p);
                }
            }
        }
    }
    void Bind(RhythmPath path)
    {
        boundPath=path;
        LiftGameplaySprites(noteRoot);
        if(effectsRoot!=null)Destroy(effectsRoot.gameObject);
        effectsRoot=new GameObject("Stage1 music landing effects").transform;
        landings.Clear();
        if(lineMaterial==null)return;
        foreach(var cue in path.Jumps)
        {
            int style=cue.endTime<49?0:cue.endTime<91?1:2;
            var landing=new Landing {time=cue.endTime,foot=(Vector3)path.Evaluate(cue.endTime)-Vector3.up*path.FeetOffset,style=style,lines=new LineRenderer[style==1?3:2]};
            for(int n=0;n<landing.lines.Length;n++)
            {
                var go=new GameObject("Landing "+cue.endTime.ToString("F2")+" / "+n);go.transform.SetParent(effectsRoot,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=lineMaterial;line.useWorldSpace=true;
                line.positionCount=style==1?2:33;line.startWidth=.035f;line.endWidth=.018f;
                line.numCapVertices=2;line.sortingOrder=24;line.enabled=false;landing.lines[n]=line;
            }
            landings.Add(landing);
        }
    }
    void OnDestroy(){if(effectsRoot!=null)Destroy(effectsRoot.gameObject);}
}
