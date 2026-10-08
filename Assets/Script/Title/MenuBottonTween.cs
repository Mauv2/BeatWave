using UnityEngine;
using DG.Tweening;

public class MenuButtonTween : MonoBehaviour
{
    public float delay = 0f;

    void Start()
    {
        transform.localScale = Vector3.zero;

        transform
            .DOScale(1f, 0.5f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack);
    }
}