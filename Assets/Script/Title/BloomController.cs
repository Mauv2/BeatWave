using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering.PostProcessing;

public class BloomController : MonoBehaviour
{
    [Header("Post Process Volume")]
    public PostProcessVolume postProcessVolume;

    [Header("Bloom Intensity")]
    public float normalIntensity = 1f;
    public float targetIntensity = 6f;

    [Header("Time")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 1f;

    private Bloom bloom;
    private Tween bloomTween;

    void Awake()
    {
        if (postProcessVolume == null)
        {
            postProcessVolume = GetComponent<PostProcessVolume>();
        }

        if (postProcessVolume == null)
        {
            Debug.LogError("PostProcessVolume이 연결되지 않았습니다.");
            return;
        }

        postProcessVolume.profile = Instantiate(postProcessVolume.profile);

        bool hasBloom = postProcessVolume.profile.TryGetSettings(out bloom);

        if (!hasBloom || bloom == null)
        {
            Debug.LogError("Post Process Profile 안에 Bloom이 없습니다.");
            return;
        }

        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = normalIntensity;
    }

    public void BloomOn()
    {
        if (bloom == null) return;

        bloomTween?.Kill();

        bloomTween = DOTween.To(
            () => bloom.intensity.value,
            x => bloom.intensity.value = x,
            targetIntensity,
            fadeInDuration
        );
    }

    public void BloomOff()
    {
        if (bloom == null) return;

        bloomTween?.Kill();

        bloomTween = DOTween.To(
            () => bloom.intensity.value,
            x => bloom.intensity.value = x,
            normalIntensity,
            fadeOutDuration
        );
    }
}