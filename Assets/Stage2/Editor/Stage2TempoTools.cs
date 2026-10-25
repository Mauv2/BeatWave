using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage2TempoTools
{
    const string Art = "Assets/Stage2/Image/gothic_ruin_assets_individual/";
    [MenuItem("BeatWave/Stage2/Apply Full Music Stage")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Stage2")
            throw new InvalidOperationException("Open Stage2 in edit mode.");
        var mover = UnityEngine.Object.FindObjectOfType<PlayerMove>();
        var play = UnityEngine.Object.FindObjectOfType<PlayNote>();
        var legacy = play.GetComponent<NoteMapPlacer>();
        var judge = play.GetComponent<JudgeManager>();
        var music = play.musicSource != null ? play.musicSource : play.GetComponent<AudioSource>();
        var camera = Camera.main;
        var background = camera.GetComponent<BackgroundLoop>();
        var route = mover.GetComponent<Stage2MusicRoute>();
        bool firstApply = route == null;
        if (route == null) route = Undo.AddComponent<Stage2MusicRoute>(mover.gameObject);
        Undo.RecordObject(route, "Bind Stage2 music route");
        route.routeFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Stage2/Data/Stage2Route.json");
        route.animator = mover.GetComponentInChildren<Animator>(); route.view = camera; route.backdrop = background;
        Undo.RecordObject(background, "Use Stage2 music backdrop framing"); background.enabled = false;
        if (route.routeFile == null) throw new InvalidOperationException("Generate the Stage2 music data first.");

        // Keep the authored prototype intact and inactive for comparison/recovery.
        var archive = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(o => o.name == "Stage2 Original Layout (archived)");
        if (archive == null)
        {
            archive = new GameObject("Stage2 Original Layout (archived)"); Undo.RegisterCreatedObjectUndo(archive, "Archive prototype layout");
            foreach (var sprite in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
            {
                if (sprite.GetComponentInParent<PlayerMove>() != null || sprite.GetComponentInParent<Canvas>() != null ||
                    sprite == background.background1 || sprite == background.background2) continue;
                if (!sprite.transform.IsChildOf(archive.transform)) Undo.SetTransformParent(sprite.transform, archive.transform, "Archive terrain art");
            }
            foreach (var trigger in UnityEngine.Object.FindObjectsOfType<JumpTrigger>())
                if (!trigger.transform.IsChildOf(archive.transform)) Undo.SetTransformParent(trigger.transform, archive.transform, "Archive old jump triggers");
            archive.SetActive(false);
        }
        foreach (var trigger in UnityEngine.Object.FindObjectsOfType<BackgroundTrigger>()) { Undo.RecordObject(trigger, "Use music section backgrounds"); trigger.enabled = false; }
        foreach (var trigger in UnityEngine.Object.FindObjectsOfType<EventTrigger>())
        {
            // Unity can deliver trigger callbacks to disabled MonoBehaviours.
            Undo.RecordObject(trigger.gameObject, "Disable unused prototype event"); trigger.gameObject.SetActive(false);
        }
        foreach (var follow in mover.GetComponents<PlayerGroundFollow>()) { Undo.RecordObject(follow, "Use music route elevation"); follow.enabled = false; }
        foreach (var placer in UnityEngine.Object.FindObjectsOfType<NoteMapPlacer>()) { Undo.RecordObject(placer, "Use Stage2-specific note placement"); placer.enabled = false; }

        var old = GameObject.Find("Stage2 Music Stage"); if (old != null) Undo.DestroyObjectImmediate(old);
        var stage = new GameObject("Stage2 Music Stage"); Undo.RegisterCreatedObjectUndo(stage, "Build full Stage2 route");
        var terrain = new GameObject("Playable platforms").transform; terrain.SetParent(stage.transform);
        var decor = new GameObject("Gothic landmarks").transform; decor.SetParent(stage.transform);
        float lastX = route.Sample(0).x - 12f, floor = route.Sample(0).y;
        int span = 0;
        foreach (var jump in route.Data.jumps)
        {
            float takeoff = route.Sample(jump.start).x, landing = route.Sample(jump.end).x;
            BuildSpan(terrain, decor, lastX, takeoff + .15f, floor, span++);
            lastX = landing - .15f; floor = jump.toY;
        }
        BuildSpan(terrain, decor, lastX, route.Sample(route.Data.duration).x + 16f, floor, span);
        var noteRoot = new GameObject("Music notes").transform; noteRoot.SetParent(stage.transform);
        var placer2 = play.GetComponent<Stage2NoteMapPlacer>();
        if (placer2 == null) placer2 = Undo.AddComponent<Stage2NoteMapPlacer>(play.gameObject);
        Undo.RecordObject(placer2, "Bind full Stage2 chart");
        placer2.chartFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Stage2/Data/Stage2Notes.json");
        placer2.songClip = music.clip; placer2.route = route; placer2.player = mover; placer2.judge = judge;
        placer2.upMarker = judge.judgePointUp.transform; placer2.downMarker = judge.judgePointDown.transform;
        placer2.noteRoot = noteRoot; placer2.tapPrefab = legacy.tapNotePrefab;
        placer2.holdPrefab = legacy.holdNotePrefab; placer2.mashPrefab = legacy.mashNotePrefab;
        Undo.RecordObject(play, "Bind Stage2 music and movement"); play.musicSource = music; play.playerMover = mover; play.judgeManager = judge;

        Physics2D.SyncTransforms();
        float feet = mover.transform.position.y - mover.GetComponent<CapsuleCollider2D>().bounds.min.y;
        Undo.RecordObject(mover.transform, "Place player at music route start");
        var start = route.Sample(0); mover.transform.position = new Vector3(start.x, start.y + feet, 0);
        Undo.SetTransformParent(camera.transform, null, "Use Stage2 music camera");
        Undo.RecordObject(camera.transform, "Frame Stage2 opening"); camera.transform.localScale = Vector3.one;
        camera.transform.position = new Vector3(start.x + 5.1f, start.y + 3f, -10);
        Undo.RecordObject(camera, "Frame Stage2 opening"); camera.orthographicSize = 5.9f;
        if (firstApply)
            foreach (var renderer in mover.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer != background.background1 && renderer != background.background2)
                { Undo.RecordObject(renderer, "Keep character and markers above terrain"); renderer.sortingOrder += 10; }

        const string controllerPath = "Assets/Stage2/Stage2Tempo.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(route.animator.runtimeAnimatorController), controllerPath))
                throw new InvalidOperationException("Cannot copy Stage2 animation controller.");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        }
        if (!controller.parameters.Any(p => p.name == Stage2MusicRoute.RunParameter))
            controller.AddParameter(new AnimatorControllerParameter { name = Stage2MusicRoute.RunParameter, type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        var run = controller.layers.SelectMany(l => l.stateMachine.states).Single(s => s.state.name == "run").state;
        route.runCycleSeconds = ((AnimationClip)run.motion).length;
        run.speedParameter = Stage2MusicRoute.RunParameter; run.speedParameterActive = true;
        Undo.RecordObject(route.animator, "Use Stage2-only run tempo"); route.animator.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(run); EditorUtility.SetDirty(controller); EditorUtility.SetDirty(route); EditorUtility.SetDirty(placer2);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Stage2 full music stage applied: 170s route, 24 jumps, 233 notes.");
    }
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
    static void ArtObject(Transform parent, string name, Sprite sprite, float x, float floor, float width, float walkFromTop, int order)
    {
        if (sprite == null) throw new InvalidOperationException("Missing sprite: " + name);
        var obj = new GameObject(name); obj.transform.SetParent(parent);
        float scale = width / sprite.bounds.size.x, height = sprite.bounds.size.y * scale;
        obj.transform.localScale = Vector3.one * scale;
        obj.transform.position = new Vector3(x, floor + (walkFromTop - .5f) * height, 0);
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
    }
    static void BuildSpan(Transform terrain, Transform decor, float left, float right, float floor, int index)
    {
        var archive = SceneManager.GetActiveScene().GetRootGameObjects()
            .Single(o => o.name == "Stage2 Original Layout (archived)");
        // Use the original authored ground surfaces, including their visual-to-floor offsets.
        var sources = archive.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.sprite != null && r.GetComponent<BoxCollider2D>() != null &&
                !r.GetComponent<BoxCollider2D>().isTrigger && r.GetComponent<BoxCollider2D>().size.x > 10)
            .OrderBy(r => r.transform.position.x).GroupBy(r => r.sprite).Select(g => g.First()).ToArray();
        if (sources.Length == 0) throw new InvalidOperationException("Archived ground sprites/colliders missing.");
        int sourceIndex = index < 5 ? 0 : index < 18 ? Mathf.Min(1, sources.Length - 1) : sources.Length - 1;
        var source = sources[sourceIndex];
        var sourceCollider = source.GetComponent<BoxCollider2D>();
        Vector3 sourceScale = source.transform.lossyScale;
        float sourceWidth = (source.drawMode == SpriteDrawMode.Simple ? source.sprite.bounds.size.x : source.size.x) * Mathf.Abs(sourceScale.x);
        float floorOffset = (sourceCollider.offset.y + sourceCollider.size.y * .5f) * sourceScale.y;
        int count = Mathf.Max(1, Mathf.CeilToInt((right - left) / sourceWidth));
        float width = (right - left) / count;
        var ground = new GameObject("Ground " + index); ground.layer = 6; ground.transform.SetParent(terrain);
        ground.transform.position = new Vector3((left + right) / 2f, floor - .225f, 0);
        ground.AddComponent<BoxCollider2D>().size = new Vector2(right - left, .45f);
        for (int i = 0; i < count; i++)
        {
            var tile = new GameObject("Original ground " + index + "." + i + " - " + source.sprite.name);
            tile.transform.SetParent(terrain);
            var renderer = tile.AddComponent<SpriteRenderer>();
            EditorUtility.CopySerialized(source, renderer);
            renderer.enabled = true;
            renderer.sortingLayerID = 0; renderer.sortingOrder = 0;
            float scaleX = sourceScale.x * (width + .025f) / sourceWidth;
            tile.transform.localScale = new Vector3(scaleX, sourceScale.y, 1);
            float centerX = source.drawMode == SpriteDrawMode.Simple ? source.sprite.bounds.center.x * scaleX : 0;
            tile.transform.position = new Vector3(left + width * (i + .5f) - centerX, floor - floorOffset, 0);
        }
    }
    [MenuItem("BeatWave/Stage2/Inspect Music and Route")]
    public static void Inspect()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Stage2")
            throw new InvalidOperationException("Open Stage2 in edit mode.");
        Directory.CreateDirectory("Temp");
        var lines = new System.Collections.Generic.List<string>();
        var mover = UnityEngine.Object.FindObjectOfType<PlayerMove>();
        var play = UnityEngine.Object.FindObjectOfType<PlayNote>();
        var audio = play.musicSource != null ? play.musicSource : play.GetComponent<AudioSource>();
        var clip = audio.clip;
        clip.LoadAudioData();
        var data = new float[clip.samples * clip.channels];
        if (!clip.GetData(data, 0)) throw new InvalidOperationException("Audio PCM is unavailable.");
        var mono = new float[clip.samples];
        for (int i = 0; i < mono.Length; i++)
            for (int c = 0; c < clip.channels; c++) mono[i] += data[i * clip.channels + c] / clip.channels;
        var bytes = new byte[mono.Length * sizeof(float)];
        Buffer.BlockCopy(mono, 0, bytes, 0, bytes.Length);
        File.WriteAllBytes("Temp/Stage2-imported-audio.f32", bytes);
        File.WriteAllText("Temp/Stage2-imported-audio-rate.txt", clip.frequency.ToString());
        lines.Add("Audio: " + AssetDatabase.GetAssetPath(clip) + " duration=" + clip.length + " rate=" + clip.frequency);
        lines.Add("Player: " + mover.transform.position + " speed=" + mover.moveSpeed + " gravity=" + mover.OriginalGravity);
        foreach (var placer in UnityEngine.Object.FindObjectsOfType<NoteMapPlacer>())
            lines.Add("Placer: " + placer.name + " chart=" + placer.jsonFileName + " start=" + placer.mainStartPoint + " sync=" + placer.syncAutomaticJumps);
        foreach (var component in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
            if (component.GetType().Name.Contains("Event") || component.GetType().Name.Contains("Camera"))
                lines.Add("Component: " + component.name + " " + component.GetType().Name + " enabled=" + component.enabled);
        foreach (var collider in UnityEngine.Object.FindObjectsOfType<Collider2D>().OrderBy(c => c.bounds.min.x))
            if (collider.gameObject.layer == 6 || collider.GetComponent<JumpTrigger>() != null)
                lines.Add("Terrain: " + collider.name + " min=" + collider.bounds.min.ToString("F3") + " max=" + collider.bounds.max.ToString("F3") + " trigger=" + collider.isTrigger + " jump=" + (collider.GetComponent<JumpTrigger>() != null ? collider.GetComponent<JumpTrigger>().jumpPower : 0));
        File.WriteAllLines("Temp/Stage2-layout.txt", lines);
        Debug.Log("Stage2 audio/layout exported to Temp.");
    }
}
