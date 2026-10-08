#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1MapPresentationBuilder
{
    const string AssetFolder = "Assets/Stage1Presentation";
    static Sprite square;
    static Material lineMaterial;
    static readonly Color Gold = new Color(0.93f, 0.71f, 0.32f);
    static readonly Color Wood = new Color(0.29f, 0.16f, 0.11f);
    [MenuItem("BeatWave/Build Stage1 Switchback Map")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Stage1" || EditorApplication.isPlaying)
            throw new InvalidOperationException("Open Stage1 in edit mode before building.");
        PrepareAssets();
        var existing = GameObject.Find("Stage1_JumpPresentation");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        foreach(var jump in UnityEngine.Object.FindObjectsOfType<JumpTrigger>()) {jump.presentation=null;EditorUtility.SetDirty(jump);}
        var root = new GameObject("Stage1_JumpPresentation").transform;
        root.gameObject.AddComponent<Grid>();
        var mover=UnityEngine.Object.FindObjectOfType<PlayerMove>();
        var source=UnityEngine.Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>().First(t=>t.name=="Tilemap");
        Island(root,source,"Launch ledge",23,0,5);
        Island(root,source,"Upper left island",11,6,9);
        Island(root,source,"Upper right island",30,11,10);
        var route=root.gameObject.AddComponent<Stage1Switchback>();
        var left=Launch(root,"Leftward seesaw",new Vector2(24.21f,0),-1);
        var right=Launch(root,"Rightward seesaw",new Vector2(15.5f,6),1);
        route.legs=new[]
        {
            new Stage1Switchback.Leg {startTime=4.38f,endTime=5.7f,from=new Vector2(24.21f,1),to=new Vector2(17,7),arcHeight=2.6f,airborne=true,launch=left},
            new Stage1Switchback.Leg {startTime=5.7f,endTime=6.05f,from=new Vector2(17,7),to=new Vector2(15.5f,7)},
            new Stage1Switchback.Leg {startTime=6.05f,endTime=7.75f,from=new Vector2(15.5f,7),to=new Vector2(34,12),arcHeight=4.2f,airborne=true,launch=right},
            new Stage1Switchback.Leg {startTime=7.75f,endTime=8.15f,from=new Vector2(34,12),to=new Vector2(36,12)},
            new Stage1Switchback.Leg {startTime=8.15f,endTime=11f,from=new Vector2(36,12),to=new Vector2(57.31f,8),arcHeight=2.8f,airborne=true}
        };
        for(int n=0;n<route.legs.Length;n++)
        {
            var leg=route.legs[n]; if(!leg.airborne)continue;
            int count=leg.launch!=null ? 12 : 16;
            var stars=new Transform[count];
            for(int i=0;i<count;i++)
            {
                var star=Child(root,"Route_"+n+"_Star_"+i,(Vector3)leg.Evaluate(Mathf.Lerp(leg.startTime,leg.endTime,(i+1f)/(count+1)))+Vector3.up*.45f);
                Rectangle(star,"Diamond",Vector2.zero,Vector2.one,Gold,4).transform.localRotation=Quaternion.Euler(0,0,45);
                Rectangle(star,"Glint",Vector2.zero,new Vector2(.3f,1.5f),new Color(1,.9f,.6f,.55f),4);
                star.localScale=Vector3.one*.12f; stars[i]=star;
            }
            if(leg.launch!=null)leg.launch.presentation.pathStars=stars;
        }
        Gate(root,new Vector3(34.5f,11f),"Canopy landing",new Color(Gold.r,Gold.g,Gold.b,.28f));
        mover.switchback=route;
        mover.facingVisual=mover.transform.Find("Visual");
        var placer=UnityEngine.Object.FindObjectsOfType<NoteMapPlacer>().First(p=>p.placeOnStart);
        mover.upMarker=placer.judgePointUp; mover.downMarker=placer.judgePointDown;
        EditorUtility.SetDirty(mover);
        var camera=Camera.main;
        var director=camera.GetComponent<Stage1CameraDirector>();
        if(director==null)director=camera.gameObject.AddComponent<Stage1CameraDirector>();
        director.player=mover; EditorUtility.SetDirty(director);
        // The source image is not seamless; keep a complete image behind the expanded map.
        foreach(var loop in UnityEngine.Object.FindObjectsOfType<MapLoop>())
        {
            loop.enabled=false;
            var image=loop.GetComponent<UnityEngine.UI.RawImage>();
            if(image!=null){image.uvRect=new Rect(0,0,1,1);EditorUtility.SetDirty(image);}
            EditorUtility.SetDirty(loop);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("Stage1 expanded: two directional seesaws, three added islands, left/right upper route, HP-zero game over.");
    }
    static void Island(Transform parent,UnityEngine.Tilemaps.Tilemap source,string name,int x,int top,int width)
    {
        var t=Child(parent,name,new Vector3(x,top-1));
        t.gameObject.layer=6;
        var map=t.gameObject.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        var renderer=t.gameObject.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>(); renderer.sortingOrder=2;
        var cap=source.GetTile(new Vector3Int(3,-4,0));
        var soil=source.GetTile(new Vector3Int(3,-5,0));
        for(int i=0;i<width;i++)
        {
            map.SetTile(new Vector3Int(i,0,0),cap);
            map.SetTile(new Vector3Int(i,-1,0),soil);
        }
        // An exact flat top is shared by the authored landing and terrain queries.
        var collider=t.gameObject.AddComponent<BoxCollider2D>(); collider.size=new Vector2(width,1.4f);
        collider.offset=new Vector2(width*.5f,.3f);
        for(int i=1;i<width;i+=3)
        {
            Line(t,"Hanging root",new[]{new Vector3(i,-.7f),new Vector3(i+.2f,-1.5f),new Vector3(i-.15f,-1.9f)},new Color(.2f,.28f,.1f),.08f,false,3);
        }
    }
    static JumpTrigger Launch(Transform root,string name,Vector2 foot,float direction)
    {
        var cueObject=Child(root,name+" cue",foot+Vector2.up);
        var cue=cueObject.gameObject.AddComponent<JumpTrigger>();
        var pivot=Child(root,name,foot+Vector2.right*(-direction*.75f));
        var set=pivot.gameObject.AddComponent<StageJumpSetPiece>();
        cue.presentation=set; set.launchDirection=direction; set.sectionName=name;
        Color accent=direction<0?new Color(.75f,.4f,1):new Color(.2f,.9f,.8f); set.accent=accent;
        Line(pivot,"Pivot",new[]{new Vector3(-.4f,-.65f),new Vector3(.4f,-.65f),new Vector3(0,-.1f)},Gold,.07f,true,5);
        Rectangle(pivot,"Base",new Vector2(0,-.68f),new Vector2(1.2f,.16f),Wood,5);
        var beam=Child(pivot,"Seesaw board",new Vector3(0,-.13f));set.beam=beam;
        Rectangle(beam,"Plank",Vector2.zero,new Vector2(3.1f,.24f),Wood,6);
        Rectangle(beam,"Upper rim",new Vector2(0,.12f),new Vector2(3.16f,.045f),Gold,7);
        Rectangle(beam,"Lower rim",new Vector2(0,-.12f),new Vector2(3.16f,.035f),Gold*.7f,7);
        for(int i=0;i<5;i++)Rectangle(beam,"Wood grain",new Vector2(-1.1f+i*.5f,0),new Vector2(.02f,.16f),Gold*.55f,7);
        var weight=Child(beam,"Crystal counterweight",new Vector3(-direction*1.05f,.57f));set.counterweight=weight;
        Rectangle(weight,"Crystal",Vector2.zero,new Vector2(.48f,.48f),accent,8).transform.localRotation=Quaternion.Euler(0,0,45);
        Rectangle(weight,"Glint",new Vector2(-.07f,.07f),new Vector2(.12f,.2f),Color.white*.9f,9).transform.localRotation=Quaternion.Euler(0,0,45);
        Circle(weight,"Cage",.42f,Gold,.04f,9);
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI/4;
            var tooth=Rectangle(weight,"Gear tooth",new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.45f,new Vector2(.16f,.08f),Gold,8);
            tooth.transform.localRotation=Quaternion.Euler(0,0,i*45);
        }
        beam.localRotation=Quaternion.Euler(0,0,-direction*7);
        set.glow=Rectangle(pivot,"Launch glow",new Vector2(direction*.75f,.05f),new Vector2(.8f,.035f),new Color(accent.r,accent.g,accent.b,.25f),7);
        var ring=Child(pivot,"Launch pulse",new Vector3(direction*.75f,.2f));
        Circle(ring,"Ring",1,accent,.035f,6);set.launchRing=ring;ring.gameObject.SetActive(false);
        return cue;
    }
    static void PrepareAssets()
    {
        Directory.CreateDirectory(AssetFolder);
        string file=AssetFolder+"/Shape.png";
        if(!File.Exists(file))
        {
            var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);
            texture.SetPixels(Enumerable.Repeat(Color.white,256).ToArray()); texture.Apply();
            File.WriteAllBytes(file,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(file);
            var importer=(TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=16; importer.mipmapEnabled=false;
            importer.SaveAndReimport();
        }
        square=AssetDatabase.LoadAssetAtPath<Sprite>(file);
        string materialPath=AssetFolder+"/Lines.mat";
        lineMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(lineMaterial==null)
        {
            var shader=Shader.Find("Sprites/Default");
            if(shader==null)throw new InvalidOperationException("Sprites/Default shader is unavailable.");
            lineMaterial=new Material(shader); AssetDatabase.CreateAsset(lineMaterial,materialPath);
        }
    }
    static Transform Child(Transform parent,string name,Vector3 position)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=position; return go.transform;
    }
    static SpriteRenderer Rectangle(Transform parent,string name,Vector2 position,Vector2 size,Color color,int order)
    {
        var t=Child(parent,name,position); t.localScale=new Vector3(size.x,size.y,1);
        var renderer=t.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite=square; renderer.color=color; renderer.sortingOrder=order; return renderer;
    }
    static void Line(Transform parent,string name,Vector3[] points,Color color,float width,bool loop,int order)
    {
        var t=Child(parent,name,Vector3.zero); var line=t.gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial=lineMaterial; line.useWorldSpace=false; line.positionCount=points.Length; line.SetPositions(points);
        line.loop=loop; line.startWidth=line.endWidth=width; line.startColor=line.endColor=color; line.sortingOrder=order;
        line.numCapVertices=3; line.numCornerVertices=3;
    }
    static void Circle(Transform parent,string name,float radius,Color color,float width,int order)
    {
        var points=new Vector3[48];
        for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length; points[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius;}
        Line(parent,name,points,color,width,true,order);
    }
    static void Gate(Transform root,Vector3 position,string name,Color color)
    {
        var gate=Child(root,name,position);
        Rectangle(gate,"Left pillar",new Vector2(-4,3.7f),new Vector2(.16f,7.4f),color,1);
        Rectangle(gate,"Right pillar",new Vector2(4,3.7f),new Vector2(.16f,7.4f),color,1);
        var halo=Child(gate,"Clock crown",new Vector3(0,5.2f));
        Circle(halo,"Outer arch",3.7f,color,.045f,1);
        Circle(halo,"Inner arch",3.4f,color,.025f,1);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;
            var tick=Rectangle(halo,"Clock marker",new Vector2(Mathf.Cos(a),Mathf.Sin(a))*3.5f,new Vector2(.25f,.06f),color,1);
            tick.transform.localRotation=Quaternion.Euler(0,0,i*30);
        }
    }
}
#endif