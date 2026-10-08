using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JudgePoint : MonoBehaviour
{
    // 현재 판정선 안에 들어와 있는 노트 저장
    public Note currentNote;

    // 노트가 판정선 범위 안으로 들어왔을 때 실행
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var timedNote = collision.GetComponentInParent<Note>();
        if (timedNote != null && (timedNote.hasTiming || timedNote.isEventNote)) return;
        if (collision.CompareTag("Note"))                       // 들어온 오브젝트의 Tag가 Note인지 확인
        {
            // Debug.Log(gameObject.name + " : 노트 들어옴");
            currentNote = collision.GetComponent<Note>();       // 충돌한 오브젝트의 Note 스크립트 가져오기

            if (currentNote != null && currentNote.kind == NoteKind.Mash)
            {
                currentNote.isMashActive = true;                // Mash 노트 활성화 상태 시작
                currentNote.mashTimer = 0f;                     // Mash 제한시간 타이머 초기화

                // PlayNote 스크립트 찾기
                PlayNote playNote = FindObjectOfType<PlayNote>();

                // PlayNote가 존재하면
                if (playNote != null)
                {
                    playNote.PauseNoteFlow();   // 다음 노트 생성 및 흐름 일시정지
                }
            }
        }
    }

    // 노트가 판정선 범위를 벗어났을 때 실행
    private void OnTriggerExit2D(Collider2D collision)
    {
        var timedNote = collision.GetComponentInParent<Note>();
        if (timedNote != null && (timedNote.hasTiming || timedNote.isEventNote)) return;
        // 나간 오브젝트의 Tag가 Note인지 확인
        if (collision.CompareTag("Note"))
        {
            Note note = collision.GetComponent<Note>();        // 나간 노트의 Note 스크립트 가져오기

            if (note == null)
            {
                return;
            }

            // 이미 판정된 노트는 성공/실패 처리가 끝난 노트이므로
            // Trigger에서 나가도 Miss 처리하면 안 됨
            if (note.isJudged)
            {
                // 현재 JudgePoint가 들고 있던 노트라면 비워줌
                if (currentNote == note)
                {
                    currentNote = null;
                }

                return;
            }

            // Hold 노트인지 확인
            if (note != null && note.kind == NoteKind.Hold)
            {
                // Hold를 시작하지 못하고 지나가면 Miss
                if (!note.isHolding && !note.isJudged)
                {
                    note.isJudged = true;

                    // JudgeManager 찾기
                    JudgeManager judgeManager = FindObjectOfType<JudgeManager>();

                    // JudgeManager가 존재하면 Miss 처리
                    if (judgeManager != null)
                    {
                        judgeManager.OnMiss(note.transform.position);
                    }

                    Destroy(collision.gameObject);

                    if (currentNote != null && collision.gameObject == currentNote.gameObject)
                    {
                        currentNote = null;
                    }
                }

                // 이미 누르고 있는 Hold는 CheckHold에서 처리하므로 여기서 삭제하지 않음
                return;
            }

            // Mash 노트는 여기서 Miss 처리하지 않음
            // Mash는 제한시간 로직에서 따로 처리
            if (note != null && note.kind == NoteKind.Mash)
            {
                return;
            }

            // Tap 노트만 여기서 Miss 처리
            if (note != null && !note.isJudged)
            {
                note.isJudged = true;

                JudgeManager judgeManager = FindObjectOfType<JudgeManager>();

                // JudgeManager 존재 시 Miss 처리
                if (judgeManager != null)
                {
                    judgeManager.OnMiss(note.transform.position);
                }

                Destroy(collision.gameObject);
            }

            // currentNote가 현재 나간 노트면 비우기
            if (currentNote != null && collision.gameObject == currentNote.gameObject)
            {
                currentNote = null;
            }
        }
    }
}
