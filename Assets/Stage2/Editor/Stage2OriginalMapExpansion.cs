using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2OriginalMapExpansion
{
    struct Surface { public float left, right, top; }
    struct Flight { public float left, right, from, to, height; }
    [MenuItem("BeatWave/Stage2/Expand Original Layout")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit play mode.");
        var scene=SceneManager.GetActiveScene();
        if(scene.name!="Stage2") throw new InvalidOperationException("Open Stage2.");
        var source=scene.GetRootGameObjects().Single(o=>o.name=="Stage2 Original Layout (archived)");
        var route=UnityEngine.Object.FindObjectOfType<Stage2MusicRoute>();
        var placer=UnityEngine.Object.FindObjectOfType<Stage2NoteMapPlacer>();
        var input=JsonUtility.FromJson<Stage2MusicRoute.Profile>(route.routeFile.text);
        if(GameObject.Find("Stage2 Original Map Extension")!=null ||
            source.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Original layout continuation ")))
            throw new InvalidOperationException("Original extension already exists; edit its geometry directly.");
        var generated=GameObject.Find("Stage2 Music Stage");
        Undo.RecordObject(source,"Restore original map"); source.SetActive(true);
        foreach(var c in source.GetComponentsInChildren<Collider2D>(true))
            if(!c.isTrigger) {Undo.RecordObject(c.gameObject,"Recognize original ground");c.gameObject.layer=6;}
        Physics2D.SyncTransforms();
        var originals=source.GetComponentsInChildren<BoxCollider2D>().Where(c=>c.enabled&&!c.isTrigger).ToArray();
        float left=originals.Min(c=>c.bounds.min.x),right=originals.Max(c=>c.bounds.max.x);
        float firstY=originals.OrderBy(c=>c.bounds.min.x).First().bounds.max.y;
        float lastY=originals.OrderBy(c=>c.bounds.max.x).Last().bounds.max.y;
        var extension=new GameObject("Stage2 Original Map Extension");Undo.RegisterCreatedObjectUndo(extension,"Extend original map");
        float length=right-left;
        int copies=Mathf.CeilToInt((input.frames.Last().x+16-right)/length);
        for(int i=1;i<=copies;i++)
        {
            var copy=UnityEngine.Object.Instantiate(source,extension.transform);
            copy.name="Original layout continuation "+i;
            copy.transform.position=source.transform.position+new Vector3(i*length,i*(lastY-firstY),0);
        }
        Undo.SetTransformParent(placer.noteRoot,extension.transform,"Keep chart on original map");
        Undo.RecordObject(generated,"Archive generated map");generated.SetActive(false);
        Physics2D.SyncTransforms();
        var colliders=source.GetComponentsInChildren<BoxCollider2D>().Concat(extension.GetComponentsInChildren<BoxCollider2D>())
            .Where(c=>c.enabled&&!c.isTrigger).ToArray();
        var surfaces=colliders.Select(c=>new Surface{left=c.bounds.min.x,right=c.bounds.max.x,top=c.bounds.max.y}).OrderBy(s=>s.left).ToList();
        Func<float,float> ground=x=>surfaces.Where(s=>x>=s.left-.001f&&x<=s.right+.001f).Select(s=>s.top).DefaultIfEmpty(float.NaN).Max();
        var flights=new List<Flight>();
        float edge=surfaces[0].right;
        foreach(var s in surfaces.Skip(1))
        {
            if(s.left>edge+.08f)
            {
                float a=edge-.12f,b=s.left+.12f;
                flights.Add(new Flight{left=a,right=b,from=ground(a),to=ground(b),height=1.35f});
            }
            edge=Mathf.Max(edge,s.right);
        }
        var triggers=source.GetComponentsInChildren<JumpTrigger>().Concat(extension.GetComponentsInChildren<JumpTrigger>()).OrderBy(t=>t.transform.position.x);
        foreach(var trigger in triggers)
        {
            float a=trigger.GetComponent<Collider2D>().bounds.min.x-.25f;
            float b=a+Mathf.Clamp(trigger.jumpPower*.55f,2,4.5f);
            if(a<input.frames[0].x||b>input.frames.Last().x||flights.Any(f=>a<f.right+.1f&&b>f.left-.1f))continue;
            float from=ground(a),to=ground(b);
            if(float.IsNaN(from)||float.IsNaN(to))continue;
            flights.Add(new Flight{left=a,right=b,from=from,to=to,height=Mathf.Clamp(trigger.jumpPower*.18f,.7f,1.6f)});
        }
        flights=flights.OrderBy(f=>f.left).ToList();
        Func<float,float> timeAt=x=>{
            int j=Array.FindIndex(input.frames,f=>f.x>=x);
            if(j<=0)return j==0?0:input.duration;
            var a=input.frames[j-1];var b=input.frames[j];return Mathf.Lerp(a.time,b.time,Mathf.InverseLerp(a.x,b.x,x));};
        var frames=new List<Stage2MusicRoute.Frame>();int segment=0;
        int samples=Mathf.CeilToInt(input.duration/.01f);
        for(int i=0;i<=samples;i++)
        {
            float t=Mathf.Min(input.duration,i*.01f);
            while(segment<input.frames.Length-2&&input.frames[segment+1].time<t)segment++;
            var a=input.frames[segment];var b=input.frames[segment+1];float u=Mathf.InverseLerp(a.time,b.time,t);
            var f=a;f.time=t;f.x=Mathf.Lerp(a.x,b.x,u);f.speed=Mathf.Lerp(a.speed,b.speed,u);f.y=ground(f.x);
            foreach(var flight in flights)if(f.x>=flight.left&&f.x<=flight.right){float v=Mathf.InverseLerp(flight.left,flight.right,f.x);f.y=Mathf.Lerp(flight.from,flight.to,v)+4*flight.height*v*(1-v);break;}
            if(float.IsNaN(f.y)||f.y < -20)throw new InvalidOperationException("Unsupported original route at "+t+" x="+f.x);
            frames.Add(f);
        }
        input.frames=frames.ToArray();input.useSampledHeight=true;
        input.jumps=flights.Where(f=>f.left>=frames[0].x&&f.right<=frames.Last().x).Select(f=>new Stage2MusicRoute.Leap{start=timeAt(f.left),end=timeAt(f.right),fromY=f.from,toY=f.to,height=f.height}).ToArray();
        const string data="Assets/Stage2/Data/Stage2OriginalRoute.json";
        System.IO.File.WriteAllText(data,JsonUtility.ToJson(input));AssetDatabase.ImportAsset(data);
        Undo.RecordObject(route,"Bind original terrain route");route.routeFile=AssetDatabase.LoadAssetAtPath<TextAsset>(data);
        // Hold bodies are straight: use two taps when a hold crosses an original jump.
        var chart=placer.ReadChart().ToList();int converted=0;
        for(int i=chart.Count-1;i>=0;i--){var n=chart[i];if(n.noteKind!=1||!input.jumps.Any(j=>n.createTime<j.end&&n.createTime+n.holdTime>j.start))continue;
            var tail=n;tail.createTime+=n.holdTime;tail.noteKind=0;tail.holdTime=0;n.noteKind=0;n.holdTime=0;chart[i]=n;chart.Add(tail);converted++;}
        string json="["+string.Join(",",chart.OrderBy(n=>n.createTime).Select(n=>JsonUtility.ToJson(n)))+"]";
        const string chartPath="Assets/Stage2/Data/Stage2OriginalNotes.json";System.IO.File.WriteAllText(chartPath,json);AssetDatabase.ImportAsset(chartPath);
        Undo.RecordObject(placer,"Bind original route chart");placer.chartFile=AssetDatabase.LoadAssetAtPath<TextAsset>(chartPath);
        EditorUtility.SetDirty(route);EditorUtility.SetDirty(placer);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        string report="Original layout restored without moving/scaling its terrain. Continuations="+copies+"; duration="+input.duration+"; samples="+frames.Count+"; jumps="+input.jumps.Length+"; holds converted to head/tail taps="+converted+"; notes="+chart.Count+". All route samples supported by original ground or a gap jump.";
        System.IO.File.WriteAllText("Reports/Stage2-original-expansion.txt",report);Debug.Log(report);
    }
}
