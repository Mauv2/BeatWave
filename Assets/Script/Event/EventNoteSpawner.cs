using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EventNoteSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject tapNotePrefab;
    public GameObject holdNotePrefab;
    public GameObject approachCirclePrefab;

    [Header("Event Points")]
    public Transform eventJudgePoint;      // 노트가 도착할 판정 원 위치
    public Transform[] eventStartPoints;   // 노트가 출발할 위치들

    [Header("Note Visual")]
    public float noteScale = 1.5f;
    public int noteSortingOrder = 50;

    [Header("Approach Circle")]
    public float circleStartScale = 3f;
    public float circleEndScale = 1f;

    [Header("Parent")]
    public Transform noteRoot;

    [Header("Judge")]
    public EventNoteJudge eventNoteJudge;

    [Header("Event Pattern")]
    public EventNoteData[] eventNotes;
    [Tooltip("Use absolute music timestamps for authored charts instead of measuring from event entry.")]
    public bool useAbsoluteSongTime;
    public float completionSongTime;

    public bool isPlaying = false;

    int currentIndex = 0;
    float eventTimer = 0f;
    RhythmClock clock;
    double eventStartTime;

    public void StartEventNotes()
    {
        StartCoroutine(EventNoteRoutine());
    }

    IEnumerator EventNoteRoutine()
    {
        isPlaying = true;
        currentIndex = 0;
        eventTimer = 0f;
        clock = RhythmClock.GetOrCreate();
        eventStartTime = useAbsoluteSongTime ? 0d : clock.SongTime;

        if (eventNoteJudge != null)
            eventNoteJudge.StartEventJudge();

        while (currentIndex < eventNotes.Length)
        {
            eventTimer = (float)(clock.SongTime - eventStartTime);

            if (eventNoteJudge != null)
                eventNoteJudge.SetEventTime(eventTimer);

            EventNoteData data = eventNotes[currentIndex];

            if (eventTimer >= data.showTime)
            {
                // 이전 이벤트 노트의 판정이 끝났을 때만 다음 노트 생성
                if (eventNoteJudge == null || !eventNoteJudge.HasCurrentNote())
                {
                    SpawnEventNote(data);
                    currentIndex++;
                }
            }

            yield return null;
        }

        while (eventNoteJudge != null && eventNoteJudge.HasCurrentNote())
        {
            eventTimer = (float)(clock.SongTime - eventStartTime);
            eventNoteJudge.SetEventTime(eventTimer);
            yield return null;
        }

        if (useAbsoluteSongTime && completionSongTime > 0f)
        {
            while (clock.SongTime < completionSongTime) yield return null;
        }
        else yield return new WaitForSeconds(0.5f);

        isPlaying = false;
    }

    void SpawnEventNote(EventNoteData data)
    {
        // Fixed presentation events always use single-press Tap notes.
        GameObject prefab = tapNotePrefab;

        if (prefab == null)
            return;

        if (eventJudgePoint == null)
        {
            Debug.LogError("Event Judge Point가 비어있습니다.");
            return;
        }

        if (eventStartPoints == null || eventStartPoints.Length == 0)
        {
            Debug.LogError("Event Start Points가 비어있습니다.");
            return;
        }

        Transform startPoint =
            eventStartPoints[Random.Range(0, eventStartPoints.Length)];

        GameObject note =
            Instantiate(
                prefab,
                startPoint.position,
                Quaternion.identity
            );
        GameObject circle =
    Instantiate(
        approachCirclePrefab,
        note.transform.position,
        Quaternion.identity
    );

        float duration =
            data.judgeTime - data.showTime;

        circle.transform.localScale =
            Vector3.one * 3f;

        circle.transform.localScale =
    Vector3.one * circleStartScale;

        circle.transform
            .DOScale(Vector3.one * circleEndScale, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Destroy(circle);
            });

        note.transform.localScale = Vector3.one * noteScale;

        SpriteRenderer sr = note.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = noteSortingOrder;
        }

        if (noteRoot != null)
            note.transform.SetParent(noteRoot);

        Note noteScript = note.GetComponent<Note>();

        if (noteScript != null)
        {
            noteScript.isEventNote = true;
            noteScript.kind = NoteKind.Tap;
            noteScript.holdTime = 0f;
            noteScript.line = NoteLine.Up;
        }


        if (eventNoteJudge != null && noteScript != null)
        {
            eventNoteJudge.SetNote(noteScript, data);
        }
    }
}

[System.Serializable]
public class EventNoteData
{
    public float showTime;
    public float judgeTime;
    public int noteKind;       // 0 = Tap, 1 = Hold
    public float holdTime = 1f;
}
