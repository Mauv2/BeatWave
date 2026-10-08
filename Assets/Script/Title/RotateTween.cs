using UnityEngine;
using DG.Tweening;

public class RotateTween : MonoBehaviour
{
    public float duration = 8f;
    public bool reverse = false;

    void Start()
    {
        float angle = reverse ? 360f : -360f;

        transform
            .DORotate(new Vector3(0, 0, angle), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1);
    }
}