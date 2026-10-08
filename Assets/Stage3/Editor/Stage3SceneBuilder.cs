#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
namespace BeatWave.Stage3
{
    public static class Stage3SceneBuilder
    {
        const string Folder = "Assets/Stage3";
        const string ScenePath = "Assets/Scenes/Stage3.unity";
        static Sprite square, bridge;
        static Material lines;
        static readonly Color Gold = new Color(.88f,.75f,.48f);
        static Transform world;
        [MenuItem("BeatWave/Build Stage3 - Last Wish")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene edits before building Stage3.");
            Directory.CreateDirectory("Reports");
            if(File.Exists(ScenePath)) File.Copy(ScenePath,"Reports/Stage3-before-rebuild-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity.bak");
            var original = EditorSceneManager.OpenScene("Assets/Scenes/Stage1.unity",OpenSceneMode.Single);
            // Unity remaps all intra-scene references, including persistent button events.
            EditorSceneManager.SaveScene(original,ScenePath,true);
            var scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var placer = UnityEngine.Object.FindObjectsOfType<NoteMapPlacer>().First(p=>p.placeOnStart);
            var tap = placer.tapNotePrefab; var hold = placer.holdNotePrefab; var mash=placer.mashNotePrefab;
            string[] keep={"Canvas","Player","PlayManager","PauseManager","EventSystem","GameResultManager"};
            foreach(var root in scene.GetRootGameObjects()) if(!keep.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
            var canvas=GameObject.Find("Canvas").GetComponent<Canvas>();
            // The old canvas also carried the background and was behind the player.
            // Stage3 owns a world background, so the unchanged menu can stay on top.
            canvas.sortingOrder=200;
            foreach(var raw in canvas.GetComponentsInChildren<RawImage>(true)) UnityEngine.Object.DestroyImmediate(raw.gameObject);
            var manager=GameObject.Find("PlayManager");
            foreach(Transform child in manager.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(var old in manager.GetComponents<NoteMapPlacer>()) UnityEngine.Object.DestroyImmediate(old);
            UnityEngine.Object.DestroyImmediate(manager.GetComponent<PlayNote>());
            var mover=UnityEngine.Object.FindObjectOfType<PlayerMove>();
            mover.enabled=false; mover.switchback=null;
            var camera=mover.GetComponentInChildren<Camera>();camera.transform.SetParent(null,true);
            var cameraDirector=camera.GetComponent<Stage1CameraDirector>();if(cameraDirector!=null) UnityEngine.Object.DestroyImmediate(cameraDirector);
            camera.orthographic=true;camera.orthographicSize=6.6f;camera.transform.position=new Vector3(4,3.2f,-10);
            camera.backgroundColor=new Color(.035f,.045f,.1f);
            var body=mover.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;body.simulated=false;
            var eventPoint=mover.transform.Find("EventJudgePoint");if(eventPoint!=null)UnityEngine.Object.DestroyImmediate(eventPoint.gameObject);
            world=new GameObject("Stage3 - Last Wish Observatory").transform;
            var game=world.gameObject.AddComponent<Stage3Gameplay>();
            game.tapPrefab=tap;game.holdPrefab=hold;game.mashPrefab=mash;game.chartFile=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/Data/LastWishChart.json");
            game.judge=manager.GetComponent<JudgeManager>();game.player=mover.transform;game.visual=mover.facingVisual;
            game.upMarker=mover.upMarker;game.downMarker=mover.downMarker;game.playerAnimation=mover.GetComponent<PlayerAnimController>();game.view=camera;
            var capsule=mover.GetComponent<CapsuleCollider2D>();
            game.footOffset=(capsule.size.y*.5f-capsule.offset.y)*mover.transform.localScale.y;
            mover.transform.position=new Vector3(0,game.footOffset,0);
            game.noteRoot=Child(world,"Notes - Stage1 original prefabs",Vector3.zero);
            var audio=manager.GetComponent<AudioSource>();
            if(audio==null)audio=manager.AddComponent<AudioSource>();
            audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/lastwish_inst.mp3");
            audio.enabled=true;audio.playOnAwake=false;audio.loop=false;audio.spatialBlend=0;audio.volume=.7f;game.music=audio;
            Stage3Route.ConfigureDuration(audio.clip.length);
            UnityEngine.Object.FindObjectOfType<PauseManager>().gameAudio=audio;
            if(UnityEngine.Object.FindObjectOfType<RhythmClock>()==null) manager.AddComponent<RhythmClock>();
            var light=new GameObject("Observatory Moonlight").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.5f;
            square=SpriteAt(Folder+"/Art/White.png");lines=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Art/Stage3Lines.mat");
            var background=ImportSprite(Folder+"/Art/LastWishObservatory.png",new Vector2(.5f,.5f));
            // Generated transparent art's level stone edge is at y=.735 of its canvas.
            bridge=ImportSprite(Folder+"/Art/LastWishWalkway.png",new Vector2(.5f,.735f));
            var bg=Child(world,"New painted observatory - parallax",new Vector3(4,4.2f,20));
            game.backdrop=bg.gameObject.AddComponent<SpriteRenderer>();game.backdrop.sprite=background;
            game.backdrop.sortingOrder=-100;game.backdrop.color=new Color(.48f,.55f,.8f);bg.localScale=Vector3.one*(32/background.bounds.size.x);
            BuildWorld(game);
            var data=AssetDatabase.LoadAssetAtPath<MusicData>(Folder+"/Data/LastWishMusic.asset");
            if(data==null){data=ScriptableObject.CreateInstance<MusicData>();AssetDatabase.CreateAsset(data,Folder+"/Data/LastWishMusic.asset");}
            data.musicName="Last Wish / Stage 3";data.sceneName="Stage3";data.gameClip=data.previewClip=audio.clip;data.musicImage=background;EditorUtility.SetDirty(data);
            var builds=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();
            // Remove the old entry first: Unity otherwise retains a deleted scene's GUID.
            EditorBuildSettings.scenes=builds.ToArray();
            var entry=new EditorBuildSettingsScene(ScenePath,true);
            entry.guid=new GUID(AssetDatabase.AssetPathToGUID(ScenePath));builds.Add(entry);
            EditorBuildSettings.scenes=builds.ToArray();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Selection.activeGameObject=world.gameObject;
            Debug.Log("STAGE3_BUILD_OK: Stage1 UI, pause actions and note prefabs; original Last Wish environment.");
        }
        static Sprite SpriteAt(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        static Sprite ImportSprite(string path,Vector2 pivot)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            return SpriteAt(path);
        }
        static Transform Child(Transform parent,string name,Vector3 p){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;return g.transform;}
        static SpriteRenderer Rect(Transform p,string n,Vector2 v,Vector2 size,Color c,int order)=>Stage3Drawing.Rect(p,n,v,size,c,order,square);
        static LineRenderer Line(Transform p,string n,Vector3[] pts,Color c,float width,int order)=>Stage3Drawing.Line(p,n,pts,c,width,order,lines);
        static LineRenderer Ring(Transform p,string n,float radius,Color c,float width,int order)=>Stage3Drawing.Ring(p,n,radius,c,width,order,lines);
        static void BuildWorld(Stage3Gameplay game)
        {
            var platforms=Child(world,"New floating walkways",Vector3.zero);
            var mechanisms=Child(world,"One seesaw - varied terrain",Vector3.zero);
            var beams=new List<Transform>();var times=new List<float>();int section=0;
            var events=world.gameObject.AddComponent<Stage3Transitions>();game.transitions=events;events.game=game;
            var pieces=new List<Transform>();var colliders=new List<Collider2D>();
            foreach(var leg in Stage3Route.Legs)
            {
                if(leg.kind==Stage3LegKind.Jump)
                {
                    var pivot=Child(mechanisms,"Single seesaw launch",leg.from);
                    Line(pivot,"Fulcrum",new[]{new Vector3(-.5f,-.7f),new Vector3(.5f,-.7f),new Vector3(0,-.05f),new Vector3(-.5f,-.7f)},Gold,.065f,14);
                    var beam=Child(pivot,"Resonating beam",new Vector3(0,-.08f));
                    Rect(beam,"Obsidian",Vector2.zero,new Vector2(3,.16f),new Color(.16f,.19f,.3f),16);
                    Rect(beam,"Gilded edge",new Vector2(0,.08f),new Vector2(3,.04f),Gold,17);
                    beams.Add(beam);times.Add(leg.start);
                    for(int j=1;j<15;j++)
                    {
                        var star=Rect(platforms,"Jump guide",leg.At(Mathf.Lerp(leg.start,leg.end,j/15f))+Vector2.up*.4f,Vector2.one*.08f,Gold,12);star.transform.localRotation=Quaternion.Euler(0,0,45);
                    }
                    continue;
                }
                if(leg.Airborne)continue;
                if(leg.kind==Stage3LegKind.Gate && Mathf.Abs(leg.start-Stage3Route.Beat(44))<.01f)
                {
                    for(int i=0;i<5;i++)
                    {
                        var from=leg.from+Vector2.right*(-4+i*1.6f);
                        var piece=Ground(platforms,"Fracture slab "+i,from,from+Vector2.right*1.6f,0);
                        pieces.Add(piece);colliders.Add(piece.GetComponent<BoxCollider2D>());
                        var stone=piece.GetComponentInChildren<SpriteRenderer>();stone.sprite=SlabSprite(i);
                        stone.transform.localScale=Vector3.one*(1.6f/stone.sprite.bounds.size.x);
                        Line(piece,"Luminous fissure",new[]{new Vector3(.1f,-.05f),new Vector3(.55f,-.18f),new Vector3(.75f,-.09f),new Vector3(1.1f,-.43f)},new Color(.55f,.8f,1,.6f),.02f,10);
                    }
                    continue;
                }
                Vector2 p=leg.from,q=leg.to;
                if(leg.kind==Stage3LegKind.Gate){p.x-=3;q.x+=3;}
                if(section==0)p.x=-14;
                string kind=Mathf.Abs(q.y-p.y)<.01f?"Level":q.y>p.y?"Uphill":"Downhill";
                Ground(platforms,kind+" walkway "+(++section),p,q,.1f);
            }
            events.collapsePieces=pieces.ToArray();events.collapseColliders=colliders.ToArray();
            game.seesaws=beams.ToArray();game.launchTimes=times.ToArray();
            var phrases=Stage3Route.Legs.Where(l=>l.kind==Stage3LegKind.Gate).ToArray();
            events.gates=new[]{
                Gate(game,"Fracture gate",phrases[0],8,"BREAK"),
                Gate(game,"Astral lift gate",phrases[1],10,"ASCEND"),
                Gate(game,"Last Wish finale",phrases[2],8,"LAST WISH")};
            var shards=new List<Transform>();
            for(int i=0;i<24;i++)
            {
                var r=Rect(world,"Falling stone shard "+i,new Vector2(82+i%8,7.7f-(i/8)*.25f),new Vector2(.12f+(i%3)*.15f,.14f+(i%4)*.1f),new Color(.35f,.5f,.69f),15);
                shards.Add(r.transform);r.gameObject.SetActive(false);
            }
            events.debris=shards.ToArray();
            events.portal=Portal("Astral lift",new Vector3(161,-.3f),2.2f);
            events.portalExit=Portal("Upper terrace arrival",new Vector3(183,10),2.5f);
            events.finale=Portal("Final wish shockwave",(Vector3)phrases[2].from+new Vector3(1,2.8f),3.2f);
            game.clockHand=Child(events.finale,"Celestial hand",Vector3.zero);
            Rect(game.clockHand,"Gold hand",new Vector2(0,1.3f),new Vector2(.045f,2.6f),Gold,-2);
            var background=ImportSprite(Folder+"/Art/LastWishDepths.png",new Vector2(.5f,.5f));
            events.depths=Child(world,"New submerged archive backdrop",new Vector3(4,4.2f,19.8f)).gameObject.AddComponent<SpriteRenderer>();
            events.depths.sprite=background;events.depths.sortingOrder=-99;events.depths.color=new Color(.7f,.85f,1,0);
            var canvas=GameObject.Find("Canvas").transform;
            var wash=new GameObject("Stage3 transition flash",typeof(RectTransform),typeof(Image));wash.transform.SetParent(canvas,false);wash.transform.SetAsFirstSibling();
            var rt=(RectTransform)wash.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            events.wash=wash.GetComponent<Image>();events.wash.raycastTarget=false;events.wash.color=Color.clear;
            var random=new System.Random(314);var dust=Child(world,"Wish motes",Vector3.zero);
            for(int i=0;i<260;i++)
            {
                float x=(float)random.NextDouble()*270-8,y=(float)random.NextDouble()*35-13,s=.018f+(float)random.NextDouble()*.04f;
                var mote=Rect(dust,"Mote",new Vector2(x,y),Vector2.one*s,new Color(.7f,.84f,1,.5f),-8);mote.transform.localRotation=Quaternion.Euler(0,0,45);
            }
            BuildLandmarks();
        }
        static void BuildLandmarks()
        {
            var props=Child(world,"Section landmarks - colonnade archive and dawn",Vector3.zero);
            // Uneven spacing and distinct silhouettes give each long passage its
            // own identity. All pieces use Stage3's generated art or native lines.
            foreach(float x in new[]{8f,23f,45f})
            {
                var arch=Child(props,"Observatory broken arch",new Vector3(x,-.2f));
                Line(arch,"Open arch",new[]{new Vector3(-2,0),new Vector3(-2,5),new Vector3(-.8f,6.3f),new Vector3(.1f,6.5f)},new Color(.31f,.41f,.62f,.4f),.12f,-12);
                Line(arch,"Gold inlay",new[]{new Vector3(-1.8f,1),new Vector3(-1.8f,4.8f)},new Color(.66f,.58f,.4f,.6f),.035f,-11);
            }
            foreach(float x in new[]{111f,120f,133f})
            {
                var pillar=Child(props,"Submerged archive buttress",new Vector3(x,-8.15f));
                Rect(pillar,"Deep stone support",new Vector2(0,-4),new Vector2(.55f,8),new Color(.07f,.14f,.22f,.7f),-6);
                Line(pillar,"Support bevel",new[]{new Vector3(-.26f,-8),new Vector3(-.26f,-.4f),new Vector3(.26f,-.4f),new Vector3(.26f,-8)},new Color(.33f,.49f,.56f,.55f),.018f,-5);
                Rect(pillar,"Stone capital",new Vector2(0,-.25f),new Vector2(.85f,.14f),new Color(.17f,.3f,.39f,.8f),-5);
                Line(pillar,"Suspended archive frame",new[]{new Vector3(-.6f,4),new Vector3(-.6f,6),new Vector3(0,6.65f),new Vector3(.6f,6),new Vector3(.6f,4)},new Color(.27f,.47f,.6f,.3f),.035f,-10);
                Rect(pillar,"Archive blue light",new Vector2(0,5.5f),new Vector2(.035f,.8f),new Color(.36f,.74f,.86f,.4f),-9);
            }
            foreach(float x in new[]{189f,225f,237f})
            {
                float y=x<200?8:6;
                var terrace=Child(props,"Dawn terrace fin",new Vector3(x,y-.12f));
                Line(terrace,"Hanging gold fin",new[]{new Vector3(-1,0),new Vector3(0,-3),new Vector3(1,0)},new Color(.82f,.74f,.52f,.5f),.025f,7);
            }
            Line(props,"Archive long cornice",new[]{new Vector3(110,-1.5f),new Vector3(120,-.8f),new Vector3(131,-1.5f)},new Color(.3f,.46f,.58f,.25f),.055f,-15);
        }
        static Transform Ground(Transform parent,string name,Vector2 from,Vector2 to,float padding)
        {
            Vector2 delta=to-from;float length=delta.magnitude;
            var platform=Child(parent,name,from);platform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            bool archive=from.x>=94 && from.x<160,upper=from.x>=180;
            int count=Mathf.Max(1,Mathf.CeilToInt(length/(upper?5:archive?8:14)));float width=length/count;
            for(int i=0;i<count;i++)
            {
                var tile=Child(platform,"Painted ramp stone "+i,new Vector3((i+.5f)*width,0));
                var r=tile.gameObject.AddComponent<SpriteRenderer>();r.sprite=archive||upper?SlabSprite((i+(upper?2:0))%5):bridge;r.sortingOrder=8;
                r.color=archive?new Color(.5f,.72f,.83f):upper?new Color(1,.93f,.8f):Color.white;
                float sx=(width+padding*2)/r.sprite.bounds.size.x;
                tile.localScale=new Vector3(sx,archive?.32f:upper?.24f:sx,1);
            }
            var collider=platform.gameObject.AddComponent<BoxCollider2D>();collider.size=new Vector2(length+padding*2,.25f);collider.offset=new Vector2(length*.5f,-.125f);
            int ground=LayerMask.NameToLayer("Ground");platform.gameObject.layer=ground<0?0:ground;
            return platform;
        }
        static Sprite SlabSprite(int index)
        {
            // Sprite rects reference the original generated texture without changing it.
            string path=Folder+"/Art/WalkwaySlab_"+index+".asset";
            var result=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(result!=null)return result;
            Rect r=bridge.rect;float width=r.width/5;
            result=Sprite.Create(bridge.texture,new Rect(r.x+index*width,r.y,width,r.height),new Vector2(.5f,.735f),100,0,SpriteMeshType.FullRect);
            result.name="Last Wish fractured stone "+index;AssetDatabase.CreateAsset(result,path);return result;
        }
        static Stage3MashGate Gate(Stage3Gameplay game,string name,Stage3Leg phrase,int hits,string action)
        {
            var root=Child(world,name,phrase.from);var gate=root.gameObject.AddComponent<Stage3MashGate>();gate.game=game;
            gate.startTime=phrase.start;gate.releaseTime=phrase.end;gate.requiredHits=hits;gate.actionLabel=action;
            var p=Child(root,"Mash presentation",new Vector3(2,game.footOffset+1.2f));gate.presentation=p.gameObject;
            var n=(GameObject)PrefabUtility.InstantiatePrefab(game.mashPrefab,p);n.transform.localPosition=Vector3.zero;n.transform.localScale=Vector3.one*1.3f;
            gate.note=n.GetComponent<Note>();gate.note.isEventNote=true;
            foreach(var r in n.GetComponentsInChildren<SpriteRenderer>())r.sortingOrder=45;
            foreach(var c in n.GetComponentsInChildren<Collider2D>())c.enabled=false;
            var movement=n.GetComponent<NoteMove>();if(movement!=null)movement.enabled=false;
            gate.rings=Child(p,"Charge rings",Vector3.zero);Ring(gate.rings,"Outer orbit",1.4f,new Color(.8f,.7f,.9f,.55f),.025f,44);
            Ring(gate.rings,"Inner orbit",1.12f,new Color(.55f,.9f,1,.65f),.035f,44);
            var label=Child(p,"Mash input hint",new Vector3(0,1.95f));gate.caption=label.gameObject.AddComponent<TextMeshPro>();
            gate.caption.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            gate.caption.text="MASH  W / S";gate.caption.fontSize=3.2f;gate.caption.alignment=TextAlignmentOptions.Center;gate.caption.rectTransform.sizeDelta=new Vector2(7,1);gate.caption.color=Gold;
            gate.caption.GetComponent<MeshRenderer>().sortingOrder=60;
            p.gameObject.SetActive(false);return gate;
        }
        static Transform Portal(string name,Vector3 position,float radius)
        {
            var root=Child(world,name,position);Ring(root,"Outer dial",radius,Gold,.055f,-4);Ring(root,"Inner dial",radius*.88f,new Color(.48f,.78f,.9f,.7f),.025f,-4);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tick=Rect(root,"Hour "+i,new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius*.93f,new Vector2(.045f,.22f),Gold,-3);tick.transform.localRotation=Quaternion.Euler(0,0,-i*30);}
            root.gameObject.SetActive(false);return root;
        }
    }
}
#endif
