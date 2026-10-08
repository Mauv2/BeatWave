using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FallTransitionEffect : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public Rigidbody2D playerRb;

    [Header("Fade")]
    public Image blackFade;
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    [Header("Fall")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.25f;
    public float fallDistance = 8f;
    public float fallDuration = 1f;
    public float jumpForwardDistance = 3f;

    [Header("Background")]
    public RawImage backgroundImage;
    public Texture nextBackground;

    public IEnumerator PlayFallTransition(Transform spawnPoint)
    {
        if (player == null || spawnPoint == null)
            yield break;

        if (blackFade != null)
            blackFade.raycastTarget = true;

        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.simulated = false;
        }

        // 점프 → 추락 Sequence
        // 점프 시작 위치
        Vector3 startPos = player.position;

        // 앞으로 점프해서 도착할 위치
        Vector3 jumpTargetPos = new Vector3(
            startPos.x + jumpForwardDistance,
            startPos.y + jumpHeight,
            startPos.z
        );

        // 떨어질 위치
        Vector3 fallTargetPos = new Vector3(
            jumpTargetPos.x,
            jumpTargetPos.y - fallDistance,
            startPos.z
        );

        Sequence fallSeq = DOTween.Sequence();

        // 앞으로 + 위로 점프
        fallSeq.Append(
            player.DOMove(jumpTargetPos, jumpDuration)
                .SetEase(Ease.OutQuad)
        );

        // 그 위치에서 아래로 추락
        fallSeq.Append(
            player.DOMove(fallTargetPos, fallDuration)
                .SetEase(Ease.InQuad)
        );

        yield return fallSeq.WaitForCompletion();

        // 검은 화면
        if (blackFade != null)
        {
            yield return blackFade.DOFade(1f, fadeInDuration)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();
        }

        // 새로운 위치로 이동
        player.position = spawnPoint.position;

        // 배경 변경
        if (backgroundImage != null && nextBackground != null)
        {
            backgroundImage.texture = nextBackground;

            // 루프 사용 안 하므로 UV를 기본값으로 고정
            backgroundImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        yield return new WaitForSeconds(0.2f);

        // 화면 복구
        if (blackFade != null)
        {
            yield return blackFade.DOFade(0f, fadeOutDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            blackFade.raycastTarget = false;
        }

        if (playerRb != null)
            playerRb.simulated = true;
    }
}