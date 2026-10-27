using System;
using UnityEngine;

// Reversible song-time choreography: no independent physics clock or new input rules.
public sealed class Stage2StageEvents : MonoBehaviour
{
    public Stage2MusicRoute route;
    public float gateTime;
    public float gateReleaseTime;
    float GateEnd => gateReleaseTime>0?gateReleaseTime:gateTime;
    public LineRenderer seal, landingRing;
    public MeshRenderer gateLight;
    public SpriteRenderer guardian;
    public LineRenderer returnSeal;
    public Collapse[] collapses;
    [Serializable] public class Collapse
    {
        public float start, end;
        public bool reconstruct;
        public Vector3 landing;
        public Piece[] pieces;
        public LineRenderer[] cracks;
        public MeshRenderer dust;
        public Vector3 dustAnchor;
    }
    [Serializable] public struct Piece
    {
        public Transform target;
        public Vector3 anchor;
        public Quaternion rotation;
        public Collider2D collider;
    }
    MaterialPropertyBlock block;
    public Vector3 CameraTarget(float time,Vector3 normal)
    {
        float enter=Mathf.SmoothStep(0,1,(time-(gateTime-1.3f))/1.1f);
        float leave=1-Mathf.SmoothStep(0,1,(time-(GateEnd+.15f))/1.1f);
        var f=route.Sample(gateTime);
        normal=Vector3.Lerp(normal,new Vector3(f.x+3,f.y+3,-10),enter*leave);
        foreach(var e in collapses) if(!e.reconstruct)
        {
            float weight=Mathf.SmoothStep(0,1,(time-e.start+1.5f)/1.2f)*(1-Mathf.SmoothStep(0,1,(time-e.end)/1.3f));
            float top=route.Sample(e.start).y;
            normal=Vector3.Lerp(normal,new Vector3(normal.x,(top+e.landing.y)*.5f+2,-10),weight);
        }
        return normal;
    }
    public float Expansion(float time)
    {
        float value=0;
        foreach(var e in collapses)
            value=Mathf.Max(value,(e.reconstruct?.65f:3.8f)*Mathf.SmoothStep(0,1,(time-e.start+1.2f)/1.2f)*(1-Mathf.SmoothStep(0,1,(time-e.end)/1.2f)));
        return value;
    }
    public float BackgroundStrength(float time)
    {
        float darkness=Mathf.SmoothStep(0,1,(time-collapses[0].start+.3f)/.8f);
        float dawn=Mathf.SmoothStep(0,1,(time-collapses[1].start+2)/3);
        return 1-.32f*darkness*(1-dawn);
    }
    public void Present(float time)
    {
        block=block??new MaterialPropertyBlock();
        float anticipation=Mathf.SmoothStep(0,1,(time-gateTime+2)/2);
        float release=Mathf.SmoothStep(0,1,(time-GateEnd-.06f)/1.1f);
        seal.enabled=time>gateTime-3&&release<1;
        seal.transform.localScale=Vector3.one*(1+release*1.8f);
        seal.transform.localRotation=Quaternion.Euler(0,0,-time*12);
        Color c=new Color(.55f,.86f,1,(.15f+.65f*anticipation)*(1-release));
        seal.startColor=seal.endColor=c;
        Tint(gateLight,new Color(.45f,.75f,1,(.08f+.35f*anticipation)*(1-release)));
        guardian.color=new Color(.55f,.78f,1,anticipation*(1-release)*.85f);
        guardian.enabled=time>gateTime-3&&release<1;
        var finale=collapses[collapses.Length-1];
        float recall=Mathf.SmoothStep(0,1,(time-finale.start+2.5f)/1.5f)*(1-Mathf.SmoothStep(0,1,(time-finale.end-1)/2));
        returnSeal.enabled=recall>0;
        returnSeal.startColor=returnSeal.endColor=new Color(.95f,.8f,.45f,recall*.8f);
        returnSeal.transform.localRotation=Quaternion.Euler(0,0,-time*12);
        landingRing.enabled=false;
        foreach(var e in collapses)
        {
            float age=time-e.start;
            float warning=Mathf.Clamp01((age+1.2f)/1.2f);
            foreach(var crack in e.cracks)
            {
                crack.enabled=!e.reconstruct&&age<.15f&&age> -1.2f;
                var color=new Color(.65f,.85f,1,.25f+warning*.65f);
                crack.startColor=crack.endColor=color;
            }
            for(int i=0;i<e.pieces.Length;i++)
            {
                var p=e.pieces[i];float t=Mathf.Max(0,age-i*.035f);
                if(e.reconstruct)
                {
                    float rise=Mathf.SmoothStep(0,1,(age+2.4f-i*.18f)/1.5f);
                    p.target.gameObject.SetActive(true);p.collider.enabled=rise>=.999f;
                    p.target.position=p.anchor+new Vector3((i-1.5f)*1.3f*(1-rise),-7*(1-rise),0);
                    p.target.rotation=p.rotation*Quaternion.Euler(0,0,(i-1.5f)*20*(1-rise));
                    continue;
                }
                p.collider.enabled=age<0;
                p.target.gameObject.SetActive(age<3);
                float side=i<e.pieces.Length/2?-1:1;
                float shake=age<0?Mathf.Sin(age*65+i)*warning*.025f:0;
                p.target.position=p.anchor+new Vector3(side*t*.85f+shake,-4*t*t,0);
                p.target.rotation=p.rotation*Quaternion.Euler(0,0,side*t*24);
            }
            e.dust.enabled=!e.reconstruct&&age>=0&&age<1.5f;
            if(e.dust.enabled)
            {
                e.dust.transform.position=e.dustAnchor+Vector3.down*age;
                e.dust.transform.localScale=new Vector3(3+age*7,1+age*3,1);
                Tint(e.dust,new Color(.65f,.74f,.85f,.35f*(1-age/1.5f)));
            }
            float landed=time-e.end;
            if(landed>=0&&landed<.75f)
            {
                landingRing.enabled=true;landingRing.transform.position=e.landing+Vector3.up*.04f;
                landingRing.transform.localScale=new Vector3(1+landed*5,.22f+landed*.35f,1);
                var color=new Color(.75f,.88f,1,.8f*(1-landed/.75f));
                landingRing.startColor=landingRing.endColor=color;
            }
        }
    }
    void Tint(MeshRenderer r,Color c){block.Clear();block.SetColor("_Tint",c);r.SetPropertyBlock(block);}
}
