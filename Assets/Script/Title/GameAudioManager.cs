
using UnityEngine;
using UnityEngine.Audio;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance;

    [SerializeField] private AudioMixer audioMixer;

    private const string VolumeKey = "MasterVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        float savedVolume =
            PlayerPrefs.GetFloat(VolumeKey, 1f);

        ApplyVolume(savedVolume);
    }

    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);

        ApplyVolume(value);

        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        float db = Mathf.Log10(
            Mathf.Max(value, 0.0001f)) * 20f;

        audioMixer.SetFloat("MasterVolume", db);
    }

    public float GetVolume()
    {
        return PlayerPrefs.GetFloat(VolumeKey, 1f);
    }
}
