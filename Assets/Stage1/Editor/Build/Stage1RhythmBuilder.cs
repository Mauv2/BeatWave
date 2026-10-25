using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class Stage1RhythmBuilder
{
    [System.Serializable] public class Timing
    {
        public float duration, eventEntry, caveResume, caveArrival, caveFirstNote;
        public int regularNotes, holds;
        public EventNoteData[] eventNotes;
        public float[] attackTimes;
    }
    public static Timing ReadTiming() => JsonUtility.FromJson<Timing>(File.ReadAllText("Assets/notedata JSON/Stage1Timing.json"));

    [MenuItem("BeatWave/Stage1/Apply Cave Audio Transition")]
    public static void ApplyCaveAudioTransition()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Stage1")
            throw new System.InvalidOperationException("Open Stage1 in edit mode first.");
        var cave = Object.FindObjectOfType<CaveEventManager>();
        Undo.RecordObject(cave, "Connect Stage1 cave music continuation");
        cave.music1 = GameObject.Find("Music1").GetComponent<AudioSource>();
        cave.useSecondMusic = false;
        cave.keepSongPlaying = true;
        cave.continuousTravelTime = ReadTiming().caveResume;
        cave.transitionVolumeRatio = .25f;
        cave.crossFadeTime = .65f;
        cave.restartDelay = 0f;
        EditorUtility.SetDirty(cave);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    [MenuItem("BeatWave/Stage1/Apply Music Chart")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Stage1")
            throw new System.InvalidOperationException("Open Stage1 in edit mode first.");
        var timing = ReadTiming();
        var music = GameObject.Find("Music1").GetComponent<AudioSource>();
        var mover = Object.FindObjectOfType<PlayerMove>();
        var body = mover.GetComponent<CapsuleCollider2D>();
        var main = Object.FindObjectsOfType<NoteMapPlacer>().Single(p => p.placeOnStart);
        var trigger = Object.FindObjectOfType<EventTrigger>();
        var triggerBody = trigger.GetComponent<Collider2D>();
        Physics2D.SyncTransforms();
        float edge = mover.transform.position.x + timing.eventEntry * mover.moveSpeed + body.bounds.extents.x;
        trigger.transform.position += Vector3.right * (edge - triggerBody.bounds.min.x);
        main.sectionEndPoint = trigger.transform;
        foreach (var placer in Object.FindObjectsOfType<NoteMapPlacer>())
        {
            placer.chartFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/notedata JSON/Stage1Notes.json");
            placer.jsonFileName = "notedata JSON/Stage1Notes.json";
            placer.songClip = music.clip;
            EditorUtility.SetDirty(placer);
        }
        Object.FindObjectOfType<PlayNote>().musicSource = music;
        var events = Object.FindObjectOfType<EventNoteSpawner>();
        events.useAbsoluteSongTime = true;
        events.eventNotes = timing.eventNotes;
        events.completionSongTime = timing.caveResume;
        // These legs are authored in world coordinates. Keep their existing islands
        // under the landing points even if the presentation parent was moved.
        if (mover.switchback != null) mover.switchback.transform.position = Vector3.zero;
        Physics2D.SyncTransforms();
        Directory.CreateDirectory("Temp");
        var report = new StringBuilder();
        var old = mover.transform.position;
        DescribePath(report, "surface", RhythmPath.Build(mover, body, main.groundLayer, 0, timing.eventEntry), timing.eventEntry);
        mover.transform.position = main.caveStartPoint.position;
        Physics2D.SyncTransforms();
        var cavePath = RhythmPath.Build(mover, body, main.groundLayer, timing.caveArrival, music.clip.length - timing.caveArrival);
        DescribePath(report, "cave", cavePath, music.clip.length);
        mover.transform.position = old;
        Physics2D.SyncTransforms();
        File.WriteAllText("Temp/Stage1-path.txt", report.ToString());
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Stage1 chart and music timeline saved. See Temp/Stage1-path.txt for route coverage.");
    }
    static void DescribePath(StringBuilder report, string name, RhythmPath path, float end)
    {
        report.AppendLine(name + " start=" + path.StartTime + " end=" + end);
        for (double t = path.StartTime; t < end; t += 2) report.AppendLine(t.ToString("F3") + " " + path.Evaluate(t));
        report.AppendLine("END " + path.Evaluate(end));
        foreach (var cue in path.Jumps) report.AppendLine("JUMP " + cue.time + " to " + cue.endTime + " " + cue.trigger);
        var notes = LitJson.JsonMapper.ToObject(File.ReadAllText("Assets/notedata JSON/Stage1Notes.json"));
        for (int i = 0; i < notes.Count; i++)
        {
            float t = float.Parse(notes[i]["createTime"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            float d = float.Parse(notes[i]["holdTime"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            if (d <= 0 || t < path.StartTime || t + d > end) continue;
            float deviation = 0;
            for (int j = 0; j <= 20; j++)
                deviation = Mathf.Max(deviation, Vector2.Distance(path.Evaluate(t + d * j / 20), Vector2.Lerp(path.Evaluate(t), path.Evaluate(t + d), j / 20f)));
            report.AppendLine("HOLD " + t + " to " + (t + d) + " deviation=" + deviation);
        }
    }
    [MenuItem("BeatWave/Stage1/Inspect Rhythm Layout")]
    public static void Inspect()
    {
        var s = new StringBuilder();
        var play = Object.FindObjectOfType<PlayNote>();
        var clip = GameObject.Find("Music1").GetComponent<AudioSource>().clip;
        clip.LoadAudioData();
        var samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0)) throw new System.InvalidOperationException("Cannot read gameplay audio.");
        using (var writer = new BinaryWriter(File.Create("Temp/Stage1-imported-audio.f32")))
            for (int i = 0; i < clip.samples; i++)
            {
                float value = 0;
                for (int channel = 0; channel < clip.channels; channel++) value += samples[i * clip.channels + channel] / clip.channels;
                writer.Write(value);
            }
        File.WriteAllText("Temp/Stage1-imported-audio-rate.txt", clip.frequency.ToString());
        s.AppendLine("PLAY SOURCE=" + (play.musicSource == null ? "null" : play.musicSource.name + " " + play.musicSource.clip.name));
        foreach (var p in Object.FindObjectsOfType<NoteMapPlacer>())
            s.AppendLine("PLACER " + p.name + " player=" + p.player + " main=" + (p.mainStartPoint == null ? "null" : p.mainStartPoint.position.ToString()) + " cave=" + (p.caveStartPoint == null ? "null" : p.caveStartPoint.position.ToString()) + " end=" + (p.sectionEndPoint == null ? "null" : p.sectionEndPoint.position.ToString()) + " root=" + p.noteRoot);
        var cave = Object.FindObjectOfType<CaveEventManager>();
        s.AppendLine("CAVE " + cave.player + " placer=" + cave.noteMapPlacer + " start=" + cave.caveStartPoint.position + " music1=" + cave.music1 + " music2=" + cave.music2 + " useSecond=" + cave.useSecondMusic);
        var mover = Object.FindObjectOfType<PlayerMove>();
        var sw = mover.switchback;
        s.AppendLine("PLAYER BODY " + mover.GetComponent<Collider2D>().bounds + " switchback=" + sw);
        if (sw != null) foreach (var leg in sw.legs) s.AppendLine("LEG " + JsonUtility.ToJson(leg));
        foreach (var c in Object.FindObjectsOfType<Collider2D>())
            if (c.gameObject.layer == 6 || c is BoxCollider2D && c.isTrigger) s.AppendLine("COLLIDER " + c.name + " " + c.GetType().Name + " " + c.bounds + " active=" + c.enabled + " trigger=" + c.isTrigger);
        foreach (var b in Object.FindObjectsOfType<MonoBehaviour>(true))
            if (b is PlayNote || b is NoteMapPlacer || b is CaveEventManager || b is FixedEventManager || b is EventNoteSpawner || b is EventTrigger || b is PlayerMove || b is JumpTrigger)
                s.AppendLine(b.GetType().Name + " " + b.name + " pos=" + b.transform.position + " active=" + b.gameObject.activeInHierarchy + " enabled=" + b.enabled + "\n" + EditorJsonUtility.ToJson(b, true));
        foreach (var a in Object.FindObjectsOfType<AudioSource>(true))
            s.AppendLine("AUDIO " + a.GetInstanceID() + " " + a.name + " enabled=" + a.enabled + " clip=" + (a.clip == null ? "null" : AssetDatabase.GetAssetPath(a.clip) + " duration=" + a.clip.length));
        foreach (var t in Object.FindObjectsOfType<Transform>(true))
            if (t.name.Contains("Start") || t.name.Contains("End")) s.AppendLine("POINT " + t.GetInstanceID() + " " + t.name + " " + t.position);
        foreach (var map in Object.FindObjectsOfType<Tilemap>(true))
        {
            s.AppendLine("TILEMAP " + map.name + " pos=" + map.transform.position + " bounds=" + map.cellBounds + " layer=" + map.gameObject.layer);
            var bounds = map.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x += 10)
            {
                var ys = Enumerable.Range(bounds.yMin, bounds.size.y).Where(y => map.HasTile(new Vector3Int(x, y, 0))).ToArray();
                if (ys.Length > 0) s.AppendLine("  x=" + x + " y=" + string.Join(",",ys));
            }
        }
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/Stage1-layout.txt", s.ToString());
        Debug.Log("Stage1 layout written to Temp/Stage1-layout.txt");
    }
}
