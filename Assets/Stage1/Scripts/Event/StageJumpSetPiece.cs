using UnityEngine;

/// <summary>Runs from the chart trajectory, so a missed note never cancels a set piece.</summary>
[DefaultExecutionOrder(200)]
public class StageJumpSetPiece : MonoBehaviour
{
    public Transform beam, counterweight, launchRing;
    public SpriteRenderer glow;
    public Transform[] pathStars;
    public string sectionName;
    public float launchDirection = -1f;
    public Color accent = new Color(0.25f, 0.9f, 0.85f);
    RhythmClock clock;
    RhythmPath path;
    double cueTime, landingTime;
    bool bound;
    public bool HasLaunched { get; private set; }
    public double CueTime => cueTime;
    public double LandingTime => landingTime;
    public void Bind(RhythmPath trajectory, RhythmPath.JumpCue cue)
    {
        path = trajectory;
        clock = RhythmClock.GetOrCreate();
        cueTime = cue.time;
        landingTime = cue.endTime;
        bound = true;
        HasLaunched = false;
        if (pathStars == null) return;
        for (int i = 0; i < pathStars.Length; i++)
        {
            if (pathStars[i] == null) continue;
            double time = cueTime + (landingTime - cueTime) * (i + 1d) / (pathStars.Length + 1d);
            pathStars[i].position = (Vector3)path.Evaluate(time) + Vector3.up * 0.45f;
        }
        Evaluate(cueTime - 1);
    }
    void Update()
    {
        if (!bound || clock == null || !clock.HasStarted) return;
        Evaluate(clock.SongTime);
    }
    public void Evaluate(double songTime)
    {
        float elapsed = (float)(songTime - cueTime);
        float anticipation = Mathf.Clamp01((elapsed + 0.55f) / 0.55f);
        float kick = elapsed >= 0 ? Mathf.Exp(-elapsed * 3.5f) : 0;
        float settle = elapsed >= 0 ? Mathf.Sin(elapsed * 13) * Mathf.Exp(-elapsed * 4) : 0;
        HasLaunched = elapsed >= 0;
        if (beam != null)
            beam.localRotation = Quaternion.Euler(0, 0, -launchDirection * (elapsed < 0 ? Mathf.Lerp(7, 11, anticipation) : -14 * kick + 3 * settle));
        if (counterweight != null && beam != null)
            counterweight.localRotation = Quaternion.Euler(0, 0, -beam.localEulerAngles.z);
        if (launchRing != null)
        {
            float scale = elapsed < 0 ? Mathf.Lerp(0.3f, 0.65f, anticipation) : Mathf.Lerp(0.65f, 2.1f, Mathf.Clamp01(elapsed / 0.4f));
            launchRing.localScale = Vector3.one * scale;
            launchRing.gameObject.SetActive(elapsed > -0.55f && elapsed < 0.45f);
        }
        if (glow != null)
        {
            var color = accent;
            color.a = elapsed < 0 ? 0.12f + anticipation * 0.2f : kick * 0.45f;
            glow.color = color;
        }
        if (pathStars != null)
            for (int i = 0; i < pathStars.Length; i++)
            {
                if (pathStars[i] == null) continue;
                float progress = (float)((songTime - cueTime) / System.Math.Max(0.01, landingTime - cueTime));
                float distance = Mathf.Abs(progress - (i + 1f) / (pathStars.Length + 1f));
                float scale = elapsed < 0 ? 0.12f : Mathf.Lerp(0.11f, 0.2f, Mathf.Clamp01(1 - distance * 5));
                pathStars[i].localScale = Vector3.one * scale;
            }
    }
}