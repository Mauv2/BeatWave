using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Stage2EventsBuilder
{
    static Material lineMaterial,glowMaterial;
    static Mesh quad;
    static Transform Child(string name,Transform parent){var o=new GameObject(name);o.transform.SetParent(parent,false);return o.transform;}
    static LineRenderer Line(string name,Transform parent,Vector3[] points,float width,int order,bool loop=false)
    {
        var r=Child(name,parent).gameObject.AddComponent<LineRenderer>();r.sharedMaterial=lineMaterial;
        r.useWorldSpace=false;r.positionCount=points.Length;r.SetPositions(points);r.widthMultiplier=width;r.loop=loop;
        r.sortingOrder=order;r.startColor=r.endColor=new Color(.65f,.85f,1,.8f);return r;
    }
    static LineRenderer Ring(string name,Transform parent,float radius,int order)
    {
        return Line(name,parent,Enumerable.Range(0,96).Select(i=>new Vector3(Mathf.Cos(i*Mathf.PI/48),Mathf.Sin(i*Mathf.PI/48),0)*radius).ToArray(),.045f,order,true);
    }
    static MeshRenderer Glow(string name,Transform parent,Vector3 position,Vector3 scale)
    {
        var t=Child(name,parent);t.position=position;t.localScale=scale;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=quad;
        var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=glowMaterial;r.sortingOrder=5;return r;
    }
    [MenuItem("BeatWave/Stage2/Build Stage Events")]
    public static void Build()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="Stage2")throw new InvalidOperationException("Stage2 edit mode required");
        var route=UnityEngine.Object.FindObjectOfType<Stage2MusicRoute>();var director=route.presentation;
        var old=director.transform.Find("11 - Moonlit stage events");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
        var root=Child("11 - Moonlit stage events",director.transform);Undo.RegisterCreatedObjectUndo(root.gameObject,"Stage2 stage events");
        var events=root.gameObject.AddComponent<Stage2StageEvents>();events.route=route;events.gateTime=director.gateTime;director.stageEvents=events;
        lineMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        glowMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Stage2/Presentation/Moonlight.mat");
        quad=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Stage2/Presentation/AtmosphereQuad.asset");
        var g=route.Sample(events.gateTime);
        events.seal=Ring("Gate lunar lock",root,2.2f,-6);events.seal.transform.position=new Vector3(g.x+3.5f,g.y+3.5f,0);
        events.gateLight=Glow("Seal awakening",root,events.seal.transform.position,new Vector3(7,7,1));
        events.landingRing=Ring("Landing shockwave",root,1,5);
        var apparition=Child("Lunar gate guardian",root);
        events.guardian=apparition.gameObject.AddComponent<SpriteRenderer>();
        events.guardian.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Stage2/Image/gothic_ruin_assets_individual/07_knight_statue.png");
        var source=events.guardian.sprite;
        const string guardianPath="Assets/Stage2/Presentation/GateGuardian.asset";
        var cropped=AssetDatabase.LoadAssetAtPath<Sprite>(guardianPath);
        if(cropped==null)
        {
            var rect=source.rect;
            cropped=Sprite.Create(source.texture,new Rect(rect.x+rect.width*.32f,rect.y+rect.height*.075f,rect.width*.63f,rect.height*.875f),new Vector2(.5f,.5f),source.pixelsPerUnit);
            cropped.name="Gate guardian silhouette";AssetDatabase.CreateAsset(cropped,guardianPath);
        }
        events.guardian.sprite=cropped;
        events.guardian.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Stage2/Presentation/CutoutStone.mat");
        events.guardian.sortingOrder=-7;apparition.position=new Vector3(g.x+4.5f,g.y+3.7f,0);
        apparition.localScale=Vector3.one*(7/events.guardian.sprite.bounds.size.y);
        events.returnSeal=Ring("Recalled lunar seal",root,3.3f,-6);
        var final=route.Sample(141);events.returnSeal.transform.position=new Vector3(final.x+3,final.y+4,0);
        float[] targets={82,141};events.collapses=new Stage2StageEvents.Collapse[targets.Length];
        for(int k=0;k<targets.Length;k++)
        {
            var jump=route.Data.jumps.OrderBy(j=>Mathf.Abs(j.start-targets[k])).First();
            var a=route.Sample(jump.start);var b=route.Sample(jump.end);
            var parent=Child("Bridge fracture "+(k+1)+" at "+jump.start.ToString("F2")+"s",root);
            var e=new Stage2StageEvents.Collapse{reconstruct=k==1,start=jump.start,end=jump.end,landing=new Vector3(b.x,b.y,0),pieces=new Stage2StageEvents.Piece[4],cracks=new LineRenderer[3]};
            events.collapses[k]=e;
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Stage2/Image/달빛에 부서진 고딕 석교.png");
            float bridgeY=k==0?a.y:b.y;
            float width=(b.x-a.x)/4;float angle=Mathf.Atan2(bridgeY-a.y,b.x-a.x)*Mathf.Rad2Deg;
            for(int i=0;i<4;i++)
            {
                float u=(i+.5f)/4;var t=Child("Fracture slab "+i,parent);t.gameObject.layer=6;
                t.position=new Vector3(Mathf.Lerp(a.x,b.x,u),Mathf.Lerp(a.y,bridgeY,u),0);t.rotation=Quaternion.Euler(0,0,angle);
                var art=Child("Existing bridge stone",t);var sr=art.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=0;
                art.localScale=new Vector3((width/Mathf.Cos(angle*Mathf.Deg2Rad)+.01f)/sprite.bounds.size.x,.6f,1);art.localPosition=Vector3.down*.78f;
                var col=t.gameObject.AddComponent<BoxCollider2D>();col.size=new Vector2(width/Mathf.Cos(angle*Mathf.Deg2Rad),.45f);col.offset=Vector2.down*.225f;
                e.pieces[i]=new Stage2StageEvents.Piece{target=t,anchor=t.position,rotation=t.rotation,collider=col};
                if(i<3)
                {
                    float x=a.x+(i+1)*width,y=Mathf.Lerp(a.y,bridgeY,(i+1)/4f);
                    e.cracks[i]=Line("Warning fracture "+i,parent,new[]{new Vector3(x,y+.05f),new Vector3(x-.12f,y-.2f),new Vector3(x+.1f,y-.5f),new Vector3(x-.05f,y-.8f)},.055f,4);
                }
            }
            e.dustAnchor=new Vector3((a.x+b.x)*.5f,(a.y+b.y)*.5f-.35f,0);
            e.dust=Glow("Fracture dust",parent,e.dustAnchor,Vector3.one);
        }
        events.Present(0);EditorUtility.SetDirty(director);EditorUtility.SetDirty(events);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
