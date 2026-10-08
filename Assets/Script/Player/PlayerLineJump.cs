using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerLineJump : MonoBehaviour
{
    [Header("Move")]
    public float defaultJumpPower = 1.5f;      // 점프 높이
    public float defaultJumpDuration = 0.35f;  // 점프 시간

    bool isJumping = false;                    // 점프 중복 방지

    public void JumpToY(float targetY, float jumpPower, float jumpDuration)
    {
        /*
         * 플레이어 X는 고정하고
         * Y 위치만 targetY로 점프 이동합니다.
         */

        if (isJumping) return;

        isJumping = true;

        Vector3 targetPos = new Vector3(
            transform.position.x,
            targetY,
            transform.position.z
        );

        transform.DOJump(
            targetPos,
            jumpPower,
            1,
            jumpDuration
        ).OnComplete(() =>
        {
            // 점프 끝나면 정확히 목표 높이에 고정
            transform.position = targetPos;
            isJumping = false;
        });
    }

    public void JumpToY(float targetY)
    {
        /*
         * 기본 점프값으로 이동할 때 사용
         */

        JumpToY(targetY, defaultJumpPower, defaultJumpDuration);
    }
}
