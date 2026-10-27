using UnityEngine;
using TMPro;

// Musical gate: the song keeps playing. The route waits for a fixed phrase,
// then transitions on its final beat whether the player succeeds or misses.
public sealed class MashNote : MonoBehaviour
{
    RhythmClock clock;
    JudgeManager judge;
    PlayerAnimController playerAnimation;
    public bool automaticInput = true;
    bool ownsNote;
    Renderer[] ordinaryRenderers;
    public float startTime, releaseTime;
    public int requiredHits=8;
    public string actionLabel="BREAK";
    public GameObject presentation;
    public Note note;
    public TMP_Text caption;
    public Transform rings;
    public int Hits { get; private set; }
    public int HitEffectsSpawned { get; private set; }
    public bool Resolved { get; private set; }
    public bool Success { get; private set; }
    public float LastPressTime { get; private set; }=-100;
    public bool Active => !Resolved && clock!=null && clock.HasStarted && clock.SongTime>=startTime && clock.SongTime<releaseTime;
    public float Charge => Mathf.Clamp01((float)Hits/requiredHits);
    bool fractureFeedback;
    bool IsFracture => fractureFeedback;
    public float PulseAt(float time) => Hits==0 || time<LastPressTime ? 0 : Mathf.Exp(-(time-LastPressTime)*18);
    public void Initialize(RhythmClock source, JudgeManager manager, PlayerAnimController animation, bool fracture = false)
    {
        clock=source;judge=manager;playerAnimation=animation;fractureFeedback=fracture;
        requiredHits=Mathf.Max(1,requiredHits);
        note.isEventNote=true;note.kind=NoteKind.Mash;note.needHitCount=requiredHits;
        note.ConfigureTiming(clock,startTime,null);
    }
    public static MashNote Attach(Note note, RhythmClock clock, JudgeManager judge)
    {
        var mash=note.gameObject.GetComponent<MashNote>();
        if(mash!=null)return mash;
        mash=note.gameObject.AddComponent<MashNote>();
        mash.note=note;mash.startTime=(float)note.judgeTime;
        mash.releaseTime=mash.startTime+Mathf.Max(.1f,note.mashLimitTime);
        mash.requiredHits=note.needHitCount;mash.automaticInput=false;mash.ownsNote=true;
        mash.Initialize(clock,judge,judge.playerAnim);
        var label=new GameObject("Mash input hint");label.transform.SetParent(note.transform,false);
        label.transform.localPosition=new Vector3(0,1.5f);
        mash.caption=label.AddComponent<TextMeshPro>();mash.caption.font=TMP_Settings.defaultFontAsset;
        mash.caption.fontSize=3.2f;mash.caption.alignment=TextAlignmentOptions.Center;
        mash.caption.rectTransform.sizeDelta=new Vector2(7,1);mash.caption.color=new Color(1,.83f,.48f);
        mash.caption.GetComponent<MeshRenderer>().sortingOrder=70;
        var ring=new GameObject("Mash pulse glyph");ring.transform.SetParent(note.transform,false);mash.rings=ring.transform;
        var line=ring.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=4;
        line.SetPositions(new[]{new Vector3(0,1),new Vector3(1,0),new Vector3(0,-1),new Vector3(-1,0)});
        line.widthMultiplier=.035f;line.sharedMaterial=note.GetComponent<SpriteRenderer>().sharedMaterial;
        line.startColor=line.endColor=new Color(.45f,.85f,1,.7f);line.sortingOrder=44;
        mash.ordinaryRenderers=note.GetComponentsInChildren<Renderer>(true);
        mash.SetVisible(false);
        return mash;
    }
    void SetVisible(bool visible)
    {
        if(presentation!=null)presentation.SetActive(visible);
        if(ordinaryRenderers!=null)foreach(var renderer in ordinaryRenderers)renderer.enabled=visible;
    }
    void Update()
    {
        if(clock==null||!clock.HasStarted||clock.IsPaused||Time.timeScale<=0)return;
        if(Active && automaticInput)
        {
            if(Input.GetKeyDown(KeyCode.W))Press();
            if(Input.GetKeyDown(KeyCode.S))Press();
        }
        float time=(float)clock.SongTime;
        if(!Resolved && time>=releaseTime)Resolve();
        bool visible=time>=startTime-2 && time<releaseTime+.65f;
        SetVisible(visible);
        if(!visible){if(ownsNote&&Resolved)Destroy(gameObject);return;}
        // The instruction stays constant; hit totals and completion thresholds
        // are internal judging data, never a counter or filled progress ring.
        caption.text=Resolved ? "" : "MASH  W / S";
        rings.localRotation=Quaternion.Euler(0,0,actionLabel=="LAST WISH"?-time*20:Mathf.Sin(time*6)*3);
        float pulse=IsFracture?PulseAt(time):Mathf.Exp(-Mathf.Max(0,time-LastPressTime)*12);
        note.transform.localScale=Vector3.one*(1.3f+.16f*pulse);
        note.isMashActive=Active;note.currentHitCount=Hits;
        rings.localScale=Vector3.one*(1+.09f*pulse);
    }
    public bool Press()
    {
        if(!Active||Resolved||clock.IsPaused||Time.timeScale<=0)return false;
        Hits++;LastPressTime=(float)clock.SongTime;
        judge.PlayNoteHitSound(NoteKind.Mash);
        if(playerAnimation!=null)playerAnimation.PlayInteract();
        if(judge.hitEffectPrefab!=null)
        {
            var effect=Instantiate(judge.hitEffectPrefab,note.transform.position,Quaternion.identity);
            effect.name="Mash impact";
            bool fracture=IsFracture;
            effect.transform.localScale*=fracture?.34f:.55f;
            foreach(var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main=particles.main;
                main.startColor=Hits%2==0?new Color(.45f,.85f,1):new Color(1,.83f,.48f);
            }
            foreach(var renderer in effect.GetComponentsInChildren<Renderer>(true))renderer.sortingOrder=70;
            Destroy(effect,fracture?.3f:.6f);
            HitEffectsSpawned++;
        }
        return true;
    }
    public void Resolve()
    {
        if(Resolved || clock==null || clock.SongTime<releaseTime)return;
        Resolved=true;Success=Hits>=requiredHits;note.isJudged=true;note.isMashActive=false;
        judge.ReportEventJudge(Success?"Perfect":"Miss",note.transform.position);
    }
}
