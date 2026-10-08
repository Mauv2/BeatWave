using UnityEngine;
using TMPro;

namespace BeatWave.Stage3
{
    // Musical gate: the song keeps playing. The route waits for a fixed phrase,
    // then transitions on its final beat whether the player succeeds or misses.
    public sealed class Stage3MashGate : MonoBehaviour
    {
        public Stage3Gameplay game;
        public float startTime, releaseTime;
        public int requiredHits=8;
        public string actionLabel="BREAK";
        public GameObject presentation;
        public Note note;
        public TMP_Text caption;
        public Transform rings;
        public int Hits { get; private set; }
        public int HitEffectsSpawned { get; private set; }
        public bool Resolved { get; private set; }
        public bool Success { get; private set; }
        public float LastPressTime { get; private set; }=-100;
        public bool Active => game.Clock!=null && game.Clock.HasStarted && game.Clock.SongTime>=startTime && game.Clock.SongTime<releaseTime;
        public float Charge => Mathf.Clamp01((float)Hits/requiredHits);
        void Start()
        {
            note.isEventNote=true;
            note.kind=NoteKind.Mash;
            note.needHitCount=requiredHits;
            // Stage1's ordinary Mash pauses audio; these transition notes own a
            // reserved phrase and report exactly one result to its same judge.
            note.ConfigureTiming(game.Clock,startTime,null);
        }
        void Update()
        {
            if(game.Clock==null||!game.Clock.HasStarted||game.Clock.IsPaused||Time.timeScale<=0)return;
            if(Active)
            {
                if(Input.GetKeyDown(KeyCode.W))Press();
                if(Input.GetKeyDown(KeyCode.S))Press();
            }
            float time=(float)game.Clock.SongTime;
            if(!Resolved && time>=releaseTime)Resolve();
            bool visible=time>=startTime-2 && time<releaseTime+.65f;
            presentation.SetActive(visible);
            if(!visible)return;
            // The instruction stays constant; hit totals and completion thresholds
            // are internal judging data, never a counter or filled progress ring.
            caption.text=Resolved ? "" : "MASH  W / S";
            rings.localRotation=Quaternion.Euler(0,0,-time*35);
            float pulse=Mathf.Exp(-Mathf.Max(0,time-LastPressTime)*12);
            note.transform.localScale=Vector3.one*(1.3f+.16f*pulse);
            note.isMashActive=Active;note.currentHitCount=Hits;
            rings.localScale=Vector3.one*(1+.09f*pulse);
        }
        public bool Press()
        {
            if(!Active||Resolved||game.Clock.IsPaused||Time.timeScale<=0)return false;
            Hits++;LastPressTime=(float)game.Clock.SongTime;
            game.playerAnimation.PlayInteract();
            if(game.judge.hitEffectPrefab!=null)
            {
                var effect=Instantiate(game.judge.hitEffectPrefab,note.transform.position,Quaternion.identity);
                effect.name="Stage3 Mash impact";
                effect.transform.localScale*=.55f;
                foreach(var particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main=particles.main;
                    main.startColor=Hits%2==0?new Color(.45f,.85f,1):new Color(1,.83f,.48f);
                }
                foreach(var renderer in effect.GetComponentsInChildren<Renderer>(true))renderer.sortingOrder=70;
                Destroy(effect,.6f);
                HitEffectsSpawned++;
            }
            return true;
        }
        void Resolve()
        {
            Resolved=true;Success=Hits>=requiredHits;note.isJudged=true;
            game.judge.ReportEventJudge(Success?"Perfect":"Miss",note.transform.position);
        }
    }
}
