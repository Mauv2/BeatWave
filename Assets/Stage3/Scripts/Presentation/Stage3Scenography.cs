using UnityEngine;

namespace BeatWave.Stage3
{
    // Everything samples song time, including the scenery. Pause/retry therefore
    // cannot leave a moving stage prop ahead of the player or the music.
    [DefaultExecutionOrder(200)]
    public sealed class Stage3Scenography : MonoBehaviour
    {
        public Stage3Gameplay game;
        public SpriteRenderer[] backdrops;
        public Transform[] gears, paperSlips;
        public Transform pendulum, lift, water, eclipse;
        public Transform[] actSets;
        public Transform[] liftDoors;
        public Transform[] finalWings;
        static readonly int[] Background={0,1,2,2,3,3,3};
        static readonly Color[] Tint={new Color(.5f,.57f,.82f),new Color(.9f,.75f,.65f),
            new Color(.55f,.83f,.96f),new Color(.66f,.5f,.86f),new Color(.15f,.19f,.3f),
            new Color(.45f,.56f,.64f),new Color(.55f,.62f,.67f)};
        public int CurrentAct => Stage3Route.Act((float)game.Clock.SongTime);
        public float LiftProgress => Mathf.InverseLerp(Stage3Route.Beat(152),Stage3Route.Beat(168),(float)game.Clock.SongTime);

        void LateUpdate()
        {
            if(game.Clock==null || game.Clock.IsPaused || Time.timeScale<=0)return;
            Present((float)game.Clock.SongTime);
        }
        public void Present(float time)
        {
            int act=Stage3Route.Act(time),previous=Mathf.Max(0,act-1);
            float blend=act==0?1:Mathf.SmoothStep(0,1,Mathf.InverseLerp(Stage3Route.Beat(Stage3Route.ActBeats[act]),Stage3Route.Beat(Stage3Route.ActBeats[act])+1.2f,time));
            for(int i=0;i<backdrops.Length;i++)
            {
                var b=backdrops[i];
                float alpha=(Background[previous]==i?1-blend:0)+(Background[act]==i?blend:0);
                // Keep the outgoing image opaque under the incoming layer.
                if(Background[previous]!=Background[act] && i==Background[previous])alpha=blend<1?1:0;
                var tint=Color.Lerp(Tint[previous],Tint[act],blend);tint.a=alpha;b.color=tint;
                b.sortingOrder=i==Background[act]?-98:-100;
                float cover=Mathf.Max(game.view.orthographicSize*2.3f/b.sprite.bounds.size.y,
                    game.view.orthographicSize*2.3f*game.view.aspect/b.sprite.bounds.size.x);
                b.transform.localScale=Vector3.one*cover;
                b.transform.position=game.view.transform.position+new Vector3(Mathf.Sin(time*.025f)*1.5f,1,30);
            }
            for(int i=0;i<actSets.Length;i++)
            {
                float begin=i==0?0:Stage3Route.Beat(Stage3Route.ActBeats[i])-2;
                float end=i+1<Stage3Route.ActBeats.Length?Stage3Route.Beat(Stage3Route.ActBeats[i+1])+2:Stage3Route.Duration+1;
                actSets[i].gameObject.SetActive(time>=begin && time<end);
            }
            for(int i=0;i<gears.Length;i++)gears[i].localRotation=Quaternion.Euler(0,0,time*(i%2==0?12:-18));
            pendulum.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*Mathf.PI*Stage3Route.Bpm/120)*23);
            lift.position=Stage3Route.Position(Mathf.Clamp(time,Stage3Route.Beat(152),Stage3Route.Beat(168)));
            water.position=new Vector3(game.view.transform.position.x,-9.5f+Mathf.Sin(time*.7f)*.12f,0);
            eclipse.localScale=Vector3.one*(1+.025f*Mathf.Sin(time*Mathf.PI*Stage3Route.Bpm/30));
            float opening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Stage3Route.LiftStart,Stage3Route.LiftRelease+.8f,time));
            for(int i=0;i<liftDoors.Length;i++)liftDoors[i].localPosition=new Vector3((i==0?-1:1)*(1.7f+opening*7),2.5f,0);
            float unfurl=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Stage3Route.FinaleStart,Stage3Route.FinaleRelease+.8f,time));
            for(int i=0;i<finalWings.Length;i++)finalWings[i].localRotation=Quaternion.Euler(0,0,(i==0?-1:1)*Mathf.Lerp(75,8,unfurl));
            for(int i=0;i<paperSlips.Length;i++)
            {
                float seed=i*.618034f;
                float x=Mathf.Repeat(seed*41+time*.9f,44)-22;
                float y=Mathf.Repeat(seed*23+time*(.22f+(i%3)*.08f),26)-13;
                paperSlips[i].position=game.view.transform.position+new Vector3(x,y,10);
                paperSlips[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*.7f+i)*35);
                paperSlips[i].gameObject.SetActive(act>=4);
            }
        }
    }
}
