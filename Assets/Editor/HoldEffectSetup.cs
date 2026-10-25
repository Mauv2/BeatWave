using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HoldEffectSetup
{
    [MenuItem("BeatWave/Notes/Apply Yellow Hold Effect")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit play mode first.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BoldVFXPackDemo/Content/Built-in/Prefabs/FX_Shine_Yellow.prefab");
        if (prefab == null) throw new InvalidOperationException("Yellow shine prefab missing.");
        var particles = prefab.GetComponentInChildren<ParticleSystem>();
        if (particles == null || !particles.main.loop) throw new InvalidOperationException("Hold effect must loop.");
        int count = 0;
        for (int i = 1; i <= 3; i++)
        {
            string path = "Assets/Scenes/Stage" + i + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var judge in root.GetComponentsInChildren<JudgeManager>(true))
                    {
                        Undo.RecordObject(judge, "Assign yellow Hold effect");
                        judge.holdEffectPrefab = prefab;
                        EditorUtility.SetDirty(judge);
                        count++;
                    }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
        }
        // Check that the consumed body and the effect's path share the same boundary.
        var test = new GameObject("Hold visual validation");
        try
        {
            var visual = test.AddComponent<HoldNoteVisual>();
            visual.startCircle = new GameObject("Head", typeof(SpriteRenderer)).transform;
            visual.holdBody = new GameObject("Body", typeof(SpriteRenderer)).transform;
            visual.endCircle = new GameObject("Tail", typeof(SpriteRenderer)).transform;
            visual.startCircle.SetParent(test.transform);
            visual.holdBody.SetParent(test.transform);
            visual.endCircle.SetParent(test.transform);
            visual.SetHoldPoints(Vector3.zero, Vector3.right * 10);
            visual.StartHold(); visual.UpdateHold(.5f);
            if (visual.startCircle.GetComponent<SpriteRenderer>().enabled ||
                Mathf.Abs(visual.holdBody.GetComponent<SpriteRenderer>().size.x - 5) > .001f ||
                Vector3.Distance(visual.GetPoint(.5f), Vector3.right * 5) > .001f)
                throw new InvalidOperationException("Hold visual progress check failed.");
            visual.CompleteHold();
            if (visual.endCircle.GetComponent<SpriteRenderer>().enabled)
                throw new InvalidOperationException("Hold completion visibility failed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(test); }
        Debug.Log("Yellow Hold effect assigned to " + count + " judges; looping prefab and Hold visual progress/completion checks passed.");
    }
}
