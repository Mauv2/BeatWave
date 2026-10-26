using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerMove : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;   // 기존 MapMover moveSpeed와 같은 값 사용
    public bool isPaused = false;  // Mash 노트 때 멈추기용

    [Header("Jump")]
    public float jumpForce = 8f;   // 나중에 점프 발판용
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.15f;

    public Stage1Switchback switchback;
    public Transform facingVisual, upMarker, downMarker;
    Vector3 visualScale, upPosition, downPosition;
    Rigidbody2D rb;
    float originalGravity;
    RhythmClock rhythmClock;
    int nextJump;
    public bool IsWalkingOff { get; private set; }
    float endingSpeed, endingFeetOffset, endingHalfWidth;

    public void BeginEndingWalk(float speed)
    {
        if (IsWalkingOff) return;
        IsWalkingOff = true;
        endingSpeed = Mathf.Max(.1f, speed);
        rhythmClock = RhythmClock.GetOrCreate();
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        var collider = GetComponent<Collider2D>();
        endingFeetOffset = collider != null ? transform.position.y - collider.bounds.min.y : 0;
        endingHalfWidth = collider != null ? collider.bounds.extents.x : .5f;
        var tempo = GetComponent<Stage1TempoMotion>();
        if (tempo != null) tempo.enabled = false;
        var animation = GetComponentInChildren<PlayerAnimController>();
        if (animation != null) { animation.SetFlying(false); animation.SetRunning(true); }
        if (facingVisual != null)
        {
            var scale = facingVisual.localScale;
            scale.x = Mathf.Abs(scale.x);
            facingVisual.localScale = scale;
        }
    }

    public bool HasExitedScreen(Camera camera, float margin)
    {
        return IsWalkingOff && camera.WorldToViewportPoint(
            transform.position - Vector3.right * (endingHalfWidth + Mathf.Max(0, margin))).x > 1f;
    }
    public RhythmPath Path { get; private set; }
    public bool HasRhythmPath => Path != null;
    public float OriginalGravity => rb != null ? originalGravity : GetComponent<Rigidbody2D>().gravityScale;
    public int AutomaticJumpCount { get; private set; }

    public void BindPath(RhythmPath path)
    {
        Path = path;
        nextJump = 0;
        rhythmClock = RhythmClock.GetOrCreate();
        rb.velocity = Vector2.zero;
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.position = path.Evaluate(path.StartTime);
        foreach (var cue in path.Jumps)
            if (cue.trigger != null && cue.trigger.presentation != null)
                cue.trigger.presentation.Bind(path, cue);
    }
    void Update()
    {
        if (IsWalkingOff)
        {
            if (rhythmClock.MenuPaused || Time.timeScale <= 0) return;
            var next = transform.position + Vector3.right * endingSpeed * Time.deltaTime;
            foreach (var hit in Physics2D.RaycastAll(new Vector2(next.x, next.y - endingFeetOffset + .5f), Vector2.down, 1.5f, groundLayer))
                if (!hit.collider.isTrigger && hit.rigidbody != rb) { next.y = hit.point.y + endingFeetOffset; break; }
            transform.position = next;
            return;
        }
        if (Path == null || rhythmClock == null || isPaused || !rhythmClock.TravelEnabled ||
            rhythmClock.IsPaused || !rhythmClock.HasStarted) return;
        // Render/input and physics use the same DSP time, including the vertical jump.
        transform.position = Path.Evaluate(rhythmClock.SongTime);
        float direction = Path.DirectionAt(rhythmClock.SongTime);
        if(facingVisual != null) facingVisual.localScale = new Vector3(Mathf.Abs(visualScale.x)*direction,visualScale.y,visualScale.z);
        if(upMarker != null) upMarker.localPosition = new Vector3(Mathf.Abs(upPosition.x)*direction,upPosition.y,upPosition.z);
        if(downMarker != null) downMarker.localPosition = new Vector3(Mathf.Abs(downPosition.x)*direction,downPosition.y,downPosition.z);
        while (nextJump < Path.Jumps.Count && Path.Jumps[nextJump].time <= rhythmClock.SongTime)
        {
            var cue = Path.Jumps[nextJump++];
            if (cue.trigger != null) cue.trigger.MarkUsed();
            AutomaticJumpCount++;
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        if(facingVisual != null) visualScale = facingVisual.localScale;
        if(upMarker != null) upPosition = upMarker.localPosition;
        if(downMarker != null) downPosition = downMarker.localPosition;
    }

    void FixedUpdate()
    {
        if (IsWalkingOff || Path != null) return;
        var clock = FindObjectOfType<RhythmClock>();
        if (clock != null)
        {
            if (clock.Source == null || isPaused || !clock.TravelEnabled || clock.IsPaused || !clock.HasStarted)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }
            float targetX = clock.PlayerX(moveSpeed);
            rb.velocity = new Vector2((targetX - rb.position.x) / Time.fixedDeltaTime, rb.velocity.y);
            return;
        }
        /*
         * X 속도는 항상 일정하게 고정합니다.
         * 그래서 내리막에서 속도가 더 붙지 않고,
         * 오르막에서도 음악 박자 기준 속도가 유지됩니다.
         */

        if (isPaused)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
    }

    public void Jump()
    {
        Jump(jumpForce);
    }

    public void Jump(float power)
    {
        if (IsWalkingOff) return;
        // 공중 점프 방지
        if (!IsGrounded())
            return;

        // 점프 높이를 일정하게 만들기 위해 Y속도 초기화
        rb.velocity = new Vector2(
            rb.velocity.x,
            0f
        );

        // 점프
        rb.AddForce(
            Vector2.up * power,
            ForceMode2D.Impulse
        );
    }

    public void TriggerJump(float power)
    {
        if (IsWalkingOff || Path != null) return;
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        rb.AddForce(
            Vector2.up * power,
            ForceMode2D.Impulse
        );
    }

    bool IsGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }
}
