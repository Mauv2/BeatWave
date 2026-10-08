using System.Collections;
using UnityEngine;

namespace BeatWave.Stage3
{
    // Only the authored route/presentation is Stage3-specific. Input, grading, HUD,
    // pause transitions and results are the original Stage1 components.
    [DefaultExecutionOrder(-150)]
    public sealed class Stage3Gameplay : MonoBehaviour
    {
        public AudioSource music;
        public TextAsset chartFile;
        public GameObject tapPrefab, holdPrefab, mashPrefab;
        public Stage3Transitions transitions;
        public JudgeManager judge;
        public Transform player, visual, upMarker, downMarker, noteRoot;
        public PlayerAnimController playerAnimation;
        public Camera view;
        public SpriteRenderer backdrop;
        public Transform[] seesaws;
        public float[] launchTimes;
        public Transform clockHand;
        public float footOffset;
        RhythmClock clock;
        Vector3 visualScale, upOffset, downOffset;
        bool resultSent;
        public RhythmClock Clock => clock;
        public int NoteCount { get; private set; }

        void Awake()
        {
            clock = RhythmClock.GetOrCreate();
            Stage3Route.ConfigureDuration(music.clip.length);
            visualScale = visual.localScale;
            upOffset = upMarker.localPosition;
            downOffset = downMarker.localPosition;
            music.Stop();
            music.playOnAwake = false;
            Time.timeScale = 1;
        }
        IEnumerator Start()
        {
            var chart = JsonUtility.FromJson<Stage3Chart>(chartFile.text);
            foreach (var item in chart.notes)
            {
                var obj = Instantiate(item.duration > 0 ? holdPrefab : tapPrefab,
                    NotePosition(item.time, item.lane), Quaternion.identity, noteRoot);
                obj.name = (item.duration > 0 ? "Hold " : "Tap ") + item.time.ToString("F3");
                var note = obj.GetComponent<Note>();
                note.line = item.lane == 0 ? NoteLine.Down : NoteLine.Up;
                note.kind = item.duration > 0 ? NoteKind.Hold : NoteKind.Tap;
                note.holdTime = item.duration;
                note.ConfigureTiming(clock, item.time, judge);
                var move = obj.GetComponent<NoteMove>();
                if (move != null) move.enabled = false;
                // Preserve Stage1's sprite, color and size; put it above scenery.
                foreach (var sprite in obj.GetComponentsInChildren<SpriteRenderer>()) sprite.sortingOrder += 40;
                if (item.duration > 0)
                    obj.GetComponent<HoldNoteVisual>().SetHoldPoints(obj.transform.position,
                        NotePosition(item.time + item.duration, item.lane));
                NoteCount++;
            }
            yield return null;
            yield return null;
            clock.BeginSong(music);
        }
        public Vector3 NotePosition(float time, int lane)
        {
            Vector3 offset = lane == 0 ? downOffset : upOffset;
            offset.x = Mathf.Abs(offset.x) * Stage3Route.Facing(time);
            return (Vector3)Stage3Route.Position(time) + Vector3.up * footOffset + offset;
        }
        void Update()
        {
            if (clock == null || !clock.HasStarted || clock.IsPaused || Time.timeScale <= 0) return;
            Present((float)clock.SongTime);
            if (!resultSent && clock.Finished && !judge.HasPendingNotes && (transitions==null || transitions.AllResolved))
            {
                resultSent = true;
                playerAnimation.SetRunning(false);
                judge.SaveResultAndGoToResult();
            }
        }
        public void Present(float time)
        {
            player.position = (Vector3)Stage3Route.Position(time) + Vector3.up * footOffset;
            float facing = Stage3Route.Facing(time);
            visual.localScale = new Vector3(Mathf.Abs(visualScale.x) * facing, visualScale.y, visualScale.z);
            upMarker.localPosition = new Vector3(Mathf.Abs(upOffset.x) * facing, upOffset.y, upOffset.z);
            downMarker.localPosition = new Vector3(Mathf.Abs(downOffset.x) * facing, downOffset.y, downOffset.z);
            var leg=Stage3Route.Leg(time);
            bool jumping = leg.Airborne;
            playerAnimation.SetFlying(jumping);
            playerAnimation.SetRunning(!jumping && leg.kind!=Stage3LegKind.Gate);
            bool gateActive=transitions!=null && transitions.ActiveGate!=null;
            upMarker.gameObject.SetActive(!gateActive);downMarker.gameObject.SetActive(!gateActive);
            for (int i = 0; i < seesaws.Length; i++)
            {
                float dt = time - launchTimes[i];
                float tilt = dt < 0 ? Mathf.Clamp01((dt + .55f) / .55f) * -10 : Mathf.Exp(-dt * 2.4f) * Mathf.Sin(dt * 12) * 26;
                seesaws[i].localRotation = Quaternion.Euler(0, 0, tilt * Stage3Route.Facing(launchTimes[i]));
            }
            if (clockHand != null) clockHand.localRotation = Quaternion.Euler(0, 0, -time * 8);
        }
        void LateUpdate()
        {
            if (clock == null) return;
            float time = (float)clock.SongTime;
            // The lookahead is sampled on the same route, so the camera rises before
            // each leap and turns with the short leftward passage.
            float facing = Stage3Route.Facing(time + .35f);
            Vector2 ahead = Stage3Route.Position(time + .35f);
            Vector3 target = new Vector3(ahead.x + facing * 3, ahead.y + 3.2f, -10);
            float smooth = 1 - Mathf.Exp(-4.5f * Time.deltaTime);
            float size = Stage3Route.Leg(time).Airborne ? 8.4f : 6.6f;
            if(transitions!=null)
            {
                Vector3 focus;float amount=transitions.Focus(time,out focus);
                target=Vector3.Lerp(target,focus,amount);
                size=Mathf.Lerp(size,3.9f,amount);
            }
            view.transform.position = Vector3.Lerp(view.transform.position, target, smooth);
            view.orthographicSize = Mathf.Lerp(view.orthographicSize, size, smooth);
            // Overscan keeps the entire viewport covered while allowing slow parallax.
            float scale = Mathf.Max(view.orthographicSize * 2.55f / backdrop.sprite.bounds.size.y,
                view.orthographicSize * 2.55f * view.aspect / backdrop.sprite.bounds.size.x);
            backdrop.transform.localScale = Vector3.one * scale;
            backdrop.transform.position = new Vector3(view.transform.position.x - Mathf.Sin(time * .035f) * 1.8f,
                view.transform.position.y + 1, 20);
            Color night = new Color(.48f, .55f, .8f);
            Color dawn = new Color(1f, .9f, .86f);
            backdrop.color = Color.Lerp(night, dawn, Mathf.SmoothStep(0, 1, time / music.clip.length));
        }
    }
}
