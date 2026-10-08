using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The same song-time trajectory drives the player and the chart.</summary>
public sealed class RhythmPath
{
    const float Step = 0.01f;
    readonly List<Vector2> positions = new List<Vector2>();
    readonly List<JumpCue> jumps = new List<JumpCue>();
    public struct JumpCue { public JumpTrigger trigger; public double time, endTime; }
    public IReadOnlyList<JumpCue> Jumps => jumps;
    public double StartTime { get; private set; }
    public float Speed { get; private set; }
    public float FeetOffset { get; private set; }
    public Vector2 Evaluate(double songTime)
    {
        float index = Mathf.Clamp((float)(songTime - StartTime) / Step, 0, positions.Count - 1);
        int a = Mathf.FloorToInt(index);
        return Vector2.Lerp(positions[a], positions[Mathf.Min(a + 1, positions.Count - 1)], index - a);
    }

    public static RhythmPath Build(PlayerMove mover, CapsuleCollider2D body, LayerMask groundMask,
        double startTime, float duration)
    {
        Physics2D.SyncTransforms();
        var path = new RhythmPath { StartTime = startTime, Speed = mover.moveSpeed,
            FeetOffset = mover.transform.position.y - body.bounds.min.y };
        var rb = mover.GetComponent<Rigidbody2D>();
        float gravity = mover.OriginalGravity * Physics2D.gravity.y;
        float mass = rb.mass;
        float halfWidth = body.bounds.extents.x;
        float bodyHeight = body.bounds.size.y;
        Vector2 origin = mover.transform.position;
        float foot = origin.y - path.FeetOffset;
        float floor;
        bool grounded = Ground(origin.x, foot + 0.75f, groundMask, out floor) && Mathf.Abs(foot - floor) < 1.5f;
        if (grounded) foot = floor;
        float velocity = 0;
        var triggers = UnityEngine.Object.FindObjectsOfType<JumpTrigger>();
        var used = new HashSet<JumpTrigger>();
        int count = Mathf.CeilToInt(Mathf.Max(1, duration) / Step);
        for (int i = 0; i <= count; i++)
        {
            float elapsed = i * Step;
            float x = origin.x + elapsed * path.Speed;
            if (i > 0)
            {
                float nextFoot = foot + velocity * Step + 0.5f * gravity * Step * Step;
                velocity += gravity * Step;
                bool hasFloor = Ground(x, Mathf.Max(foot, nextFoot) + 0.75f, groundMask, out floor);
                if (hasFloor && ((grounded && floor >= foot - 0.5f) ||
                    (velocity <= 0 && foot >= floor - 0.05f && nextFoot <= floor)))
                { foot = floor; velocity = 0; grounded = true; }
                else { foot = nextFoot; grounded = false; }
            }
            foreach (var trigger in triggers)
            {
                if (used.Contains(trigger) || trigger.IsUsed) continue;
                var collider = trigger.GetComponent<Collider2D>();
                if (collider == null || !collider.enabled) continue;
                Bounds bounds = collider.bounds;
                // Use the player's collider, rather than a centre crossing, just like OnTriggerEnter.
                if (x + halfWidth < bounds.min.x || x - halfWidth > bounds.max.x ||
                    foot + bodyHeight < bounds.min.y || foot > bounds.max.y) continue;
                used.Add(trigger);
                velocity = trigger.jumpPower / mass;
                grounded = false;
                path.jumps.Add(new JumpCue { trigger = trigger, time = startTime + elapsed });
            }
            path.positions.Add(new Vector2(x, foot + path.FeetOffset));
        }
        for (int j = 0; j < path.jumps.Count; j++)
        {
            var cue = path.jumps[j];
            int start = Mathf.RoundToInt((float)(cue.time - startTime) / Step);
            int limit = j + 1 < path.jumps.Count ? Mathf.RoundToInt((float)(path.jumps[j + 1].time - startTime) / Step) : count;
            int end = limit;
            for (int i = start + 12; i <= limit && i < path.positions.Count; i++)
            {
                Vector2 point = path.positions[i];
                float ground;
                if (Ground(point.x, point.y - path.FeetOffset + 0.15f, groundMask, out ground) &&
                    Mathf.Abs(point.y - path.FeetOffset - ground) < 0.025f)
                { end = i; break; }
            }
            cue.endTime = startTime + end * Step;
            path.jumps[j] = cue;
        }
        if (mover.switchback != null && startTime <= mover.switchback.StartTime && startTime + duration >= mover.switchback.EndTime)
            path.ApplySwitchback(mover.switchback);
        return path;
    }
    public float DirectionAt(double songTime)
    {
        float dx = Evaluate(songTime + 0.015).x - Evaluate(songTime - 0.015).x;
        return dx < -0.0001f ? -1f : 1f;
    }
    void ApplySwitchback(Stage1Switchback section)
    {
        for(int i=0;i<positions.Count;i++)
        {
            double time=StartTime+i*Step;
            if(section.Contains(time)) positions[i]=section.Evaluate(time);
        }
        jumps.RemoveAll(cue=>cue.time >= section.StartTime-0.02 && cue.time < section.EndTime);
        foreach(var leg in section.legs)
            if(leg.airborne)
                jumps.Add(new JumpCue {trigger=leg.launch,time=leg.startTime,endTime=leg.endTime});
        jumps.Sort((a,b)=>a.time.CompareTo(b.time));
    }
    static bool Ground(float x, float originY, LayerMask mask, out float y)
    {
        foreach (var hit in Physics2D.RaycastAll(new Vector2(x, originY), Vector2.down, 250f, mask))
            if (!hit.collider.isTrigger && hit.normal.y > 0.2f) { y = hit.point.y; return true; }
        y = 0;
        return false;
    }
}