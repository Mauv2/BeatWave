using System;
using UnityEngine;

/// <summary>An authored upper route, evaluated in song time independently of note results.</summary>
public class Stage1Switchback : MonoBehaviour
{
    [Serializable]
    public struct Leg
    {
        public float startTime, endTime;
        public Vector2 from, to;
        public float arcHeight;
        public bool airborne;
        public JumpTrigger launch;
        public Vector2 Evaluate(double time)
        {
            float t = Mathf.Clamp01((float)(time - startTime) / (endTime - startTime));
            return Vector2.Lerp(from, to, t) + Vector2.up * (4 * arcHeight * t * (1 - t));
        }
    }
    public Leg[] legs;
    public double StartTime => legs[0].startTime;
    public double EndTime => legs[legs.Length - 1].endTime;
    public bool Contains(double time) => legs != null && legs.Length > 0 && time >= StartTime && time <= EndTime;
    public Vector2 Evaluate(double time)
    {
        foreach(var leg in legs)
            if(time <= leg.endTime) return leg.Evaluate(time);
        return legs[legs.Length - 1].to;
    }
    void OnDrawGizmosSelected()
    {
        if(legs == null) return;
        Gizmos.color = new Color(1,.75f,.25f);
        foreach(var leg in legs)
            for(int i=0;i<40;i++)
                Gizmos.DrawLine(leg.Evaluate(Mathf.Lerp(leg.startTime,leg.endTime,i/40f)),
                    leg.Evaluate(Mathf.Lerp(leg.startTime,leg.endTime,(i+1)/40f)));
    }
}