using System.Collections.Generic;
using UnityEngine;

namespace BeatWave.Stage3
{
    // First gate only: readable feedback and landing anticipation on the song clock.
    // Created by Stage3Transitions so existing scenes and rebuilt scenes agree.
    public sealed class Stage3FracturePresentation
    {
        readonly Stage3Transitions transitions;
        readonly List<LineRenderer> fissures=new List<LineRenderer>();
        readonly LineRenderer descent, landing, release;
        readonly Vector3 landingPoint;
        static readonly Color Guide=new Color(1f,.84f,.5f);

        public Stage3FracturePresentation(Stage3Transitions transitions)
        {
            this.transitions=transitions;
            landingPoint=Stage3Route.Position(Stage3Route.FractureLanding);
            foreach(var piece in transitions.collapsePieces)
                foreach(var line in piece.GetComponentsInChildren<LineRenderer>(true))
                    if(line.name=="Luminous fissure")fissures.Add(line);
            // Use the scene's existing line material; no new art or shader dependency.
            if(fissures.Count==0)return;
            var material=fissures[0].sharedMaterial;
            var root=new GameObject("Fracture landing guidance").transform;
            root.SetParent(transitions.transform,false);
            var points=new Vector3[25];
            for(int i=0;i<points.Length;i++)
                points[i]=(Vector3)Stage3Route.Position(Mathf.Lerp(Stage3Route.FractureRelease,
                    Stage3Route.FractureLanding,i/(float)(points.Length-1)))+new Vector3(-.7f,.12f);
            descent=Stage3Drawing.Line(root,"Descent guide",points,Guide,.025f,18,material,true);
            landing=Stage3Drawing.Ring(root,"Landing ripple",1,Guide,.035f,19,material);
            landing.transform.position=landingPoint+Vector3.up*.12f;
            release=Stage3Drawing.Ring(root,"Fracture release",1,Color.white,.045f,42,material);
            Present(0);
        }

        public void Present(float time)
        {
            var gate=transitions.gates[0];
            float pulse=gate.PulseAt(time);
            float after=time-gate.releaseTime;
            bool active=time>=gate.startTime && time<gate.releaseTime;
            var crack=Color.Lerp(new Color(.32f,.52f,.65f),new Color(.7f,.96f,1),pulse);
            crack.a=active?.25f+.35f*gate.Charge+.4f*pulse:.18f;
            foreach(var line in fissures)
            {
                line.startColor=line.endColor=crack;
                line.startWidth=line.endWidth=active?.025f+.045f*pulse:.02f;
            }
            if(descent==null)return;
            float reveal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(gate.releaseTime-.15f,gate.releaseTime+.55f,time));
            float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Stage3Route.FractureLanding,
                Stage3Route.FractureLanding+.45f,time));
            SetAlpha(descent,Guide,.36f*reveal*fade);
            SetAlpha(landing,Guide,.7f*reveal*fade);
            float touchdown=Mathf.Clamp01((time-Stage3Route.FractureLanding)/.45f);
            landing.transform.localScale=new Vector3(1.4f+touchdown,.16f+touchdown*.08f,1);

            // A short local ring distinguishes a clean break from a missed gate.
            // Both outcomes retain the same timed fall and landing.
            bool success=gate.Resolved && gate.Success;
            Color releaseColor=success?new Color(.55f,.94f,1):new Color(.66f,.53f,.42f);
            float alpha=after>=0 && after<.45f?(success?.7f:.3f)*(1-after/.45f):0;
            release.transform.position=gate.note.transform.position;
            release.transform.localScale=Vector3.one*(1.2f+Mathf.Clamp01(after/.45f)*2.3f);
            SetAlpha(release,releaseColor,alpha);
        }

        public void FrameCamera(float time,ref Vector3 target,ref float size)
        {
            float enter=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Stage3Route.FractureRelease-.2f,
                Stage3Route.FractureRelease+.55f,time));
            float exit=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Stage3Route.FractureLanding,
                Stage3Route.FractureLanding+1.25f,time));
            float weight=enter*exit;
            if(weight<=0)return;
            // Look down the fall while retaining the player; settle before input resumes.
            Vector3 current=Stage3Route.Position(time);
            Vector3 ahead=Stage3Route.Position(Mathf.Min(time+.7f,Stage3Route.FractureLanding));
            float landingReveal=Mathf.InverseLerp(Stage3Route.FractureLanding-1.35f,
                Stage3Route.FractureLanding-.45f,time);
            ahead=Vector3.Lerp(ahead,landingPoint,landingReveal);
            var framed=(current+ahead)*.5f+new Vector3(2.5f,transitions.game.footOffset+1.8f,-10);
            target=Vector3.Lerp(target,framed,weight);
            size=Mathf.Lerp(size,9.2f,weight);
        }

        static void SetAlpha(LineRenderer line,Color color,float alpha)
        {
            line.enabled=alpha>.001f;
            color.a=alpha;
            line.startColor=line.endColor=color;
        }
    }
}
