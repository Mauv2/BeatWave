using UnityEngine;

/// <summary>Shows the landing ahead while keeping the HUD in screen space.</summary>
[DefaultExecutionOrder(500)]
public class Stage1CameraDirector : MonoBehaviour
{
    public PlayerMove player;
    public float lookAhead = 4.2f;
    public float heightOffset = 2.9f;
    public float normalSize = 6.5f;
    public float jumpSize = 7.2f;
    Camera view;
    float depth;
    Vector3 velocity;
    RhythmClock clock;
    void Awake()
    {
        view = GetComponent<Camera>();
        depth = transform.position.z;
        transform.SetParent(null, true);
        clock = RhythmClock.GetOrCreate();
    }
    void LateUpdate()
    {
        if (player == null || view == null) return;
        if (clock != null && (clock.MenuPaused || (clock.FlowPaused && clock.TravelEnabled))) return;
        Vector3 target = player.transform.position;
        float emphasis = 0;
        float direction = 1;
        if (player.Path != null && clock != null && clock.TravelEnabled)
        {
            direction = player.Path.DirectionAt(clock.SongTime + 0.2);
            Vector2 ahead = player.Path.Evaluate(clock.SongTime + 0.25);
            target.y = Mathf.Lerp(target.y, ahead.y, 0.35f);
            foreach (var cue in player.Path.Jumps)
            {
                double t = clock.SongTime;
                if (t >= cue.time - 0.6 && t <= cue.endTime + 0.4)
                    emphasis = Mathf.Max(emphasis, Mathf.Clamp01((float)System.Math.Min((t - cue.time + 0.6) / 0.6, (cue.endTime + 0.4 - t) / 0.4)));
            }
        }
        bool switchback = player.switchback != null && clock != null &&
            clock.SongTime >= player.switchback.StartTime - 0.4 && clock.SongTime <= player.switchback.EndTime + 0.3;
        target += new Vector3(lookAhead * direction, heightOffset, 0);
        if(switchback)
        {
            // Frame the landing on each authored leg before it reaches the screen edge.
            foreach(var leg in player.switchback.legs)
                if(clock.SongTime <= leg.endTime)
                {
                    if(leg.airborne) target = (Vector3)Vector2.Lerp(player.transform.position,leg.to,0.3f)+Vector3.up*2f;
                    break;
                }
        }
        target.z = depth;
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, 0.18f);
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, (switchback ? 8.5f : Mathf.Lerp(normalSize, jumpSize, emphasis)), 1 - Mathf.Exp(-Time.unscaledDeltaTime * 8));
    }
}