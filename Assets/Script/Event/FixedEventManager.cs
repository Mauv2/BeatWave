using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FixedEventManager : MonoBehaviour
{
    [Header("Player")]
    public PlayerMove playerMove;
    public Rigidbody2D playerRb;

    [Header("Animation")]
    public PlayerAnimController playerAnim;

    [Header("Music")]
    public AudioSource music;

    [Header("Music Fade")]
    public float eventVolume = 0.25f;
    public float volumeFadeTime = 0.5f;

    [Header("Judge")]
    public JudgeManager judgeManager;

    [Header("Notes")]
    public Transform noteRoot;

    [Header("Background")]
    public RawImage backgroundImage;
    public Texture eventBackgroundTexture;

    [Header("Event Note")]
    public EventNoteSpawner eventNoteSpawner;

    [Header("Cave")]
    public CaveEventManager caveEventManager;

    bool isEventPlaying = false;

    public void StartFixedEvent()
    {
        if (isEventPlaying) return;

        StartCoroutine(FixedEventRoutine());
    }

    IEnumerator FixedEventRoutine()
    {
        isEventPlaying = true;
        RhythmClock.GetOrCreate().SuspendTravel();
        music = RhythmClock.GetOrCreate().Source;

        if (playerMove != null)
            playerMove.isPaused = true;

        if (playerAnim != null)
            playerAnim.SetRunning(false);

        if (playerRb != null)
            playerRb.velocity = Vector2.zero;

        if (music != null)
        {
            music.DOKill();
        }

        if (judgeManager != null)
            judgeManager.enabled = false;

        ClearNotes();

        if (backgroundImage != null && eventBackgroundTexture != null)
        {
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.texture = eventBackgroundTexture;
            backgroundImage.transform.SetSiblingIndex(1);
        }

        yield return new WaitForSeconds(0.5f);

        if (eventNoteSpawner != null)
            eventNoteSpawner.StartEventNotes();

        while (eventNoteSpawner != null && eventNoteSpawner.isPlaying)
        {
            yield return null;
        }

        Debug.Log("고정 이벤트 종료");

        ClearNotes();

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        // 여기서 바로 이동하지 않음
        // 동굴 이벤트 매니저가 낙하 연출 + 스폰 이동 + 음악/노트 처리를 담당
        if (caveEventManager != null)
        {
            caveEventManager.StartCaveEvent();
        }
        else
        {
            Debug.LogWarning("CaveEventManager가 연결되어 있지 않습니다.");
        }

        isEventPlaying = false;
    }

    void ClearNotes()
    {
        if (judgeManager != null) judgeManager.ClearActiveNotes();
        if (noteRoot == null) return;

        foreach (Transform child in noteRoot)
        {
            Destroy(child.gameObject);
        }
    }
}
