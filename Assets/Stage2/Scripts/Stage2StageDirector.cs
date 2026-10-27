using System;
using UnityEngine;

// All animation is evaluated from song time: Mash and menu pauses freeze the stage too.
public sealed class Stage2StageDirector : MonoBehaviour
{
    public Stage2MusicRoute route;
    public Stage2StageEvents stageEvents;
    public Sprite[] backgrounds;
    public SpriteRenderer[] backdropTiles;
    public Decoration[] decorations;
    public MeshRenderer[] motes;
    public MeshRenderer[] shafts;
    public MeshRenderer landingGlow;
    public Lantern[] lanterns;
    public Transform gate;
    public Vector3 gateAnchor;
    public float gateTime;
    [Serializable] public struct Lantern { public MeshRenderer renderer; public float worldX, phase; }
    [Serializable] public struct Decoration
    {
        public Transform target;
        public Vector3 anchor;
        public float parallax;
    }
    MaterialPropertyBlock block;
    static readonly int Tint = Shader.PropertyToID("_Tint");
    static readonly Color[] Palette = {
        new Color(.66f,.74f,1f), new Color(.53f,.62f,.85f),
        new Color(.63f,.74f,.96f), new Color(.72f,.75f,1f),
        new Color(.50f,.59f,.78f), new Color(.68f,.71f,.92f),
        new Color(.77f,.71f,.94f), new Color(.80f,.78f,.97f) };

    int SectionAt(float time)
    {
        var sections = route.Data.sections;
        for (int i=sections.Length-1;i>=0;i--) if(time>=sections[i].start) return i;
        return 0;
    }
    public float CameraExpansion(float time)
    {
        int section=SectionAt(time);
        float target=section==3||section==6 ? .25f : section==1 ? -.15f : 0f;
        return target*Mathf.SmoothStep(0,1,(time-route.Data.sections[section].start)/2f)+(stageEvents!=null?stageEvents.Expansion(time):0);
    }
    public void Present(float time)
    {
        if(route==null||route.view==null||backgrounds==null||backgrounds.Length==0) return;
        var camera=route.view;
        var f=route.Sample(time);
        int section=SectionAt(time);
        float transition=section==0?1:Mathf.SmoothStep(0,1,(time-route.Data.sections[section].start)/1.8f);
        int previous=Mathf.Max(0,section-1);
        float pulse=Mathf.Pow(.5f+.5f*Mathf.Cos(f.beat*Mathf.PI*2),7);
        for(int layer=0;layer<2;layer++)
        {
            int index=layer==0?previous:section;
            var sprite=backgrounds[Mathf.Clamp(route.Data.sections[index].background,0,backgrounds.Length-1)];
            float scale=Mathf.Max(camera.orthographicSize*2.12f/sprite.bounds.size.y,
                camera.orthographicSize*2.12f*camera.aspect/sprite.bounds.size.x);
            float width=sprite.bounds.size.x*scale;
            float scroll=f.x*.14f;
            int tile=Mathf.FloorToInt(scroll/width);
            float offset=Mathf.Repeat(scroll,width);
            for(int i=0;i<3;i++)
            {
                var r=backdropTiles[layer*3+i];
                r.sprite=sprite; r.flipX=((tile+i-1)&1)!=0;
                r.transform.localScale=Vector3.one*scale;
                r.transform.position=new Vector3(camera.transform.position.x-offset+(i-1)*width,camera.transform.position.y,12);
                var color=Palette[Mathf.Min(index,Palette.Length-1)]*(1f+pulse*.018f)*(stageEvents!=null?stageEvents.BackgroundStrength(time):1);
                color.a=layer==0?1:transition; r.color=color;
            }
        }
        if(decorations!=null) foreach(var d in decorations)
        {
            if(d.target==null) continue;
            var pos=d.anchor;
            pos.x+=(camera.transform.position.x-d.anchor.x)*d.parallax;
            d.target.position=pos;
        }
        if(stageEvents!=null)stageEvents.Present(time);
        block=block??new MaterialPropertyBlock();
        if(gate!=null) gate.position=gateAnchor+Vector3.up*(7f*Mathf.SmoothStep(0,1,(time-gateTime-.06f)/1.1f));
        if(lanterns!=null) foreach(var lamp in lanterns)
        {
            float chase=Mathf.Exp(-Mathf.Abs(lamp.worldX-f.x-5f)*.11f);
            SetTint(lamp.renderer,new Color(1,.66f,.25f,.14f+chase*.34f+pulse*.035f));
        }
        float widthView=camera.orthographicSize*2*camera.aspect;
        if(motes!=null) for(int i=0;i<motes.Length;i++)
        {
            float x=Mathf.Repeat(i*.6180339f+time*(.004f+(i%3)*.001f),1f);
            float y=Mathf.Repeat(i*.381966f+time*.012f,1f);
            motes[i].transform.position=new Vector3(camera.transform.position.x+(x-.5f)*widthView,
                camera.transform.position.y+(y-.5f)*camera.orthographicSize*2,0);
            float a=(.10f+.20f*Mathf.Pow(Mathf.Sin(i+time*.6f),2))*(section==6?1.3f:1);
            SetTint(motes[i],new Color(.65f,.78f,1f,a));
        }
        if(shafts!=null) for(int i=0;i<shafts.Length;i++)
        {
            var tr=shafts[i].transform;
            tr.position=new Vector3(camera.transform.position.x+(i-1)*widthView*.39f+Mathf.Sin(time*.08f+i)*2,
                camera.transform.position.y+2,1);
            tr.localScale=new Vector3(2.1f,camera.orthographicSize*2.6f,1);
            tr.rotation=Quaternion.Euler(0,0,-19);
            Color c=section==1||section==5||section==7?new Color(1,.72f,.38f,.055f):new Color(.45f,.65f,1,.045f);
            c.a*=1+pulse*.18f; SetTint(shafts[i],c);
        }
        if(landingGlow!=null)
        {
            float elapsed=10; Stage2MusicRoute.Leap landed=default;
            foreach(var jump in route.Data.jumps) if(time>=jump.end&&time-jump.end<elapsed) {elapsed=time-jump.end;landed=jump;}
            landingGlow.enabled=elapsed<.42f;
            if(landingGlow.enabled)
            {
                landingGlow.transform.position=new Vector3(route.Sample(landed.end).x,landed.toY+.04f,-.1f);
                landingGlow.transform.localScale=new Vector3(1.3f+elapsed*4,.12f+elapsed*.25f,1);
                SetTint(landingGlow,new Color(.55f,.85f,1,(1-elapsed/.42f)*.35f));
            }
        }
    }
    void SetTint(MeshRenderer renderer,Color color)
    {
        block.Clear(); block.SetColor(Tint,color); renderer.SetPropertyBlock(block);
    }
}
