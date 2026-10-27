using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{
    public Animator animator;
    [Header("Optional separate weapon arm")]
    public Transform attackArm;
    [Header("Original animation with held baton")]
    public SpriteRenderer heldBaton;
    public SpriteRenderer attackUpperBody;
    public SpriteRenderer runningLowerBody;
    public Sprite[] downstrokePoses;
    [System.Serializable]
    public struct HandAnchor
    {
        public Sprite frame;
        public Vector2 position;
        public Vector2 upperBodyOffset;
    }
    public HandAnchor[] handAnchors;
    private SpriteRenderer body;
    private readonly Dictionary<Sprite, Sprite> lowerFrames = new Dictionary<Sprite, Sprite>();
    [Min(.1f)] public float armAttackDuration = .24f;
    private float armAttackTime = 1f;
    private int lastAttackFrame = -1;
    private RhythmClock clock;
    private const float ArmRestAngle = -55f;
    private const float ArmRaisedAngle = 80f;

    void LateUpdate()
    {
        if (heldBaton != null)
        {
            if (Time.timeScale > 0f && (clock == null || !clock.IsPaused))
                armAttackTime += Time.deltaTime;
            UpdateBatonPose();
            return;
        }
        if (attackArm == null || Time.timeScale <= 0f || (clock != null && clock.IsPaused)) return;
        armAttackTime += Time.deltaTime;
        float t = Mathf.Clamp01(armAttackTime / armAttackDuration);
        float angle;
        // Strike upward from the lowered carry pose, then settle back down.
        if (t < .35f) angle = Mathf.Lerp(ArmRestAngle, ArmRaisedAngle,
            Mathf.SmoothStep(0f, 1f, t / .35f));
        else if (t < .45f) angle = ArmRaisedAngle;
        else angle = Mathf.Lerp(ArmRaisedAngle, ArmRestAngle,
            Mathf.SmoothStep(0f, 1f, (t - .45f) / .55f));
        attackArm.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    void UpdateBatonPose()
    {
        if (body == null && animator != null) body = animator.GetComponent<SpriteRenderer>();
        if (body == null || body.sprite == null) return;
        Vector2 offset = Vector2.zero;
        if (handAnchors != null)
            foreach (var anchor in handAnchors)
                if (anchor.frame == body.sprite)
                {
                    heldBaton.transform.localPosition = anchor.position;
                    offset = anchor.upperBodyOffset;
                    break;
                }
        bool striking = armAttackTime < armAttackDuration && downstrokePoses != null &&
            downstrokePoses.Length == 3 && attackUpperBody != null && runningLowerBody != null;
        heldBaton.enabled = !striking;
        body.enabled = !striking;
        if (attackUpperBody != null) attackUpperBody.enabled = striking;
        if (runningLowerBody != null) runningLowerBody.enabled = striking;
        if (!striking) return;

        // The original Animator keeps advancing. Only its upper body is replaced
        // during the strike, so its original running/jumping legs keep moving.
        float phase = armAttackTime / armAttackDuration;
        attackUpperBody.sprite = downstrokePoses[phase < .28f ? 0 : phase < .48f ? 1 : 2];
        attackUpperBody.transform.localPosition = offset;
        var source = body.sprite;
        if (!lowerFrames.TryGetValue(source, out var lower))
        {
            var rect = source.rect;
            float height = Mathf.Clamp(source.pivot.y - .68f * source.pixelsPerUnit, 1f, rect.height);
            rect.height = height;
            lower = Sprite.Create(source.texture, rect,
                new Vector2(source.pivot.x / rect.width, source.pivot.y / height),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            lower.name = source.name + "_Legs";
            lowerFrames.Add(source, lower);
        }
        runningLowerBody.sprite = lower;
    }

    void OnDisable()
    {
        armAttackTime = armAttackDuration;
        if (body != null) body.enabled = true;
        if (heldBaton != null) heldBaton.enabled = true;
        if (attackUpperBody != null) attackUpperBody.enabled = false;
        if (runningLowerBody != null) runningLowerBody.enabled = false;
        if (attackArm != null) attackArm.localRotation = Quaternion.Euler(0f, 0f, ArmRestAngle);
    }

    void OnDestroy()
    {
        foreach (var sprite in lowerFrames.Values) Destroy(sprite);
        lowerFrames.Clear();
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        clock = FindObjectOfType<RhythmClock>();
        armAttackTime = armAttackDuration;
        if (attackArm != null) attackArm.localRotation = Quaternion.Euler(0f, 0f, ArmRestAngle);

        // 게임 시작 후 자동 이동 상태라면 run 시작
        SetRunning(true);
    }

    public void SetRunning(bool value)
    {
        if (animator == null) return;
        animator.SetBool("isRunning", value);
    }

    public void SetFlying(bool value)
    {
        if (animator == null) return;
        animator.SetBool("isFlying", value);
    }

    public void PlayInteract()
    {
        if (animator == null) return;
        if (heldBaton != null || attackArm != null)
        {
            // A press can be reported by both JudgeManager and Mash in one frame.
            // Keep the base run/jump Animator playing; animate only the weapon arm.
            if (lastAttackFrame == Time.frameCount) return;
            lastAttackFrame = Time.frameCount;
            armAttackTime = 0f;
            return;
        }
        animator.ResetTrigger("Interact");
        animator.SetTrigger("Interact");
    }
}
