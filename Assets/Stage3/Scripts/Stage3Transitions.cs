using UnityEngine;
using UnityEngine.UI;

namespace BeatWave.Stage3
{
    public sealed class Stage3Transitions : MonoBehaviour
    {
        public Stage3Gameplay game;
        public Stage3MashGate[] gates;
        public Transform[] collapsePieces, debris;
        public Collider2D[] collapseColliders;
        public Transform portal, portalExit, finale;
        public SpriteRenderer depths;
        public Image wash;
        Vector3[] pieceOrigins,debrisOrigins;
        public bool AllResolved { get {foreach(var gate in gates)if(!gate.Resolved)return false;return true;} }
        public Stage3MashGate ActiveGate {get{foreach(var gate in gates)if(gate.Active)return gate;return null;}}
        void Awake()
        {
            pieceOrigins=new Vector3[collapsePieces.Length];for(int i=0;i<pieceOrigins.Length;i++)pieceOrigins[i]=collapsePieces[i].position;
            debrisOrigins=new Vector3[debris.Length];for(int i=0;i<debris.Length;i++)debrisOrigins[i]=debris[i].position;
        }
        void Update()
        {
            if(game.Clock==null||!game.Clock.HasStarted||game.Clock.IsPaused||Time.timeScale<=0)return;
            Present((float)game.Clock.SongTime);
        }
        public void Present(float time)
        {
            float collapse=time-gates[0].releaseTime;
            for(int i=0;i<collapsePieces.Length;i++)
            {
                float dt=Mathf.Max(0,collapse-i*.07f);
                float shake=gates[0].Active?Mathf.Sin(time*43+i)*(.012f+.045f*gates[0].Charge):0;
                collapsePieces[i].position=pieceOrigins[i]+new Vector3(shake+(i-2)*dt*.36f,-4.7f*dt*dt,0);
                collapsePieces[i].rotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*dt*26);
                collapsePieces[i].gameObject.SetActive(dt<3.2f);
                collapseColliders[i].enabled=collapse<0;
            }
            for(int i=0;i<debris.Length;i++)
            {
                float dt=collapse-(i%4)*.08f;
                debris[i].gameObject.SetActive(dt>=0&&dt<3.2f);
                if(dt<0)continue;
                debris[i].position=debrisOrigins[i]+new Vector3((i%7-3)*dt*.9f,dt*(1+i%3)-5*dt*dt,0);
                debris[i].rotation=Quaternion.Euler(0,0,i*25+dt*(i%2==0?100:-120));
            }
            float transfer=time-gates[1].releaseTime;
            portal.gameObject.SetActive(time>=gates[1].startTime-2 && transfer<3.4f);
            portalExit.gameObject.SetActive(time>=gates[1].startTime && transfer<3.4f);
            portal.localScale=Vector3.one*(1+gates[1].Charge*.4f+Mathf.Max(0,transfer)*.25f);
            portal.localRotation=Quaternion.Euler(0,0,-time*40);
            portalExit.localRotation=Quaternion.Euler(0,0,time*30);
            float end=time-gates[2].releaseTime;
            finale.gameObject.SetActive(time>=gates[2].startTime-1);
            finale.localScale=Vector3.one*(1+gates[2].Charge*.2f+Mathf.Max(0,end)*3.5f);
            finale.localRotation=Quaternion.Euler(0,0,time*12);
            float flash=0;
            for(int i=0;i<gates.Length;i++)
            {
                float dt=time-gates[i].releaseTime;
                if(dt>=0)flash=Mathf.Max(flash,Mathf.Exp(-dt*5)*(gates[i].Success?.5f:.18f));
            }
            wash.color=new Color(.7f,.88f,1,flash);
        }
        public float Focus(float time,out Vector3 target)
        {
            foreach(var gate in gates)
            {
                float fadeIn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(gate.startTime-.65f,gate.startTime+.25f,time));
                float fadeOut=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(gate.releaseTime,gate.releaseTime+.75f,time));
                if(time>=gate.startTime-.65f && time<gate.releaseTime+.75f)
                { target=(Vector3)Stage3Route.Position(gate.startTime)+new Vector3(1.1f,2.4f,-10);return fadeIn*fadeOut; }
            }
            target=Vector3.zero;return 0;
        }
        void LateUpdate()
        {
            if(game.Clock==null)return;
            float time=(float)game.Clock.SongTime;
            depths.transform.position=game.backdrop.transform.position+Vector3.back*.2f;
            depths.transform.localScale=game.backdrop.transform.localScale;
            float enter=Mathf.SmoothStep(0,1,Mathf.InverseLerp(gates[0].releaseTime,gates[0].releaseTime+1.1f,time));
            float leave=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(gates[1].releaseTime,gates[1].releaseTime+1.2f,time));
            depths.color=new Color(.7f,.85f,1,enter*leave);
        }
    }
}
