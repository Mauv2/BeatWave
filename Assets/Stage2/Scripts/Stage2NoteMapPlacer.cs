using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-20)]
public sealed class Stage2NoteMapPlacer : MonoBehaviour
{
    public TextAsset chartFile;
    public AudioClip songClip;
    public Stage2MusicRoute route;
    public PlayerMove player;
    public JudgeManager judge;
    public Transform upMarker, downMarker, noteRoot;
    public GameObject tapPrefab, holdPrefab, mashPrefab;
    [Serializable] public struct Entry
    {
        public int noteType, noteKind, needHitCount;
        public float createTime, holdTime, mashLimitTime;
    }
    [Serializable] class Chart { public Entry[] notes; }
    public IReadOnlyList<Note> Notes => notes;
    readonly List<Note> notes = new List<Note>();
    Vector3 upOffset, downOffset;
    public Entry[] ReadChart() => JsonUtility.FromJson<Chart>("{\"notes\":" + chartFile.text + "}").notes;
    void Start()
    {
        var clock = RhythmClock.GetOrCreate();
        var body = player.GetComponent<CapsuleCollider2D>();
        upOffset = upMarker.position - player.transform.position;
        downOffset = downMarker.position - player.transform.position;
        foreach(var entry in ReadChart()) if(entry.noteKind==(int)NoteKind.Mash)
        {
            route.ConfigureMash(entry.createTime,entry.createTime+entry.mashLimitTime);
            route.presentation.gateReleaseTime=entry.createTime+entry.mashLimitTime;
            route.presentation.stageEvents.gateReleaseTime=entry.createTime+entry.mashLimitTime;
        }
        player.BindPath(route.BuildPath(body, 0, songClip.length));
        foreach (var entry in ReadChart())
        {
            var kind = (NoteKind)entry.noteKind;
            var line = entry.noteType == 0 ? NoteLine.Up : NoteLine.Down;
            var prefab = kind == NoteKind.Hold ? holdPrefab : kind == NoteKind.Mash ? mashPrefab : tapPrefab;
            var obj = Instantiate(prefab, PositionAtTime(entry.createTime, line), Quaternion.identity, noteRoot);
            obj.name = kind + " " + entry.createTime.ToString("F3");
            var note = obj.GetComponent<Note>();
            note.kind = kind; note.line = line; note.holdTime = entry.holdTime;
            note.needHitCount = Mathf.Max(1, entry.needHitCount);
            note.mashLimitTime = Mathf.Max(.1f, entry.mashLimitTime);
            note.ConfigureTiming(clock, entry.createTime, judge);
            var movement = obj.GetComponent<NoteMove>();
            if (movement != null) movement.enabled = false;
            if (kind == NoteKind.Hold)
                obj.GetComponent<HoldNoteVisual>().SetHoldPoints(obj.transform.position,
                    PositionAtTime(entry.createTime + entry.holdTime, line));
            foreach (var sprite in obj.GetComponentsInChildren<SpriteRenderer>(true)) sprite.sortingOrder += 40;
            notes.Add(note);
        }
    }
    public Vector3 PositionAtTime(double time, NoteLine line)
    {
        return (Vector3)player.Path.Evaluate(time) + (line == NoteLine.Up ? upOffset : downOffset);
    }
}
