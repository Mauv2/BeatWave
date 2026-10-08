using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerGroundFollow : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheck;      // 플레이어 발밑 위치
    public LayerMask groundLayer;      // 땅으로 인식할 Layer

    [Header("Ground Follow")]
    public float rayDistance = 5f;     // 아래로 땅을 찾는 Ray 길이
    public float groundOffset = 0.7f;  // 땅 위에서 플레이어가 떠 있을 높이
    public float followSpeed = 10f;    // 땅 높이를 따라가는 속도

    [Header("Jump Effect")]
    public float defaultJumpPower = 1.2f;      // 기본 점프 높이
    public float defaultJumpDuration = 0.35f;  // 기본 점프 시간

    bool isJumping = false;            // 현재 점프 연출 중인지 확인

    void Update()
    {
        /*
         * 점프 중일 때도 GroundCheck를 완전히 끄면
         * 착지 후 위치가 이상해질 수 있습니다.
         *
         * 그래서 여기서는 점프 중이면 위치 보정만 잠깐 약하게 하고,
         * 일반 상태에서는 땅 높이를 부드럽게 따라갑니다.
         */

        FollowGround();
    }

    //void Start()
    //{
    //    // 게임 시작하자마자 플레이어 발밑에서 아래로 Raycast를 쏴서 땅을 찾음
    //    RaycastHit2D hit = Physics2D.Raycast(
    //        groundCheck.position,
    //        Vector2.down,
    //        rayDistance,
    //        groundLayer
    //    );

    //    // 땅을 찾았으면 플레이어를 바로 땅 위 위치로 이동
    //    if (hit.collider != null)
    //    {
    //        float startY = hit.point.y + groundOffset;

    //        transform.position = new Vector3(
    //            transform.position.x,
    //            startY,
    //            transform.position.z
    //        );
    //    }
    //}

    void FollowGround()
    {
        /*
         * 플레이어 발밑에서 아래 방향으로 Raycast를 쏩니다.
         * 아래에 Ground Layer가 있으면 그 위치를 기준으로
         * 플레이어 Y 위치를 맞춥니다.
         */

        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            rayDistance,
            groundLayer
        );

        // Scene 창에서 Ray가 어디로 나가는지 확인용
        Debug.DrawRay(
            groundCheck.position,
            Vector2.down * rayDistance,
            Color.green
        );

        // 아래에 땅이 없으면 따라갈 곳이 없으므로 종료
        if (hit.collider == null)
        {
            return;
        }

        // 땅의 실제 Y 위치 + 플레이어가 땅 위에 서야 하는 높이
        float targetY = hit.point.y + groundOffset;

        // 점프 중이 아닐 때, 현재 위치보다 너무 높은 땅은 따라가지 않음
        if (!isJumping && targetY > transform.position.y + 0.3f)
        {
            return;
        }

        // 점프 중에는 GroundFollow가 너무 강하게 끌어내리지 않도록 속도를 낮춤
        float speed = isJumping ? followSpeed * 0.25f : followSpeed;

        // 플레이어 X는 고정하고, Y만 땅 높이에 맞춰 부드럽게 이동
        transform.position = new Vector3(
            transform.position.x,
            Mathf.Lerp(transform.position.y, targetY, speed * Time.deltaTime),
            transform.position.z
        );
    }

    public void JumpEffect(float jumpPower, float jumpDuration)
    {
        /*
         * JumpTrigger에서 호출하는 점프 연출 함수입니다.
         * 플레이어는 제자리에서 위로 점프하는 연출만 하고,
         * 실제 착지 높이는 FollowGround()가 아래 발판을 감지해서 맞춥니다.
         */

        if (isJumping) return;

        isJumping = true;

        transform.DOJump(
            transform.position, // 목표 위치는 현재 위치 그대로
            jumpPower,          // 점프 높이
            1,                  // 점프 횟수
            jumpDuration        // 점프 시간
        ).OnComplete(() =>
        {
            isJumping = false;
        });
    }

    public void JumpEffectDefault()
    {
        /*
         * 기본값으로 점프하고 싶을 때 사용하는 함수입니다.
         * 버튼 테스트용이나 기본 JumpTrigger에서 사용할 수 있습니다.
         */

        JumpEffect(defaultJumpPower, defaultJumpDuration);
    }
}
