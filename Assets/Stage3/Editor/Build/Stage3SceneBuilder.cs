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
            const string audioPath=Folder+"/Audio/LastWishOriginal.wav";
            var audioImporter=(AudioImporter)AssetImporter.GetAtPath(audioPath);
            var audioSettings=audioImporter.defaultSampleSettings;
            audioSettings.loadType=AudioClipLoadType.DecompressOnLoad;
            audioSettings.compressionFormat=AudioCompressionFormat.PCM;
            audioSettings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            audioSettings.preloadAudioData=true;
            audioImporter.defaultSampleSettings=audioSettings;
            audioImporter.forceToMono=false;audioImporter.loadInBackground=false;
            audioImporter.SaveAndReimport();
            audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
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
            Stage3SceneryBuilder.Configure(square,lines);
            var platforms=Child(world,"New floating walkways",Vector3.zero);
            var mechanisms=Child(world,"Phrase seesaws - varied terrain",Vector3.zero);
            var beams=new List<Transform>();var times=new List<float>();int section=0;
            var events=world.gameObject.AddComponent<Stage3Transitions>();game.transitions=events;events.game=game;
            var pieces=new List<Transform>();var colliders=new List<Collider2D>();
            foreach(var leg in Stage3Route.Legs)
            {
                if(leg.kind==Stage3LegKind.Jump)
                {
                    var pivot=Child(mechanisms,"Phrase seesaw launch",leg.from);
                    if(beams.Count==0)
                    {
                    Line(pivot,"Fulcrum",new[]{new Vector3(-.5f,-.7f),new Vector3(.5f,-.7f),new Vector3(0,-.05f),new Vector3(-.5f,-.7f)},Gold,.065f,14);
                    var beam=Child(pivot,"Resonating beam",new Vector3(0,-.08f));
                    Rect(beam,"Obsidian",Vector2.zero,new Vector2(3,.16f),new Color(.16f,.19f,.3f),16);
                    Rect(beam,"Gilded edge",new Vector2(0,.08f),new Vector2(3,.04f),Gold,17);
                    beams.Add(beam);times.Add(leg.start);
                    }
                    else
                    {
                        for(int k=0;k<3;k++)Line(pivot,"Launch spring",new[]{new Vector3(-1,.05f+k*.15f),new Vector3(0,-.2f+k*.15f),new Vector3(1,.05f+k*.15f)},Gold,.05f,12);
                    }
                    for(int j=1;j<15;j++)
                    {
                        var star=Rect(platforms,"Jump guide",leg.At(Mathf.Lerp(leg.start,leg.end,j/15f))+Vector2.up*.4f,Vector2.one*.08f,Gold,12);star.transform.localRotation=Quaternion.Euler(0,0,45);
                    }
                    continue;
                }
                if(leg.Airborne || leg.kind==Stage3LegKind.Lift)continue;
                if(leg.kind==Stage3LegKind.Gate && Mathf.Abs(leg.start-Stage3Route.FractureStart)<.01f)
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
                Ground(platforms,kind+" walkway "+(++section),p,q,.1f,Stage3Route.Act(leg.start+.01f));
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
                var r=Rect(world,"Falling stone shard "+i,phrases[0].from+new Vector2(-4+i%8,-.3f-(i/8)*.25f),new Vector2(.12f+(i%3)*.15f,.14f+(i%4)*.1f),new Color(.35f,.5f,.69f),15);
                shards.Add(r.transform);r.gameObject.SetActive(false);
            }
            events.debris=shards.ToArray();
            events.portal=Portal("Astral lift",(Vector3)phrases[1].from+new Vector3(2,2.7f),2.2f);
            events.portalExit=Portal("Upper terrace arrival",(Vector3)Stage3Route.Position(Stage3Route.Beat(312))+Vector3.up*2,2.5f);
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
            float routeWidth=Stage3Route.Position(Stage3Route.Duration).x+24;
            for(int i=0;i<800;i++)
            {
                float x=(float)random.NextDouble()*routeWidth-8,y=(float)random.NextDouble()*39-13,s=.018f+(float)random.NextDouble()*.04f;
                var mote=Rect(dust,"Mote",new Vector2(x,y),Vector2.one*s,new Color(.7f,.84f,1,.5f),-8);mote.transform.localRotation=Quaternion.Euler(0,0,45);
            }
            Stage3SceneryBuilder.Build(game,square,lines,
                ImportSprite(Folder+"/Art/ClockworkTheater.png",new Vector2(.5f,.5f)),
                ImportSprite(Folder+"/Art/PaperSky.png",new Vector2(.5f,.5f)));
        }
        static Transform Ground(Transform parent,string name,Vector2 from,Vector2 to,float padding,int act=0)
        {
            // Reverse traversal must not rotate painted terrain upside down.
            if(to.x<from.x){var swap=from;from=to;to=swap;}
            Vector2 delta=to-from;float length=delta.magnitude;
            var platform=Child(parent,name,from);platform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            bool archive=act==2,upper=act>=5;
            int count=Mathf.Max(1,Mathf.CeilToInt(length/(upper?5:archive?8:14)));float width=length/count;
            for(int i=0;i<count;i++)
            {
                if(act==1 || act==3 || act==4 || act>=5)
                {
                    Stage3SceneryBuilder.Platform(platform,act,i*width,width);
                    continue;
                }
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
            gate.rings=Child(p,"Charge geometry",Vector3.zero);
            if(action=="BREAK")
                Line(gate.rings,"Fracture glyph",new[]{new Vector3(-1.3f,1.2f),new Vector3(-.4f,.4f),new Vector3(-.7f,-.1f),new Vector3(.5f,-.5f),new Vector3(1.2f,-1.3f)},new Color(.5f,.86f,1,.8f),.06f,44);
            else if(action=="ASCEND")
                for(int j=0;j<3;j++)Line(gate.rings,"Rising chevron",new[]{new Vector3(-1,j*.5f-.8f),new Vector3(0,j*.5f),new Vector3(1,j*.5f-.8f)},Gold,.06f,44);
            else
                for(int j=0;j<6;j++){float a=j*Mathf.PI/3;Line(gate.rings,"Wish ray",new[]{new Vector3(Mathf.Cos(a),Mathf.Sin(a))*.7f,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*1.5f},Gold,.045f,44);}
            var label=Child(p,"Mash input hint",new Vector3(0,1.95f));gate.caption=label.gameObject.AddComponent<TextMeshPro>();
            gate.caption.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            gate.caption.text="MASH  W / S";gate.caption.fontSize=3.2f;gate.caption.alignment=TextAlignmentOptions.Center;gate.caption.rectTransform.sizeDelta=new Vector2(7,1);gate.caption.color=Gold;
            gate.caption.GetComponent<MeshRenderer>().sortingOrder=60;
            p.gameObject.SetActive(false);return gate;
        }
        static Transform Portal(string name,Vector3 position,float radius)
        {
            var root=Child(world,name,position);
            if(name=="Final wish shockwave")
            {
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/4;var direction=new Vector3(Mathf.Cos(a),Mathf.Sin(a));
                    Line(root,"Radiating wish",new[]{direction*radius*.6f,direction*radius*1.7f},Gold,.055f,-4);
                }
            }
            else
            {
                Line(root,"Diamond transit frame",new[]{Vector3.up*radius,Vector3.right*radius,Vector3.down*radius,Vector3.left*radius,Vector3.up*radius},new Color(.48f,.78f,.9f,.7f),.055f,-4);
            }
            root.gameObject.SetActive(false);return root;
        }
    }
}
#endif
