using System;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1MusicDirectionBuilder
{
    [MenuItem("BeatWave/Stage1/Inspect Musical Trajectory")]
    public static void Inspect()
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="Stage1")throw new InvalidOperationException("Open Stage1 in edit mode.");
        var player=UnityEngine.Object.FindObjectOfType<PlayerMove>();
        var placer=UnityEngine.Object.FindObjectsOfType<NoteMapPlacer>().Single(p=>p.placeOnStart);
        var timing=Stage1RhythmBuilder.ReadTiming();
        var position=player.transform.position;
        var report=new StringBuilder();
        var direction=player.GetComponent<Stage1MusicDirection>();
        report.AppendLine("Direction enabled="+direction.enabled+"; attacks="+direction.AttackTimes.Length);
        try
        {
            for(int section=0;section<2;section++)
            {
                double start=section==0?0:timing.caveResume;
                float end=section==0?timing.eventEntry:timing.duration;
                if(section==1)player.transform.position=placer.caveStartPoint.position;
                Physics2D.SyncTransforms();
                var path=RhythmPath.Build(player,player.GetComponent<CapsuleCollider2D>(),player.groundLayer,start,end-(float)start);
                foreach(var cue in path.Jumps)
                    report.AppendLine($"{cue.trigger}: {cue.time:F3} -> {cue.endTime:F3}; nearest attack error {timing.attackTimes.Min(t=>Math.Abs(t-cue.time)):F3} / {timing.attackTimes.Min(t=>Math.Abs(t-cue.endTime)):F3}");
            }
        }
        finally {player.transform.position=position;Physics2D.SyncTransforms();}
        Directory.CreateDirectory("Reports");File.WriteAllText("Reports/Stage1-musical-jumps.txt",report.ToString());
    }
    [MenuItem("BeatWave/Stage1/Apply Musical Direction")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.name!="Stage1"||EditorApplication.isPlaying)throw new InvalidOperationException("Open Stage1 in edit mode.");
        var player=UnityEngine.Object.FindObjectOfType<PlayerMove>();
        var direction=player.GetComponent<Stage1MusicDirection>();
        if(direction==null)direction=Undo.AddComponent<Stage1MusicDirection>(player.gameObject);
        Undo.RecordObject(direction,"Configure Stage1 musical direction");
        direction.timingFile=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/notedata JSON/Stage1Timing.json");
        direction.lineMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Stage1/Art/Lines.mat");
        direction.noteRoot=UnityEngine.Object.FindObjectsOfType<NoteMapPlacer>().Single(p=>p.placeOnStart).noteRoot;
        if(direction.timingFile==null||direction.lineMaterial==null)throw new InvalidOperationException("Stage1 timing or line material missing.");
        var director=Camera.main.GetComponent<Stage1CameraDirector>();
        if(director==null)director=Undo.AddComponent<Stage1CameraDirector>(Camera.main.gameObject);
        Undo.RecordObject(director,"Frame Stage1 music phrases");
        director.player=player;director.presentationStrength=1;
        var events=UnityEngine.Object.FindObjectOfType<EventNoteSpawner>();
        director.eventPoints=events.eventStartPoints.Concat(new[]{events.eventJudgePoint}).Where(t=>t!=null).ToArray();
        EditorUtility.SetDirty(direction);EditorUtility.SetDirty(director);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("Stage1 musical jump timing, phrase camera and landing accents saved.");
    }
}
