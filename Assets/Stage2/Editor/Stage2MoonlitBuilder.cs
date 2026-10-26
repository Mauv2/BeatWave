using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Stage2-specific bridge architecture. Retains the authored audio, chart and horizontal travel.
public static class Stage2MoonlitBuilder
{
    const string RootName="Stage2 Moonlit Causeway";
    const string Art="Assets/Stage2/Image/";
    const string SmallArt=Art+"gothic_ruin_assets_individual/";
    const string RoutePath="Assets/Stage2/Data/Stage2MoonlitRoute.json";
    const string Presentation="Assets/Stage2/Presentation";
    static Stage2MusicRoute route;
    static Material glowMaterial;
    static Material stoneMaterial,guideMaterial;
    static Mesh quad;
    static readonly List<Stage2StageDirector.Decoration> decorations=new List<Stage2StageDirector.Decoration>();
    static readonly List<Stage2StageDirector.Lantern> lanterns=new List<Stage2StageDirector.Lantern>();

    static T Component<T>(Scene scene) where T:Component => scene.GetRootGameObjects()
        .SelectMany(o=>o.GetComponentsInChildren<T>(true)).First();
    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static Transform Group(string name,Transform parent)
    {
        var o=new GameObject(name); if(parent!=null)o.transform.SetParent(parent); return o.transform;
    }
    [MenuItem("BeatWave/Stage2/Build Moonlit Causeway")]
    public static void Build()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.name!="Stage2")throw new InvalidOperationException("Open Stage2 in edit mode.");
        Directory.CreateDirectory("Reports/Backups");
        string backup="Reports/Backups/Stage2-before-moonlit.unity";
        if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
        route=Component<Stage2MusicRoute>(scene);
        var placer=Component<Stage2NoteMapPlacer>(scene);
        if(AssetDatabase.GetAssetPath(placer.chartFile)!="Assets/Stage2/Data/Stage2OriginalNotes.json")
            throw new InvalidOperationException("Unexpected chart binding; preserve the authored chart before rebuilding.");
        var source=JsonUtility.FromJson<Stage2MusicRoute.Profile>(File.ReadAllText("Assets/Stage2/Data/Stage2OriginalRoute.json"));
        BuildRoute(source,placer.ReadChart());
        File.WriteAllText(RoutePath,JsonUtility.ToJson(source));AssetDatabase.ImportAsset(RoutePath);
        Undo.RecordObject(route,"Stage2 causeway route");route.routeFile=AssetDatabase.LoadAssetAtPath<TextAsset>(RoutePath);
        var old=scene.GetRootGameObjects().FirstOrDefault(o=>o.name==RootName);
        if(old!=null)
        {
            if(placer.noteRoot!=null&&placer.noteRoot.IsChildOf(old.transform)) placer.noteRoot.SetParent(null);
            Undo.DestroyObjectImmediate(old);
        }
        var root=Group(RootName,null);Undo.RegisterCreatedObjectUndo(root.gameObject,"Build Stage2 causeway");
        var original=scene.GetRootGameObjects().First(o=>o.name=="Stage2 Original Layout (archived)");
        Undo.SetTransformParent(placer.noteRoot,root,"Retain Stage2 note root");
        Undo.RecordObject(original,"Preserve prototype layout");original.SetActive(false);
        var director=root.gameObject.AddComponent<Stage2StageDirector>();
        director.route=route;route.presentation=director;
        director.backgrounds=route.backdrop.backgrounds;
        route.backdrop.background1.enabled=false;route.backdrop.background2.enabled=false;
        var back=Group("01 - Moonlit horizons",root);
        director.backdropTiles=new SpriteRenderer[6];
        for(int i=0;i<6;i++)
        {
            var sr=Group("Backdrop crossfade "+i,back).gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder=-60+i/3;director.backdropTiles[i]=sr;
        }
        EnsureResources(); decorations.Clear();lanterns.Clear();
        var sections=new Transform[source.sections.Length];
        for(int i=0;i<sections.Length;i++) sections[i]=Group((i+2).ToString("00")+" - "+source.sections[i].name,root);
        float left=source.frames[0].x-14, floor=source.frames[0].y;
        for(int j=0;j<=source.jumps.Length;j++)
        {
            float right=j<source.jumps.Length?route.Sample(source.jumps[j].start).x:source.frames.Last().x+22;
            BuildSurface(left,right,floor,sections,j);
            if(j==source.jumps.Length)break;
            var leap=source.jumps[j];
            // Small warm pinlights mark the arch of each bridge gap, without resembling input notes.
            var gap=Group("Gap "+(j+1)+" - "+leap.start.ToString("F2")+"s",sections[SectionAt((leap.start+leap.end)*.5f)]);
            for(int k=1;k<=3;k++)
            {
                var f=route.Sample(Mathf.Lerp(leap.start,leap.end,k/4f));
                var light=Glow("Gap pinlight",gap,new Vector3(f.x,f.y-.18f,0),new Vector3(.18f,.18f,1),3);
                light.sharedMaterial=guideMaterial;
            }
            left=route.Sample(leap.end).x;floor=leap.toY;
        }
        for(int i=0;i<sections.Length;i++) BuildArchitecture(i,sections[i]);
        var atmosphere=Group("10 - Song-time atmosphere",root);
        director.motes=Enumerable.Range(0,42).Select(i=>Glow("Dust "+i,atmosphere,Vector3.zero,
            Vector3.one*(i%7==0?.15f:.07f),-8)).ToArray();
        director.shafts=Enumerable.Range(0,3).Select(i=>Glow("Moon shaft "+i,atmosphere,Vector3.zero,Vector3.one,-12)).ToArray();
        director.landingGlow=Glow("Stone landing shimmer",atmosphere,Vector3.zero,Vector3.one,4);
        director.decorations=decorations.ToArray();director.lanterns=lanterns.ToArray();
        director.gateTime=placer.ReadChart().First(n=>n.noteKind==2).createTime;
        var gateFrame=route.Sample(director.gateTime);
        var gate=ArtObject("Nave portcullis",Sprite(SmallArt+"23_gate_ruin.png"),sections[1],gateFrame.x+3.5f,gateFrame.y,7.2f,-9,Color.white);
        director.gate=gate.transform;director.gateAnchor=gate.transform.position;
        // All old event triggers remain archived; only the sampled route controls travel.
        foreach(var trigger in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<JumpTrigger>()))
            trigger.gameObject.SetActive(false);
        var start=route.Sample(0);var body=placer.player.GetComponent<CapsuleCollider2D>();
        Physics2D.SyncTransforms();float offset=placer.player.transform.position.y-body.bounds.min.y;
        Undo.RecordObject(placer.player.transform,"Position Stage2 player");
        placer.player.transform.position=new Vector3(start.x,start.y+offset,0);
        route.view.transform.position=new Vector3(start.x+5.1f,start.y+3,-10);route.view.orthographicSize=5.9f;
        route.view.backgroundColor=new Color(.035f,.045f,.085f);
        director.Present(0);
        EditorUtility.SetDirty(route);EditorUtility.SetDirty(placer);EditorUtility.SetDirty(director);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("Stage2 Moonlit Causeway built: "+source.jumps.Length+" music-aligned bridge gaps; original chart and horizontal speed retained.");
    }
    static void BuildRoute(Stage2MusicRoute.Profile p,Stage2NoteMapPlacer.Entry[] chart)
    {
        float period=60/p.referenceBpm;
        float phase=-p.frames[0].beat*period;
        float[] targets={33.5f,37.5f,44,50.5f,54.5f,61.5f,65,72,76,82,90,99,106,110,117,125,132,141,146,152,159};
        // A single castle crossing: climb the outer arcade, cross roof bridges, descend through the
        // cloister, then reach the lit nave. No reverse movement, lift, theatrical drop or sky finale.
        float[] floors={-2.65f,-2.05f,-1.45f,-1.05f,-.55f,-.55f,-1.15f,-1.15f,-1.75f,
            -2.45f,-3.15f,-3.75f,-3.75f,-3.15f,-2.55f,-1.95f,-1.35f,-.75f,-.75f,-1.35f,-1.95f};
        var jumps=new List<Stage2MusicRoute.Leap>(); float ground=p.frames[0].y;
        for(int i=0;i<targets.Length;i++)
        {
            float start=phase+Mathf.Round((targets[i]-phase)/period)*period;
            float length=period*2;
            bool safe=false;
            for(int attempt=0;attempt<12;attempt++)
            {
                float candidate=start+((attempt+1)/2)*(attempt%2==0?1:-1)*period;
                if(candidate<31||candidate+length>161||jumps.Any(j=>candidate<j.end+.7f))continue;
                if(chart.Any(n=>n.noteKind==1&&candidate<n.createTime+n.holdTime+.25f&&candidate+length>n.createTime-.25f))continue;
                start=candidate;safe=true;break;
            }
            if(!safe)throw new InvalidOperationException("No safe bridge gap near "+targets[i]);
            jumps.Add(new Stage2MusicRoute.Leap{start=start,end=start+length,fromY=ground,toY=floors[i],height=i>=15?1.55f:1.3f});
            ground=floors[i];
        }
        p.jumps=jumps.ToArray();p.useSampledHeight=false;
        for(int i=0;i<p.frames.Length;i++)
        {
            float t=p.frames[i].time,y=p.frames[0].y;
            foreach(var j in jumps)
            {
                if(t<j.start)break;
                if(t>=j.end){y=j.toY;continue;}
                float u=(t-j.start)/(j.end-j.start);y=Mathf.Lerp(j.fromY,j.toY,u)+4*j.height*u*(1-u);break;
            }
            p.frames[i].y=y;
        }
        string[] names={"Moonlit outer causeway","Candle nave and sealed gate","Ascending buttress bridges","Rooftop pursuit",
            "Sunken cloister bridges","Lantern approach","Illuminated cathedral crossing","Quiet sanctuary"};
        int[] backgrounds={0,1,2,2,1,0,3,3};
        for(int i=0;i<p.sections.Length;i++){p.sections[i].name=names[i];p.sections[i].background=backgrounds[i];}
    }
    static int SectionAt(float time)
    {
        for(int i=route.Data.sections.Length-1;i>=0;i--)if(time>=route.Data.sections[i].start)return i;return 0;
    }
    static float TimeAt(float x)
    {
        var frames=route.Data.frames;int lo=0,hi=frames.Length-1;
        while(lo<hi){int mid=(lo+hi)/2;if(frames[mid].x<x)lo=mid+1;else hi=mid;}
        if(lo==0)return 0;var a=frames[lo-1];var b=frames[lo];return Mathf.Lerp(a.time,b.time,Mathf.InverseLerp(a.x,b.x,x));
    }
    static void BuildSurface(float left,float right,float floor,Transform[] sections,int span)
    {
        var cuts=new List<float>{left};
        foreach(var s in route.Data.sections){float x=route.Sample(s.start).x;if(x>left+.05f&&x<right-.05f)cuts.Add(x);}
        cuts.Add(right);
        for(int c=0;c<cuts.Count-1;c++)
        {
            float a=cuts[c],b=cuts[c+1];int section=SectionAt(TimeAt((a+b)*.5f));
            var parent=Group("Stone span "+span+"."+c,sections[section]);
            var ground=Group("Walkable stone",parent);ground.gameObject.layer=6;
            ground.position=new Vector3((a+b)/2,floor-.25f,0);
            ground.gameObject.AddComponent<BoxCollider2D>().size=new Vector2(b-a+.02f,.5f);
            string file=section==1||section>=5?"고딕 성당의 촛불 석조 플랫폼.png":section>=2?"달빛에 부서진 고딕 석교.png":"고딕 석조 플랫폼 타일.png";
            var sprite=Sprite(Art+file);
            float sy=section>=2&&section<=4?.60f:1f;
            float floorOffset=section==1||section>=5?-1.12f:section>=2?.78f:-.09554f;
            float naturalWidth=sprite.bounds.size.x*(section>=2&&section<=4?.71f:1f);
            int count=Mathf.Max(1,Mathf.CeilToInt((b-a)/naturalWidth));
            float width=(b-a)/count;
            for(int i=0;i<count;i++)
            {
                var o=Group("Authored "+file+" "+i,parent);var r=o.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=0;
                o.localScale=new Vector3((width+.015f)/sprite.bounds.size.x,sy,1);
                o.position=new Vector3(a+width*(i+.5f),floor-floorOffset,0);
                r.color=section==4?new Color(.78f,.85f,.96f):Color.white;
            }
        }
    }
    static SpriteRenderer ArtObject(string name,Sprite sprite,Transform parent,float x,float floor,float height,int order,Color tint)
    {
        if(sprite==null)throw new InvalidOperationException("Missing art: "+name);
        var o=Group(name,parent);var r=o.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;r.color=tint;r.sharedMaterial=stoneMaterial;
        float scale=height/sprite.bounds.size.y;o.localScale=Vector3.one*scale;
        o.position=new Vector3(x,floor-sprite.bounds.min.y*scale,0);return r;
    }
    static void Decor(string name,string file,Transform parent,float x,float floor,float height,int order,Color tint,float parallax)
    {
        var r=ArtObject(name,Sprite(SmallArt+file+".png"),parent,x,floor,height,order,tint);
        decorations.Add(new Stage2StageDirector.Decoration{target=r.transform,anchor=r.transform.position,parallax=parallax});
    }
    static void BuildArchitecture(int section,Transform parent)
    {
        var s=route.Data.sections[section];float start=route.Sample(s.start).x,end=route.Sample(s.end).x;
        var middle=Group("Buttresses and lamps",parent);
        for(float x=start+7;x<end;x+=section==1?14:23)
        {
            var f=route.Sample(TimeAt(x));float floor=f.y;
            string structure=section==1||section==6||section==7?"13_broken_arch":"14_ruined_column_1";
            Decor("Stone architecture",structure,middle,x,floor-1,section==1||section>=6?7:5.5f,-18,new Color(.62f,.69f,.86f),.08f);
            if(section==2||section==4)Decor("Hanging banner","18_hanging_banner",middle,x+3,floor+3.8f,3.5f,-15,new Color(.68f,.72f,.90f),.08f);
            if(section==0||section==5||section>=6)
            {
                Decor("Causeway banner","16_banner_post",middle,x+2,floor-1,5,-10,new Color(.78f,.82f,.97f),0);
                var lamp=Glow("Passing lantern",middle,new Vector3(x+2,floor+3,0),new Vector3(1.4f,2.8f,1),-9);
                lanterns.Add(new Stage2StageDirector.Lantern{renderer=lamp,worldX=x+2,phase=lanterns.Count*.7f});
            }
        }
        // Distinct silhouettes frame phrase boundaries rather than duplicating whole maps.
        float anchor=start+Mathf.Min(18,(end-start)*.4f);float baseY=route.Sample(TimeAt(anchor)).y;
        if(section==0||section==4||section==7)
            Decor("Sentinel statue","07_knight_statue",middle,anchor,baseY-.5f,6,-7,new Color(.72f,.79f,.93f),0);
        if(section==2||section==3||section==6)
            Decor("Broken overhead arch","13_broken_arch",middle,anchor,baseY-1.8f,11,-16,new Color(.50f,.59f,.80f),.06f);
        // Foreground masonry stays below the note band and adds depth without hiding inputs.
        for(float x=start+13;x<end;x+=38)
        {
            float y=route.Sample(TimeAt(x)).y;
            Decor("Near ruined parapet","20_broken_railing",middle,x,y-3.6f,2.1f,6,new Color(.22f,.29f,.47f),-.10f);
        }
    }
    static void EnsureResources()
    {
        Directory.CreateDirectory(Presentation);AssetDatabase.Refresh();
        quad=AssetDatabase.LoadAssetAtPath<Mesh>(Presentation+"/AtmosphereQuad.asset");
        if(quad==null)
        {
            quad=new Mesh{name="Stage2 atmosphere quad"};quad.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
            quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();AssetDatabase.CreateAsset(quad,Presentation+"/AtmosphereQuad.asset");
        }
        glowMaterial=AssetDatabase.LoadAssetAtPath<Material>(Presentation+"/Moonlight.mat");
        if(glowMaterial==null)
        {
            var shader=Shader.Find("BeatWave/Stage2Atmosphere");if(shader==null)throw new InvalidOperationException("Compile Stage2 atmosphere shader first.");
            glowMaterial=new Material(shader);AssetDatabase.CreateAsset(glowMaterial,Presentation+"/Moonlight.mat");
        }
        stoneMaterial=AssetDatabase.LoadAssetAtPath<Material>(Presentation+"/CutoutStone.mat");
        if(stoneMaterial==null)
        {
            var shader=Shader.Find("BeatWave/Stage2CutoutStone");if(shader==null)throw new InvalidOperationException("Compile stone shader first.");
            stoneMaterial=new Material(shader);AssetDatabase.CreateAsset(stoneMaterial,Presentation+"/CutoutStone.mat");
        }
        guideMaterial=AssetDatabase.LoadAssetAtPath<Material>(Presentation+"/GuideGold.mat");
        if(guideMaterial==null)
        {
            guideMaterial=new Material(glowMaterial);guideMaterial.SetColor("_Tint",new Color(1,.75f,.32f,.65f));
            AssetDatabase.CreateAsset(guideMaterial,Presentation+"/GuideGold.mat");
        }
    }
    static MeshRenderer Glow(string name,Transform parent,Vector3 position,Vector3 scale,int order)
    {
        var o=Group(name,parent);o.position=position;o.localScale=scale;
        o.gameObject.AddComponent<MeshFilter>().sharedMesh=quad;
        var r=o.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=glowMaterial;r.sortingOrder=order;return r;
    }
}
