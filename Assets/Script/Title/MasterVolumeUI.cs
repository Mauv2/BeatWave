
using UnityEngine;
using UnityEngine.UI;

public class MasterVolumeUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;

    private void OnEnable()
    {
        if (masterSlider == null ||
            GameAudioManager.Instance == null)
            return;

        masterSlider.SetValueWithoutNotify(
            GameAudioManager.Instance.GetVolume());

        masterSlider.onValueChanged.AddListener(
            GameAudioManager.Instance.SetVolume);
    }

    private void OnDisable()
    {
        if (masterSlider == null ||
            GameAudioManager.Instance == null)
            return;

        masterSlider.onValueChanged.RemoveListener(
            GameAudioManager.Instance.SetVolume);
    }
}
