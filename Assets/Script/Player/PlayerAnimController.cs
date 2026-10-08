using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{
    public Animator animator;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

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
        animator.ResetTrigger("Interact");
        animator.SetTrigger("Interact");
    }
}
