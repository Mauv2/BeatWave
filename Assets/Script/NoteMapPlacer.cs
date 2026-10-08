using LitJson;
using System.Globalization;
using System.IO;
using UnityEngine;

public class NoteMapPlacer : MonoBehaviour
{
    public string jsonFileName = "notedata.json";
    public bool placeOnStart = true;
    public GameObject tapNotePrefab, holdNotePrefab, mashNotePrefab;
    public Transform noteRoot;
    public float playerSpeed = 5f;
    public LayerMask groundLayer;
    public float rayStartY = 100f, rayDistance = 1000f;
    public Transform judgePointUp, judgePointDown, groundCheck, player;
    public Transform mainStartPoint, caveStartPoint;
    public float caveStartTime = 20f;
    public CapsuleCollider2D playerCollider;
    public float noteHeightAdjust;
    public float sectionRayHeadroom = 20f;
    [Header("Automatic jump synchronization")]
    public bool syncAutomaticJumps;
    public Transform sectionEndPoint;
    RhythmPath path;
    Vector3 upOffset, downOffset;
    JsonData noteData;
    float sectionCeiling = float.PositiveInfinity;

    void Start() { if (placeOnStart) { LoadJson(); PlaceAllNotes(); } }
    public void LoadJson()
    {
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        if (!File.Exists(path)) path = Path.Combine(Application.dataPath, jsonFileName);
        if (!File.Exists(path)) { Debug.LogError("Missing note chart: " + jsonFileName, this); noteData = null; return; }
        noteData = JsonMapper.ToObject(File.ReadAllText(path));
    }
    public void PlaceAllNotes() { PlaceNotesFromTime(0f, mainStartPoint); }
    public void PlaceNotesFromTime(float startTime, Transform sectionStart)
    {
        if (noteData == null || player == null || playerCollider == null || judgePointUp == null || judgePointDown == null) return;
        var judge = FindObjectOfType<JudgeManager>();
        var clock = RhythmClock.GetOrCreate();
        var mover = player.GetComponent<PlayerMove>();
        if (mover != null) playerSpeed = mover.moveSpeed;
        Physics2D.SyncTransforms();
        // Underground sections must not accidentally use the surface map above them.
        sectionCeiling = sectionStart != null && sectionStart != mainStartPoint
            ? player.position.y + sectionRayHeadroom : float.PositiveInfinity;
        float originX = player.position.x;
        float endTime = float.PositiveInfinity;
        if (sectionEndPoint != null)
        {
            var endCollider = sectionEndPoint.GetComponent<Collider2D>();
            float endX = endCollider != null ? endCollider.bounds.min.x - playerCollider.bounds.extents.x : sectionEndPoint.position.x;
            endTime = startTime + Mathf.Max(0, endX - originX) / playerSpeed;
        }
        path = null;
        if (syncAutomaticJumps && mover != null)
        {
            float lastTime = startTime + 1;
            for (int i = 0; i < noteData.Count; i++)
                lastTime = Mathf.Max(lastTime, ReadFloat(noteData[i], "createTime") + ReadFloat(noteData[i], "holdTime"));
            path = RhythmPath.Build(mover, playerCollider, groundLayer, startTime, Mathf.Min(lastTime + 3, endTime) - startTime);
            upOffset = judgePointUp.position - player.position;
            downOffset = judgePointDown.position - player.position;
            mover.BindPath(path);
        }
        float upHeight = judgePointUp.position.y - playerCollider.bounds.min.y;
        float downHeight = judgePointDown.position.y - playerCollider.bounds.min.y;
        for (int i = 0; i < noteData.Count; i++)
        {
            float time = ReadFloat(noteData[i], "createTime");
            if (time < startTime || time >= endTime) continue;
            NoteLine line = ReadFloat(noteData[i], "noteType") == 0 ? NoteLine.Up : NoteLine.Down;
            NoteKind kind = (NoteKind)(int)ReadFloat(noteData[i], "noteKind");
            GameObject prefab = kind == NoteKind.Tap ? tapNotePrefab : kind == NoteKind.Hold ? holdNotePrefab : mashNotePrefab;
            if (prefab == null) continue;
            Transform point = line == NoteLine.Up ? judgePointUp : judgePointDown;
            float offsetX = point.position.x - player.position.x;
            float height = (line == NoteLine.Up ? upHeight : downHeight) + noteHeightAdjust;
            // Sample where the PLAYER will stand, not the marker ahead of them.
            float footX = originX + (time - startTime) * playerSpeed;
            Vector3 start = path != null ? PositionAtTime(time, line) : PositionOnGround(footX, offsetX, height);
            GameObject obj = Instantiate(prefab, start, Quaternion.identity, noteRoot);
            Note note = obj.GetComponent<Note>();
            if (note == null) { Destroy(obj); continue; }
            note.line = line;
            note.kind = kind;
            note.holdTime = ReadFloat(noteData[i], "holdTime", 1.5f);
            note.needHitCount = Mathf.Max(1, (int)ReadFloat(noteData[i], "needHitCount", 5f));
            note.mashLimitTime = Mathf.Max(0.1f, ReadFloat(noteData[i], "mashLimitTime", 2f));
            note.ConfigureTiming(clock, time, judge);
            var movement = obj.GetComponent<NoteMove>();
            if (movement != null) movement.enabled = false;
            var visual = obj.GetComponent<HoldNoteVisual>();
            if (kind == NoteKind.Hold && visual != null)
            {
                note.holdTime = Mathf.Min(note.holdTime, endTime - time);
                Vector3 end = path != null ? PositionAtTime(time + note.holdTime, line)
                    : PositionOnGround(footX + note.holdTime * playerSpeed, offsetX, height);
                visual.SetHoldPoints(start, end);
            }
        }
    }
    public Vector3 PositionAtTime(double time, NoteLine line)
    {
        Vector3 offset = line == NoteLine.Up ? upOffset : downOffset;
        offset.x = Mathf.Abs(offset.x) * path.DirectionAt(time);
        return (Vector3)path.Evaluate(time) + offset + Vector3.up * noteHeightAdjust;
    }
    public Vector3 PositionOnGround(float playerX, float markerOffsetX, float height)
    {
        float groundY;
        if (!TryFindGround(playerX, out groundY))
            groundY = playerCollider != null ? playerCollider.bounds.min.y : player.position.y;
        return new Vector3(playerX + markerOffsetX, groundY + height, 0f);
    }
    public bool TryFindGround(float x, out float y)
    {
        y = float.MinValue;
        var hits = Physics2D.RaycastAll(new Vector2(x, Mathf.Min(rayStartY, sectionCeiling)), Vector2.down, rayDistance, groundLayer);
        foreach (var hit in hits)
            if (!hit.collider.isTrigger && hit.normal.y > 0.2f) { y = hit.point.y; return true; }
        return false;
    }
    static float ReadFloat(JsonData data, string key, float fallback = 0f)
    {
        if (!((System.Collections.IDictionary)data).Contains(key)) return fallback;
        return float.Parse(data[key].ToString(), CultureInfo.InvariantCulture);
    }
}
