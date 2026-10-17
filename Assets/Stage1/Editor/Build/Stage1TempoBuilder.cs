using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1TempoBuilder
{
    [MenuItem("BeatWave/Stage1/Apply Music Tempo Pacing")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.name!="Stage1")throw new InvalidOperationException("Open Stage1 in edit mode.");
        var player=UnityEngine.Object.FindObjectOfType<PlayerMove>();
        var tempo=player.GetComponent<Stage1TempoMotion>();
        if(tempo==null)tempo=Undo.AddComponent<Stage1TempoMotion>(player.gameObject);
        Undo.RecordObject(tempo,"Bind Stage1 music tempo");
        tempo.tempoFile=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/notedata JSON/Stage1Tempo.json");
        tempo.chartFile=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/notedata JSON/Stage1Notes.json");
        if(tempo.tempoFile==null || tempo.chartFile==null)throw new InvalidOperationException("Generate Stage1Tempo.json first.");
        tempo.animator=player.GetComponentInChildren<Animator>();
        const string folder="Assets/Stage1/Animation",path=folder+"/Stage1Tempo.controller";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Stage1","Animation");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(controller==null)
        {
            if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(tempo.animator.runtimeAnimatorController),path))
                throw new InvalidOperationException("Cannot copy Stage1 animation controller.");
            controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        if(!controller.parameters.Any(p=>p.name==Stage1TempoMotion.RunParameter))
            controller.AddParameter(new AnimatorControllerParameter{name=Stage1TempoMotion.RunParameter,type=AnimatorControllerParameterType.Float,defaultFloat=1});
        var run=controller.layers.SelectMany(l=>l.stateMachine.states).Single(s=>s.state.name=="run").state;
        tempo.runCycleSeconds=((AnimationClip)run.motion).length;
        run.speedParameter=Stage1TempoMotion.RunParameter;run.speedParameterActive=true;
        Undo.RecordObject(tempo.animator,"Use Stage1 tempo animation controller");
        tempo.animator.runtimeAnimatorController=controller;
        EditorUtility.SetDirty(run);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(tempo);EditorUtility.SetDirty(tempo.animator);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
