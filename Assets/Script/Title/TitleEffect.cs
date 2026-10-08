using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class TitleEffect : MonoBehaviour
{
    [Header("Post Processing")]
    public PostProcessVolume volume;

    private Bloom bloom;

    [Header("Bloom Settings")]
    public float speed = 1f;

    public float minIntensity = 2f;
    public float maxIntensity = 10f;

    private float currentTime = 0f;

    void Start()
    {
        if (volume == null)
        {
            Debug.LogError("PostProcessVolume이 연결되지 않았습니다.");
            return;
        }

        // 런타임용 프로필 복사
        volume.profile = Instantiate(volume.profile);

        if (volume.profile.TryGetSettings(out bloom))
        {
            bloom.intensity.overrideState = true;
            bloom.intensity.value = minIntensity;
        }
        else
        {
            Debug.LogError("Bloom 설정을 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        if (bloom == null) return;

        currentTime += Time.deltaTime * speed;

        // 0~1 반복
        float t = (Mathf.Sin(currentTime) + 1f) * 0.5f;

        // 최소~최대 사이 자연스럽게 변화
        bloom.intensity.value = Mathf.Lerp(minIntensity, maxIntensity, t);
    }
}