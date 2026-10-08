using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
namespace BeatWave.Stage3
{
    public sealed class Stage3Director : MonoBehaviour
    {
        public AudioSource music;
        public TextAsset chartFile;
        public Transform player,visual;
        public Animator animator;
        public Camera view;
        public Sprite shape;
        public Material lineMaterial;
        public SpriteRenderer[] backdrops;
        public Transform[] seesaws;
        public float[] seesawTimes;
        public Transform clockHand;
        public TMP_Text scoreLabel,comboLabel,sectionLabel,feedbackLabel,timeLabel,healthLabel;
        public TMP_Text overlayTitle,overlayCopy,startLabel,demoLabel;
        public GameObject overlay;
        public Button startButton,demoButton,exitButton;
        public Image healthFill,progressFill;
        public Slider volumeSlider,offsetSlider;
        public TMP_Text offsetLabel;
        public Stage3Session Session {get;private set;}
        public Stage3Chart Chart {get;private set;}
        public float SongTime {get;private set;}
        public bool IsRunning {get;private set;}
        public bool IsPaused {get;private set;}
        public bool IsDemo {get;private set;}
        public bool Finished {get;private set;}
        double epoch;
        float feedbackAge=10,pausedTime;
        Vector3 visualScale;
        readonly List<NoteView> notes=new List<NoteView>();
        readonly List<Transform> markers=new List<Transform>();
        readonly List<LineRenderer> pulses=new List<LineRenderer>();
        readonly float[] pulseAge={10,10};
        readonly Color low=new Color(.22f,1,.9f),high=new Color(1,.38f,.83f);
        static readonly string[] Sections={"I  /  THE MOONLIT BRIDGE","II  /  STAIRWAY OF WISHES","III  /  THE FALLEN CHAPEL","IV  /  THE LAST CLOCK"};
        class NoteView {public GameObject root;public LineRenderer ring,tail;public Transform core;}
        void Awake()
        {
            Time.timeScale=1;
            if(chartFile==null||music==null||music.clip==null){Debug.LogError("Stage3 requires chart and music.",this);enabled=false;return;}
            Chart=JsonUtility.FromJson<Stage3Chart>(chartFile.text);
            Session=new Stage3Session(Chart);Session.Judged+=OnJudged;
            visualScale=visual.localScale;
            music.playOnAwake=false;music.loop=false;music.Stop();music.volume=PlayerPrefs.GetFloat("Stage3.Volume",.7f);
            volumeSlider.SetValueWithoutNotify(music.volume);
            volumeSlider.onValueChanged.AddListener(v=>{music.volume=v;PlayerPrefs.SetFloat("Stage3.Volume",v);});
            offsetSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Stage3.Offset",0));
            offsetSlider.onValueChanged.AddListener(v=>{PlayerPrefs.SetFloat("Stage3.Offset",v);RefreshOffset();});RefreshOffset();
            startButton.onClick.AddListener(StartOrResume);demoButton.onClick.AddListener(DemoOrRetry);exitButton.onClick.AddListener(Leave);
            for(int lane=0;lane<2;lane++)
            {
                var m=new GameObject(lane==0?"D - Cyan judgement":"F - Pink judgement").transform;
                Stage3Drawing.Ring(m,"Judgement ring",.32f,LaneColor(lane),.025f,40,lineMaterial);
                Stage3Drawing.Rect(m,"Centre",Vector2.zero,Vector2.one*.07f,Color.white,41,shape);
                markers.Add(m);
                var pulse=Stage3Drawing.Ring(null,"Hit pulse",.5f,LaneColor(lane),.06f,43,lineMaterial);pulses.Add(pulse);pulse.gameObject.SetActive(false);
            }
            foreach(var n in Chart.notes)
            {
                var root=new GameObject("Note "+n.time.ToString("F3")+" "+(n.lane==0?"D":"F"));root.transform.position=Stage3Route.Marker(n.time,n.lane);
                var core=Stage3Drawing.Rect(root.transform,"Crystal",Vector2.zero,Vector2.one*.31f,LaneColor(n.lane),36,shape).transform;core.localRotation=Quaternion.Euler(0,0,45);
                Stage3Drawing.Rect(root.transform,"Gleam",new Vector2(-.03f,.05f),new Vector2(.065f,.18f),Color.white,37,shape).transform.localRotation=Quaternion.Euler(0,0,45);
                var ring=Stage3Drawing.Ring(root.transform,"Approach",.36f,LaneColor(n.lane),.023f,35,lineMaterial);
                LineRenderer tail=null;
                if(n.duration>0)
                {
                    var points=new Vector3[28];for(int j=0;j<points.Length;j++)points[j]=Stage3Route.Marker(n.time+n.duration*j/(points.Length-1),n.lane);
                    tail=Stage3Drawing.Line(root.transform,"Hold ribbon",points,LaneColor(n.lane)*new Color(1,1,1,.65f),.08f,34,lineMaterial,true);
                    var end=Stage3Drawing.Ring(root.transform,"Release",.19f,LaneColor(n.lane),.04f,36,lineMaterial);end.transform.position=points[points.Length-1];
                }
                notes.Add(new NoteView{root=root,ring=ring,tail=tail,core=core});root.SetActive(false);
            }
            SongTime=0;UpdateWorld(true);UpdateHud();
        }
        Color LaneColor(int lane)=>lane==0?low:high;
        void RefreshOffset(){offsetLabel.text="TIMING  "+offsetSlider.value.ToString("+0;-0;0")+" ms";}
        public void StartRun(bool demo)
        {
            if(IsRunning||Finished)return;
            IsDemo=demo;IsRunning=true;IsPaused=false;overlay.SetActive(false);SongTime=-2;
            epoch=AudioSettings.dspTime+2;music.time=0;music.PlayScheduled(epoch);
            animator.SetBool("isRunning",true);
        }
        void StartOrResume(){if(Finished){Retry();return;}if(IsPaused){Pause(false);return;}StartRun(false);}
        void DemoOrRetry(){if(IsRunning||Finished)Retry();else StartRun(true);}
        void Update()
        {
            if(!IsRunning)
            {
                if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)){if(Finished)Retry();else StartRun(false);}
                if(Finished&&Input.GetKeyDown(KeyCode.R))Retry();return;
            }
            if(Input.GetKeyDown(KeyCode.Escape)&&SongTime>=0)Pause(!IsPaused);
            if(IsPaused){if(Input.GetKeyDown(KeyCode.R))Retry();return;}
            SongTime=(float)(AudioSettings.dspTime-epoch);
            float judgedTime=SongTime+offsetSlider.value*.001f;
            if(IsDemo)
            {
                for(int i=0;i<Chart.notes.Length;i++)if(Session.States[i]==NoteState.Pending&&judgedTime>=Chart.notes[i].time)Session.Press(Chart.notes[i].lane,Chart.notes[i].time);
            }
            else
            {
                if(Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.DownArrow))Session.Press(0,judgedTime);
                if(Input.GetKeyDown(KeyCode.F)||Input.GetKeyDown(KeyCode.UpArrow))Session.Press(1,judgedTime);
            }
            Session.Tick(judgedTime,IsDemo||Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.DownArrow),IsDemo||Input.GetKey(KeyCode.F)||Input.GetKey(KeyCode.UpArrow));
            UpdateWorld(false);UpdateHud();
            if(Session.Failed)EndRun(false);
            else if(SongTime>=music.clip.length+.2f)EndRun(true);
        }
        public void Pause(bool pause)
        {
            if(!IsRunning||SongTime<0||pause==IsPaused)return;
            IsPaused=pause;animator.speed=pause?0:1;
            if(pause)
            {
                pausedTime=SongTime;music.Pause();overlay.SetActive(true);overlayTitle.text="INTERMISSION";
                overlayCopy.text="Your place in the music is saved.\nD / DOWN  -  cyan     F / UP  -  pink\nKeep the key pressed through a hold ribbon.";
                startLabel.text="RESUME";demoLabel.text="RETRY";
            }
            else {epoch=AudioSettings.dspTime-pausedTime;music.UnPause();overlay.SetActive(false);}
        }
        void EndRun(bool clear)
        {
            IsRunning=false;Finished=true;music.Stop();animator.SetBool("isRunning",false);animator.SetBool("isFlying",false);
            string rank=Session.Accuracy>=97?"S":Session.Accuracy>=90?"A":Session.Accuracy>=75?"B":"C";
            if(!IsDemo&&clear){int best=PlayerPrefs.GetInt("Stage3.Best",0);PlayerPrefs.SetInt("Stage3.Best",Mathf.Max(best,Session.Score));PlayerPrefs.Save();}
            overlay.SetActive(true);overlayTitle.text=IsDemo?"DEMO COMPLETE":clear?"WISH FULFILLED  /  "+rank:"WISH INTERRUPTED";
            overlayCopy.text=(IsDemo?"DEMONSTRATION  -  score is not saved\n":"")+"SCORE  "+Session.Score.ToString("N0")+"     ACCURACY  "+Session.Accuracy.ToString("F1")+"%\nPERFECT  "+Session.Perfect+"    GOOD  "+Session.Good+"    MISS  "+Session.Miss+"\nMAX COMBO  "+Session.MaxCombo+"     BEST  "+PlayerPrefs.GetInt("Stage3.Best",0).ToString("N0");
            startLabel.text="PLAY AGAIN";demoLabel.text="RETRY";sectionLabel.text=clear?"FINALE  /  THANK YOU FOR PLAYING":"PRESS ENTER TO RETRY";
        }
        void Retry(){music.Stop();Time.timeScale=1;SceneManager.LoadScene("Stage3");}
        void Leave(){music.Stop();Time.timeScale=1;if(Application.CanStreamedLevelBeLoaded("Select"))SceneManager.LoadScene("Select");else Retry();}
        void OnDestroy(){if(Session!=null)Session.Judged-=OnJudged;}
        void OnApplicationFocus(bool focused){if(!focused&&IsRunning&&!IsPaused&&SongTime>=0)Pause(true);}
        void OnJudged(int i,string label)
        {
            feedbackLabel.text=label;feedbackLabel.color=label=="MISS"?new Color(1,.45f,.43f):LaneColor(Chart.notes[i].lane);feedbackAge=0;
            if(label!="MISS")pulseAge[Chart.notes[i].lane]=0;
        }
        void UpdateHud()
        {
            scoreLabel.text=Session.Score.ToString("D7");comboLabel.text=Session.Combo.ToString("D3")+" <size=16>COMBO</size>";
            healthFill.fillAmount=Session.Health/100;healthFill.color=Session.Health<30?new Color(1,.33f,.4f):low;
            healthLabel.text="RESONANCE  "+Mathf.CeilToInt(Session.Health)+"%";
            progressFill.fillAmount=Mathf.Clamp01(SongTime/music.clip.length);
            timeLabel.text=Mathf.Max(0,SongTime).ToString("00.0")+" / "+music.clip.length.ToString("00.0");
            if(SongTime<0&&IsRunning)sectionLabel.text="READY  "+Mathf.CeilToInt(-SongTime);
            else sectionLabel.text=(IsDemo?"DEMO  /  ":"")+Sections[Stage3Route.Section(Mathf.Max(0,SongTime))];
        }
        void UpdateWorld(bool snap)
        {
            float t=Mathf.Max(0,SongTime);var leg=Stage3Route.Leg(t);player.position=Stage3Route.Position(t);
            float direction=Stage3Route.Facing(t);visual.localScale=new Vector3(visualScale.x*direction,visualScale.y,visualScale.z);
            animator.SetBool("isFlying",leg.arc>0&&IsRunning);animator.SetBool("isRunning",IsRunning);
            Vector3 target=(Vector3)Stage3Route.Position(t+.4f)+new Vector3(direction*3.2f,2.9f,-10);
            view.transform.position=snap?target:Vector3.Lerp(view.transform.position,target,1-Mathf.Exp(-6*Time.unscaledDeltaTime));
            float desiredSize=leg.arc>0?7.5f:6.4f;view.orthographicSize=snap?desiredSize:Mathf.Lerp(view.orthographicSize,desiredSize,1-Mathf.Exp(-2*Time.unscaledDeltaTime));
            int section=Stage3Route.Section(t);
            for(int i=0;i<backdrops.Length;i++)
            {
                var b=backdrops[i];float a=i==section?1:0;
                var c=b.color;c.a=snap?a:Mathf.MoveTowards(c.a,a,Time.unscaledDeltaTime*.7f);b.color=c;
                b.transform.position=new Vector3(view.transform.position.x,view.transform.position.y+1,5+i*.1f);
                float cover=Mathf.Max(view.orthographicSize*2.35f/b.sprite.bounds.size.y,view.orthographicSize*view.aspect*2.35f/b.sprite.bounds.size.x);
                b.transform.localScale=Vector3.one*cover;
            }
            for(int lane=0;lane<2;lane++)
            {
                var point=Stage3Route.Marker(t,lane);markers[lane].position=point;
                pulseAge[lane]+=Time.unscaledDeltaTime;bool active=pulseAge[lane]<.35f;pulses[lane].gameObject.SetActive(active);
                if(active){pulses[lane].transform.position=point;pulses[lane].transform.localScale=Vector3.one*(.6f+pulseAge[lane]*3);Color c=LaneColor(lane);c.a=1-pulseAge[lane]/.35f;pulses[lane].startColor=pulses[lane].endColor=c;}
            }
            for(int i=0;i<notes.Count;i++)
            {
                var n=Chart.notes[i];var v=notes[i];var state=Session.States[i];
                bool visible=state!=NoteState.Hit&&state!=NoteState.Miss&&n.time-t<3.0f&&n.time+n.duration-t>-.3f;
                if(v.root.activeSelf!=visible)v.root.SetActive(visible);
                if(!visible)continue;
                v.ring.transform.localScale=Vector3.one*Mathf.Lerp(1,2.6f,Mathf.Clamp01((n.time-t)/2.5f));
                if(state==NoteState.Holding)
                {
                    v.core.position=Stage3Route.Marker(t,n.lane);v.ring.transform.position=v.core.position;
                    for(int j=0;j<v.tail.positionCount;j++)v.tail.SetPosition(j,Stage3Route.Marker(Mathf.Lerp(t,n.time+n.duration,j/(v.tail.positionCount-1f)),n.lane));
                }
            }
            for(int i=0;i<seesaws.Length;i++)
            {
                float u=Mathf.Clamp01((t-seesawTimes[i])/.24f);float d=Stage3Route.Facing(seesawTimes[i]+.01f);
                seesaws[i].localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-d*12,d*16,u));
            }
            if(clockHand!=null)clockHand.localRotation=Quaternion.Euler(0,0,-t*18);
            feedbackAge+=Time.unscaledDeltaTime;var fc=feedbackLabel.color;fc.a=Mathf.Clamp01(1-feedbackAge/0.65f);feedbackLabel.color=fc;
        }
    }
    public static class Stage3Drawing
    {
        public static SpriteRenderer Rect(Transform parent,string name,Vector2 pos,Vector2 size,Color color,int order,Sprite sprite)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=new Vector3(size.x,size.y,1);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;return r;
        }
        public static LineRenderer Line(Transform parent,string name,Vector3[] points,Color color,float width,int order,Material material,bool world=false)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=world;
            line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;line.sortingOrder=order;line.numCapVertices=4;line.numCornerVertices=3;return line;
        }
        public static LineRenderer Ring(Transform parent,string name,float radius,Color color,float width,int order,Material material)
        {
            var pts=new Vector3[49];for(int i=0;i<pts.Length;i++){float a=i*Mathf.PI*2/(pts.Length-1);pts[i]=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius;}
            return Line(parent,name,pts,color,width,order,material);
        }
    }
}
