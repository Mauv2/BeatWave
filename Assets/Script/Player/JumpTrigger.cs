using UnityEngine;

public class JumpTrigger : MonoBehaviour
{
    [Header("Jump Setting")]
    public float jumpPower = 8f;
    public bool oneTimeOnly = true;
    public StageJumpSetPiece presentation;
    bool isUsed;
    public bool IsUsed => oneTimeOnly && isUsed;
    public void MarkUsed() { isUsed = true; }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsUsed || !collision.CompareTag("Player")) return;
        var mover = collision.GetComponent<PlayerMove>();
        if (mover == null || mover.HasRhythmPath) return;
        mover.TriggerJump(jumpPower);
        MarkUsed();
    }
}